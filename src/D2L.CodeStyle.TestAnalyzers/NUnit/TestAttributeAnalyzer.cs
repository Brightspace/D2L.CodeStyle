﻿using D2L.CodeStyle.TestAnalyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace D2L.CodeStyle.TestAnalyzers.NUnit {
    internal sealed class TestAttributeAnalyzer : DiagnosticAnalyzer {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Diagnostics.TestAttributeMissed
        );

        public override void Initialize( AnalysisContext context ) {
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction( OnCompilationStart );
        }

        private static void OnCompilationStart( CompilationStartAnalysisContext context ) {
            if ( !TryLoadNUnitTypes( context.Compilation, out NUnitTypes types ) ) {
                return;
            }

            context.RegisterSyntaxNodeAction(
                ctx => AnalyzeMethod(
                    context: ctx,
                    types: types,
                    syntax: ctx.Node as MethodDeclarationSyntax
                ),
                SyntaxKind.MethodDeclaration
            );
        }

        private static void AnalyzeMethod(
            SyntaxNodeAnalysisContext context,
            NUnitTypes types,
            MethodDeclarationSyntax syntax
        ) {
            SemanticModel model = context.SemanticModel;

            IMethodSymbol method = model.GetDeclaredSymbol( syntax, context.CancellationToken );
            if ( method == null ) {
                return;
            }

            // Any private/helper methods should be private/internal and can be ignored
            if ( method.DeclaredAccessibility != Accessibility.Public ) {
                return;
            }

            // We need the declaring class to be a [TestFixture] to continue
            INamedTypeSymbol declaringClass = method.ContainingType;
            if ( !declaringClass.GetAttributes().Any( attr => attr.AttributeClass == types.TestFixtureAttribute ) ) {
                return;
            }

            bool isTest = IsTestMethod( types, method );
            if ( !isTest ) {
                context.ReportDiagnostic( Diagnostic.Create(
                        Diagnostics.TestAttributeMissed, syntax.Identifier.GetLocation(), method.Name )
                    );
                return;
            }
        }

        private static bool IsTestMethod(
            NUnitTypes types,
            IMethodSymbol method
        ) {
            foreach ( AttributeData attribute in method.GetAttributes() ) {
                INamedTypeSymbol attributeType = attribute.AttributeClass;
                if ( types.TestAttributes.Contains( attributeType ) || types.SetupTeardownAttributes.Contains( attributeType ) ) {
                    return true;
                }
            }

            return false;
        }

        private static bool TryLoadNUnitTypes(
            Compilation compilation,
            out NUnitTypes types
        ) {
            ImmutableHashSet<INamedTypeSymbol> testAttributes = ImmutableHashSet
                .Create(
                    compilation.GetTypeByMetadataName( "NUnit.Framework.TestAttribute" ),
                    compilation.GetTypeByMetadataName( "NUnit.Framework.TestCaseAttribute" ),
                    compilation.GetTypeByMetadataName( "NUnit.Framework.TestCaseSourceAttribute" ),
                    compilation.GetTypeByMetadataName( "NUnit.Framework.TheoryAttribute" )
                );
            ImmutableHashSet<INamedTypeSymbol> setupTeardownAttributes = ImmutableHashSet
                .Create(
                    compilation.GetTypeByMetadataName( "NUnit.Framework.SetUpAttribute" ),
                    compilation.GetTypeByMetadataName( "NUnit.Framework.OneTimeSetUpAttribute" ),
                    compilation.GetTypeByMetadataName( "NUnit.Framework.TearDownAttribute" ),
                    compilation.GetTypeByMetadataName( "NUnit.Framework.OneTimeTearDownAttribute" )
                );

            INamedTypeSymbol testFixtureAttribute = compilation.GetTypeByMetadataName( "NUnit.Framework.TestFixtureAttribute" );

            types = new NUnitTypes( testAttributes, setupTeardownAttributes, testFixtureAttribute );
            return true;
        }

        private sealed class NUnitTypes {

            internal NUnitTypes(
                ImmutableHashSet<INamedTypeSymbol> testAttributes,
                ImmutableHashSet<INamedTypeSymbol> setupTeardownAttributes,
                INamedTypeSymbol testFixtureAttribute
            ) {
                TestAttributes = testAttributes;
                SetupTeardownAttributes = setupTeardownAttributes;
                TestFixtureAttribute = testFixtureAttribute;
            }

            public ImmutableHashSet<INamedTypeSymbol> TestAttributes { get; }
            public INamedTypeSymbol TestFixtureAttribute { get; }
            public ImmutableHashSet<INamedTypeSymbol> SetupTeardownAttributes { get; }
        }
    }
}
