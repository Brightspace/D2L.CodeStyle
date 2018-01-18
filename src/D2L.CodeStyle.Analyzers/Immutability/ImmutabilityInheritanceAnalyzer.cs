using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common;
using D2L.CodeStyle.Analyzers.Extensions;
using D2L.CodeStyle.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class ImmutabilityInheritanceAnalyzer : DiagnosticAnalyzer {

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
				Diagnostics.ImmutableMemberIsMorePermissiveThanContainingType,
				Diagnostics.ImmutableTypeIsMorePermissiveThanBaseType
			);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			context.RegisterSyntaxNodeAction(
					AnalyzeTypeMembers,
					SyntaxKind.ClassDeclaration,
					SyntaxKind.StructDeclaration
				);

			context.RegisterSyntaxNodeAction(
					AnalyzeBaseTypes,
					SyntaxKind.ClassDeclaration,
					SyntaxKind.StructDeclaration,
					SyntaxKind.InterfaceDeclaration
				);
		}

		private void AnalyzeTypeMembers( SyntaxNodeAnalysisContext context ) {

			var root = (TypeDeclarationSyntax)context.Node;

			var symbol = context.SemanticModel
				.GetDeclaredSymbol( root );

			// skip types not marked immutable
			if( !symbol.IsTypeMarkedImmutable() ) {
				return;
			}

			foreach( var member in symbol.GetExplicitNonStaticMembers() ) {
				if( member.Kind == SymbolKind.Property ) {
					ImmutabilityInheritanceResult result = InspectType( symbol, ( (IPropertySymbol)member ).Type, member.Locations.First() );
					CheckAndReportResult( context, result, Diagnostics.ImmutableMemberIsMorePermissiveThanContainingType );
				} else if( member.Kind == SymbolKind.Field ) {
					ImmutabilityInheritanceResult result = InspectType( symbol, ( (IFieldSymbol)member ).Type, member.Locations.First() );
					CheckAndReportResult( context, result, Diagnostics.ImmutableMemberIsMorePermissiveThanContainingType );
				}
			}
		}

		private void AnalyzeBaseTypes( SyntaxNodeAnalysisContext context ) {

			var root = (TypeDeclarationSyntax)context.Node;

			var symbol = context.SemanticModel
				.GetDeclaredSymbol( root );

			// skip types not marked immutable
			if( !symbol.IsTypeMarkedImmutable() ) {
				return;
			}

			foreach( INamedTypeSymbol iface in symbol.Interfaces ) {
				ImmutabilityInheritanceResult result = InspectType( iface, symbol, symbol.Locations.First() );
				CheckAndReportResult( context, result, Diagnostics.ImmutableTypeIsMorePermissiveThanBaseType );
			}

			if( symbol.BaseType != null ) {
				ImmutabilityInheritanceResult result = InspectType( symbol.BaseType, symbol, symbol.Locations.First() );
				CheckAndReportResult( context, result, Diagnostics.ImmutableTypeIsMorePermissiveThanBaseType );
			}
		}

		private static void CheckAndReportResult( SyntaxNodeAnalysisContext context, ImmutabilityInheritanceResult result, DiagnosticDescriptor diagnostic ) {
			if( !result.IsOk ) {
				context.ReportDiagnostic(
					Diagnostic.Create(
						diagnostic,
						result.Location,
						AllowedExceptionsSetToString( result.ExpectedExceptions )
					)
				);
			}
		}

		private ImmutabilityInheritanceResult InspectType( ITypeSymbol expectedSupersetType, ITypeSymbol expectedSubsetType, Location location ) {
			
			// If it's not marked immutable, we don't care about it. The actual immutability analyzer can report on issues with that.
			if( !expectedSubsetType.IsTypeMarkedImmutable() || !expectedSupersetType.IsTypeMarkedImmutable() ) {
				return ImmutabilityInheritanceResult.Ok();
			}

			IImmutableSet<Because> expectedSubsetExceptions = BecauseHelpers.GetImmutabilityExceptions( expectedSubsetType );
			IImmutableSet<Because> expectedSupersetExceptions = BecauseHelpers.GetImmutabilityExceptions( expectedSupersetType );

			if( expectedSubsetExceptions.IsSubsetOf( expectedSupersetExceptions ) ) {
				return ImmutabilityInheritanceResult.Ok();
			}

			return ImmutabilityInheritanceResult.TooPermissive( location, expectedSupersetExceptions );
		}

		private static string AllowedExceptionsSetToString( IEnumerable<Because> allowedExceptions ) {
			return string.Join( ", ", allowedExceptions.Select( e => Enum.GetName( typeof( Because ), e ) ) );
		}

		private class ImmutabilityInheritanceResult {

			private static readonly ImmutabilityInheritanceResult OkResult = new ImmutabilityInheritanceResult( true, null, ImmutableHashSet<Because>.Empty );

			public bool IsOk { get; }
			public Location Location { get; }
			public IImmutableSet<Because> ExpectedExceptions { get; }

			private ImmutabilityInheritanceResult( bool isOk, Location location, IImmutableSet<Because> expectedExceptions ) {
				IsOk = isOk;
				Location = location;
				ExpectedExceptions = expectedExceptions;
			}

			public static ImmutabilityInheritanceResult Ok() {
				return OkResult;
			}

			public static ImmutabilityInheritanceResult TooPermissive( Location location, IImmutableSet<Because> expectedExceptions ) {
				return new ImmutabilityInheritanceResult( false, location, expectedExceptions );
			}

		}

	}
}
