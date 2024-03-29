#nullable disable

using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using D2L.CodeStyle.Analyzers.Extensions;
using System.Diagnostics;
using D2L.CodeStyle.Analyzers.CommonFixes;

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

		private static readonly ImmutableDictionary<string, string> FixArgs = new Dictionary<string, string>() {
			{ AddAttributeCodeFixArgs.UsingNamespace, "D2L.CodeStyle.Annotations" },
			{ AddAttributeCodeFixArgs.AttributeName, "Blocking" }
		}.ToImmutableDictionary();

		public override void Initialize( AnalysisContext context ) {
			context.ConfigureGeneratedCodeAnalysis(
				GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics
			);

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( CompilationStart );
		}

		private static void CompilationStart( CompilationStartAnalysisContext ctx ) {
			var blockingAttr = ctx.Compilation.GetTypeByMetadataName(
				"D2L.CodeStyle.Annotations.BlockingAttribute"
			);

			if( blockingAttr == null ) {
				return;
			}

			var asyncResultType = ctx.Compilation.GetTypeByMetadataName(
				"System.IAsyncResult"
			);

			if( asyncResultType == null ) {
				return;
			}

			var havePossiblyUnusedBlockingAttribute = new ConcurrentDictionary<IMethodSymbol, bool>(
				SymbolEqualityComparer.Default
			);

			ctx.RegisterSymbolAction(
				ctx => InspectMethodDeclaration(
					ctx,
					blockingAttr: blockingAttr,
					asyncResultType: asyncResultType,
					noteMethodWithPossiblyUnusedBlockingAttribute: m
						=> havePossiblyUnusedBlockingAttribute[m] = true
				),
				SymbolKind.Method
			);

			var haveCallsToSomethingBlocking = new ConcurrentDictionary<IMethodSymbol, bool>(
				SymbolEqualityComparer.Default
			);

			ctx.RegisterOperationAction(
				ctx => InspectMethodCall(
					ctx,
					blockingAttr: blockingAttr,
					asyncResultType: asyncResultType,
					noteMethodHasBlockingCall: m => haveCallsToSomethingBlocking[m] = true
				),
				OperationKind.Invocation
			);

			// After the rest of analysis report about potentially unnecessary [Blocking]s
			ctx.RegisterCompilationEndAction(
				ctx => WarnForUnusedBlockingAttributes(
					ctx,
					blockingAttr,
					havePossiblyUnusedBlockingAttribute: havePossiblyUnusedBlockingAttribute.Keys,
					haveCallsToSomethingBlocking: haveCallsToSomethingBlocking.Keys
				)
			);
		}

		private static void InspectMethodDeclaration(
			SymbolAnalysisContext ctx,
			INamedTypeSymbol blockingAttr,
			INamedTypeSymbol asyncResultType,
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

					var decl = methodSymbol.DeclaringSyntaxReferences.First().GetSyntax( ctx.CancellationToken ) as MethodDeclarationSyntax;

					ctx.ReportDiagnostic(
						Diagnostics.NonBlockingImplementationOfBlockingThing,
						decl.Identifier.GetLocation(),
						properties: FixArgs,
						messageArgs: new[] {
							$"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}",
							$"{implementedBlockingThings[ 0 ].ContainingType.Name}.{implementedBlockingThings[ 0 ].Name}"
						}
					);
					return;
				}
			}

			// So we do have a [Blocking] attribute.

			if( ReturnsAwaitableValue( methodSymbol, asyncResultType ) ) {
				// TODO: code fix to remove [Blocking]

				ctx.ReportDiagnostic(
					Diagnostics.AsyncMethodCannotBeBlocking,
					attrData.ApplicationSyntaxReference.GetSyntax( ctx.CancellationToken ).GetLocation(),
					messageArgs: new[] { methodSymbol.Name }
				);
				return;
			}

			if( implementedBlockingThings.Count == 0 ) {
				// We're not forced to have [Blocking] due to what we implement, so check
				// if we need it at all.
				noteMethodWithPossiblyUnusedBlockingAttribute( methodSymbol );
			}

			// Don't let us use [Blocking] if we're implementing something non-blocking,
			// callers need to know that they may block.
			if( implementedNonBlockingThings.Count > 0 ) {
				// TODO: code fix to remove [Blocking]

				ctx.ReportDiagnostic(
					Diagnostics.DontIntroduceBlockingInImplementation,
					attrData.ApplicationSyntaxReference.GetSyntax( ctx.CancellationToken ).GetLocation(),
					messageArgs: new[] {
						$"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}",
						// Just use the first one as an example:
						$"{implementedNonBlockingThings[0].ContainingType.Name}.{implementedNonBlockingThings[0].Name}"
					}
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
			OperationAnalysisContext ctx,
			INamedTypeSymbol blockingAttr,
			INamedTypeSymbol asyncResultType,
			Action<IMethodSymbol> noteMethodHasBlockingCall
		) {
			// Goal: find calls to blocking methods inside non-blocking methods, or in properties.

			var operation = (IInvocationOperation)ctx.Operation;

			var invokedMethodSymbol = operation.TargetMethod;

			// If the thing we're calling isn't [Blocking] then OK.
			// TODO: support other means of inferring blockingness for 3rd party libraries.
			if( !HasAttribute( invokedMethodSymbol, blockingAttr ) ) {
				return;
			}

			var invocation = (InvocationExpressionSyntax)operation.Syntax;

			if( !TryGetContainingMethod( ctx, invocation, invokedMethodSymbol, out var myMethodDeclaration, out var methodNameLocation ) ) {
				return;
			}

			var myMethodSymbol = (IMethodSymbol)operation.SemanticModel.GetDeclaredSymbol( myMethodDeclaration , ctx.CancellationToken );

			if( myMethodSymbol == null ) {
				return;
			}

			if( ReturnsAwaitableValue( myMethodSymbol, asyncResultType ) ) {
				ctx.ReportDiagnostic(
					Diagnostics.AsyncMethodCannotCallBlockingMethod,
					invocation.GetLocation(),
					messageArgs: new[] { invokedMethodSymbol.Name }
				);

				// TODO: code fix that looks for an async equivalent and suggests calling it instead.
				return;
			}

			if( HasAttribute( myMethodSymbol, blockingAttr ) ) {
				// We have [Blocking] so we're allowed to call other [Blocking] things,
				// but make note that we definitely require blocking.
				noteMethodHasBlockingCall( myMethodSymbol );
				return;
			}

			ctx.ReportDiagnostic(
				Diagnostics.BlockingCallersMustBeBlocking,
				location: invocation.GetLocation(),
				additionalLocations: new[] { methodNameLocation },
				properties: FixArgs,
				messageArgs: new[] {
					invokedMethodSymbol.Name,
					myMethodSymbol.Name
				}
			);
		}

		private static bool ReturnsAwaitableValue( IMethodSymbol method, INamedTypeSymbol asyncResultType ) =>
			method.ReturnType.AllInterfaces
				.Contains( asyncResultType, SymbolEqualityComparer.Default );

		private static void WarnForUnusedBlockingAttributes(
			CompilationAnalysisContext ctx,
			INamedTypeSymbol blockingAttr,
			ICollection<IMethodSymbol> havePossiblyUnusedBlockingAttribute,
			ICollection<IMethodSymbol> haveCallsToSomethingBlocking
		) {
			// Emit a warning when [Blocking] looks unnecessary.

			var unnecessaryThings = havePossiblyUnusedBlockingAttribute.Except( haveCallsToSomethingBlocking );

			foreach( var thing in unnecessaryThings ) {
				if( thing.IsAbstract ) {
					continue;
				}

				var attr = GetAttribute( thing, blockingAttr );

				ctx.ReportDiagnostic(
					Diagnostics.UnnecessaryBlocking,
					attr.ApplicationSyntaxReference.GetSyntax( ctx.CancellationToken ).GetLocation(),
					messageArgs: new[] { thing.Name }
				);

				// TODO: code fix to remove blocking
			}
		}

		private static bool TryGetContainingMethod(
			OperationAnalysisContext ctx,
			InvocationExpressionSyntax invocation,
			IMethodSymbol invokedMethodSymbol,
			out SyntaxNode decl,
			out Location identifierLocation
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

			if( earliestAncestor.IsKind( SyntaxKind.MethodDeclaration ) ) {
				decl = earliestAncestor;
				identifierLocation = ( (MethodDeclarationSyntax)decl ).Identifier.GetLocation();
				return true;
			}

			if( earliestAncestor.IsKind( SyntaxKind.LocalFunctionStatement ) ) {
				decl = earliestAncestor;
				identifierLocation = ( (LocalFunctionStatementSyntax)decl ).Identifier.GetLocation();
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
				Diagnostics.OnlyCallBlockingMethodsFromMethods,
				invocation.GetLocation(),
				messageArgs: new[] {
					invokedMethodSymbol.Name,
					place
				}
			);

			identifierLocation = null;
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
