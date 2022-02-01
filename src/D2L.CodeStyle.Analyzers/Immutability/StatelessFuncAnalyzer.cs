using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
			ISymbol? statelessFuncAttr = compilation.GetTypeByMetadataName( "D2L.CodeStyle.Annotations.Contract.StatelessFuncAttribute" );

			if( statelessFuncAttr == null ) {
				return;
			}

			ImmutableHashSet<ISymbol> statelessFuncs = GetStatelessFuncTypes( compilation );

			context.RegisterOperationAction(
				ctx => {
					AnalyzeArgument(
						ctx,
						(IArgumentOperation)ctx.Operation,
						statelessFuncAttr,
						statelessFuncs
					);
				},
				OperationKind.Argument
			);
		}

		private static void AnalyzeArgument(
			OperationAnalysisContext context,
			IArgumentOperation argumentOperation,
			ISymbol statelessFuncAttribute,
			ImmutableHashSet<ISymbol> statelessFuncs
		) {
			if( argumentOperation.ArgumentKind == ArgumentKind.DefaultValue ) {
				return;
			}

			IParameterSymbol? param = argumentOperation.Parameter;
			if( param == null ) {
				return;
			}

			if( !IsParameterMarkedStateless( statelessFuncAttribute, param ) ) {
				return;
			}

			IOperation operationToInspect = argumentOperation.Value;
			switch( operationToInspect ) {
				/**
				 * Any statement defining an anonymous function:
				 *   () => 0
				 *   x => 0
				 *   delegate () { return 0; }
				 * We grab the target to get the actual function out instead of
				 * the act of defining it.
				 */
				case IDelegateCreationOperation delegateCreation:
					operationToInspect = delegateCreation.Target;
					break;

				/**
				 * If the argument is being converted, get the original value
				 * and check its type / operation instead. This comes up for
				 * this case specifically:
				 *   new StatelessFunc<int>( new StatelessFunc<int>( ... ) );
				 * Because new StatelessFunc takes Funcs, the argument is
				 * implicitly converted to a Func. We need to look at the value
				 * to determine it was of type StatelessFunc and accept it.
				 */
				case IConversionOperation conversionOperation:
					operationToInspect = conversionOperation.Operand;
					break;
			}

			/**
			* Even though we haven't specifically accounted for its
			* source if it's a StatelessFunc<T> we're reasonably
			* certain its been analyzed.
			*/
			ISymbol? type = operationToInspect.Type;
			if( type != null && IsStatelessFunc( type, statelessFuncs ) ) {
				return;
			}

			Diagnostic diag;
			switch( operationToInspect ) {
				// this is the case when a method reference is used
				// eg Func<string, int> func = int.Parse
				case IMemberReferenceOperation memberReference:
					if( memberReference.Member.IsStatic ) {
						return;
					}

					// non-static member access means that state could
					// be used / held.
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argumentOperation.Syntax.GetLocation(),
						$"{ argumentOperation.Syntax } is not static"
					);
					break;

				/**
				 * Any lambda / delegate. See first switch re DelegateCreation.
				 */
				case IAnonymousFunctionOperation lambda:
					if( lambda.Symbol.IsStatic ) {
						return;
					}

					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argumentOperation.Syntax.GetLocation(),
						"Lambda is not static"
					);
					break;

				// this is the case where an expression is invoked,
				// which returns a Func<T>:
				//   ( () => { return () => 1 } )()
				// Invocations which return a StatelessFunc were handled by the
				// type check.
				case IInvocationOperation:
					// we are rejecting this because it is tricky to
					// analyze properly, but also a bit ugly and should
					// never really be necessary
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argumentOperation.Syntax.GetLocation(),
						$"Invocations are not allowed: { argumentOperation.Syntax }"
					);

					break;

				/**
				 * This is the case where a paraeter is passed in
				 *   void MyMethod<T>( Func<T> f ) {
				 *     Foo( f )
				 *   }
				 *   void MyMethod<T>( [StatelessFunc] Func<T> f ) {
				 *     Foo( f )
				 *   }
				 * If the parameter was StatelesFunc<T>, it was allowed via the
				 * type check.
				 */
				case IParameterReferenceOperation paramReference:
					/**
					 * If it's a local parameter marked with [StatelessFunc] we're reasonably
					 * certain it was analyzed on the caller side.
					 */
					if( IsParameterMarkedStateless(
						statelessFuncAttribute,
						paramReference.Parameter
					) ) {
						return;
					}

					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argumentOperation.Syntax.GetLocation(),
						$"Parameter { argumentOperation.Syntax } is not marked [StatelessFunc]."
					);
					break;

				/**
				 * This is the case where a local variable is passed in
				 *   var f = () => 0;
				 *   Foo( f )
				 * If the variable was StatelessFunc<T>, it was allowed via the
				 * type check.
				 */
				case ILocalReferenceOperation:
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argumentOperation.Syntax.GetLocation(),
						$"Unable to determine if { argumentOperation.Syntax } is stateless."
					);

					break;

				default:
					// we need StatelessFunc<T> to be ultra safe, so we'll
					// reject usages we do not understand yet
					diag = Diagnostic.Create(
						Diagnostics.StatelessFuncIsnt,
						argumentOperation.Syntax.GetLocation(),
						$"Unable to determine safety of { argumentOperation.Syntax }. This is an unexpectes usage of StatelessFunc<T>"
					);

					break;
			}

			context.ReportDiagnostic( diag );
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
			ISymbol statelessFuncAttribute,
			IParameterSymbol param
		) {
			ImmutableArray<AttributeData> paramAttributes = param.GetAttributes();
			foreach( AttributeData a in paramAttributes ) {
				if( SymbolEqualityComparer.Default.Equals( a.AttributeClass, statelessFuncAttribute ) ) {
					return true;
				}
			}

			return false;
		}

		private static ImmutableHashSet<ISymbol> GetStatelessFuncTypes( Compilation compilation) {

			var builder = ImmutableHashSet.CreateBuilder<ISymbol>( SymbolEqualityComparer.Default );

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
				INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName( typeName );

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

			return builder.ToImmutable();
		}
	}
}
