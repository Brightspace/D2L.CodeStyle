using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.ServiceLocator {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class SingletonLocatorAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.SingletonLocatorMisuse );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterSingletonLocatorAnalyzer );
		}

		public void RegisterSingletonLocatorAnalyzer( CompilationStartAnalysisContext context ) {
			// Cache some important type lookups
			var locatorType = context.Compilation.GetTypeByMetadataName( "D2L.LP.Extensibility.Activation.Domain.SingletonLocator" );

			// If this type lookup failed then SingletonLocator cannot resolve
			// and we don't need to register our analyzer.
			if( locatorType.IsNullOrErrorType() ) {
				return;
			}

			IDictionary<INamedTypeSymbol, int> containerTypes = new [] {
					new { TypeName = "D2L.LP.Extensibility.Activation.Domain.IPlugins`1", ContainedTypeIdx = 0 },
					new { TypeName = "D2L.LP.Extensibility.Activation.Domain.IPlugins`2", ContainedTypeIdx = 1 },
					new { TypeName = "D2L.LP.Extensibility.Plugins.IInstancePlugins`1",   ContainedTypeIdx = 0 },
					new { TypeName = "D2L.LP.Extensibility.Plugins.IInstancePlugins`2",   ContainedTypeIdx = 0 }
				}
				.Select( x => new { Type = context.Compilation.GetTypeByMetadataName( x.TypeName ), x.ContainedTypeIdx } )
				.Where( x => !x.Type.IsNullOrErrorType() )
				.ToDictionary( x => x.Type, x => x.ContainedTypeIdx );

			context.RegisterSyntaxNodeAction(
				ctx => EnforceSingletonsOnly(
					ctx,
					IsSingletonLocator,
					IsContainerType
				),
				SyntaxKind.SimpleMemberAccessExpression,
				SyntaxKind.InvocationExpression
			);

			bool IsSingletonLocator( INamedTypeSymbol other ) {
				if( !locatorType.IsNullOrErrorType() && other.Equals( locatorType, SymbolEqualityComparer.Default ) ) {
					return true;
				}

				return false;
			}

			bool IsContainerType( ITypeSymbol type, out ITypeSymbol containedType ) {

				if( !( type is INamedTypeSymbol namedType ) ) {
					containedType = null;
					return false;
				}

				if( containerTypes.TryGetValue( namedType.OriginalDefinition, out int typeArgumentIndex ) ) {
					containedType = namedType.TypeArguments[ typeArgumentIndex ];
					return true;
				}

				containedType = null;
				return false;
			}
		}

		private delegate bool IsContainerType( ITypeSymbol type, out ITypeSymbol containedType );

		// Enforce that SingletonLocator can only load actual [Singleton]s
		private static void EnforceSingletonsOnly(
			SyntaxNodeAnalysisContext context,
			Func<INamedTypeSymbol, bool> isSingletonLocator,
			IsContainerType isContainerType
		) {
			var root = GetRootNode( context );
			if( root == null ) {
				return;
			}
			var symbolinfo = context.SemanticModel.GetSymbolInfo( root );

			var method = symbolinfo.Symbol as IMethodSymbol;

			if( method == null ) {
				if( symbolinfo.CandidateSymbols == null ) {
					return;
				}

				if( symbolinfo.CandidateSymbols.Length != 1 ) {
					return;
				}

				//This happens on method groups, such as
				//  Func<IFoo> fooFunc = OldAndBrokenServiceLocator.Get<IFoo>;
				method = symbolinfo.CandidateSymbols.First() as IMethodSymbol;

				if( method == null ) {
					return;
				}
			}

			// At this point method is a non-null IMethodSymbol
			if( !isSingletonLocator( method.ContainingType ) ) {
				return;

			}

			if( !IsSingletonGet( method ) ) {
				return;
			}

			//It's ok as long as the attribute is present, error otherwise
			ITypeSymbol typeArg = method.TypeArguments.First();

			if( isContainerType( typeArg, out ITypeSymbol containedType ) ) {
				typeArg = containedType;
			}

			if( Attributes.Singleton.IsDefined( typeArg ) ) {
				return;
			}

			context.ReportDiagnostic(
				Diagnostic.Create( Diagnostics.SingletonLocatorMisuse, context.Node.GetLocation(), typeArg.GetFullTypeName() )
			);
		}

		private static ExpressionSyntax GetRootNode( SyntaxNodeAnalysisContext context ) {
			//It turns out that depending on how you call the locator, it might contain any of:
			//* a SimpleMemberAccessExpression
			//* an InvocationExpression
			//* an InvocationExpression wrapped around a SimpleMemberAccessExpression
			//We want to count each of these as a single error (avoid double counting the last case)
			ExpressionSyntax root = context.Node as InvocationExpressionSyntax;
			if( root != null ) {
				return root;
			}

			root = context.Node as MemberAccessExpressionSyntax;
			if( root == null ) {
				return null;
			}

			if( root.Parent.IsKind( SyntaxKind.InvocationExpression ) ) {
				return null;
			}
			return root;
		}

		private static bool IsSingletonGet( IMethodSymbol method ) {
			return "Get".Equals( method.Name )
				&& method.IsGenericMethod
				&& method.TypeArguments.Length == 1;
		}

	}
}
