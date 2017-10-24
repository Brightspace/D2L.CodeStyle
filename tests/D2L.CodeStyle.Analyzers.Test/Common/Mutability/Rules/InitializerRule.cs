using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	[TestFixture]
	public sealed class InitializerRuleTests {
		// private T x = new U( 1, false ); // --> ConcreteType( U )
		[Test]
		public void InitializerObjectCreateionSyntax_ConcreteTypeSubgoal() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;
			var typeOfExpr = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;
			var expr = SyntaxFactory.ObjectCreationExpression( SyntaxFactory.ParseTypeName( "SomeType" ) );

			var model = new Mock<ISemanticModel>( MockBehavior.Strict );
			model.Setup( m => m.GetTypeForSyntax( expr ) ).Returns( typeOfExpr );

			var goal = new InitializerGoal( type, expr );
			var subgoals = InitializerRule.Apply( model.Object, goal );

			CollectionAssert.AreEquivalent(
				new Goal[] { new ConcreteTypeGoal( typeOfExpr ) },
				subgoals
			);
		}

		// private T x = SomeMethodReturningU(); // --> Type( U )
		[Test]
		public void InitializerArbitraryExpression_ConcreteTypeSubgoal() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;
			var typeOfExpr = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;
			var expr = SyntaxFactory.InvocationExpression( SyntaxFactory.IdentifierName( "SomeMethod" ) );

			var model = new Mock<ISemanticModel>( MockBehavior.Strict );
			model.Setup( m => m.GetTypeForSyntax( expr ) ).Returns( typeOfExpr );

			var goal = new InitializerGoal( type, expr );
			var subgoals = InitializerRule.Apply( model.Object, goal );

			CollectionAssert.AreEquivalent(
				new Goal[] { new TypeGoal( typeOfExpr ) },
				subgoals
			);
		}
	}
}
