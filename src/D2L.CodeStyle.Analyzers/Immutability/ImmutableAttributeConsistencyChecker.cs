using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using D2L.CodeStyle.Analyzers.CommonFixes;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal sealed class ImmutableAttributeConsistencyChecker {

		internal delegate void DiagnosticSink( Diagnostic diagnostic );

		private static readonly ImmutableDictionary<string, string> FixArgs = new Dictionary<string, string> {
			{ AddAttributeCodeFix.USING_STATIC_ARG, "true" },
			{ AddAttributeCodeFix.USING_NAMESPACE_ARG, "D2L.CodeStyle.Annotations.Objects" },
			{ AddAttributeCodeFix.ATTRIBUTE_NAME_ARG, "Immutable" }
		}.ToImmutableDictionary();

		private readonly Compilation m_compilation;
		private readonly DiagnosticSink m_diagnosticSink;
		private readonly ImmutabilityContext m_context;

		public ImmutableAttributeConsistencyChecker(
			Compilation compilation,
			DiagnosticSink diagnosticSink,
			ImmutabilityContext context
		) {
			m_compilation = compilation;
			m_diagnosticSink = diagnosticSink;
			m_context = context;
		}

		public void CheckTypeDeclaration(
			INamedTypeSymbol typeSymbol
		) {
			ImmutableTypeInfo typeInfo = m_context.GetImmutableTypeInfo( typeSymbol );

			if( typeSymbol.BaseType != null ) {
				CompareConsistencyToBaseType( typeInfo, m_context.GetImmutableTypeInfo( typeSymbol.BaseType ) );
			}

			foreach( INamedTypeSymbol interfaceType in typeSymbol.Interfaces ) {
				CompareConsistencyToBaseType( typeInfo, m_context.GetImmutableTypeInfo( interfaceType ) );
			}
		}

		public void CheckMethodDeclaration(
			IMethodSymbol methodSymbol
		) {
			if( methodSymbol.TypeParameters.Length == 0 ) {
				return;
			}

			ImmutableArray<IMethodSymbol> implementedMethods = methodSymbol.GetImplementedMethods();
			foreach( IMethodSymbol implementedMethod in implementedMethods ) {
				Debug.Assert( methodSymbol.TypeParameters.Length == implementedMethod.TypeParameters.Length );

				for( int i = 0; i < methodSymbol.TypeParameters.Length; ++i ) {
					ITypeParameterSymbol thisParameter = methodSymbol.TypeParameters[ i ];
					ITypeParameterSymbol implementedParameter = implementedMethod.TypeParameters[ i ];

					bool thisIsImmutable = Attributes.Objects.Immutable.IsDefined( thisParameter );
					bool implementedIsImmutable = Attributes.Objects.Immutable.IsDefined( implementedParameter );

					if( thisIsImmutable != implementedIsImmutable ) {
						m_diagnosticSink( Diagnostic.Create(
							Diagnostics.InconsistentMethodAttributeApplication,
							GetLocationOfNthTypeParameter( methodSymbol, i ),
							"Immutable",
							$"{ methodSymbol.ContainingType.Name }.{ methodSymbol.Name }",
							$"{ implementedMethod.ContainingType.Name }.{ implementedMethod.Name }"
						) );
					}
				}
			}
		}

		private void CompareConsistencyToBaseType(
			ImmutableTypeInfo typeInfo,
			ImmutableTypeInfo baseTypeInfo
		) {
			switch( baseTypeInfo.Kind ) {
				case ImmutableTypeKind.None:
					return;

				case ImmutableTypeKind.Instance:
					return;

				case ImmutableTypeKind.Total:
					if( typeInfo.Kind == ImmutableTypeKind.Total ) {
						break;
					}

					// The docs say the only things that can have BaseType == null
					// are interfaces, System.Object itself (won't come up in our
					// analysis because (1) it !hasTheImmutableAttribute (2) you
					// can't explicitly list it as a base class anyway) and
					// pointer types (the base value type probably also doesn't
					// have it.)
					bool isInterface =
						baseTypeInfo.Type.BaseType == null
						&& baseTypeInfo.Type.SpecialType != SpecialType.System_Object
						&& baseTypeInfo.Type.SpecialType != SpecialType.System_ValueType
						&& baseTypeInfo.Type.Kind != SymbolKind.PointerType;

					TypeDeclarationSyntax syntax = FindDeclarationImplementingType(
						typeSymbol: typeInfo.Type,
						baseTypeSymbol: baseTypeInfo.Type
					);

					m_diagnosticSink(
						Diagnostic.Create(
							Diagnostics.MissingTransitiveImmutableAttribute,
							syntax.Identifier.GetLocation(),
							properties: FixArgs,
							typeInfo.Type.GetFullTypeName(),
							baseTypeInfo.IsConditional ? " (or [ConditionallyImmutable])" : "",
							isInterface ? "interface" : "base class",
							baseTypeInfo.Type.GetFullTypeName()
						)
					);

					break;

				default:
					throw new NotImplementedException();
			}
		}

		private TypeDeclarationSyntax FindDeclarationImplementingType(
			INamedTypeSymbol typeSymbol,
			INamedTypeSymbol baseTypeSymbol
		) {
			TypeDeclarationSyntax anySyntax = null;
			foreach( var reference in typeSymbol.DeclaringSyntaxReferences ) {
				var syntax = reference.GetSyntax() as TypeDeclarationSyntax;
				anySyntax = syntax;

				var baseTypes = syntax.BaseList?.Types;
				if( baseTypes == null ) {
					continue;
				}

				SemanticModel model = m_compilation.GetSemanticModel( syntax.SyntaxTree );
				foreach( var baseTypeSyntax in baseTypes ) {
					TypeSyntax typeSyntax = baseTypeSyntax.Type;

					ITypeSymbol thisTypeSymbol = model.GetTypeInfo( typeSyntax ).Type;

					if( baseTypeSymbol.Equals( thisTypeSymbol, SymbolEqualityComparer.Default ) ) {
						return syntax;
					}
				}
			}

			return anySyntax;
		}

		private static Location GetLocationOfNthTypeParameter( IMethodSymbol methodSymbol, int N ) {
			MethodDeclarationSyntax syntax = methodSymbol
				.DeclaringSyntaxReferences[ 0 ]
				.GetSyntax() as MethodDeclarationSyntax;

			Location loc = syntax.TypeParameterList.Parameters[ N ].GetLocation();
			return loc;
		}

	}
}
