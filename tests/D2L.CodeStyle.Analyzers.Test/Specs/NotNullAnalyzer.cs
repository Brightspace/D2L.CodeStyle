// analyzer: D2L.CodeStyle.Analyzers.Contract.NotNullAnalyzer

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D2L.CodeStyle.Annotations.Contract;

namespace D2L.CodeStyle.Annotations.Contract {

	/// <summary>
	/// Indicates that a paramater may not be called with `null`
	/// </summary>
	public sealed class NotNullAttribute : Attribute {
	}
}

namespace D2L.CodeStyle.Analyzers.NotNull.Examples {

	internal sealed class TestProvider {
		public void TestMethod(
			[NotNull] string testName
		) { }

		public void TestMethod(
			object allowedToBeNull,
			[NotNull] string testName
		) { }

		public void MultiNotNull(
			[NotNull] string testName,
			[NotNull] string anotherName
		) { }

		public void TestMethodCanTakeNull( string testName ) { }

		public bool ShouldDoStuff => false;
	}

	internal sealed class ConsumerWithIssues {

		public void NullIsPassed() {
			var provider = new TestProvider();
			provider.TestMethod( /* NullPassedToNotNullParameter */ null /**/ );
		}

		public void TestMethod() {
			DoStuff( /* NullPassedToNotNullParameter */ null /**/ );
		}

		public void DoStuff(
			[NotNull] string stuff
		) { }

		public void NullVariableIsPassed() {
			var provider = new TestProvider();
			string name = null;
			provider.TestMethod( /* NullPassedToNotNullParameter */ name /**/ );
		}

		public void NullVariableIsPassedInConstructor() {
			var provider = new TestProvider();
			string name = null;
			provider.TestMethod( /* NullPassedToNotNullParameter */ name /**/ );
		}

		public void _RealValueAssignedAfterPassing() {
			var provider = new TestProvider();
			string name;
			provider.TestMethod( /* NullPassedToNotNullParameter */ name /**/ );
			name = "Antidisestablishmentarianism";
		}

		public void VariableNotAlwaysAssignedValue() {
			var provider = new TestProvider();
			string name;
			if( provider.ShouldDoStuff ) {
				name = "Antidisestablishmentarianism";
			}
			provider.TestMethod( /* NullPassedToNotNullParameter */ name /**/ );
		}

		public void VariableNotAlwaysAssignedNonNullValue() {
			var provider = new TestProvider();
			string name = null;
			if( provider.ShouldDoStuff ) {
				name = "Antidisestablishmentarianism";
			}
			provider.TestMethod( /* NullPassedToNotNullParameter */ name /**/ );
		}
		public void NullVariableIsInClosureContext() {
			var provider = new TestProvider();
			string name = null;
			var action = () => provider.TestMethod( /* NullPassedToNotNullParameter */ name /**/ );
		}

		public void MultipleParamtersWithIssue() {
			var provider = new TestProvider();
			string name = null;
			provider.TestMethod( "Hello!" );
			provider.MultiNotNull(
					/* NullPassedToNotNullParameter */ name /**/,
					/* NullPassedToNotNullParameter */ null /**/
				);
		}

		public void NamedArguments_OneIsNull() {
			var provider = new TestProvider();
			provider.TestMethod(
				/* NullPassedToNotNullParameter */ testName: null /**/,
				allowedToBeNull: "This is an object"
			);
		}
	}

	internal sealed class ConsumerWithoutIssues {

		public void ValueIsPassed() {
			var provider = new TestProvider();
			provider.TestMethod( "My Name" );
		}

		public void VariableWithValueAssigned_AtDeclaration() {
			var provider = new TestProvider();
			string name = "Hello!";
			provider.TestMethod( name );
		}
		public void VariableWithValueAssigned_AfterDeclaration() {
			var provider = new TestProvider();
			string name;
			name = "Supercalifragilisticexpialidocious"
			provider.TestMethod( name );
		}
		public void VariablAssignedOtherVariable_AtDeclaration( string intakeName ) {
			var provider = new TestProvider();
			string name = intakeName;
			provider.TestMethod( name );
		}

		public void VariablAssignedOtherVariable_AfterDeclaration( string intakeName ) {
			var provider = new TestProvider();
			string name;
			name = intakeName;
			provider.TestMethod( name );
		}

		public void MethodParameterPassed( string intakeName ) {
			var provider = new TestProvider();
			provider.TestMethod( intakeName );
		}

		public void VariableAlwaysAssignedValue() {
			var provider = new TestProvider();
			string name;
			if( provider.ShouldDoStuff ) {
				name = "Do some stuff";
			} else {
				name = "Do some other stuff";
			}
			provider.TestMethod( name );

			string otherName = provider.ShouldDoStuff ? "Do ?" : "Or do not ?";

			provider.TestMethod( otherName );
		}

		public void NullVariableAtDeclaration_AlwaysAssignedValueAfterDeclaration() {
			var provider = new TestProvider();
			string name = null;
			if( provider.ShouldDoStuff ) {
				name = "Do some stuff";
			} else {
				name = "Do some other stuff";
			}
			provider.TestMethod( name );

			string otherName = null;
			otherName = provider.ShouldDoStuff ? "Do ?" : "Or do not ?";

			provider.TestMethod( otherName );
		}

		public void NoAttribute() {
			var provider = new TestProvider();
			provider.TestMethodCanTakeNull( null );
		}

		public void OneParamNotNull_NullPassedToNullable() {
			var provider = new TestProvider();
			provider.TestMethod( null, ""This is the not - nullable one"" );
		}
	}
}
