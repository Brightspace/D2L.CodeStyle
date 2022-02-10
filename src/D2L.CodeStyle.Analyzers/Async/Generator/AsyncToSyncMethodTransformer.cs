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
			.WithExpressionBody( MaybeTransform( decl.ExpressionBody, Transform ) )
			.WithBody( MaybeTransform( decl.Body, Transform ) );

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

	private static SyntaxTokenList RemoveAsyncModifier( SyntaxTokenList modifiers ) =>
		TransformAll(
			modifiers,
			static token => token.IsKind( SyntaxKind.AsyncKeyword ) ? null : token
		);

	private SyntaxToken RemoveAsyncSuffix( SyntaxToken ident ) {
		if( !ident.ValueText.EndsWith( "Async", StringComparison.Ordinal ) || ident.ValueText == "Async" ) {
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

	private ArrowExpressionClauseSyntax Transform( ArrowExpressionClauseSyntax body )
		=> body.WithExpression( Transform( body.Expression ) );

	private BlockSyntax Transform( BlockSyntax block )
		=> block.WithStatements( TransformAll( block.Statements, Transform ) );

	private StatementSyntax Transform( StatementSyntax stmt )
		=> stmt switch {
			BlockSyntax blockStmt => Transform( blockStmt ),

			BreakStatementSyntax => stmt,

			ContinueStatementSyntax => stmt,

			DoStatementSyntax doStmt => doStmt
				.WithStatement( Transform( doStmt.Statement ) )
				.WithCondition( Transform( doStmt.Condition ) ),

			EmptyStatementSyntax => stmt,

			ExpressionStatementSyntax exprStmt => exprStmt
				.WithExpression( Transform( exprStmt.Expression) ),

			GotoStatementSyntax => stmt,

			IfStatementSyntax ifStmt => ifStmt
				.WithCondition( Transform( ifStmt.Condition ) )
				.WithStatement( ifStmt.Statement )
				.WithElse( MaybeTransform( ifStmt.Else, Transform ) ),

			LabeledStatementSyntax labeledStmt => labeledStmt
				.WithStatement( Transform( labeledStmt.Statement ) ),

			ThrowStatementSyntax throwStmt => throwStmt
				.WithExpression( MaybeTransform( throwStmt.Expression, Transform ) ),

			ReturnStatementSyntax returnStmt => returnStmt
				.WithExpression( MaybeTransform( returnStmt.Expression, Transform ) ),

			WhileStatementSyntax whileStmt => whileStmt
				.WithCondition( Transform( whileStmt.Condition ) )
				.WithStatement( Transform( whileStmt.Statement ) ),

			_ => UnhandledSyntax( stmt )
		};

	private ExpressionSyntax Transform( ExpressionSyntax expr )
		=> expr switch {
			AssignmentExpressionSyntax asgnExpr => asgnExpr
				.WithLeft( Transform( asgnExpr.Left ) )
				.WithRight( Transform( asgnExpr.Right ) ),

			BinaryExpressionSyntax binExpr => binExpr
				.WithLeft( Transform( binExpr.Left ) )
				.WithRight( Transform( binExpr.Right ) ),


			ConditionalExpressionSyntax condExpr => condExpr
				.WithCondition( Transform( condExpr.Condition ) )
				.WithWhenTrue( Transform( condExpr.WhenTrue ) )
				.WithWhenFalse( Transform( condExpr.WhenFalse ) ),

			DefaultExpressionSyntax => expr,

			ElementAccessExpressionSyntax eaExpr => eaExpr
				.WithExpression( Transform( eaExpr.Expression ) )
				.WithArgumentList( TransformAll( eaExpr.ArgumentList, Transform ) ),

			LiteralExpressionSyntax => expr,

			ObjectCreationExpressionSyntax newExpr => newExpr
				.WithArgumentList( MaybeTransform( newExpr.ArgumentList, Transform ) )
				.WithInitializer( MaybeTransform( newExpr.Initializer, Transform ) ),

			ParenthesizedExpressionSyntax pExpr => pExpr
				.WithExpression( Transform( pExpr.Expression ) ),

			PostfixUnaryExpressionSyntax postfixExpr => postfixExpr
				.WithOperand( Transform( postfixExpr.Operand ) ),

			PrefixUnaryExpressionSyntax prefixExpr => prefixExpr
				.WithOperand( Transform( prefixExpr.Operand ) ),

			SizeOfExpressionSyntax => expr,

			ThisExpressionSyntax => expr,


			_ => UnhandledSyntax( expr )
		};

	private ElseClauseSyntax Transform( ElseClauseSyntax clause )
		=> clause.WithStatement( clause.Statement );

	private ArgumentListSyntax Transform( ArgumentListSyntax argList )
		=> argList.WithArguments( TransformAll( argList.Arguments, Transform ) );

	private ArgumentSyntax Transform( ArgumentSyntax argument )
		=> argument.WithExpression( Transform( argument.Expression ) );

	private InitializerExpressionSyntax Transform( InitializerExpressionSyntax initializer )
		=> initializer.WithExpressions( TransformAll( initializer.Expressions, Transform ) );

	private bool IsGenerateSyncAttribute( AttributeSyntax attribute ) {
		var attributeConstructorSymbol = m_model.GetSymbolInfo( attribute, m_token ).Symbol as IMethodSymbol;

		if( attributeConstructorSymbol == null ) {
			return false;
		}

		return attributeConstructorSymbol.ContainingType.ToDisplayString()
			== "D2L.CodeStyle.Annotations.GenerateSyncAttribute";
	}

	public T UnhandledSyntax<T>( T node ) where T : SyntaxNode {
		GeneratorError(
			node.GetLocation(),
			$"unhandled syntax kind: {node.Kind()}"
		);

		return node;
	}

}
