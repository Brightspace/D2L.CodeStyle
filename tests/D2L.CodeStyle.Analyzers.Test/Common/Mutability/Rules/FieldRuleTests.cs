using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	[TestFixture]
	internal sealed class FieldRuleTests {
		// private T x; --> ReadOnly( x ), Type( T )
		[Test]
		public void PropertySingleVarNoInit_ReadOnlyAndType() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;
			var field = CreateField( type, (ExpressionSyntax)null ).Item1;

			var goal = new FieldGoal( field );

			var subgoals = FieldRule.Apply( goal );

			CollectionAssert.AreEquivalent(
				subgoals,
				new Goal[] {
					new ReadOnlyGoal( field ), 
					new TypeGoal( type ), 
				}
			);
		}

		// private T x = Foo(); --> ReadOnly( x ), Initializer( "Foo()" )
		[Test]
		public void PropertySingleVarInit_ReadOnlyAndInit() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;

			var fieldData = CreateField(
				type,
				SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression )
			);

			var field = fieldData.Item1;

			Assert.AreEqual( 1, fieldData.Item2.Length );

			var goal = new FieldGoal( field );

			var subgoals = FieldRule.Apply( goal ).ToImmutableArray();

			CollectionAssert.AreEquivalent(
				subgoals,
				new Goal[] {
					new ReadOnlyGoal( field ),
					new InitializerGoal( fieldData.Item2[0] ),
				}
			);
		}

		// private T x = Foo(), y; --> ReadOnly( x ), Type( T )
		[Test]
		public void PropertyMixedInit_ReadOnlyAndType() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;

			var fieldData = CreateField(
				type,
				SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression ),
				null
			);

			var field = fieldData.Item1;

			var goal = new FieldGoal( field );

			var subgoals = FieldRule.Apply( goal ).ToImmutableArray();

			CollectionAssert.AreEquivalent(
				subgoals,
				new Goal[] {
					new ReadOnlyGoal( field ),
					new TypeGoal( type ),
				}
			);
		}

		// private T x = Foo(), y = Bar(); --> ReadOnly( x ), Initializer( Foo() ), Initializer( Bar() )
		[Test]
		public void PropertyTwoInit_ReadOnlyAndInits() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;

			var fieldData = CreateField(
				type,
				SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression ),
				SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression )
			);

			var field = fieldData.Item1;

			Assert.AreEqual( 2, fieldData.Item2.Length );

			var goal = new FieldGoal( field );

			var subgoals = FieldRule.Apply( goal ).ToImmutableArray();

			CollectionAssert.AreEquivalent(
				subgoals,
				new Goal[] {
					new ReadOnlyGoal( field ),
					new InitializerGoal( fieldData.Item2[0] ), 
					new InitializerGoal( fieldData.Item2[1] ), 
				}
			);
		}

		private Tuple<IFieldSymbol, ImmutableArray<ExpressionSyntax>> CreateField(
			ITypeSymbol type,
			params ExpressionSyntax[] initializerExprs
		) {
			var variables = initializerExprs
				.Select( e => {
					var vdecl = SyntaxFactory.VariableDeclarator(
						SyntaxFactory.Identifier( Guid.NewGuid().ToString() )
					);

					if( e == null ) {
						return vdecl;
					}

					return vdecl.WithInitializer(
						SyntaxFactory.EqualsValueClause(
							e
						)
					);
				} ).ToImmutableArray();

			var fDecl = SyntaxFactory.VariableDeclaration(
				type: SyntaxFactory.ParseTypeName( "asdf" ),
				variables: SyntaxFactory.SeparatedList( variables )
			);

			var field = SyntaxFactory.FieldDeclaration( fDecl );

			var reference = new Mock<SyntaxReference>( MockBehavior.Strict );
			reference
				.Setup( r => r.GetSyntax( default( CancellationToken ) ) )
				.Returns( field );

			var fieldSymbol = new Mock<IFieldSymbol>( MockBehavior.Strict );

			fieldSymbol
				.Setup( f => f.DeclaringSyntaxReferences )
				.Returns( ImmutableArray.Create( reference.Object ) );

			fieldSymbol
				.Setup( f => f.Type )
				.Returns( type );

			return new Tuple<IFieldSymbol, ImmutableArray<ExpressionSyntax>>(
				fieldSymbol.Object,
				field.Declaration.Variables
					.Where( v => v.Initializer != null )
					.Select( v => v.Initializer.Value )
					.ToImmutableArray()
			);
		}
	}
}
