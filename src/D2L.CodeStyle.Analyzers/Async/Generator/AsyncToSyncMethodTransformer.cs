using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace D2L.CodeStyle.Analyzers.Async.Generator;

internal sealed class AsyncToSyncMethodTransformer : SyntaxTransformer {
	public AsyncToSyncMethodTransformer(
		SemanticModel model,
		CancellationToken token
	) : base( model, token ) { }

	public TransformResult<MethodDeclarationSyntax> Transform( MethodDeclarationSyntax decl ) {
		// TODO: remove CancellationToken parameters
		decl = decl.WithAttributeLists( ReplaceGenerateSyncAttribute( decl.AttributeLists ) )
			.WithModifiers( RemoveAsyncModifier( decl.Modifiers ) )
			.WithIdentifier( RemoveAsyncSuffix( decl.Identifier ) )
			.WithReturnType( TransformReturnType( decl.ReturnType ) )
			.WithExpressionBody( Transform( decl.ExpressionBody ) )
			.WithBody( Transform( decl.Body ) );

		return GetResult( decl );
	}

	private SyntaxList<AttributeListSyntax> ReplaceGenerateSyncAttribute(
		SyntaxList<AttributeListSyntax> attributeLists
	) => TransformAll( attributeLists, ReplaceGenerateSyncAttribute );

	private AttributeListSyntax? ReplaceGenerateSyncAttribute(
		AttributeListSyntax attributeList
	) => attributeList.WithAttributes(
		TransformAll( attributeList.Attributes, ReplaceGenerateSyncAttribute )
	);

	private AttributeSyntax ReplaceGenerateSyncAttribute(
		AttributeSyntax attribute
	) {
		// Attributes other than [GenerateSync] pass through
		if( !IsGenerateSyncAttribute( attribute ) ) {
			return attribute;
		}

		// Turn [GenerateSync] into [Blocking].
		// This makes some assumptions about the ambient using directives.
		return attribute.WithName(
			SyntaxFactory.ParseName( "Blocking" ).WithTriviaFrom( attribute.Name )
		);
	}

	private SyntaxTokenList RemoveAsyncModifier( SyntaxTokenList modifiers ) =>
		TransformAll(
			modifiers,
			static token => token.IsKind( SyntaxKind.AsyncKeyword ) ? null : token
		);

	private SyntaxToken RemoveAsyncSuffix( SyntaxToken ident ) {
		if( !ident.ValueText.EndsWith( "Async" ) || ident.ValueText == "Async" ) {
			ReportDiagnostic(
				Diagnostics.ExpectedAsyncSuffix,
				ident.GetLocation(),
				ident.ValueText
			);

			return ident;
		}

		const int AsyncSuffixLength = 5;

		return SyntaxFactory.Identifier(
			ident.ValueText.Substring( 0, ident.ValueText.Length - AsyncSuffixLength )
		).WithTriviaFrom( ident );
	}

	private TypeSyntax TransformReturnType( TypeSyntax returnType ) {
		var returnTypeInfo = m_model.GetTypeInfo( returnType, m_token );

		if( returnTypeInfo.Type == null ) {
			GeneratorError( returnType.GetLocation(), "Couldn't resolve type" );
			return returnType;
		}

		if( returnTypeInfo.Type.ContainingNamespace.ToString() == "System.Threading.Tasks" ) {
			switch( returnTypeInfo.Type.MetadataName ) {
				case "Task":
					return SyntaxFactory.ParseTypeName( "void" )
						.WithTriviaFrom( returnType );
				case "Task`1":
					return ( (GenericNameSyntax)returnType )
						.TypeArgumentList.Arguments.First()
						.WithTriviaFrom( returnType );

				default:
					GeneratorError(
						returnType.GetLocation(),
						$"Unexpected Task type: {returnTypeInfo.Type.MetadataName}"
					);
					return returnType;
			}
		}

		ReportDiagnostic( Diagnostics.NonTaskReturnType, returnType.GetLocation() );
		return returnType;
	}

	private ArrowExpressionClauseSyntax? Transform(
		ArrowExpressionClauseSyntax? body
	) {
		if( body == null ) {
			return null;
		}

		GeneratorError(
			body.GetLocation(),
			"ArrowExpressionClauseSyntax is not supported"
		);

		return body;
	}

	private BlockSyntax? Transform( BlockSyntax? block ) {
		if( block == null ) {
			return null;
		}

		// { throw null; }
		return SyntaxFactory.Block(
			SyntaxFactory.ThrowStatement(
				SyntaxFactory.Token( SyntaxKind.ThrowKeyword )
					.WithTrailingTrivia( SyntaxFactory.Space ),
				SyntaxFactory.LiteralExpression(
					SyntaxKind.NullLiteralExpression,
					SyntaxFactory.Token( SyntaxKind.NullKeyword )
				),
				SyntaxFactory.Token( SyntaxKind.SemicolonToken )
			)
		).WithTriviaFrom( block );
	}

	private bool IsGenerateSyncAttribute( AttributeSyntax attribute ) {
		var attributeConstructorSymbol = m_model.GetSymbolInfo( attribute, m_token ).Symbol as IMethodSymbol;

		if( attributeConstructorSymbol == null ) {
			return false;
		}

		return attributeConstructorSymbol.ContainingType.ToDisplayString()
			== "D2L.CodeStyle.Annotations.GenerateSyncAttribute";
	}
}
