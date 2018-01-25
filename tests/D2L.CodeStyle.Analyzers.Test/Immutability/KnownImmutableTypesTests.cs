using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static D2L.CodeStyle.Analyzers.RoslynSymbolFactory;

namespace D2L.CodeStyle.Analyzers.Immutability {
	[TestFixture]
	public class KnownImmutableTypesTests {

		[Test]
		public void IsTypeKnownImmutable_AssemblyDeclaredImmutable_True() {
			var cs = @"
using System;
using D2L.CodeStyle.Annotations;

[assembly: Types.Audited( typeof( Test.Foo ), ""Random Owner"", ""Random Date"", ""Random rationale"" )] 

namespace Test {
	public class Foo { }
}

namespace D2L.CodeStyle.Annotations {
    public static partial class Types {
        [AttributeUsage( validOn: AttributeTargets.Assembly, AllowMultiple = true )]
		public sealed class Audited : Attribute {
			public Audited( Type type, string owner, string auditedDate, string rationale ) {
				Type = type;
				Owner = owner;
				AuditedDate = auditedDate;
				Rationale = rationale;
			}

			public Type Type { get; }
			public string Owner { get; }
			public string AuditedDate { get; }
			public string Rationale { get; }
		}
	}
}
";
			var compilation = Compile( cs );
			var knownTypes = new KnownImmutableTypes( compilation.Assembly );
			var fooType = compilation.GetSymbolsWithName(
				predicate: n => true,
				filter: SymbolFilter.Type
			).OfType<ITypeSymbol>().FirstOrDefault();
			Assert.IsNotNull( fooType );
			Assert.AreNotEqual( TypeKind.Error, fooType.TypeKind );

			var result = knownTypes.IsTypeKnownImmutable( fooType );

			Assert.True( result );
		}

		[Test]
		public void IsTypeKnownImmutable_DefaultlyImmutable_True() {
			var knownTypes = new KnownImmutableTypes( ImmutableHashSet<string>.Empty );
			var type = Field( "System.Version foo" ).Symbol.Type;

			var result = knownTypes.IsTypeKnownImmutable( type );

			Assert.True( result );
		}

		[Test]
		public void IsTypeKnownImmutable_DefaultlyNotImmutable_False() {
			var knownTypes = new KnownImmutableTypes( ImmutableHashSet<string>.Empty );
			var type = Field( "System.IDisposable foo" ).Symbol.Type;

			var result = knownTypes.IsTypeKnownImmutable( type );

			Assert.False( result );
		}

		[Test]
		public void IsTypeKnownImmutable_DeclaredImmutable_True() {
			var knownTypes = new KnownImmutableTypes( new HashSet<string> {
				"System.IDisposable"
			}.ToImmutableHashSet() );
			var type = Field( "System.IDisposable foo" ).Symbol.Type;

			var result = knownTypes.IsTypeKnownImmutable( type );

			Assert.True( result );
		}

		[Test]
		public void IsTypeKnownImmutable_NotDeclaredImmutableAndNotDefault_False() {
			var knownTypes = new KnownImmutableTypes( ImmutableHashSet<string>.Empty );
			var type = Field( "System.IDisposable foo" ).Symbol.Type;

			var result = knownTypes.IsTypeKnownImmutable( type );

			Assert.False( result );
		}
	}
}
