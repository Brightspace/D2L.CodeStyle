#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.CommonFixes;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal sealed class ImmutableAttributeConsistencyChecker {

		internal delegate void DiagnosticSink( Diagnostic diagnostic );

		private static readonly ImmutableDictionary<string, string> FixArgs = new Dictionary<string, string> {
			{ AddAttributeCodeFixArgs.UsingStatic, "true" },
			{ AddAttributeCodeFixArgs.UsingNamespace, "D2L.CodeStyle.Annotations.Objects" },
			{ AddAttributeCodeFixArgs.AttributeName, "Immutable" }
		}.ToImmutableDictionary();

		private readonly Compilation m_compilation;
		private readonly DiagnosticSink m_diagnosticSink;
		private readonly ImmutabilityContext m_context;
		private readonly AnnotationsContext m_annotationsContext;

		public ImmutableAttributeConsistencyChecker(
			Compilation compilation,
			DiagnosticSink diagnosticSink,
			ImmutabilityContext context,
			AnnotationsContext annotationsContext
		) {
			m_compilation = compilation;
			m_diagnosticSink = diagnosticSink;
			m_context = context;
			m_annotationsContext = annotationsContext;
		}

		public void CheckTypeDeclaration(
			INamedTypeSymbol typeSymbol,
			CancellationToken cancellationToken
		) {
			ImmutableTypeInfo typeInfo = m_context.GetImmutableTypeInfo( typeSymbol );

			if( typeSymbol.BaseType != null ) {
				CompareConsistencyToBaseType( typeInfo, m_context.GetImmutableTypeInfo( typeSymbol.BaseType ), cancellationToken );
			}

			foreach( INamedTypeSymbol interfaceType in typeSymbol.Interfaces ) {
				CompareConsistencyToBaseType( typeInfo, m_context.GetImmutableTypeInfo( interfaceType ), cancellationToken );
			}
		}

		public void CheckMethodDeclaration(
			IMethodSymbol methodSymbol,
			CancellationToken cancellationToken
		) {
			if( methodSymbol.TypeParameters.Length == 0 ) {
				return;
			}

			ImmutableArray<IMethodSymbol> implementedMethods = methodSymbol.GetImplementedMethods();
			foreach( IMethodSymbol implementedMethod in implementedMethods ) {
				for( int i = 0; i < methodSymbol.TypeParameters.Length; ++i ) {
					ITypeParameterSymbol thisParameter = methodSymbol.TypeParameters[ i ];
					ITypeParameterSymbol implementedParameter = implementedMethod.TypeParameters[ i ];

					bool thisIsImmutable = m_annotationsContext.Objects.Immutable.IsDefined( thisParameter );
					bool implementedIsImmutable = m_annotationsContext.Objects.Immutable.IsDefined( implementedParameter );

					if( thisIsImmutable != implementedIsImmutable ) {
						m_diagnosticSink( Diagnostic.Create(
							Diagnostics.InconsistentMethodAttributeApplication,
							GetLocationOfNthTypeParameter( methodSymbol, i, cancellationToken ),
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
			ImmutableTypeInfo baseTypeInfo,
			CancellationToken cancellationToken
		) {
			switch( baseTypeInfo.Kind ) {
				// The base type doesn't require its subtypes to be immutable
				case ImmutableTypeKind.None:
				case ImmutableTypeKind.Instance:
					return;

				case ImmutableTypeKind.Total:
					CompareConsistencyToTotallyImmutableBaseType( typeInfo, baseTypeInfo, cancellationToken );
					break;

				default:
					throw new NotImplementedException();
			}
		}

		private void CompareConsistencyToTotallyImmutableBaseType(
			ImmutableTypeInfo typeInfo,
			ImmutableTypeInfo baseTypeInfo,
			CancellationToken cancellationToken
		) {
			bool missingConditionalParameterUsage = false;
			if( typeInfo.Kind == ImmutableTypeKind.Total ) {
				if( !typeInfo.IsConditional ) {
					return;
				}

				if( baseTypeInfo.IsConditional ) {
					foreach( ITypeParameterSymbol typeParameter in typeInfo.ConditionalTypeParameters ) {
						bool parameterUsed = false;
						for( int i = 0; i < baseTypeInfo.Type.TypeArguments.Length && !parameterUsed; i++ ) {
							ITypeSymbol typeArgument = baseTypeInfo.Type.TypeArguments[ i ];
							if( !SymbolEqualityComparer.Default.Equals( typeParameter, typeArgument ) ) {
								continue;
							}

							ITypeParameterSymbol baseTypeParameter = baseTypeInfo.Type.TypeParameters[ i ];
							if( baseTypeInfo.ConditionalTypeParameters.Contains( baseTypeParameter, SymbolEqualityComparer.Default ) ) {
								parameterUsed = true;
								break;
							}
						}

						if( !parameterUsed ) {
							missingConditionalParameterUsage = true;
							break;
						}
					}

					if( !missingConditionalParameterUsage ) {
						return;
					}
				}
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

			(TypeDeclarationSyntax syntax, _) = typeInfo.Type.ExpensiveGetSyntaxImplementingType(
				baseTypeOrInterface: baseTypeInfo.Type,
				compilation: m_compilation,
				cancellationToken
			);

			if( missingConditionalParameterUsage ) {
				m_diagnosticSink(
					Diagnostic.Create(
						Diagnostics.UnappliedConditionalImmutability,
						syntax.Identifier.GetLocation(),
						typeInfo.Type.GetFullTypeName(),
						isInterface ? "interface" : "base class",
						baseTypeInfo.Type.GetFullTypeName()
					)
				);
			} else {
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
			}

		}

		private static Location GetLocationOfNthTypeParameter(
				IMethodSymbol methodSymbol,
				int N,
				CancellationToken cancellationToken
			) {

			MethodDeclarationSyntax syntax = methodSymbol
				.DeclaringSyntaxReferences[ 0 ]
				.GetSyntax( cancellationToken ) as MethodDeclarationSyntax;

			Location loc = syntax.TypeParameterList.Parameters[ N ].GetLocation();
			return loc;
		}

	}
}
