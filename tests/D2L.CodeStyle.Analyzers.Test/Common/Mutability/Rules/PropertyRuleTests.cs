using System;
using System.Collections.Generic;
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
	internal sealed class PropertyRuleTests {
		[Flags]
		internal enum Accessors {
			AutoGet,
			AutoSet,
			Get,
			Set
		}

		[TestCase( Accessors.Get )] // public T P { get { ... } } --> ()
		[TestCase( Accessors.Set )] // public T P { set { ... } } --> ()
		[TestCase( Accessors.Get | Accessors.Set )] // public T P { get { ... } set { ... } } --> ()
		public void NonAutoProperty_NoSubGoals( Accessors accessors ) {
			var model = new Mock<ISemanticModel>( MockBehavior.Strict ).Object;

			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;

			ExpressionSyntax expr = null;

			var prop = CreateProperty(
				type,
				ref expr,
				accessors
			);

			var goal = new PropertyGoal( prop );

			var subgoals = PropertyRule.Apply( model, goal );

			CollectionAssert.IsEmpty( subgoals );
		}

		[TestCase( Accessors.AutoGet )] // public T P { get; } --> ReadOnly( P ), Type( T )
		[TestCase( Accessors.AutoGet | Accessors.AutoSet )] // public T P { get; set; } --> ReadOnly( P ), Type( T )
		public void AutoPropertyNoInitializer_ReadOnlyAndType( Accessors accessors ) {
			var model = new Mock<ISemanticModel>( MockBehavior.Strict ).Object;

			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;

			ExpressionSyntax expr = null;

			var prop = CreateProperty(
				type,
				ref expr,
				accessors
			);

			var goal = new PropertyGoal( prop );

			var subgoals = PropertyRule.Apply( model, goal );

			CollectionAssert.AreEquivalent(
				new Goal[] {
					new ReadOnlyGoal( prop ),
					new TypeGoal( type ),
				},
				subgoals
			);
		}

		[TestCase( Accessors.AutoGet )] // public T P { get; } = ...; --> ReadOnly( P ), Initializer( ... )
		[TestCase( Accessors.AutoGet | Accessors.AutoSet )] // public T P { get; set; } = ...; --> ReadOnly( P ), Initializer( ... )
		public void AutoPropertyInitializer_ReadOnlyAndInitializer( Accessors accessors ) {
			var model = new Mock<ISemanticModel>( MockBehavior.Strict ).Object;

			var type = new Mock<ITypeSymbol>( MockBehavior.Strict ).Object;

			ExpressionSyntax expr = SyntaxFactory.LiteralExpression( SyntaxKind.NullLiteralExpression );

			var prop = CreateProperty(
				type,
				ref expr,
				accessors
			);

			var goal = new PropertyGoal( prop );

			var subgoals = PropertyRule.Apply( model, goal );

			CollectionAssert.AreEquivalent(
				new Goal[] {
					new ReadOnlyGoal( prop ),
					new InitializerGoal( type, expr ),
				},
				subgoals
			);
		}

		private static IPropertySymbol CreateProperty(
			ITypeSymbol type,
			ref ExpressionSyntax initializerExpr,
			Accessors methods
		) {
			var propDecl = SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName( "T" ),
				"P"
			);

			if ( initializerExpr != null ) {
				propDecl = propDecl.WithInitializer(
					SyntaxFactory.EqualsValueClause(
						initializerExpr
					)
				);
			}

			var accessors = new List<AccessorDeclarationSyntax>();

			if ( methods.HasFlag( Accessors.Get ) ) {
				accessors.Add( SyntaxFactory.AccessorDeclaration(
					SyntaxKind.GetAccessorDeclaration,
					SyntaxFactory.Block()
				) );
			}

			if ( methods.HasFlag( Accessors.Set ) ) {
				accessors.Add( SyntaxFactory.AccessorDeclaration(
					SyntaxKind.SetAccessorDeclaration,
					SyntaxFactory.Block()
				) );
			}

			if ( methods.HasFlag( Accessors.AutoGet ) ) {
				accessors.Add( SyntaxFactory.AccessorDeclaration(
					SyntaxKind.GetAccessorDeclaration,
					body: null
				) );
			}

			if ( methods.HasFlag( Accessors.AutoSet ) ) {
				accessors.Add( SyntaxFactory.AccessorDeclaration(
					SyntaxKind.SetAccessorDeclaration,
					body: null
				) );
			}

			var accessorList = SyntaxFactory.AccessorList(
				SyntaxFactory.List( accessors ) );

			propDecl = propDecl.WithAccessorList( accessorList );

			var reference = new Mock<SyntaxReference>( MockBehavior.Strict );
			reference
				.Setup( r => r.GetSyntax( default( CancellationToken ) ) )
				.Returns( propDecl );

			var prop = new Mock<IPropertySymbol>( MockBehavior.Strict );
			prop.Setup( p => p.DeclaringSyntaxReferences )
				.Returns( ImmutableArray.Create( reference.Object ) );
			prop.Setup( p => p.Type )
				.Returns( type );

			initializerExpr = propDecl.Initializer?.Value;

			return prop.Object;
		}
	}
}
