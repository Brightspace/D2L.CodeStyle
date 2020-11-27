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

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.TooManyUnnamedArgs,
			Diagnostics.LiteralArgShouldBeNamed,
			Diagnostics.NamedArgumentsRequired
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

			// Literal arguments should always be named
			foreach( var arg in unnamedArgs ) {

				// Check if the argument type is literal
				if( arg.Syntax.Expression is LiteralExpressionSyntax ) {

					var fixerContext = CreateFixerContext( ImmutableArray.Create( arg ) );

					ctx.ReportDiagnostic( Diagnostic.Create(
							descriptor: Diagnostics.LiteralArgShouldBeNamed,
							location: arg.Syntax.Expression.GetLocation(),
							properties: fixerContext,
							messageArgs: arg.ParamName
						)
					);
				}
			}

			// TODO: if there are duplicate typed args then they should be named
			// These will create a bit more cleanup. Fix should probably name
			// all the args instead to avoid craziness with overloading.
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

		/// <summary>
		/// Get the arguments which are unnamed and not "params"
		/// </summary>
		private static IEnumerable<ArgParamBinding> GetUnnamedArgs(
				SemanticModel model,
				ArgumentListSyntax args
			) {

			for( int idx = 0; idx < args.Arguments.Count; idx++ ) {
				ArgumentSyntax arg = args.Arguments[ idx ];

				// Ignore args that already have names
				if( arg.NameColon != null ) {
					continue;
				}

				IParameterSymbol param = arg.DetermineParameter(
					model,

					// Don't map params arguments. It's okay that they are
					// unnamed. Some things like ImmutableArray.Create() could
					// take a large number of args and we don't want anything to
					// be named. Named params really suck so we may still
					// encourage it but APIs that take params and many other
					// args would suck anyway.
					allowParams: false
				);

				// Not sure if this can happen but it'd be hard to name this
				// param so ignore it.
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
				if( SyntaxFacts.GetReservedKeywordKinds().Any( reservedKind => reservedKind == paramNameKind ) ) {
					paramName = "@" + param.Name;
				} else {
					paramName = param.Name;
				}

				string psuedoName = GetPsuedoName( arg );
				if( psuedoName != null ) {

					bool matchesParamName = string.Equals(
						psuedoName,
						param.Name,
						StringComparison.OrdinalIgnoreCase
					);

					if( matchesParamName ) {
						continue;
					}
				}

				yield return new ArgParamBinding(
					position: idx,
					paramName: paramName, // Use the verbatim parameter name if applicable
					syntax: arg
				);
			}
		}

		private static string GetPsuedoName( ArgumentSyntax arg ) {

			string ident = arg.Expression.TryGetInferredMemberName();
			if( ident == null ) {
				return null;
			}

			// Strip uninteresting prefixes off the identifier
			if( ident.StartsWith( "m_" ) || ident.StartsWith( "s_" ) ) {
				// e.g. m_foo -> foo
				ident = ident.Substring( 2 );
			} else if( ident[ 0 ] == '_' && ident.Length > 1 && ident[ 1 ] != '_' ) {
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
