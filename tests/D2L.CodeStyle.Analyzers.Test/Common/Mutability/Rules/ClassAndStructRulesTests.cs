using System;
using System.Collections.Immutable;
using System.Linq;
using D2L.CodeStyle.Analyzers.Common.Mutability.Goals;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework;

namespace D2L.CodeStyle.Analyzers.Common.Mutability.Rules {
	[TestFixture]
	internal sealed class ClassAndStructRulesTests {
		private IAssemblySymbol m_assembly;
		private ISemanticModel m_model;

		[OneTimeSetUp]
		public void SetUp() {
			m_assembly = new Mock<IAssemblySymbol>( MockBehavior.Strict ).Object;
			m_model = CreateSemanticModel();
		}

		// struct Foo {} --> nothing
		[Test]
		public void StructWithNoMembers_NoSubboals() {
			var structType = CreateType();
			var goal = new StructGoal( structType );

			var result = ClassAndStructRules.Apply( m_model, goal );

			CollectionAssert.IsEmpty( result );
		}

		// class Foo : SomeType {} --> ConcreteType(SomeType)
		[Test]
		public void ClassWithNoMembers_OnlyObjectSubgoalForBase() {
			var baseType = CreateType();
			var classType = CreateTypeWithBase( baseType );
			var goal = new ClassGoal( classType );

			var result = ClassAndStructRules.Apply( m_model, goal );

			CollectionAssert.AreEquivalent(
				new[] { new ConcreteTypeGoal( baseType ) },
				result
			);
		}

		// struct Foo {
		//   private Foo x; // --> PropertyGoal(x)
		//   private Foo y; // --> PropertyGoal(y)
		// }
		[Test]
		public void StructWithTwoProperties_ReturnsTwoPropertySubgoals() {
			var propertySymbol1 = CreateMember<IPropertySymbol>( SymbolKind.Property );
			var propertySymbol2 = CreateMember<IPropertySymbol>( SymbolKind.Property );

			var structType = CreateType(
				propertySymbol1,
				propertySymbol2
			);

			var goal = new StructGoal( structType );

			var result = ClassAndStructRules.Apply( m_model, goal );

			CollectionAssert.AreEquivalent(
				new[] {
					new PropertyGoal( propertySymbol1 ),
					new PropertyGoal( propertySymbol2 )
				},
				result
			);
		}

		// struct Foo {
		//   private static int x;
		//   private Foo y; // --> PropertyGoal(y)
		// }
		[Test]
		public void StructWithStaticProperty_IgnoresIt() {
			var staticProperty = CreateMember<IPropertySymbol>(
				SymbolKind.Property,
				isStatic: true
			);

			var nonStaticProperty = CreateMember<IPropertySymbol>(
				SymbolKind.Property
			);

			var structType = CreateType(
				staticProperty,
				nonStaticProperty
			);

			var goal = new StructGoal( structType );

			var result = ClassAndStructRules.Apply( m_model, goal );

			CollectionAssert.AreEquivalent(
				new[] { new PropertyGoal( nonStaticProperty ) },
				result
			);
		}

		// struct Foo {
		//   private Foo x; // --> PropertyGoal(x)
		//
		//   void SomeMethod() {}
		// }
		[Test]
		public void StructWithMethod_IgnoresIt() {
			var property = CreateMember<IPropertySymbol>(
				SymbolKind.Property
			);

			var method = CreateMember<IMethodSymbol>(
				SymbolKind.Method
			);

			var structType = CreateType(
				property,
				method
			);

			var goal = new StructGoal( structType );

			var result = ClassAndStructRules.Apply( m_model, goal );

			CollectionAssert.AreEquivalent(
				new[] { new PropertyGoal( property ) },
				result
			);
		}

		// This should be the same as if it were a struct, but with the extra
		// base type subgoal
		//
		// class Foo : BaseType { // --> ConcreteType(BaseType)
		//   static int x; // static variable ignored
		//   private Event ev; // --> EventGoal(ev)
		//   void SomeMethod() {} // methods are ignored
		//   public int prop { get; } // --> PropertyGoal(prop)
		//   public class InnerClass { int x; } // embedded classes are ignored
		//   public int field; // ---> FieldGoal(field)
		//
		//
		// }
		[Test]
		public void ComplexClassAndStruct() {
			var baseType = CreateType();

			var staticProperty = CreateMember<IPropertySymbol>(
				SymbolKind.Property,
				isStatic: true
			);

			var ev = CreateMember<IEventSymbol>(
				SymbolKind.Event
			);

			var method = CreateMember<IMethodSymbol>(
				SymbolKind.Method
			);

			var prop = CreateMember<IPropertySymbol>(
				SymbolKind.Property
			);

			var innerClass = CreateMember<INamedTypeSymbol>(
				SymbolKind.NamedType
			);

			var field = CreateMember<IFieldSymbol>(
				SymbolKind.Field
			);

			var members = new ISymbol[] {
				staticProperty,
				ev,
				method,
				prop,
				innerClass,
				field
			};

			var structType = CreateType( members );

			var classType = CreateTypeWithBase( baseType, members );

			var structGoal = new StructGoal( structType );
			var classGoal = new ClassGoal( classType );

			var structSubgoals = ClassAndStructRules.Apply( m_model, structGoal ).ToList();
			var classSubgoals = ClassAndStructRules.Apply( m_model, classGoal );


			CollectionAssert.AreEquivalent(
				new Goal[] {
					new EventGoal( ev ),
					new PropertyGoal( prop ),
					new FieldGoal( field )
				},
				structSubgoals
			);

			CollectionAssert.AreEquivalent(
				new Goal[] {
					new EventGoal( ev ),
					new PropertyGoal( prop ),
					new FieldGoal( field ),
					new ConcreteTypeGoal( baseType )
				},
				classSubgoals
			);
		}

		private ISemanticModel CreateSemanticModel() {
			var model = new Mock<ISemanticModel>( MockBehavior.Strict );
			model.Setup( m => m.Assembly() ).Returns( m_assembly );
			return model.Object;
		}

		private ITypeSymbol CreateTypeWithBase(
			INamedTypeSymbol baseType,
			params ISymbol[] members
		) {
			return CreateTypeHelper( baseType, members );
		}

		private INamedTypeSymbol CreateType(
			params ISymbol[] members
		) {
			return CreateTypeHelper( null, members );
		}
		
		private INamedTypeSymbol CreateTypeHelper(
			INamedTypeSymbol baseType,
			ISymbol[] members
		) {
			var type = new Mock<INamedTypeSymbol>( MockBehavior.Strict );

			type.Setup( t => t.GetMembers() )
				.Returns( members.ToImmutableArray() );

			type.Setup( t => t.ContainingAssembly )
				.Returns( m_assembly );

			if ( baseType != null ) {
				type.Setup( t => t.BaseType )
					.Returns( baseType );
			}

			return type.Object;
		}

		private static TSymbolKind CreateMember<TSymbolKind>(
			SymbolKind kind,
			bool isStatic = false
		) where TSymbolKind : class, ISymbol {
			var member = new Mock<TSymbolKind>( MockBehavior.Strict );

			member.Setup( m => m.Kind ).Returns( kind );
			member.Setup( m => m.IsStatic ).Returns( isStatic );
			member.Setup( m => m.IsImplicitlyDeclared ).Returns( false );
			member.Setup( m => m.Name ).Returns( Guid.NewGuid().ToString() );

			return member.Object;
		}
	}
}

