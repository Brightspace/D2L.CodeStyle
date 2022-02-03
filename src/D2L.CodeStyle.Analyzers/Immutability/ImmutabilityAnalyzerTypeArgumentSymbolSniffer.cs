using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static D2L.CodeStyle.Analyzers.Immutability.ImmutabilityAnalyzerTypeArgumentReport;

namespace D2L.CodeStyle.Analyzers.Immutability {

	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public sealed class ImmutabilityAnalyzerTypeArgumentSymbolSniffer : DiagnosticAnalyzer {

		private static readonly string ReportName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create(
				Diagnostics.TypeArgumentLengthMismatch
			);

		public override void Initialize( AnalysisContext context ) {
			//Debugger.Launch();
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics );
			context.RegisterCompilationStartAction( CompilationStart );
		}

		public void CompilationStart( CompilationStartAnalysisContext context ) {

			ConcurrentBag<SimpleNameTuple> simpleNames = new();

			context.RegisterSymbolAction(
				ctx => {
					INamedTypeSymbol namedType = (INamedTypeSymbol)ctx.Symbol;
					AnalyzeNamedType( ctx, namedType, simpleNames );
				},
				SymbolKind.NamedType
			);

			context.RegisterSymbolAction(
				ctx => {
					IFieldSymbol field = (IFieldSymbol)ctx.Symbol;
					AnalyzeField( ctx, field, simpleNames );
				},
				SymbolKind.Field
			);

			context.RegisterSymbolAction(
				ctx => {
					IMethodSymbol method = (IMethodSymbol)ctx.Symbol;
					AnalyzeMethod( ctx, method, simpleNames );
				},
				SymbolKind.Method
			);

			context.RegisterCompilationEndAction(
				ctx => {
					WriteReports( ReportName, simpleNames );
				}
			);
		}

		private void AnalyzeField(
				SymbolAnalysisContext ctx,
				IFieldSymbol field,
				ConcurrentBag<SimpleNameTuple> simpleNames
			) {

			ImmutableHashSet<INamedTypeSymbol> namedFieldTypes = ExpandNamedTypes( field.Type )
				.Where( HasTypeAguments )
				.ToImmutableHashSet<INamedTypeSymbol>( SymbolEqualityComparer.Default );

			if( namedFieldTypes.IsEmpty ) {
				return;
			}

			foreach( SyntaxReference syntaxReference in field.DeclaringSyntaxReferences ) {

				SyntaxNode syntax = syntaxReference.GetSyntax( ctx.CancellationToken );
				if( syntax is not FieldDeclarationSyntax fieldDeclaration ) {
					throw new InvalidOperationException( "IFieldSymbol should have been declared by FieldDeclarationSyntax" );
				}
			}
		}

		private static void AnalyzeNamedType(
				SymbolAnalysisContext ctx,
				INamedTypeSymbol namedType,
				ConcurrentBag<SimpleNameTuple> simpleNames
			) {

			ImmutableHashSet<ITypeSymbol> baseTypeSymbols = GetBaseAndInterfaceTypes( namedType )
				.SelectMany( ExpandNamedTypes )
				.Where( HasTypeAguments )
				.ToImmutableHashSet<ITypeSymbol>( SymbolEqualityComparer.Default );

			if( baseTypeSymbols.Count == 0 ) {
				return;
			}

			foreach( SyntaxReference syntaxReference in namedType.DeclaringSyntaxReferences ) {

				SyntaxNode syntax = syntaxReference.GetSyntax( ctx.CancellationToken );
				if( syntax is not TypeDeclarationSyntax typeDeclaration ) {
					throw new InvalidOperationException( "INamedTypeSymbol should have referenced TypeDeclarationSyntax" );
				}

				BaseListSyntax? baseList = typeDeclaration.BaseList;
				if( baseList == null ) {
					continue;
				}

				SemanticModel model = ctx.Compilation.GetSemanticModel( typeDeclaration.SyntaxTree );

				foreach( BaseTypeSyntax baseType in baseList.Types ) {

					if( baseType.Type.IsKind( SyntaxKind.ErrorKeyword ) ) {
						continue;
					}

					if( baseType.Type is not NameSyntax baseTypeNameSyntax ) {
						throw new InvalidOperationException( "The BaseTypeSyntax's Type should be a NameSyntax" );
					}

					ITypeSymbol? baseTypeSymbol = model
						.GetTypeInfo( baseType.Type, ctx.CancellationToken )
						.Type;

					if( baseTypeSymbol == null ) {
						continue;
					}

					if( !baseTypeSymbols.Contains( baseTypeSymbol ) ) {
						continue;
					}

					SimpleNameSyntax baseTypeUnqualifiedName = baseTypeNameSyntax.GetUnqualifiedName();
					simpleNames.Add( new( baseTypeUnqualifiedName, baseTypeSymbol.Kind ) );
				}
			}
		}

