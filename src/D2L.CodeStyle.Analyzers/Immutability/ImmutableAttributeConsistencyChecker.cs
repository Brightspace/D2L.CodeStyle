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
			switch( ((baseTypeInfo.Kind, baseTypeInfo.IsConditional), (typeInfo.Kind, typeInfo.IsConditional)) ) {
				default:
					throw new NotImplementedException();

				// If the base type doesn't require us to be immutable then there is nothing to check
				case ((ImmutableTypeKind.None, _), _):

				case ((ImmutableTypeKind.Instance, _), _):
					return;

				// When the base type is [Immutable] or [ConditionallyImmutable] we need to be as well
				case ((ImmutableTypeKind.Total, _), (ImmutableTypeKind.None, _)):

				case ((ImmutableTypeKind.Total, _), (ImmutableTypeKind.Instance, _)):
					RaiseMissingAttribute();
					return;

				// If the implementing type is [Immutable] then the conditionality of the
				// base type doesn't matter (for this diagnostic)
				case ((ImmutableTypeKind.Total, _), (ImmutableTypeKind.Total, false)):
					return;

				// If the base type is [Immutable] then the implementing type should be as well
				case ((ImmutableTypeKind.Total, false), (ImmutableTypeKind.Total, true)):
					RaiseMissingAttribute();
					return;

				case ((ImmutableTypeKind.Total, true), (ImmutableTypeKind.Total, true)):
					InspectConditionalParameterApplication();
					return;
			}

			void RaiseMissingAttribute() {
				(TypeDeclarationSyntax syntax, _) = typeInfo.Type.ExpensiveGetSyntaxImplementingType(
					baseTypeOrInterface: baseTypeInfo.Type,
					compilation: m_compilation,
					cancellationToken
				);

				m_diagnosticSink(
					Diagnostic.Create(
						Diagnostics.MissingTransitiveImmutableAttribute,
						syntax.Identifier.GetLocation(),
						properties: FixArgs,
						typeInfo.Type.GetFullTypeName(),
						baseTypeInfo.IsConditional ? " (or [ConditionallyImmutable])" : "",
						baseTypeInfo.Type.TypeKind == TypeKind.Interface ? "interface" : "base class",
						baseTypeInfo.Type.GetFullTypeName()
					)
				);
			}

			void InspectConditionalParameterApplication() {
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
						(TypeDeclarationSyntax syntax, _) = typeInfo.Type.ExpensiveGetSyntaxImplementingType(
							baseTypeOrInterface: baseTypeInfo.Type,
							compilation: m_compilation,
							cancellationToken
						);
						m_diagnosticSink(
							Diagnostic.Create(
								Diagnostics.UnappliedConditionalImmutability,
								syntax.Identifier.GetLocation(),
								typeInfo.Type.GetFullTypeName(),
								baseTypeInfo.Type.TypeKind == TypeKind.Interface ? "interface" : "base class",
								baseTypeInfo.Type.GetFullTypeName()
							)
						);
						return;
					}
				}
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
