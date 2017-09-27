﻿using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Contract {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class NotNullAnalyzer : DiagnosticAnalyzer {

		private const string Namespace = "D2L.CodeStyle.Annotations.Contract.";
		private const string NotNullAttribute = Namespace + "NotNullAttribute";
		private const string NotNullTypeAttribute = Namespace + "NotNullWhenParameterAttribute";
		private const string AllowNullAttribute = Namespace + "AllowNullAttribute";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create( Diagnostics.NullPassedToNotNullParameter );

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterNotNullAnalyzer );
		}

		public static void RegisterNotNullAnalyzer(
			CompilationStartAnalysisContext context
		) {
			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeInvocation(
					ctx,
					(InvocationExpressionSyntax)ctx.Node
				),
				SyntaxKind.InvocationExpression
			);

			context.RegisterSyntaxNodeAction(
				ctx => AnalyzeObjectCreation(
					ctx,
					(ObjectCreationExpressionSyntax)ctx.Node
				),
				SyntaxKind.ObjectCreationExpression
			);
		}

		private static void AnalyzeInvocation(
			SyntaxNodeAnalysisContext context,
			InvocationExpressionSyntax invocation
		) {
			AnalyzeInvocationLikeThing(
				context,
				invocation.ArgumentList
			);
		}

		private static void AnalyzeObjectCreation(
			SyntaxNodeAnalysisContext context,
			ObjectCreationExpressionSyntax construction
		) {
			AnalyzeInvocationLikeThing(
				context,
				construction.ArgumentList
			);
		}

		/// <summary>
		/// Checks that some sort of method call respects [NotNull] annotations
		/// from the methods declaration.
		/// </summary>
		private static void AnalyzeInvocationLikeThing(
			SyntaxNodeAnalysisContext context,
			ArgumentListSyntax argumentList
		) {
			// If a constructor takes no arguments, and is populated using
			// object initializer syntax, then the argument list will be null.
			// There's nothing for us to analyze.
			if( argumentList == null ) {
				return;
			}

			// Validate that all arguments that are provided are either not
			// null or associated with a parameter that does not have [NotNull]
			// attribute. This would miss a null being passed to a params 
			// parameter which may be reasonable to always complain about
			// because it's a weird and ugly edge-case.
			SeparatedSyntaxList<ArgumentSyntax> arguments = argumentList.Arguments;
			foreach( var argument in arguments ) {
				// If the argument expression looks safe we don't need to
				// inspect the parameter.
				if ( !ThereIsSufficientConcernThatThisExpressionIsNull( argument.Expression ) ) {
					continue;
				}

				IParameterSymbol parameter = argument.DetermineParameter(
					context.SemanticModel,
					allowParams: true
				);

				// This corresponds to some manner of broken code. The
				// analyzer only needs to emit diagnostics (at worst) for
				// otherwise correct code.
				if ( parameter == null ) {
					continue;
				}

				// If the parameter and its type aren't marked as [NotNull]
				// we can skip this argument.
				if( !ParameterMustNotBeNull( parameter ) ) {
					continue;
				}

				// We know that the argument looks null enough and the
				// parameter must not be passed null. Emit a diagnostic.
				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.NullPassedToNotNullParameter,
					argument.GetLocation(),
					parameter.Name
				) );
			}
		}

		private static bool ThereIsSufficientConcernThatThisExpressionIsNull(
			ExpressionSyntax expr
		) {
			var litExpr = expr as LiteralExpressionSyntax;

			// We aren't handling anything this fancy at this point in time
			if ( litExpr == null ) {
				return false;
			}

			return litExpr.Token.Kind() == SyntaxKind.NullKeyword;
		}

		private static bool ParameterMustNotBeNull(
			IParameterSymbol parameter
		) {
			// If the parameter is marked [NotNull] we can skip checking for other attributes.
			if( SymbolHasAttribute( parameter, NotNullAttribute ) ) {
				return true;
			}

			// If the parameter's type is marked [NotNullWhenParameter], and
			// the parameter itself is not marked with [AllowNull], then the
			// parameter is considered equivalent to having [NotNull] applied.
			ITypeSymbol paramType = parameter.Type;
			if( SymbolHasAttribute( paramType, NotNullTypeAttribute )
				&& !SymbolHasAttribute( parameter, AllowNullAttribute )
			) {
				return true;
			}

			return false;
		}

		private static bool SymbolHasAttribute(
			ISymbol symbol,
			string attributeClassName
		) {
			// TODO: don't compare type names as strings
			bool hasExpectedAttribute = symbol.GetAttributes().Any(
				x => x.AttributeClass.GetFullTypeName() == attributeClassName
			);

			return hasExpectedAttribute;
		}
	}
}
 