		private void AnalyzeMethod(
				SymbolAnalysisContext ctx,
				IMethodSymbol method,
				ConcurrentBag<SimpleNameTuple> simpleNames
			) {

			Lazy<ImmutableArray<MethodDeclarationSyntax>> methodDeclarations = new(
				() => GetDeclaringMethodSyntax( method, ctx.CancellationToken )
			);

			ImmutableHashSet<INamedTypeSymbol> namedReturnTypes = ExpandNamedTypes( method.ReturnType )
				.Where( HasTypeAguments )
				.ToImmutableHashSet<INamedTypeSymbol>( SymbolEqualityComparer.Default );

			if( !namedReturnTypes.IsEmpty ) {

				foreach( MethodDeclarationSyntax methodDeclaration in methodDeclarations.Value ) {

					TypeSyntax returnType = methodDeclaration.ReturnType;

					if( returnType.IsKind( SyntaxKind.ErrorKeyword ) ) {
						continue;
					}

					switch( returnType ) {

						case NameSyntax returnTypeName:
							SimpleNameSyntax returnTypeUnqualifiedName = returnTypeName.GetUnqualifiedName();
							simpleNames.Add( new( returnTypeUnqualifiedName, method.ReturnType.Kind ) );
							break;

						default:
							// TODO: Investigate
							break;
					}
				}
			}
		}

		private static ImmutableArray<MethodDeclarationSyntax> GetDeclaringMethodSyntax(
				IMethodSymbol method,
				CancellationToken cancellationToken
			) {

			ImmutableArray<SyntaxReference> syntaxReferences = method.DeclaringSyntaxReferences;
			if( syntaxReferences.IsEmpty ) {
				return ImmutableArray<MethodDeclarationSyntax>.Empty;
			}

			var builder = ImmutableArray.CreateBuilder<MethodDeclarationSyntax>( syntaxReferences.Length );

			foreach( SyntaxReference syntaxReference in syntaxReferences ) {

				SyntaxNode syntax = syntaxReference.GetSyntax( cancellationToken );
				switch( syntax ) {

					case MethodDeclarationSyntax methodDeclaration:
						builder.Add( methodDeclaration );
						break;

					default:
						// TODO: Investigate
						break;
				}
			}

			return builder.ToImmutableArray();
		}

		private static IEnumerable<INamedTypeSymbol> GetBaseAndInterfaceTypes( ITypeSymbol namedType ) {

			INamedTypeSymbol? baseType = namedType.BaseType;
			if( baseType != null ) {
				yield return baseType;
			}

			foreach( INamedTypeSymbol interfaceType in namedType.Interfaces ) {
				yield return interfaceType;
			}
		}

		private static IEnumerable<INamedTypeSymbol> ExpandNamedTypes( ITypeSymbol type ) {

			Stack<ITypeSymbol> stack = new Stack<ITypeSymbol>();
			stack.Push( type );

			do {
				ITypeSymbol current = stack.Pop();
				switch( current ) {

					case IArrayTypeSymbol arrayType:
						stack.Push( arrayType.ElementType );
						break;

					case INamedTypeSymbol namedType:
						yield return namedType;

						foreach( ITypeSymbol typeArgument in namedType.TypeArguments ) {
							stack.Push( typeArgument );
						}

						break;

					case IPointerTypeSymbol pointerType:
						stack.Push( pointerType.PointedAtType );
						break;

					case IDynamicTypeSymbol _:
					case IFunctionPointerTypeSymbol _:
						break;

					default:
						// TODO
						break;
				}

			} while( stack.Count > 0 );
		}

		private static bool HasTypeAguments( INamedTypeSymbol namedType ) {
			return !namedType.TypeArguments.IsEmpty;
		}

	}
}
