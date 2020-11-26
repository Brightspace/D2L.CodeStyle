using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class StatelessFuncAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.StatelessFuncIsnt
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private static void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;
			ISymbol statelessFuncAttr = compilation.GetTypeByMetadataName( "D2L.CodeStyle.Annotations.Contract.StatelessFuncAttribute" );

			if( statelessFuncAttr == null ) {
				return;
			}

			ImmutableHashSet<ISymbol> statelessFuncs = GetStatelessFuncTypes( compilation );

			context.RegisterSyntaxNodeAction(
				ctx => {
					AnalyzeArgument(
						ctx,
						statelessFuncAttr,
						statelessFuncs
					);
				},
				SyntaxKind.Argument
			);
		}

		private static void AnalyzeArgument(
			SyntaxNodeAnalysisContext context,
			ISymbol statelessFuncAttribute,
			ImmutableHashSet<ISymbol> statelessFuncs
		) {
			ArgumentSyntax syntax = context.Node as ArgumentSyntax;

			SemanticModel model = context.SemanticModel;

			IParameterSymbol param = syntax.DetermineParameter( model );

			if( param == null ) {
				return;
			}

			ImmutableArray<AttributeData> paramAttributes = param.GetAttributes();
			if( !paramAttributes.Any( a => a.AttributeClass.Equals( statelessFuncAttribute, SymbolEqualityComparer.Default ) ) ) {
				return;
			}

			ExpressionSyntax argument = syntax.Expression;

			/**
			* Even though we haven't specifically accounted for its
			* source if it's a StatelessFunc<T> we're reasonably
			* certain its been analyzed.
			*/
			ISymbol type = model.GetTypeInfo( argument ).Type;
			if( type != null && IsStatelessFunc( type, statelessFuncs ) ) {
				return;
			}

			Diagnostic diag;
			SyntaxKind kind = argument.Kind();
			switch( kind ) {

				// this is the case when a method reference is used
				// eg Func<string, int> func = int.Parse
				case SyntaxKind.SimpleMemberAccessExpression:
					if( IsStaticMemberAccess( context, argument ) ) {
						return;
					}

					// non-static member access means that state could
					// be used / held.
					// TODO: Look for [Immutable] on the member's type
					// to determine if the non-static member is safe
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argument.GetLocation(),
						$"{ argument.ToString() } is not static"
					);
					break;

				// this is the case when a "delegate" is used
				// eg delegate( int x, int y ) { return x + y; }
				case SyntaxKind.AnonymousMethodExpression:
				// this is the case when the left hand side of the
				// lambda has parens
				// eg () => 1, (x, y) => x + y
				case SyntaxKind.ParenthesizedLambdaExpression:
				// this is the case when the left hand side of the
				// lambda does not have parens
				// eg x => x + 1
				case SyntaxKind.SimpleLambdaExpression:
					bool hasCaptures = TryGetCaptures(
						context,
						argument,
						out ImmutableArray<ISymbol> captures
					);
					if( !hasCaptures ) {
						return;
					}

					string captured = string.Join( ", ", captures.Select( c => c.Name ) );
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argument.GetLocation(),
						$"Captured variable(s): { captured }"
					);
					break;

				// this is the case where an expression is invoked,
				// which returns a Func<T>
				// eg ( () => { return () => 1 } )()
				case SyntaxKind.InvocationExpression:
					// we are rejecting this because it is tricky to
					// analyze properly, but also a bit ugly and should
					// never really be necessary
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argument.GetLocation(),
						$"Invocations are not allowed: { argument.ToString() }"
					);

					break;

				/**
				 * This is the case where a variable is passed in
				 * eg Foo( f )
				 * Where f might be a local variable, a parameter, or field
				 *
				 * class C<T> {
				 *   StatelessFunc<T> m_f;
				 *
				 *   void P( StatelessFunc<T> f ) { Foo( f ); }
				 *   void Q( [StatelessFunc] Func<T> f ) { Foo( f ); }
				 *   void R() { StatelessFunc<T> f; Foo( f ); }
				 *   void S() { Foo( m_f ); }
				 *   void T() : this( StaticMemberMethod ) {}
				 * }
				 */
				case SyntaxKind.IdentifierName:
					/**
					 * If it's a local parameter marked with [StatelessFunc] we're reasonably
					 * certain it was analyzed on the caller side.
					 */
					if( IsParameterMarkedStateless(
						model,
						statelessFuncAttribute,
						argument as IdentifierNameSyntax,
						context.CancellationToken
					) ) {
						return;
					}

					if( IsStaticMemberAccess(
						context,
						argument as IdentifierNameSyntax
					) ) {
						return;
					}

					/**
					 * If it's any other variable. We're not sure
					 */
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argument.GetLocation(),
						$"Unable to determine if { argument.ToString() } is stateless."
					);

					break;

				default:
					// we need StatelessFunc<T> to be ultra safe, so we'll
					// reject usages we do not understand yet
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argument.GetLocation(),
						$"Unable to determine safety of { argument.ToString() }. This is an unexpectes usage of StatelessFunc<T>"
					);

					break;
			}

			context.ReportDiagnostic( diag );
		}

		private static bool IsStaticMemberAccess(
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax expression
		) {
			ISymbol symbol = context
				.SemanticModel
				.GetSymbolInfo( expression ).Symbol;

			if( symbol == null ) {
				return false;
			}

			return symbol.IsStatic;
		}

		private static bool TryGetCaptures(
			SyntaxNodeAnalysisContext context,
			ExpressionSyntax expression,
			out ImmutableArray<ISymbol> captures
		) {

			DataFlowAnalysis dataFlow = context
				.SemanticModel
				.AnalyzeDataFlow( expression );

			captures = dataFlow.Captured;
			return captures.Length > 0;
		}

		private static bool IsStatelessFunc(
			ISymbol symbol,
			ImmutableHashSet<ISymbol> statelessFuncs
		) {
			// Generics work a bit different, in that the symbol we have to work
			// with is not (eg StatelessFunc<int> ), but the list of symbols we're
			// checking against are the definitions, which are (eg
			// StatelessFunc<T> ) generic. So check the "parent" definition.
			return statelessFuncs.Contains( symbol.OriginalDefinition );
		}

		private static bool IsParameterMarkedStateless(
			SemanticModel model,
			ISymbol statelessFuncAttr,
			IdentifierNameSyntax identifer,
			CancellationToken ct
		) {
			ISymbol symbol = model.GetSymbolInfo( identifer ).Symbol;
			if( symbol == null ) {
				return false;
			}

			var declarations = symbol.DeclaringSyntaxReferences;
			if( declarations.Length != 1 ) {
				return false;
			}

			ParameterSyntax parameterSyntax = declarations[0].GetSyntax( ct ) as ParameterSyntax;
			if( parameterSyntax == null ) {
				return false;
			}

			IParameterSymbol parameter = model.GetDeclaredSymbol( parameterSyntax, ct );

			foreach( AttributeData attr in parameter.GetAttributes() ) {
				if( attr.AttributeClass.Equals( statelessFuncAttr, SymbolEqualityComparer.Default ) ) {
					return true;
				}
			}

			return false;
		}

		private static ImmutableHashSet<ISymbol> GetStatelessFuncTypes( Compilation compilation) {

			var builder = ImmutableHashSet.CreateBuilder<ISymbol>();

			var types = new string[] {
				"D2L.StatelessFunc`1",
				"D2L.StatelessFunc`2",
				"D2L.StatelessFunc`3",
				"D2L.StatelessFunc`4",
				"D2L.StatelessFunc`5",
				"D2L.StatelessFunc`6",
				"D2L.StatelessFunc`7",
				"D2L.StatelessFunc`8",
				"D2L.StatelessFunc`9",
				"D2L.StatelessFunc`10",
				"D2L.StatelessFunc`11",
			};

			foreach( string typeName in types ) {
				INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName( typeName );

				if( typeSymbol == null ) {
					// These types are usually defined in another assembly,
					// ( usually ) and thus we have a bit of a circular
					// dependency. Allowing this lookup to return null
					// allow us to update the analyzer before the other
					// assembly, which should be safer
					continue;
				}

				builder.Add( typeSymbol.OriginalDefinition );
			}

			return builder.ToImmutableHashSet();
		}
	}
}
