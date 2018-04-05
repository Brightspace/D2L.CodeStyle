using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace D2L.CodeStyle.Analyzers.DependencyScope {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class DependencyScopeAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
			Diagnostics.SingletonDependencyIsWebRequest
		);

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction( RegisterAnalysis );
		}

		private void RegisterAnalysis( CompilationStartAnalysisContext context ) {
			context.RegisterSyntaxNodeAction(
				AnalyzeClass,
				SyntaxKind.ClassDeclaration
			);
		}

		private void AnalyzeClass( SyntaxNodeAnalysisContext context ) {
			var root = context.Node as ClassDeclarationSyntax;
			if( root == null ) {
				return;
			}

			var symbol = context.SemanticModel.GetDeclaredSymbol( root );
			if( symbol == null ) {
				return;
			}

			// skip classes not marked singleton
			if( !symbol.IsTypeMarkedSingleton() ) {
				return;
			}

			AnalyzeType( context, root, symbol );
		}

		private void AnalyzeType( SyntaxNodeAnalysisContext context, ClassDeclarationSyntax root, ITypeSymbol type ) {
			if( type.IsTypeMarkedWebRequest() ) {
				var location = GetLocationOfClassIdentifierAndGenericParameters( root );
				context.ReportDiagnostic( Diagnostic.Create(
					Diagnostics.SingletonDependencyIsWebRequest,
					location
				) );
			}

			foreach( ISymbol member in type.GetExplicitNonStaticMembers() ) {
				if( member is IErrorTypeSymbol ) {
					continue;
				}

				foreach (var memberSymbol in GetTypeSymbols( member ) ) { 
					switch( memberSymbol.TypeKind ) {
						case TypeKind.Class:
						case TypeKind.Struct:
						case TypeKind.Interface:
							AnalyzeType( context, root, memberSymbol );
							break;
					}
				}
			}
		}

		private IEnumerable<ITypeSymbol> GetTypeSymbols(ISymbol symbol) {
			ITypeSymbol result;
			if( symbol is IFieldSymbol f ) {
				result = f.Type;
			} else if (symbol is IPropertySymbol p) {
				result = p.Type;
			} else {
				result = default( ITypeSymbol );
			}

			if (result is INamedTypeSymbol nt) {
				if (nt.IsGenericType) {
					return nt.TypeArguments;
				}
			}

			return new List<ITypeSymbol> { result };
		}

		private Location GetLocationOfClassIdentifierAndGenericParameters(
			TypeDeclarationSyntax decl
		) {
			var location = decl.Identifier.GetLocation();

			if( decl.TypeParameterList != null ) {
				location = Location.Create(
					decl.SyntaxTree,
					TextSpan.FromBounds(
						location.SourceSpan.Start,
						decl.TypeParameterList.GetLocation().SourceSpan.End
					)
				);
			}

			return location;
		}

		private static T GetDeclarationSyntax<T>( ISymbol symbol )
			where T : SyntaxNode {
			var decls = symbol.DeclaringSyntaxReferences;

			if( decls.Length != 1 ) {
				throw new NotImplementedException(
					"Unexepected number of decls: "
					+ decls.Length
				);
			}

			SyntaxNode syntax = decls[0].GetSyntax();

			var decl = syntax as T;
			if( decl == null ) {

				string msg = String.Format(
						"Couldn't cast declaration syntax of type '{0}' as type '{1}': {2}",
						syntax.GetType().FullName,
						typeof( T ).FullName,
						symbol.ToDisplayString()
					);

				throw new InvalidOperationException( msg );
			}

			return decl;
		}
	}
}