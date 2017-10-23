using System;
using System.Collections.Immutable;
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
		public void PropertyNoInit_ReadOnlyAndType() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;
			ExpressionSyntax expr = null;
			var field = CreateField( type, ref expr );

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
		public void PropertyInit_ReadOnlyAndInit() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;
			ExpressionSyntax expr = SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression );

			var field = CreateField( type, ref expr );

			var goal = new FieldGoal( field );

			var subgoals = FieldRule.Apply( goal ).ToImmutableArray();

			CollectionAssert.AreEquivalent(
				subgoals,
				new Goal[] {
					new ReadOnlyGoal( field ),
					new InitializerGoal( expr ),
				}
			);
		}

		private IFieldSymbol CreateField(
			ITypeSymbol type,
			ref ExpressionSyntax initializerExpr
		) {
			var vdecl = SyntaxFactory.VariableDeclarator(
				SyntaxFactory.Identifier( Guid.NewGuid().ToString() )
			);

			if( initializerExpr != null ) {
				vdecl = vdecl.WithInitializer(
					SyntaxFactory.EqualsValueClause(
						initializerExpr
					)
				);
			}

			var reference = new Mock<SyntaxReference>( MockBehavior.Strict );
			reference
				.Setup( r => r.GetSyntax( default( CancellationToken ) ) )
				.Returns( vdecl );

			var fieldSymbol = new Mock<IFieldSymbol>( MockBehavior.Strict );

			fieldSymbol
				.Setup( f => f.DeclaringSyntaxReferences )
				.Returns( ImmutableArray.Create( reference.Object ) );

			fieldSymbol
				.Setup( f => f.Type )
				.Returns( type );

			initializerExpr = vdecl.Initializer?.Value;

			return fieldSymbol.Object;
		}
	}
}
