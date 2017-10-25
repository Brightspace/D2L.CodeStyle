using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	[TestFixture]
	public sealed class ConcreteTypeRuleTests {
		private readonly IAssemblySymbol m_assembly = new Mock<IAssemblySymbol>( MockBehavior.Strict ).Object;
		private readonly ISemanticModel m_model;

		public ConcreteTypeRuleTests() {
			var model = new Mock<ISemanticModel>( MockBehavior.Strict );
			model.Setup( m => m.Assembly() ).Returns( m_assembly );
			m_model = model.Object;
		}

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

		[TestCase( false )]
		[TestCase( true )]
		public void ConcreteTypeForClass( bool withinAssembly ) {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict );
			type.Setup( t => t.TypeKind ).Returns( TypeKind.Class );
			type.Setup( t => t.ContainingAssembly ).Returns(
				withinAssembly ? m_assembly : new Mock<IAssemblySymbol>( MockBehavior.Strict ).Object
			);

			var goal = new ConcreteTypeGoal( type.Object );

			var subgoals = ConcreteTypeRule.Apply( m_model, goal );

			var expectedSubgoal = withinAssembly
				? (Goal)new ClassGoal( type.Object )
				: goal;

			CollectionAssert.AreEquivalent(
				new[] { expectedSubgoal },
				subgoals
			);
		}

		[TestCase( false )]
		[TestCase( true )]
		public void ConcreteTypeForStruct( bool withinAssembly ) {
			var type = new Mock<ITypeSymbol>( MockBehavior.Strict );
			type.Setup( t => t.TypeKind ).Returns( TypeKind.Struct );
			type.Setup( t => t.ContainingAssembly ).Returns(
				withinAssembly ? m_assembly : new Mock<IAssemblySymbol>( MockBehavior.Strict ).Object
			);

			var goal = new ConcreteTypeGoal( type.Object );

			var subgoals = ConcreteTypeRule.Apply( m_model, goal );

			var expectedSubgoal = withinAssembly
				? (Goal)new StructGoal( type.Object )
				: goal;

			CollectionAssert.AreEquivalent(
				new[] { expectedSubgoal },
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
