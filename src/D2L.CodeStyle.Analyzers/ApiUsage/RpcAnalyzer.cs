﻿using System.Collections.Immutable;
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
			var dependencyAttributeType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.DependencyAttribute" );
			var deserializableType = context.Compilation.GetTypeByMetadataName( "D2L.Serialization.IDeserializable" );
			var deserializerType = context.Compilation.GetTypeByMetadataName( "D2L.Serialization.IDeserializer" );
			var dictionaryType = context.Compilation.GetTypeByMetadataName( "System.Collections.Generic.IDictionary`2" );

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

			// intentionally not checking dependencyAttributeType: still want
			// the analyzer to run if that is not in scope.
			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeMethod(
					ctx,
					rpcAttributeType: rpcAttributeType,
					rpcContextType: rpcContextType,
					rpcPostContextType: rpcPostContextType,
					rpcPostContextBaseType: rpcPostContextBaseType,
					dependencyAttributeType: dependencyAttributeType,
					deserializableType: deserializableType,
					deserializerType: deserializerType,
					dictionaryType: dictionaryType,
					knownRpcParameterTypes: knownRpcParameterTypes
				),
				SyntaxKind.MethodDeclaration
			);
		}

		private static void AnalyzeMethod(
			SyntaxNodeAnalysisContext context,
			INamedTypeSymbol rpcAttributeType,
			INamedTypeSymbol rpcContextType,
			INamedTypeSymbol rpcPostContextType,
			INamedTypeSymbol rpcPostContextBaseType,
			INamedTypeSymbol dependencyAttributeType,
			INamedTypeSymbol deserializableType,
			INamedTypeSymbol deserializerType,
			INamedTypeSymbol dictionaryType,
			ImmutableHashSet<INamedTypeSymbol> knownRpcParameterTypes
		) {
			var method = context.Node as MethodDeclarationSyntax;

			if( method == null ) {
				return;
			}

			if( !CheckThatMethodIsRpc( context, method, rpcAttributeType ) ) {
				return;
			}

			CheckThatFirstArgumentIsIRpcContext(
				context,
				method,
				rpcContextType: rpcContextType,
				rpcPostContextType: rpcPostContextType,
				rpcPostContextBaseType: rpcPostContextBaseType,
				dependencyAttributeType: dependencyAttributeType
			);

			// dependencyAttributeType may be null if that DLL isn't
			// referenced. In that case don't do some of these checks.
			if( dependencyAttributeType != null ) {
				CheckThatDependencyArgumentsAreSortedCorrectly(
					context,
					method.ParameterList.Parameters,
					dependencyAttributeType
				);

				// other things to check:
				// - appropriate use of [Dependency]
			}

			CheckThatRpcParametersAreValid(
				context: context,
				ps: method.ParameterList.Parameters,
				dependencyAttributeType: dependencyAttributeType,
				deserializableType: deserializableType,
				deserializerType: deserializerType,
				dictionaryType: dictionaryType,
				knownRpcParameterTypes: knownRpcParameterTypes
			);
		}

		private static void CheckThatRpcParametersAreValid(
			SyntaxNodeAnalysisContext context,
			SeparatedSyntaxList<ParameterSyntax> ps,
			INamedTypeSymbol dependencyAttributeType,
			INamedTypeSymbol deserializableType,
			INamedTypeSymbol deserializerType,
			INamedTypeSymbol dictionaryType,
			ImmutableHashSet<INamedTypeSymbol> knownRpcParameterTypes
		) {
			foreach( var parameter in ps.Skip( 1 ) ) {
				// Skip injected parameters
				if( dependencyAttributeType != null ) {
					if( IsMarkedDependency(
						dependencyAttributeType: dependencyAttributeType,
						parameter: parameter,
						model: context.SemanticModel
					) ) {
						continue;
					}
				}

				var parameterTypeSymbol = context.SemanticModel.GetSymbolInfo( parameter.Type ).Symbol as ITypeSymbol;
				bool isValidParameterType = IsValidParameterType(
					context: context,
					type: parameterTypeSymbol,
					deserializableType: deserializableType,
					deserializerType: deserializerType,
					dictionaryType: dictionaryType,
					knownRpcParameterTypes: knownRpcParameterTypes
				);
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
			INamedTypeSymbol rpcAttributeType
		) {
			bool isRpc = method
				.AttributeLists
				.SelectMany( al => al.Attributes )
				.Any( attr => IsAttribute( rpcAttributeType, attr, context.SemanticModel ) );

			return isRpc;
		}

		private static void CheckThatFirstArgumentIsIRpcContext(
			SyntaxNodeAnalysisContext context,
			MethodDeclarationSyntax method,
			INamedTypeSymbol rpcContextType,
			INamedTypeSymbol rpcPostContextType,
			INamedTypeSymbol rpcPostContextBaseType,
			INamedTypeSymbol dependencyAttributeType
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

			var firstParamIsReasonableType =
				rpcContextType.Equals( firstParamType, SymbolEqualityComparer.Default ) ||
				rpcPostContextType.Equals( firstParamType, SymbolEqualityComparer.Default ) ||
				rpcPostContextBaseType.Equals( firstParamType, SymbolEqualityComparer.Default );

			if( !firstParamIsReasonableType ) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.RpcContextFirstArgument, firstParam.GetLocation() )
				);
			} else if( dependencyAttributeType != null
				&& IsMarkedDependency( dependencyAttributeType, firstParam, context.SemanticModel )
			) {
				context.ReportDiagnostic(
					Diagnostic.Create( Diagnostics.RpcContextMarkedDependency, firstParam.GetLocation() )
				);
			}
		}

		private static void CheckThatDependencyArgumentsAreSortedCorrectly(
			SyntaxNodeAnalysisContext context,
			SeparatedSyntaxList<ParameterSyntax> ps,
			INamedTypeSymbol dependencyAttributeType
		) {
			bool doneDependencies = false;
			foreach( var param in ps.Skip( 1 ) ) {
				var isDep = IsMarkedDependency( dependencyAttributeType, param, context.SemanticModel );

				if( !isDep && !doneDependencies ) {
					doneDependencies = true;
				} else if( isDep && doneDependencies ) {
					context.ReportDiagnostic( Diagnostic.Create( Diagnostics.RpcArgumentSortOrder, param.GetLocation() ) );
				}
			}
		}

		private static bool IsMarkedDependency(
			INamedTypeSymbol dependencyAttributeType,
			ParameterSyntax parameter,
			SemanticModel model
		) => parameter
			.AttributeLists
			.SelectMany( al => al.Attributes )
			.Any( attr => IsAttribute( dependencyAttributeType, attr, model ) );

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
	}
}
