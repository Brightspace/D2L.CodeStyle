using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using D2L.CodeStyle.Analyzers.Extensions;
using System.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Async {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class BlockingAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
			Diagnostics.AsyncMethodCannotBeBlocking,
			Diagnostics.AsyncMethodCannotCallBlockingMethod,
			Diagnostics.OnlyCallBlockingMethodsFromMethods,
			Diagnostics.BlockingCallersMustBeBlocking,
			Diagnostics.UnnecessaryBlocking,
			Diagnostics.DontIntroduceBlockingInImplementation,
			Diagnostics.NonBlockingImplementationOfBlockingThing
		);

		public override void Initialize( AnalysisContext context ) {
			context.ConfigureGeneratedCodeAnalysis(
				GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics
			);

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( CompilationStart );
		}

		private static void CompilationStart( CompilationStartAnalysisContext ctx ) {
			var attr = ctx.Compilation.GetTypeByMetadataName(
				"D2L.CodeStyle.Annotations.BlockingAttribute"
			);

			if( attr == null ) {
				return;
			}

			var havePossiblyUnusedBlockingAttribute = new ConcurrentDictionary<IMethodSymbol, bool>(
				SymbolEqualityComparer.Default
			);

			ctx.RegisterSymbolAction(
				ctx => InspectMethodDeclaration(
					ctx,
					attr,
					noteMethodWithPossiblyUnusedBlockingAttribute: m
						=> havePossiblyUnusedBlockingAttribute[m] = true
				),
				SymbolKind.Method
			);

			var callsSomethingBlocking = new ConcurrentDictionary<IMethodSymbol, bool>(
				SymbolEqualityComparer.Default
			);

			ctx.RegisterSyntaxNodeAction(
				ctx => InspectMethodCall(
					ctx,
					attr,
					noteMethodHasBlockingCall: m => callsSomethingBlocking[m] = true
				),
				SyntaxKind.InvocationExpression
			);

			// After the rest of analysis report about potentially unnecessary [Blocking]s
			ctx.RegisterCompilationEndAction(
				ctx => WarnForUnusedBlockingAttributes(
					ctx,
					attr,
					havePossiblyUnusedBlockingAttribute: havePossiblyUnusedBlockingAttribute.Keys,
					callsSomethingBlocking: callsSomethingBlocking.Keys
				)
			);
		}

		private static void InspectMethodDeclaration(
			SymbolAnalysisContext ctx,
			INamedTypeSymbol blockingAttr,
			Action<IMethodSymbol> noteMethodWithPossiblyUnusedBlockingAttribute
		) {
			var methodSymbol = (IMethodSymbol)ctx.Symbol;

			// For non-async methods, look at the things we implement and
			// whether they are blocking or not.
			var implementedThings = methodSymbol.GetImplementedMethods();

			var implementedBlockingThings = new List<IMethodSymbol>();
			var implementedNonBlockingThings = new List<IMethodSymbol>();

			foreach( var implementedMethod in implementedThings ) {
				if( HasAttribute( implementedMethod, blockingAttr ) ) {
					implementedBlockingThings.Add( implementedMethod );
				} else {
					implementedNonBlockingThings.Add( implementedMethod );
				}
			}

			var attrData = GetAttribute( methodSymbol, blockingAttr );

			if( attrData == null ) {
				if( implementedBlockingThings.Count == 0 ) {
					// We don't have [Blocking] and none of the things we implement are
					// blocking, so there is nothing to do here.
					return;
				} else {
					// We implement something blocking but don't have [Blocking]. That's
					// fine: direct callers get to know that we're not actually blocking,
					// and indirect callers have to assume the worse.
					// Still output a diagnostic so that we can suggest a code fix in the
					// IDE to add [Blocking].

					// TODO: code fix to add [Blocking]

					var decl = methodSymbol.DeclaringSyntaxReferences.First().GetSyntax() as MethodDeclarationSyntax;

					ctx.ReportDiagnostic(
						Diagnostic.Create(
							Diagnostics.NonBlockingImplementationOfBlockingThing,
							decl.Identifier.GetLocation(),
							$"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}",
							$"{implementedBlockingThings[0].ContainingType.Name}.{implementedBlockingThings[0].Name}"
						)
					);
					return;
				}
			}

			// So we do have a [Blocking] attribute.

			if( implementedBlockingThings.Count == 0 ) {
				// We're not forced to have [Blocking] due to what we implement, so check
				// if we need it at all.
				noteMethodWithPossiblyUnusedBlockingAttribute( methodSymbol );
			}

			if( methodSymbol.IsAsync ) {
				// TODO: code fix to remove [Blocking]

				ctx.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.AsyncMethodCannotBeBlocking,
						attrData.ApplicationSyntaxReference.GetSyntax().GetLocation(),
						methodSymbol.Name
					)
				);
				return;
			}

			// Don't let us use [Blocking] if we're implementing something non-blocking,
			// callers need to know that they may block.
			if( implementedNonBlockingThings.Count > 0 ) {
				// TODO: code fix to remove [Blocking]

				ctx.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.DontIntroduceBlockingInImplementation,
						attrData.ApplicationSyntaxReference.GetSyntax().GetLocation(),
						$"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}",
						// Just use the first one as an example:
						$"{implementedNonBlockingThings[0].ContainingType.Name}.{implementedNonBlockingThings[0].Name}"
					)
				);
				return;
			}

			Debug.Assert(
				// We are [Blocking]
				attrData != null
				// everything we implemement is too
				&& implementedNonBlockingThings.Count == 0
			);
		}

		private static void InspectMethodCall(
			SyntaxNodeAnalysisContext ctx,
			INamedTypeSymbol blockingAttr,
			Action<IMethodSymbol> noteMethodHasBlockingCall
		) {
			// Goal: find calls to blocking methods inside non-blocking methods, or in properties.

			var invocation = (InvocationExpressionSyntax)ctx.Node;

			var invokedSymbolInfo = ctx.SemanticModel.GetSymbolInfo( invocation.Expression );

			// Unfortunately there are ways to use this to dodge analysis, but
			// there is only so much we can do.
			if( invokedSymbolInfo.Symbol?.Kind != SymbolKind.Method ) {
				return;
			}

			var invokedMethodSymbol = (IMethodSymbol)invokedSymbolInfo.Symbol;

			// If the thing we're calling isn't [Blocking] then OK.
			// TODO: support other means of inferring blockingness for 3rd party libraries.
			if( !HasAttribute( invokedMethodSymbol, blockingAttr ) ) {
				return;
			}

			if( !TryGetContainingMethod( ctx, invocation, invokedMethodSymbol, out var myMethodDeclaration ) ) {
				return;
			}

			var myMethodSymbol = (IMethodSymbol)ctx.SemanticModel.GetDeclaredSymbol( myMethodDeclaration );

			if( myMethodSymbol == null ) {
				return;
			}

			if( HasAttribute( myMethodSymbol, blockingAttr ) ) {
				// We have [Blocking] so we're allowed to call other [Blocking] things,
				// but make note that we definitely require blocking.
				noteMethodHasBlockingCall( myMethodSymbol );
				return;
			}

			if( myMethodSymbol.IsAsync ) {
				ctx.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.AsyncMethodCannotCallBlockingMethod,
						invocation.GetLocation(),
						invokedMethodSymbol.Name
					)
				);

				// TODO: code fix that looks for an async equivalent and suggests calling it instead.
				return;
			}

			ctx.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.BlockingCallersMustBeBlocking,
					invocation.GetLocation(),
					invokedMethodSymbol.Name,
					myMethodSymbol.Name
				)
			);

			// TODO: code fix to add blocking
		}

		private static void WarnForUnusedBlockingAttributes(
			CompilationAnalysisContext ctx,
			INamedTypeSymbol blockingAttr,
			ICollection<IMethodSymbol> havePossiblyUnusedBlockingAttribute,
			ICollection<IMethodSymbol> callsSomethingBlocking
		) {
			// Emit a warning when [Blocking] looks unnecessary.

			var unnecessaryThings = havePossiblyUnusedBlockingAttribute.Except( callsSomethingBlocking );

			foreach( var thing in unnecessaryThings ) {
				if( thing.IsAbstract || thing.IsAsync ) {
					continue;
				}

				var attr = GetAttribute( thing, blockingAttr );

				ctx.ReportDiagnostic(
					Diagnostic.Create(
						Diagnostics.UnnecessaryBlocking,
						attr.ApplicationSyntaxReference.GetSyntax().GetLocation(),
						thing.Name
					)
				);

				// TODO: code fix to remove blocking
			}
		}

		private static bool TryGetContainingMethod(
			SyntaxNodeAnalysisContext ctx,
			InvocationExpressionSyntax invocation,
			IMethodSymbol invokedMethodSymbol,
			out SyntaxNode decl
		) {
			// Find the thing that "owns" this invocation... hopefully its a
			// method (maybe a local one) but it constructor or an initializer or...
			// This is a bit shady.
			var earliestAncestor = invocation.FirstAncestorOrSelf<SyntaxNode>(
				d => d.Kind() switch {
					SyntaxKind.LocalFunctionStatement => true,
					SyntaxKind.SimpleLambdaExpression => true,
					SyntaxKind.ParenthesizedLambdaExpression => true,
					SyntaxKind.AnonymousMethodExpression => true,
					_ => d is MemberDeclarationSyntax
				}
			);

			if( earliestAncestor.IsKind( SyntaxKind.MethodDeclaration )
			 || earliestAncestor.IsKind( SyntaxKind.LocalFunctionStatement )
			) {
				decl = earliestAncestor;
				return true;
			}

			string place;

			switch( earliestAncestor.Kind() ) {
				case SyntaxKind.PropertyDeclaration:
					// could be initializers or impls, this should be enough
					// for the user to figure it out.
					place = "properties";
					break;
				case SyntaxKind.FieldDeclaration:
					place = "field initializers";
					break;
				case SyntaxKind.ConstructorDeclaration:
					place = "constructors";
					break;
				case SyntaxKind.ParenthesizedLambdaExpression:
				case SyntaxKind.SimpleLambdaExpression:
					place = "lambdas";
					break;
				case SyntaxKind.AnonymousMethodExpression:
					place = "delegates";
					break;
				default:
					// ugly fallback
					place = earliestAncestor.Kind().ToString();
					break;
			}

			ctx.ReportDiagnostic(
				Diagnostic.Create(
					Diagnostics.OnlyCallBlockingMethodsFromMethods,
					invocation.GetLocation(),
					invokedMethodSymbol.Name,
					place
				)
			);

			decl = null;
			return false;
		}

		private static AttributeData GetAttribute(
			ISymbol symbol,
			INamedTypeSymbol attrSymbol
		) => symbol.GetAttributes().FirstOrDefault(
			ad => SymbolEqualityComparer.Default.Equals( ad.AttributeClass, attrSymbol )
		);

		private static bool HasAttribute(
			ISymbol symbol,
			INamedTypeSymbol attrSymbol
		) => GetAttribute( symbol, attrSymbol ) != default;
	}
}
