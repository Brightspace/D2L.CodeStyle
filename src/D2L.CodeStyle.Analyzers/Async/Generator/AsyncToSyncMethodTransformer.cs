using D2L.CodeStyle.Analyzers.Extensions;
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

	private SyntaxToken RemoveAsyncSuffix( SyntaxToken ident, bool optional = false ) {
		if( !ident.ValueText.EndsWith( "Async", StringComparison.Ordinal ) || ident.ValueText == "Async" ) {
			if( optional ) {
				return ident;
			}

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
		var returnTypeInfo = Model.GetTypeInfo( returnType, Token );

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

	private TypeSyntax TransformType( TypeSyntax typeSynt ) {
		var returnTypeInfo = Model.GetTypeInfo( typeSynt, Token );

		if( returnTypeInfo.Type == null ) {
			GeneratorError( typeSynt.GetLocation(), "Couldn't resolve type" );
			return typeSynt;
		}

		if( returnTypeInfo.Type.ContainingNamespace.ToString() == "System.Threading.Tasks" ) {
			switch( returnTypeInfo.Type.MetadataName ) {
				case "Task":
					return SyntaxFactory.ParseTypeName( "void" )
						.WithTriviaFrom( typeSynt );
				case "Task`1":
					return ( (GenericNameSyntax)typeSynt )
						.TypeArgumentList.Arguments.First()
						.WithTriviaFrom( typeSynt );

				default:
					GeneratorError(
						typeSynt.GetLocation(),
						$"Unexpected Task type: {returnTypeInfo.Type.MetadataName}"
					);
					return typeSynt;
			}
		}

		return typeSynt;
	}

	private ArrowExpressionClauseSyntax Transform( ArrowExpressionClauseSyntax body )
		=> body.WithExpression( Transform( body.Expression ) );

	private BlockSyntax Transform( BlockSyntax block )
		=> block.WithStatements( TransformAll( block.Statements, Transform ) );

	private SimpleNameSyntax Transform( SimpleNameSyntax simpleExpr )
		=> simpleExpr.WithIdentifier( RemoveAsyncSuffix( simpleExpr.Identifier, optional: true ) );

	private StatementSyntax Transform( StatementSyntax stmt )
		=> stmt switch {
			BlockSyntax blockStmt => Transform( blockStmt ),

			BreakStatementSyntax => stmt,

			ContinueStatementSyntax => stmt,

			DoStatementSyntax doStmt => doStmt
				.WithStatement( Transform( doStmt.Statement ) )
				.WithCondition( Transform( doStmt.Condition ) ),

			EmptyStatementSyntax => stmt,

			ExpressionStatementSyntax exprStmt => Transform( exprStmt ),

			GotoStatementSyntax => stmt,

			IfStatementSyntax ifStmt => ifStmt
				.WithCondition( Transform( ifStmt.Condition ) )
				.WithStatement( Transform( ifStmt.Statement ) )
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

			LocalDeclarationStatementSyntax localDeclStmt => localDeclStmt
				.WithDeclaration( Transform( localDeclStmt.Declaration ) ),

			TryStatementSyntax tryStmt => tryStmt
				.WithBlock( Transform( tryStmt.Block ) )
				.WithCatches( TransformAll( tryStmt.Catches, Transform ) ),

			_ => UnhandledSyntax( stmt )
		};

	private ExpressionSyntax Transform( ExpressionSyntax expr )
		=> expr switch {
			AssignmentExpressionSyntax asgnExpr => asgnExpr
				.WithLeft( Transform( asgnExpr.Left ) )
				.WithRight( Transform( asgnExpr.Right ) ),

			AwaitExpressionSyntax awaitExpr =>
				// Extract the inner expression, removing the "await"
				Transform( awaitExpr.Expression ).ConcatLeadingTriviaFrom( awaitExpr ),

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

			IdentifierNameSyntax identExpr => identExpr
				.WithIdentifier( RemoveAsyncSuffix( identExpr.Identifier, optional: true ) ),

			InvocationExpressionSyntax invocationExpr => invocationExpr
				.WithExpression( Transform( invocationExpr.Expression ) )
				.WithArgumentList( TransformAll( invocationExpr.ArgumentList, Transform ) ),

			CastExpressionSyntax castExpr => castExpr
				.WithType( TransformType( castExpr.Type ) )
				.WithExpression( Transform( castExpr.Expression ) ),

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

			MemberAccessExpressionSyntax memberAccessExpr => Transform( memberAccessExpr ),

			DeclarationExpressionSyntax declExpr => declExpr
				.WithType( TransformType( declExpr.Type ) )
				.WithDesignation( Transform( declExpr.Designation ) ),

			PredefinedTypeSyntax => expr,

			_ => UnhandledSyntax( expr )
		};

	private VariableDesignationSyntax Transform( VariableDesignationSyntax des )
		=> des switch {
			SingleVariableDesignationSyntax varDes => varDes
				.WithIdentifier( RemoveAsyncSuffix( varDes.Identifier, optional: true ) ),

			_ => UnhandledSyntax( des )
		};

	private StatementSyntax Transform( ExpressionStatementSyntax exprStmt ) {
		var result = Transform( exprStmt.Expression );

		// "await foo;" is redundant in sync land, so turn it into an
		// EmptyStatementSyntax (just a bare ";".)
		if( exprStmt.Expression is AwaitExpressionSyntax && result is IdentifierNameSyntax ) {
			return SyntaxFactory.EmptyStatement().WithTriviaFrom( exprStmt );
		}

		return exprStmt.WithExpression( result );
	}

	private MemberAccessExpressionSyntax Transform( MemberAccessExpressionSyntax memberAccessExpr ) {
		if( memberAccessExpr.IsKind( SyntaxKind.SimpleMemberAccessExpression ) ) {
			return memberAccessExpr
				.WithExpression( Transform( memberAccessExpr.Expression ) )
				.WithName( Transform( memberAccessExpr.Name ) );
		}

		return UnhandledSyntax( memberAccessExpr );
	}

	private VariableDeclarationSyntax Transform( VariableDeclarationSyntax varDecl ) {
		return varDecl
			.WithType( TransformType( varDecl.Type ) )
			.WithVariables( TransformVariables( varDecl.Variables ) );
	}

	private SeparatedSyntaxList<VariableDeclaratorSyntax> TransformVariables( SeparatedSyntaxList<VariableDeclaratorSyntax> varDecls ) {
		return SyntaxFactory.SeparatedList(
			varDecls.Select( varDecl =>
				SyntaxFactory.VariableDeclarator(
					RemoveAsyncSuffix( varDecl.Identifier, optional: true ),
					MaybeTransform( varDecl.ArgumentList, Transform ),
					MaybeTransform( varDecl.Initializer, Transform )
				)
			)
		);
	}

	private EqualsValueClauseSyntax Transform( EqualsValueClauseSyntax arg )
		=> arg.WithValue( Transform( arg.Value ) );

	private BracketedArgumentListSyntax Transform( BracketedArgumentListSyntax argList )
		=> TransformAll( argList, Transform );

	private ElseClauseSyntax Transform( ElseClauseSyntax clause )
		=> clause.WithStatement( Transform( clause.Statement ) );

	private ArgumentListSyntax Transform( ArgumentListSyntax argList )
		=> TransformAll( argList, Transform );

	private ArgumentSyntax Transform( ArgumentSyntax argument )
		=> argument.WithExpression( Transform( argument.Expression ) );

	private CatchClauseSyntax Transform( CatchClauseSyntax catchClause ) {
		return catchClause
			.WithDeclaration( catchClause.Declaration != null ? Transform( catchClause.Declaration ) : null )
			.WithBlock( Transform( catchClause.Block ) );
	}

	private CatchDeclarationSyntax Transform( CatchDeclarationSyntax catchDecl ) {
		return catchDecl
			.WithType( TransformType( catchDecl.Type ) )
			.WithIdentifier( RemoveAsyncSuffix( catchDecl.Identifier, optional: true ) );
	}

	private InitializerExpressionSyntax Transform( InitializerExpressionSyntax initializer )
		=> initializer.WithExpressions( TransformAll( initializer.Expressions, Transform ) );

	private bool IsGenerateSyncAttribute( AttributeSyntax attribute ) {
		var attributeConstructorSymbol = Model.GetSymbolInfo( attribute, Token ).Symbol as IMethodSymbol;

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
