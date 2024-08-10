#nullable enable

using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D2L.CodeStyle.Analyzers.Language {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class RequireNamedArgumentsAnalyzer : DiagnosticAnalyzer {

		private const string LambdaExpressionMetadataName = "System.Linq.Expressions.LambdaExpression";
		private const string RequireNamedArgumentsAttribute = "D2L.CodeStyle.Annotations.Contract.RequireNamedArgumentsAttribute";

		public sealed class ArgParamBinding {
			public ArgParamBinding(
				int position,
				string paramName,
				IArgumentOperation operation
			) {
				Position = position;
				ParamName = paramName;
				Operation = operation;
			}

			public int Position { get; }
			public string ParamName { get; }
			public IArgumentOperation Operation { get; }
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.TooManyUnnamedArgs,
			Diagnostics.LiteralArgShouldBeNamed,
			Diagnostics.NamedArgumentsRequired
		);

		public const int TooManyUnnamedArgs = 5;

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );

			context.RegisterCompilationStartAction( OnCompilationStart );
		}

		private static void OnCompilationStart( CompilationStartAnalysisContext context ) {
			INamedTypeSymbol? requiredNamedArguments = context.Compilation.GetTypeByMetadataName( RequireNamedArgumentsAttribute );
			if( requiredNamedArguments is null ) {
				return;
			}

			ImmutableHashSet<ISymbol> exemptions = new ExemptSymbolsBuilder( context )
				.AddFromAdditionalFiles( "D2L.CodeStyle.RequireNamedArguments.Exemptions" )

				// HashCode.Combine takes a series of args named value1..valueN which are not useful to name
				.AddFromDocumentationCommentId( "M:System.HashCode.Combine``1(``0)" )
				.AddFromDocumentationCommentId( "M:System.HashCode.Combine``2(``0,``1)" )
				.AddFromDocumentationCommentId( "M:System.HashCode.Combine``3(``0,``1,``2)" )
				.AddFromDocumentationCommentId( "M:System.HashCode.Combine``4(``0,``1,``2,``3)" )
				.AddFromDocumentationCommentId( "M:System.HashCode.Combine``5(``0,``1,``2,``3,``4)" )
				.AddFromDocumentationCommentId( "M:System.HashCode.Combine``6(``0,``1,``2,``3,``4,``5)" )
				.AddFromDocumentationCommentId( "M:System.HashCode.Combine``7(``0,``1,``2,``3,``4,``5,``6)" )
				.AddFromDocumentationCommentId( "M:System.HashCode.Combine``8(``0,``1,``2,``3,``4,``5,``6,``7)" )

				.Build();

			INamedTypeSymbol? lambdaExpresssion = context.Compilation.GetTypeByMetadataName( LambdaExpressionMetadataName );

			context.RegisterOperationAction(
				ctx => AnalyzeInvocation(
					ctx,
					requiredNamedArguments,
					lambdaExpresssion,
					exemptions
				),
				OperationKind.Invocation,
				OperationKind.ObjectCreation
			);
		}

		private static void AnalyzeInvocation(
			OperationAnalysisContext ctx,
			INamedTypeSymbol requireNamedArgumentsSymbol,
			INamedTypeSymbol? lambdaExpresssionSymbol,
			ImmutableHashSet<ISymbol> exemptions
		) {
			(IMethodSymbol targetMethod, ImmutableArray<IArgumentOperation> args) = ctx.Operation switch {
				IInvocationOperation op => (op.TargetMethod, op.Arguments),
				IObjectCreationOperation op => (op.Constructor!, op.Arguments),
				_ => default
			};

			if( targetMethod == default || args == default ) {
				return;
			}

			if( args.IsEmpty ) {
				return;
			}

			if( exemptions.Contains( targetMethod.OriginalDefinition ) ) {
				return;
			}

			bool requireNamedArguments = HasRequireNamedArgumentsAttribute( requireNamedArgumentsSymbol, targetMethod );

			// Don't complain about single argument functions because they're
			// very likely to be understandable
			if( !requireNamedArguments && args.Length == 1 ) {
				return;
			}

			ImmutableArray<ArgParamBinding> unnamedArgs = GetUnnamedArgs( args ).ToImmutableArray();
			if( unnamedArgs.IsEmpty ) {
				return;
			}

			// Don't complain about expression trees, since they aren't allowed
			// to have named arguments
			if( IsExpressionTree( lambdaExpresssionSymbol, ctx.Operation ) ) {
				return;
			}

			if( requireNamedArguments ) {

				var fixerContext = CreateFixerContext( unnamedArgs );

				ctx.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.NamedArgumentsRequired,
						location: ctx.Operation.Syntax.GetLocation(),
						properties: fixerContext
					)
				);

				return;
			}

			if( unnamedArgs.Length >= TooManyUnnamedArgs ) {

				var fixerContext = CreateFixerContext( unnamedArgs );

				ctx.ReportDiagnostic(
					Diagnostic.Create(
						descriptor: Diagnostics.TooManyUnnamedArgs,
						location: ctx.Operation.Syntax.GetLocation(),
						properties: fixerContext,
						messageArgs: TooManyUnnamedArgs
					)
				);

				return;
			}

			// Literal arguments should always be named
			foreach( var arg in unnamedArgs ) {

				// Check if the argument type is literal
				IOperation valueOperation = UnwrapConversionOperation( arg.Operation.Value );
				if( valueOperation is ILiteralOperation ) {

					var fixerContext = CreateFixerContext( ImmutableArray.Create( arg ) );

					ctx.ReportDiagnostic( Diagnostic.Create(
							descriptor: Diagnostics.LiteralArgShouldBeNamed,
							location: arg.Operation.Value.Syntax.GetLocation(),
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

		private static ImmutableDictionary<string, string?> CreateFixerContext( ImmutableArray<ArgParamBinding> unnamedArgs ) {

			// Pass the names and positions for each unnamed arg to the codefix.
			ImmutableDictionary<string, string?> context = unnamedArgs
				.ToImmutableDictionary<ArgParamBinding, string, string?>(
					keySelector: binding => binding.Position.ToString(),
					elementSelector: binding => binding.ParamName
				);

			return context;
		}

		private static bool IsExpressionTree(
			INamedTypeSymbol? expressionType,
			IOperation operation
		) {
			if( expressionType == null || expressionType.Kind == SymbolKind.ErrorType ) {
				return false;
			}

			IOperation? current = operation;
			for( ; ; ) {
				current = current?.Parent;

				if( current is null ) {
					break;
				}

				if( current is not IConversionOperation conversion ) {
					continue;
				}

				if( SymbolEqualityComparer.Default.Equals( expressionType, conversion.Type?.BaseType ) ) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Get the arguments which are unnamed and not "params"
		/// </summary>
		private static IEnumerable<ArgParamBinding> GetUnnamedArgs(
				ImmutableArray<IArgumentOperation> args
			) {

			for( int idx = 0; idx < args.Length; idx++ ) {
				IArgumentOperation arg = args[ idx ];

				if( arg.ArgumentKind != ArgumentKind.Explicit ) {
					continue;
				}

				IParameterSymbol? param = arg.Parameter;

				if( param is null ) {
					continue;
				}

				// IParameterSymbol.Name is documented to be possibly empty in
				// which case it is "unnamed", so ignore it.
				if( param.Name == "" ) {
					continue;
				}

				if( arg.Syntax is not ArgumentSyntax syntax ) {
					continue;
				}

				// Ignore args that already have names
				if( syntax.NameColon != null ) {
					continue;
				}

				string? psuedoName = GetPsuedoName( arg );
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

				// C# allows us to create variables with the same names as reserved keywords,
				// as long as we prefix with @ (e.g. @int is a valid identifier)
				// So any parameters which are reserved must have the @ prefix
				string paramName = param.Name;
				if( SyntaxFacts.GetKeywordKind( paramName ) != SyntaxKind.None
					|| SyntaxFacts.GetContextualKeywordKind( paramName ) != SyntaxKind.None
				) {
					paramName = $"@{paramName}";
				}

				yield return new ArgParamBinding(
					position: idx,
					paramName: paramName, // Use the verbatim parameter name if applicable
					operation: arg
				);
			}
		}

		private static string? GetPsuedoName( IArgumentOperation arg ) {
			IOperation valueOperation = GetNamableValueOperation( arg.Value );
			string? ident = valueOperation switch {
				IFieldReferenceOperation op => op.Field.Name,
				ILocalReferenceOperation op => op.Local.Name,
				IParameterReferenceOperation op => op.Parameter.Name,
				IPropertyReferenceOperation op => op.Property.Name,
				_ => null
			};

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

			static IOperation GetNamableValueOperation( IOperation op ) => op switch {
				IConversionOperation conv => GetNamableValueOperation( conv.Operand ),
				IDeclarationExpressionOperation decl => decl.Expression,
				_ => op,
			};
		}

		private static IOperation UnwrapConversionOperation( IOperation op ) => op switch {
			IConversionOperation conv => UnwrapConversionOperation( conv.Operand ),
			_ => op,
		};

		private static bool HasRequireNamedArgumentsAttribute(
				INamedTypeSymbol requireNamedArgumentsSymbol,
				IMethodSymbol method
			) {

			bool hasAttribute = method
				.GetAttributes()
				.Any( x => SymbolEqualityComparer.Default.Equals( x.AttributeClass, requireNamedArgumentsSymbol ) );

			return hasAttribute;
		}
	}
}
