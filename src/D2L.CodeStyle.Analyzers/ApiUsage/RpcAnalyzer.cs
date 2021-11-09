using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed partial class RpcAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.RpcContextFirstArgument,
			Diagnostics.RpcArgumentSortOrder,
			Diagnostics.RpcContextMarkedDependency,
			Diagnostics.RpcInvalidParameterType
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterRpcAnalyzer );
		}

		public static void RegisterRpcAnalyzer( CompilationStartAnalysisContext context ) {
			// Cache some important type lookups
			var rpcAttributeType = context.Compilation.GetTypeByMetadataName( "D2L.Web.RpcAttribute" );
			var rpcContextType = context.Compilation.GetTypeByMetadataName( "D2L.Web.IRpcContext" );
			var rpcPostContextType = context.Compilation.GetTypeByMetadataName( "D2L.Web.IRpcPostContext" );
			var rpcPostContextBaseType = context.Compilation.GetTypeByMetadataName( "D2L.Web.RequestContext.IRpcPostContextBase" );

			// If any of those type lookups failed then presumably D2L.Web is
			// not referenced and we don't need to register our analyzer.

			if( rpcAttributeType == null || rpcAttributeType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			if( rpcContextType == null || rpcContextType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			if( rpcPostContextType == null || rpcPostContextType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			if( rpcPostContextBaseType == null || rpcPostContextBaseType.Kind == SymbolKind.ErrorType ) {
				return;
			}

			ImmutableHashSet<INamedTypeSymbol> knownRpcParameterTypes = ImmutableHashSet.Create<INamedTypeSymbol>(
				SymbolEqualityComparer.Default,
				context.Compilation.GetSpecialType( SpecialType.System_Boolean ),
				context.Compilation.GetSpecialType( SpecialType.System_Decimal ),
				context.Compilation.GetSpecialType( SpecialType.System_Double ),
				context.Compilation.GetSpecialType( SpecialType.System_Single ),
				context.Compilation.GetSpecialType( SpecialType.System_Int32 ),
				context.Compilation.GetSpecialType( SpecialType.System_Int64 ),
				context.Compilation.GetSpecialType( SpecialType.System_String )
			);

			var treeNodeType = context.Compilation.GetTypeByMetadataName( "D2L.Web.UI.TreeControl.ITreeNode" );
			if( treeNodeType != null && treeNodeType.Kind != SymbolKind.ErrorType ) {
				knownRpcParameterTypes = knownRpcParameterTypes.Add( treeNodeType );
			}

			RpcTypes rpcTypes = new(
				RpcAttribute: rpcAttributeType,
				RpcContexts: ImmutableHashSet.Create<INamedTypeSymbol>(
					SymbolEqualityComparer.Default,
					rpcContextType,
					rpcPostContextType,
					rpcPostContextBaseType
				),
				DependencyAttribute: context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.DependencyAttribute" ),
				IDeserializable: context.Compilation.GetTypeByMetadataName( "D2L.Serialization.IDeserializable" ),
				IDeserializer: context.Compilation.GetTypeByMetadataName( "D2L.Serialization.IDeserializer" ),
				IDictionary: context.Compilation.GetTypeByMetadataName( "System.Collections.Generic.IDictionary`2" ),
				KnownRpcParameters: knownRpcParameterTypes
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeMethod( ctx, rpcTypes ),
				SyntaxKind.MethodDeclaration
			);
		}

		private static void AnalyzeMethod(
			SyntaxNodeAnalysisContext context,
			RpcTypes rpcTypes
		) {
			var method = context.Node as MethodDeclarationSyntax;

			if( method == null ) {
				return;
			}

			if( !CheckThatMethodIsRpc( context, method, rpcTypes ) ) {
				return;
			}

			CheckThatFirstArgumentIsIRpcContext( context, method, rpcTypes );

			// dependencyAttributeType may be null if that DLL isn't
			// referenced. In that case don't do some of these checks.
			CheckThatDependencyArgumentsAreSortedCorrectly(
				context,
				method.ParameterList.Parameters,
				rpcTypes
			);

			// other things to check:
			// - appropriate use of [Dependency]

			CheckThatRpcParametersAreValid(
				context: context,
				ps: method.ParameterList.Parameters,
				rpcTypes
			);
		}

		private static void CheckThatRpcParametersAreValid(
			SyntaxNodeAnalysisContext context,
			SeparatedSyntaxList<ParameterSyntax> ps,
			RpcTypes rpcTypes
		) {
			foreach( var parameter in ps.Skip( 1 ) ) {
				// Skip injected parameters
				if( IsMarkedDependency( context.SemanticModel, parameter, rpcTypes ) ) {
					continue;
				}

				var parameterTypeSymbol = context.SemanticModel.GetSymbolInfo( parameter.Type ).Symbol as ITypeSymbol;
				bool isValidParameterType = IsValidParameterType( context, parameterTypeSymbol, rpcTypes );
				if( !isValidParameterType ) {
					context.ReportDiagnostic(
						Diagnostic.Create( Diagnostics.RpcInvalidParameterType, parameter.Type.GetLocation() )
					);
				}
			}
		}

		private static bool CheckThatMethodIsRpc(
			SyntaxNodeAnalysisContext context,
			MethodDeclarationSyntax method,
			RpcTypes rpcTypes
		) {
			bool isRpc = method
				.AttributeLists
				.SelectMany( al => al.Attributes )
				.Any( attr => IsAttribute( rpcTypes.RpcAttribute, attr, context.SemanticModel ) );

			return isRpc;
		}

		private static void CheckThatFirstArgumentIsIRpcContext(
			SyntaxNodeAnalysisContext context,
			MethodDeclarationSyntax method,
			RpcTypes rpcTypes
		) {
			var ps = method.ParameterList.Parameters;

			if( ps.Count == 0 ) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.RpcContextFirstArgument, method.ParameterList.GetLocation() )
				);
				return;
			}

			var firstParam = method.ParameterList.Parameters[0];
			var firstParamType = context.SemanticModel.GetSymbolInfo( firstParam.Type ).Symbol;

			if( !rpcTypes.RpcContexts.Contains( firstParamType ) ) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.RpcContextFirstArgument, firstParam.GetLocation() )
				);
			} else if( IsMarkedDependency( context.SemanticModel, firstParam, rpcTypes ) ) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.RpcContextMarkedDependency, firstParam.GetLocation() )
				);
			}
		}

		private static void CheckThatDependencyArgumentsAreSortedCorrectly(
			SyntaxNodeAnalysisContext context,
			SeparatedSyntaxList<ParameterSyntax> ps,
			RpcTypes rpcTypes
		) {
			if( rpcTypes.DependencyAttribute == null ) {
				return;
			}

			bool doneDependencies = false;
			foreach( var param in ps.Skip( 1 ) ) {
				var isDep = IsMarkedDependency( context.SemanticModel, param, rpcTypes );

				if( !isDep && !doneDependencies ) {
					doneDependencies = true;
				} else if( isDep && doneDependencies ) {
					context.ReportDiagnostic( Diagnostic.Create( Diagnostics.RpcArgumentSortOrder, param.GetLocation() ) );
				}
			}
		}

		private static bool IsMarkedDependency(
			SemanticModel model,
			ParameterSyntax parameter,
			RpcTypes rpcTypes
		) => parameter
			.AttributeLists
			.SelectMany( al => al.Attributes )
			.Any( attr => IsAttribute( rpcTypes.DependencyAttribute, attr, model ) );

		private static bool IsAttribute(
			INamedTypeSymbol expectedType,
			AttributeSyntax attr,
			SemanticModel model
		) {
			var attributeConstructorType = model.GetSymbolInfo( attr ).Symbol;

			// Note: symbol corresponds to the constructor for the attribute,
			// so we need to look at symbol.ContainingType
			return expectedType.Equals( attributeConstructorType?.ContainingType, SymbolEqualityComparer.Default );
		}

		//private static (IParameterSymbol context, ImmutableArray<IParameterSymbol> dependencies, ImmutableArray<IParameterSymbol> parameters) Split(
		//	SyntaxNodeAnalysisContext context,
		//	MethodDeclarationSyntax method,
		//	Func<IParameterSymbol, bool> isDependency
		//) {

		//}

		private sealed record RpcTypes(
			INamedTypeSymbol RpcAttribute,
			ImmutableHashSet<INamedTypeSymbol> RpcContexts,
			INamedTypeSymbol DependencyAttribute,
			INamedTypeSymbol IDeserializable,
			INamedTypeSymbol IDeserializer,
			INamedTypeSymbol IDictionary,
			ImmutableHashSet<INamedTypeSymbol> KnownRpcParameters
		);
	}
}
