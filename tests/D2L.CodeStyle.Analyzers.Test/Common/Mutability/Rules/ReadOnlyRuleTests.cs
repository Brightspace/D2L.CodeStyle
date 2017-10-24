using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	[TestFixture]
	public sealed class ReadOnlyRuleTests {
		private readonly ISemanticModel m_model = new Mock<ISemanticModel>( MockBehavior.Strict ).Object;

		[TestCase( false )] // private T P { get; private set; } --> ReadOnly( P )
		[TestCase( true )] // private T P { get; } --> ()
		public void Properties( bool isReadOnly ) {
			var property = new Mock<IPropertySymbol>( MockBehavior.Strict );
			property.Setup( p => p.IsReadOnly ).Returns( isReadOnly );

			var goal = new ReadOnlyGoal( property.Object );

			var subgoals = ReadOnlyRule.Apply( m_model, goal );

			if( isReadOnly ) {
				CollectionAssert.IsEmpty( subgoals );
			} else {
				CollectionAssert.AreEquivalent( new[] { goal }, subgoals );
			}
		}

		[TestCase( false )] // private T f; --> ReadOnly( f )
		[TestCase( true )] // private readonly T f --> ()
		public void Fields( bool isReadOnly ) {
			var field = new Mock<IFieldSymbol>( MockBehavior.Strict );
			field.Setup( p => p.IsReadOnly ).Returns( isReadOnly );

			var goal = new ReadOnlyGoal( field.Object );

			var subgoals = ReadOnlyRule.Apply( m_model, goal );

			if( isReadOnly ) {
				CollectionAssert.IsEmpty( subgoals );
			} else {
				CollectionAssert.AreEquivalent( new[] { goal }, subgoals );
			}
		}
	}
}
