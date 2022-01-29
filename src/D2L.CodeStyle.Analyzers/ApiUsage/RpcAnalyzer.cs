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

			ImmutableHashSet<ITypeSymbol> knownRpcParameterTypes = ImmutableHashSet.Create<ITypeSymbol>(
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
				RpcContexts: ImmutableHashSet.Create<ITypeSymbol>(
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

			var (rpcContext, parameters) = Split( context, method, rpcTypes );

			CheckThatFirstArgumentIsIRpcContext( context, method, rpcContext, rpcTypes );

			CheckThatRpcParametersAreValid( context, parameters, rpcTypes );
		}

		private static bool CheckThatMethodIsRpc(
			SyntaxNodeAnalysisContext context,
			MethodDeclarationSyntax method,
			RpcTypes rpcTypes
		) {
			bool isRpc = context
				.SemanticModel
				.GetDeclaredSymbol( method, context.CancellationToken )
				.GetAttributes()
				.Any( a => SymbolEqualityComparer.Default.Equals( a.AttributeClass, rpcTypes.RpcAttribute ) );

			return isRpc;
		}

		private static void CheckThatFirstArgumentIsIRpcContext(
			SyntaxNodeAnalysisContext context,
			MethodDeclarationSyntax method,
			(IParameterSymbol Symbol, ParameterSyntax Syntax) rpcContext,
			RpcTypes rpcTypes
		) {
			if( rpcContext == default) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.RpcContextFirstArgument, method.ParameterList.GetLocation() )
				);
				return;
			}

			if( !rpcTypes.RpcContexts.Contains( rpcContext.Symbol.Type ) ) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.RpcContextFirstArgument, rpcContext.Syntax.GetLocation() )
				);
				return;
			}

			if( IsMarkedAsDependency( rpcContext.Symbol, rpcTypes ) ) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.RpcContextMarkedDependency, rpcContext.Syntax.GetLocation() )
				);
			}
		}

		private static void CheckThatRpcParametersAreValid(
			SyntaxNodeAnalysisContext context,
			ImmutableArray<(IParameterSymbol Symbol, ParameterSyntax Syntax)> parameters,
			RpcTypes rpcTypes
		) {
			foreach( var parameter in parameters ) {
				bool isValidParameterType = IsValidParameterType( context, parameter.Symbol.Type, rpcTypes );
				if( !isValidParameterType ) {
					context.ReportDiagnostic(
						Diagnostic.Create( Diagnostics.RpcInvalidParameterType, parameter.Syntax.Type.GetLocation() )
					);
				}
			}
		}

		private static ((IParameterSymbol Symbol, ParameterSyntax Syntax) rpcContext, ImmutableArray<(IParameterSymbol Symbol, ParameterSyntax Syntax)> parameters) Split(
			SyntaxNodeAnalysisContext context,
			MethodDeclarationSyntax method,
			RpcTypes rpcTypes
		) {
			var parameters = method.ParameterList.Parameters;

			if( parameters.Count == 0 ) {
				return (default, ImmutableArray<(IParameterSymbol, ParameterSyntax)>.Empty);
			}

			SemanticModel model = context.SemanticModel;

			int index = 0;

			ParameterSyntax rpcContextSyntax = parameters[ index++ ];
			IParameterSymbol rpcContext = model.GetDeclaredSymbol( rpcContextSyntax, context.CancellationToken );
			var parametersBuilder = ImmutableArray.CreateBuilder<(IParameterSymbol, ParameterSyntax)>();

			for( ; index < parameters.Count; ++index ) {
				ParameterSyntax syntax = parameters[ index ];
				IParameterSymbol parameter = model.GetDeclaredSymbol( syntax, context.CancellationToken );

				if( IsMarkedAsDependency( parameter, rpcTypes ) ) {
					if( parametersBuilder.Count > 0 ) {
						context.ReportDiagnostic( Diagnostic.Create( Diagnostics.RpcArgumentSortOrder, syntax.GetLocation() ) );
					}

					continue;
				}

				parametersBuilder.Add( (parameter, syntax) );
			}

			return ((rpcContext, rpcContextSyntax), parametersBuilder.ToImmutable());
		}

		private static bool IsMarkedAsDependency(
			IParameterSymbol parameter,
			RpcTypes rpcTypes
		) {
			if( rpcTypes.DependencyAttribute == null ) {
				return false;
			}

			foreach( AttributeData attribute in parameter.GetAttributes() ) {
				if( SymbolEqualityComparer.Default.Equals( attribute.AttributeClass, rpcTypes.DependencyAttribute ) ) {
					return true;
				}
			}

			return false;
		}

		private sealed record RpcTypes(
			INamedTypeSymbol RpcAttribute,
			ImmutableHashSet<ITypeSymbol> RpcContexts,
			INamedTypeSymbol DependencyAttribute,
			INamedTypeSymbol IDeserializable,
			INamedTypeSymbol IDeserializer,
			INamedTypeSymbol IDictionary,
			ImmutableHashSet<ITypeSymbol> KnownRpcParameters
		);
	}
}
