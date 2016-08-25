using System;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analysis;
using D2L.CodeStyle.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers {
    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public sealed class UnsafeStaticsAnalyzer : DiagnosticAnalyzer {

        public const string DiagnosticId = "D2L0002";
        private const string Category = "Safety";

        private const string Title = "Ensure that static field is safe in undifferentiated servers.";
        private const string Description = "Static fields should not have client-specific or mutable data, otherwise they will not be safe in undifferentiated servers.";
        internal const string MessageFormat = "This static is unsafe because: '{0}'.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, 
            Title, 
            MessageFormat, 
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Rule );

        private readonly MutabilityInspector m_immutabilityInspector = new MutabilityInspector();

        public override void Initialize( AnalysisContext context ) {
            context.RegisterCompilationStartAction( RegisterIfNotTestAssembly );
        }

        private void RegisterIfNotTestAssembly( CompilationStartAnalysisContext compilation ) {
            var references = compilation.Compilation.ReferencedAssemblyNames;
            if( references.Any( r => r.Name.ToUpper().Contains( "NUNIT" ) ) ) {
                // Compilation is a test assembly, skip
                return;
            }

            compilation.RegisterSyntaxNodeAction( AnalyzeField, SyntaxKind.FieldDeclaration );
            compilation.RegisterSyntaxNodeAction( AnalyzeProperty, SyntaxKind.PropertyDeclaration );
        }

        private void AnalyzeField( SyntaxNodeAnalysisContext context ) {
            var root = context.Node as FieldDeclarationSyntax;
            if( root == null ) {
                return;
            }

            if( !root.Modifiers.Any( SyntaxKind.StaticKeyword ) ) {
                // ignore non-static
                return;
            }

            foreach( var variable in root.Declaration.Variables ) {
                var symbol = context.SemanticModel.GetDeclaredSymbol( variable ) as IFieldSymbol;

                if( symbol == null ) {
                    continue;
                }

#pragma warning disable CS0618 // Type or member is obsolete
                if( symbol.GetAttributes().Any( a => a.AttributeClass.MetadataName == nameof( Statics.Unaudited ) ) ) {
                    // anyhing marked unaudited should not break the build, it's temporary
                    return;
                }
#pragma warning restore CS0618 // Type or member is obsolete

                if( symbol.GetAttributes().Any( a => a.AttributeClass.MetadataName == nameof( Statics.Audited ) ) ) {
                    // anything marked audited has been explicitly marked as a safe static
                    return;
                }

                if( m_immutabilityInspector.IsFieldMutable( symbol ) ) {
                    var diagnostic = Diagnostic.Create( Rule, variable.GetLocation(), BadStaticReason.StaticIsMutable );
                    context.ReportDiagnostic( diagnostic );
                }

                if( m_immutabilityInspector.IsTypeMarkedImmutable( symbol.Type ) ) {
                    // if the type is marked immutable, skip checking it, to avoid reporting a diagnostic for each usage of non-immutable types that are marked immutable (another analyzer catches this already)
                    continue;
                }

                if( m_immutabilityInspector.IsTypeMutable( symbol.Type ) ) {
                    var diagnostic = Diagnostic.Create( Rule, variable.GetLocation(), BadStaticReason.TypeOfStaticIsMutable );
                    context.ReportDiagnostic( diagnostic );
                }
            }
        }

        private void AnalyzeProperty( SyntaxNodeAnalysisContext context ) {
            var root = context.Node as PropertyDeclarationSyntax;
            if( root == null ) {
                return;
            }

            if( !root.Modifiers.Any( SyntaxKind.StaticKeyword ) ) {
                // ignore non-static
                return;
            }

            var prop = context.SemanticModel.GetDeclaredSymbol( root );
            if( prop == null ) {
                return;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if( prop.GetAttributes().Any( a => a.AttributeClass.MetadataName == nameof( Statics.Unaudited ) ) ) {
                              // anyhing marked unaudited should not break the build, it's temporary
                return;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            if( prop.GetAttributes().Any( a => a.AttributeClass.MetadataName == nameof( Statics.Audited ) ) ) {
                // anything marked audited has been explicitly marked as a safe static
                return;
            }

            if( m_immutabilityInspector.IsPropertyMutable( prop ) ) {
                var diagnostic = Diagnostic.Create( Rule, root.GetLocation(), BadStaticReason.StaticIsMutable );
                context.ReportDiagnostic( diagnostic );
            }

            if( m_immutabilityInspector.IsTypeMarkedImmutable( prop.Type ) ) {
                // if the type is marked immutable, skip checking it, to avoid reporting a diagnostic for each usage of non-immutable types that are marked immutable (another analyzer catches this already)
                return;
            }

            if( m_immutabilityInspector.IsTypeMutable( prop.Type ) ) {
                var diagnostic = Diagnostic.Create( Rule, root.GetLocation(), BadStaticReason.TypeOfStaticIsMutable );
                context.ReportDiagnostic( diagnostic );
            }
        }

    }
}
