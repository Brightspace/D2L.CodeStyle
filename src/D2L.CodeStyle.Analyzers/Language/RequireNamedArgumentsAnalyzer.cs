using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Language {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class RequireNamedArgumentsAnalyzer : DiagnosticAnalyzer {

		private const string RequireNamedArgumentsAttribute = "D2L.CodeStyle.Annotations.Contract.RequireNamedArgumentsAttribute";

		public sealed class ArgParamBinding {
			public ArgParamBinding(
				int position,
				string paramName,
				ArgumentSyntax syntax
			) {
				Position = position;
				ParamName = paramName;
				Syntax = syntax;
			}

			public int Position { get; }
			public string ParamName { get; }
			public ArgumentSyntax Syntax { get; }
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(
				Diagnostics.TooManyUnnamedArgs,
				Diagnostics.LiteralArgShouldBeNamed,
				Diagnostics.NamedArgumentsRequired,
				Diagnostics.SwappableArgsShouldBeNamed
			);

		public const int TOO_MANY_UNNAMED_ARGS = 5;

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );

			context.RegisterSyntaxNodeAction(
				AnalyzeCallSyntax,
				SyntaxKind.InvocationExpression
			);

			context.RegisterSyntaxNodeAction(
				AnalyzeCallSyntax,
				SyntaxKind.ObjectCreationExpression
			);

			context.RegisterSyntaxNodeAction(
				AnalyzeCallSyntax,
				SyntaxKind.ThisConstructorInitializer
			);

			context.RegisterSyntaxNodeAction(
				AnalyzeCallSyntax,
				SyntaxKind.BaseConstructorInitializer
			);
		}

		private static void AnalyzeCallSyntax(
			SyntaxNodeAnalysisContext ctx
		) {
			ArgumentListSyntax args = GetArgs( ctx.Node );
			if( args == null ) {
				return;
			}

			// Don't complain about single argument functions because they're
			// very likely to be understandable
			if( args.Arguments.Count <= 1 ) {
				return;
			}

			// Don't complain about expression trees, since they aren't allowed
			// to have named arguments
			if( IsExpressionTree( ctx.Node, ctx.SemanticModel ) ) {
				return;
			}

			ImmutableArray<ArgParamBinding> unnamedArgs =
				GetUnnamedArgs( ctx.SemanticModel, args )
				   .ToImmutableArray();

			if( unnamedArgs.IsEmpty ) {
				return;
			}

			bool requireNamedArguments = HasRequireNamedArgumentsAttribute( ctx.SemanticModel, args );
			if( requireNamedArguments ) {
				var fixerContext = CreateFixerContext( unnamedArgs );

				ctx.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.NamedArgumentsRequired,
						location: ctx.Node.GetLocation(),
						properties: fixerContext
					)
				);

				return;
			}

			if( unnamedArgs.Length >= TOO_MANY_UNNAMED_ARGS ) {
				var fixerContext = CreateFixerContext( unnamedArgs );

				ctx.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.TooManyUnnamedArgs,
						location: ctx.Node.GetLocation(),
						properties: fixerContext,
						messageArgs: TOO_MANY_UNNAMED_ARGS
					)
				);

				return;
			}

			// Retrieve a list of arguments that don't need to be named if
			// they are swappable
			ImmutableArray<ArgParamBinding> ignoredArgs = GetIgnoredArgs( ctx.SemanticModel, args ).ToImmutableArray();

			// Iterate through the unnamed arguments in the method
			HashSet<ArgParamBinding> flaggedArguments = new();
			foreach( var arg in unnamedArgs ) {
				// Literal arguments should always be named
				if( arg.Syntax.Expression is LiteralExpressionSyntax ) {
					var fixerContext = CreateFixerContext( ImmutableArray.Create( arg ) );
					ctx.ReportDiagnostic(
						Diagnostic.Create(
							descriptor: Diagnostics.LiteralArgShouldBeNamed,
							location: arg.Syntax.Expression.GetLocation(),
							properties: fixerContext,
							messageArgs: arg.ParamName
						)
					);
					continue;
				}

				// If the argument is ignored, we don't care anymore
				if( ignoredArgs.Count( ignoredArg => ignoredArg.ParamName == arg.ParamName ) > 0 ) {
					continue;
				}

				// Take note of the type of the argument
				ITypeSymbol argType = ctx.SemanticModel.GetTypeInfo( arg.Syntax.Expression ).Type;

				// Take note of the associated parameter
				IParameterSymbol param = arg.Syntax.DetermineParameter(
					ctx.SemanticModel,
					allowParams: false
				);

				// If the type of the argument is somehow null, ignore it
				if( argType == null ) {
					continue;
				}

				// Iterate through the unnamed arguments in the method for comparison
				foreach( var compareArg in unnamedArgs ) {
					// If the arguments are the same, we don't care
					if( arg.ParamName.Equals( compareArg.ParamName ) ) {
						continue;
					}

					// If the comparison argument is a literal, we don't care
					if( compareArg.Syntax.Expression is LiteralExpressionSyntax ) {
						continue;
					}

					// If the comparison argument has already been flagged as
					// swappable, we don't care
					if( flaggedArguments.Contains( compareArg ) ) {
						continue;
					}

					// If the comparison argument is ignored, we don't care
					if( ignoredArgs.Count( ignoredArg => ignoredArg.ParamName == compareArg.ParamName ) > 0 ) {
						continue;
					}

					// Take note of the type of the comparison argument
					ITypeSymbol compareArgType = ctx.SemanticModel.GetTypeInfo( compareArg.Syntax.Expression ).Type;

					// Take note of the associated parameter
					IParameterSymbol compareParam = compareArg.Syntax.DetermineParameter(
						ctx.SemanticModel,
						allowParams: false
					);

					// If the type of the comparison argument is somehow null, ignore it
					if( compareArgType == null ) {
						continue;
					}

					// Get the conversion information for both directions
					var conversion = ctx.SemanticModel.Compilation.ClassifyConversion(
						argType,
						compareParam.Type
					);
					var reverseConversion = ctx.SemanticModel.Compilation.ClassifyConversion(
						compareArgType,
						param.Type
					);

					// If swapping cannot go both ways, we don't care
					if( !conversion.Exists || !reverseConversion.Exists ) {
						continue;
					}

					// Report the first argument if it was not already
					if( flaggedArguments.Add( arg ) ) {
						var fixerContext = CreateFixerContext( ImmutableArray.Create( arg ) );
						ctx.ReportDiagnostic(
							Diagnostic.Create(
								descriptor: Diagnostics.SwappableArgsShouldBeNamed,
								location: arg.Syntax.Expression.GetLocation(),
								properties: fixerContext,
								messageArgs: new object[] {arg.ParamName, compareArg.ParamName}
							)
						);
					}

					// Report the second argument if it was not already
					if( flaggedArguments.Add( compareArg ) ) {
						var fixerContext = CreateFixerContext( ImmutableArray.Create( compareArg ) );
						ctx.ReportDiagnostic(
							Diagnostic.Create(
								descriptor: Diagnostics.SwappableArgsShouldBeNamed,
								location: compareArg.Syntax.Expression.GetLocation(),
								properties: fixerContext,
								messageArgs: new object[] {compareArg.ParamName, arg.ParamName}
							)
						);
					}
				}
			}
		}

		/// <summary>
		/// Retrieves the parameters of each argument and binds them together.
		/// </summary>
		/// <param name="model">The SemanticModel needed to retrieve the parameters</param>
		/// <param name="args">The arguments to retrieve parameters for</param>
		/// <returns>An enumeration of the ArgParamBinding objects</returns>
		private static IEnumerable<ArgParamBinding> GetArgParamBindings(
			SemanticModel model,
			BaseArgumentListSyntax args
		) {
			// Iterate through the arguments of the method
			for( int i = 0 ; i < args.Arguments.Count ; i++ ) {
				ArgumentSyntax arg = args.Arguments[i];

				IParameterSymbol param = arg.DetermineParameter(
					model,
					allowParams: false
				);

				// Not sure if this can happen but it'd be hard to do anything
				// with this param so ignore it.
				if( param == null ) {
					continue;
				}

				// IParameterSymbol.Name is documented to be possibly empty in
				// which case it is "unnamed", so ignore it.
				if( param.Name == "" ) {
					continue;
				}

				// C# allows us to create variables with the same names as reserved keywords,
				// as long as we prefix with @ (e.g. @int is a valid identifier)
				// So any parameters which are reserved must have the @ prefix
				string paramName;
				SyntaxKind paramNameKind = SyntaxFacts.GetKeywordKind( param.Name );
				if( SyntaxFacts.GetReservedKeywordKinds()
				               .Any( reservedKind => reservedKind == paramNameKind )
				) {
					paramName = "@" + param.Name;
				} else {
					paramName = param.Name;
				}

				yield return new ArgParamBinding(
					position: i,
					paramName: paramName,
					syntax: arg
				);
			}
		}

		/// <summary>
		/// Retrieves a list of arguments which do not need to be named.
		/// </summary>
		/// <param name="model">The SemanticModel needed to retrieve the parameters</param>
		/// <param name="args">The arguments to retrieve parameters for</param>
		/// <returns>An enumeration of the ArgParamBinding objects</returns>
		private static IEnumerable<ArgParamBinding> GetIgnoredArgs(
			SemanticModel model,
			BaseArgumentListSyntax args
		) {
			// Get all argument-parameter bindings for method
			ImmutableArray<ArgParamBinding> bindings
				= GetArgParamBindings( model, args ).ToImmutableArray();

			HashSet<ArgParamBinding> ignoredArgParams = new();

			// Iterate through the arguments
			foreach( ArgParamBinding argParam in bindings ) {
				string name = argParam.ParamName;

				// Iterate through the arguments to compare against
				foreach( ArgParamBinding compareArgParam in bindings ) {
					string compareName = compareArgParam.ParamName;

					// If the arguments are the same, we don't care
					if( name.Equals( compareName ) ) {
						continue;
					}

					// If the parameter names are different lengths, we don't care
					if( name.Length != compareName.Length ) {
						continue;
					}

					// Iterate through the characters in the parameter names
					bool areCloseEnough = true;
					int numOfDiffs = 0;
					for( int i = 0 ; i < name.Length ; i++ ) {
						char char1 = name.ElementAt( i );
						char char2 = compareName.ElementAt( i );

						// If the characters are the same, we don't care
						if( char1.Equals( char2 ) ) {
							continue;
						}

						// If the characters are more than 1 apart, abort
						if( Math.Abs( char1 - char2 ) > 1 ) {
							areCloseEnough = false;
							break;
						}

						// If there are more than 1 difference, abort
						numOfDiffs++;
						if( numOfDiffs > 1 ) {
							areCloseEnough = false;
							break;
						}
					}

					// If the parameter names have only one character difference,
					// and that character is different by only a single (ASCII)
					// value, then add them to the ignore list
					if( areCloseEnough ) {
						ignoredArgParams.Add( argParam );
						ignoredArgParams.Add( compareArgParam );
					}
				}
			}

			return ignoredArgParams.ToImmutableArray();
		}

		/// <summary>
		/// Retrieves a list of arguments which are unnamed.
		/// </summary>
		/// <param name="model">The SemanticModel needed to retrieve the parameters</param>
		/// <param name="args">The arguments to retrieve parameters for</param>
		/// <returns>An enumeration of the ArgParamBinding objects</returns>
		private static IEnumerable<ArgParamBinding> GetUnnamedArgs(
			SemanticModel model,
			BaseArgumentListSyntax args
		) {
			// Get all argument-parameter bindings for method
			foreach( ArgParamBinding argParam in GetArgParamBindings( model, args ) ) {
				// Ignore already named arguments
				if( argParam.Syntax.NameColon != null ) {
					continue;
				}

				// Retrieve the name of the argument without certain prefixes
				string pseudoName = GetPseudoName( argParam.Syntax );

				// Make sure the name still contains something
				if( pseudoName != null ) {
					// If the parameter name matches the argument name,
					// consider the argument to be named
					if( string.Equals(
						pseudoName,
						argParam.ParamName.Replace( "@", "" ),
						StringComparison.OrdinalIgnoreCase
					) ) {
						continue;
					}
				}

				// If the argument is unnamed, return it
				yield return argParam;
			}
		}

		private static ImmutableDictionary<string, string> CreateFixerContext( ImmutableArray<ArgParamBinding> unnamedArgs ) {

			// Pass the names and positions for each unnamed arg to the codefix.
			ImmutableDictionary<string, string> context = unnamedArgs
				.ToImmutableDictionary(
					keySelector: binding => binding.Position.ToString(),
					elementSelector: binding => binding.ParamName
				);

			return context;
		}

		private static bool IsExpressionTree( SyntaxNode node, SemanticModel model ) {
			// Expression trees aren't compatible with named arguments,
			// so skip any expressions
			// Only lambda type expressions have arguments,
			// so this only applies to LambdaExpression
			var expressionType = model.Compilation.
				GetTypeByMetadataName( "System.Linq.Expressions.LambdaExpression" );

			if( expressionType == null || expressionType.Kind == SymbolKind.ErrorType ) {
				return false;
			}

			// the current call could be nested inside an expression tree, so
			// check every call we are nested inside
			foreach( var syntax in node.AncestorsAndSelf() ) {
				if( !( syntax is InvocationExpressionSyntax || syntax is ObjectCreationExpressionSyntax ) ) {
					continue;
				}

				var implicitType = model.GetTypeInfo( syntax.Parent ).ConvertedType;
				if( implicitType != null && implicitType.Kind != SymbolKind.ErrorType ) {

					var baseExprType = implicitType.BaseType;
					if( expressionType.OriginalDefinition.Equals( baseExprType, SymbolEqualityComparer.Default ) ) {
						return true;
					}
				}
			}

			return false;
		}

		private static string GetPseudoName( ArgumentSyntax arg ) {

			string ident = arg.Expression.TryGetInferredMemberName();
			if( ident == null ) {
				return null;
			}

			// Strip uninteresting prefixes off the identifier
			if( ident.StartsWith( "m_" ) || ident.StartsWith( "s_" ) ) {
				// e.g. m_foo -> foo
				ident = ident.Substring( 2 );
			} else if( ident[0] == '_' && ident.Length > 1 && ident[1] != '_' ) {
				// e.g. _foo -> foo
				ident = ident.Substring( 1 );
			}

			return ident;
		}

		// Not an extension method because there may be more cases (e.g. in the
		// future) and if more than this fix + its analyzer used this logic
		// there could be undesirable coupling if we handled more cases.
		internal static ArgumentListSyntax GetArgs( SyntaxNode syntax ) {
			switch( syntax ) {
				case InvocationExpressionSyntax invocation:
					return invocation.ArgumentList;
				case ObjectCreationExpressionSyntax objectCreation:
					return objectCreation.ArgumentList;
				case ConstructorInitializerSyntax constructorInitializer:
					return constructorInitializer.ArgumentList;
				default:
					if( syntax.Parent is ArgumentSyntax ) {
						return (ArgumentListSyntax)syntax.Parent.Parent;
					}
					return null;
			}
		}

		private static bool HasRequireNamedArgumentsAttribute(
				SemanticModel model,
				ArgumentListSyntax args
			) {

			ISymbol symbol = model.GetSymbolInfo( args.Parent ).Symbol;
			if( symbol == null ) {
				return false;
			}

			bool hasAttribute = symbol
				.GetAttributes()
				.Any( x => x.AttributeClass.GetFullTypeName() == RequireNamedArgumentsAttribute );

			return hasAttribute;
		}
	}
}
