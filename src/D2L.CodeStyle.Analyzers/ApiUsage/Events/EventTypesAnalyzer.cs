﻿using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers.ApiUsage.Events {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	internal sealed class EventTypesAnalyzer : DiagnosticAnalyzer {

		private const string EventAttributeFullName = "D2L.LP.Distributed.Events.Domain.EventAttribute";
		private const string ImmutableAttributeFullName = "D2L.CodeStyle.Annotations.Objects+Immutable";

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.EventTypeMissingImmutableAttribute
		);

		public override void Initialize( AnalysisContext context ) {

			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {

			Compilation compilation = context.Compilation;

			INamedTypeSymbol eventAttributeType = compilation.GetTypeByMetadataName( EventAttributeFullName );
			if( eventAttributeType == null ) {
				return;
			}

			INamedTypeSymbol immutableAttributeType = compilation.GetTypeByMetadataName( ImmutableAttributeFullName );

			context.RegisterSyntaxNodeAction(
					ctxt => AnalyzeMethodInvocation(
						ctxt,
						(ClassDeclarationSyntax)ctxt.Node,
						eventAttributeType,
						immutableAttributeType
					),
					SyntaxKind.ClassDeclaration
				);
		}

		private void AnalyzeMethodInvocation(
				SyntaxNodeAnalysisContext context,
				ClassDeclarationSyntax declaration,
				INamedTypeSymbol eventAttributeType,
				INamedTypeSymbol immutableAttributeType
			) {

			INamedTypeSymbol declarationType = context.SemanticModel.GetDeclaredSymbol( declaration );

			bool hasEventAttribute = HasAttribute( declarationType, eventAttributeType );
			if( !hasEventAttribute ) {
				return;
			}

			bool hasImmutableAttirbute = HasAttribute( declarationType, immutableAttributeType );
			if( hasImmutableAttirbute ) {
				return;
			}

			Diagnostic diagnostic = Diagnostic.Create(
					Diagnostics.EventTypeMissingImmutableAttribute,
					declaration.Identifier.GetLocation(),
					declarationType.ToDisplayString()
				);

			context.ReportDiagnostic( diagnostic );
		}

		private static bool HasAttribute(
				INamedTypeSymbol declarationType,
				INamedTypeSymbol attributeType
			) {

			if( attributeType == null ) {
				return false;
			}

			bool hasAttribute = declarationType
				.GetAttributes()
				.Any( attr => attr.AttributeClass.Equals( attributeType ) );

			return hasAttribute;
		}
	}
}
