using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	[TestFixture]
	public sealed class ConcreteTypeRuleTests {
		private readonly ISemanticModel m_model = new Mock<ISemanticModel>( MockBehavior.Strict ).Object;

		[TestCase( TypeKind.Array )]
		[TestCase( TypeKind.Delegate )]
		[TestCase( TypeKind.Dynamic )]
		public void ConcreteTypeForThingsThatDontReduce( TypeKind kind ) {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict );
			type.Setup( t => t.TypeKind ).Returns( kind );

			var goal = new ConcreteTypeGoal( type.Object );

			var subgoals = ConcreteTypeRule.Apply( m_model, goal );

			CollectionAssert.AreEquivalent(
				new[] { goal },
				subgoals
			);
		}

		[TestCase( TypeKind.Enum )]
		[TestCase( TypeKind.Error )]
		public void ConcreteTypeForThingsThatReduceToNothing( TypeKind kind ) {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict );
			type.Setup( t => t.TypeKind ).Returns( kind );

			var goal = new ConcreteTypeGoal( type.Object );

			var subgoals = ConcreteTypeRule.Apply( m_model, goal );

			CollectionAssert.IsEmpty( subgoals );
		}

		[Test]
		public void ConcreteTypeForClass() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict );
			type.Setup( t => t.TypeKind ).Returns( TypeKind.Class );

			var goal = new ConcreteTypeGoal( type.Object );

			var subgoals = ConcreteTypeRule.Apply( m_model, goal );

			CollectionAssert.AreEquivalent(
				new[] { new ClassGoal( type.Object ) },
				subgoals
			);
		}

		[Test]
		public void ConcreteTypeForStruct() {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict );
			type.Setup( t => t.TypeKind ).Returns( TypeKind.Struct );

			var goal = new ConcreteTypeGoal( type.Object );

			var subgoals = ConcreteTypeRule.Apply( m_model, goal );

			CollectionAssert.AreEquivalent(
				new[] { new StructGoal( type.Object ) },
				subgoals
			);
		}

		[Test]
		public void ConcreteTypeForGenericTypeParameter() {
			var type = new Mock<ITypeParameterSymbol>( MockBehavior.Strict );
			type.Setup( t => t.TypeKind ).Returns( TypeKind.TypeParameter );

			var goal = new ConcreteTypeGoal( type.Object as ITypeSymbol );

			var subgoals = ConcreteTypeRule.Apply( m_model, goal );

			CollectionAssert.AreEquivalent(
				new[] { new GenericTypeParameterGoal( type.Object ) },
				subgoals
			);
		}
	}
}
