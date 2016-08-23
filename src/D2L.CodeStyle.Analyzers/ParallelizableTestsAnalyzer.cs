using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D2L.CodeStyle.Analyzers {
    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public class ParallelizableTestsAnalyzer : DiagnosticAnalyzer {
        public const string DiagnosticId = "D2L0001";
        private const string Category = "Safety";

        private const string Title = "Ensure test is parallelizable.";
        private const string Description = "Classes with side-effects should not be used in non-Isolated tests.";
        internal const string MessageFormat = "'{0}' is not safe for use outside of isolated tests. Consider marking this test as isolated, or use `{1}` instead.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description
        );

        private static readonly ImmutableDictionary<string, string> DefaultOffendingTypeSuggestions = new Dictionary<string, string> {
            {"D2L.LP.TestFramework.Configuration.FeatureToggling.Domain.ITestFeatureToggler", "D2L.LP.Configuration.FeatureToggling.Domain.TestFeatureToggleFactory" },
            {"D2L.LP.TestFramework.Configuration.FeatureToggling.Domain.TestFeatureTogglerExtensions", "D2L.LP.Configuration.FeatureToggling.Domain.TestFeatureToggleFactory" }
        }.ToImmutableDictionary();

        private readonly ImmutableDictionary<string, string> m_offendingTypeSuggestions;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create( Rule );

        /// <summary>
        /// Default constructor
        /// </summary>
        public ParallelizableTestsAnalyzer() : this( DefaultOffendingTypeSuggestions ) { }

        internal ParallelizableTestsAnalyzer( ImmutableDictionary<string, string> ofendingTypeSuggestionsOverride ) {
            m_offendingTypeSuggestions = ofendingTypeSuggestionsOverride;
        }

        public override void Initialize( AnalysisContext context ) {
            context.RegisterCompilationStartAction( RegisterIfTestAssembly );
        }

        private void RegisterIfTestAssembly( CompilationStartAnalysisContext compilation ) {
            var references = compilation.Compilation.ReferencedAssemblyNames;
            if( !references.Any( r => r.Name.ToUpper().Contains( "NUNIT" ) ) ) {
                // Compilation is not a test assembly, skip
                return;
            }

            compilation.RegisterSyntaxNodeAction( 
                AnalyzeSyntaxNode, 
                SyntaxKind.SimpleMemberAccessExpression, 
                SyntaxKind.ObjectCreationExpression 
            );
        }

        private void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext context ) {
            var root = context.Node as ExpressionSyntax;
            if( root == null ) {
                return;
            }

            var invokedMethod = context.SemanticModel.GetSymbolInfo( root ).Symbol;
            if( invokedMethod == null ) {
                return;
            }

            // If not an offending method/member/field, ignore
            var offendingType = invokedMethod.ContainingSymbol.ToString();
            if( !m_offendingTypeSuggestions.ContainsKey( offendingType ) ) {
                return;
            }

            // Check test for Isolated
            var containingMethod = root.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if( containingMethod == null ) {
                return;
            }
            if( IsMarkedIsolated( context.SemanticModel.GetDeclaredSymbol( containingMethod ) ) ) {
                return;
            }

            // Check fixture for Isolated
            var containingClass = root.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if( containingClass == null ) {
                return;
            }
            if( IsMarkedIsolated( context.SemanticModel.GetDeclaredSymbol( containingClass ) ) ) {
                return;
            }

            // Check assembly for Isolated
            var containingAssembly = context.SemanticModel.Compilation.Assembly;
            if( IsMarkedIsolated( containingAssembly ) ) {
                return;
            }

            // Use of offending method in non-Isolated test; let's register a diagnostic
            var diagnostic = Diagnostic.Create( Rule, root.GetLocation(), offendingType, m_offendingTypeSuggestions[offendingType] );
            context.ReportDiagnostic( diagnostic );
        }

        private static bool IsMarkedIsolated( ISymbol symbol ) {
            var categories = GetCategories( symbol );

            if( categories.Contains( "Isolated" ) ) {
                return true;
            }

            return false;
        }

        private static IEnumerable<string> GetCategories( ISymbol symbol ) {
            var categoryAttributes = symbol.GetAttributes()
                .Where( a => a.AttributeClass.MetadataName == "CategoryAttribute" );
            var categories = categoryAttributes
                .SelectMany( a => a.ConstructorArguments )
                .Select( arg => arg.Value as string )
                .Where( c => c != null );

            return categories;
        }
    }
}
