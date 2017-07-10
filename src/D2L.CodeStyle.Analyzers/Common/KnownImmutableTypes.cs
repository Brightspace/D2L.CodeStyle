using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common {
	internal sealed class KnownImmutableTypes {

		internal static readonly KnownImmutableTypes Default = new KnownImmutableTypes( ImmutableHashSet<string>.Empty );

		/// <summary>
		/// A list of known immutable types.
		/// </summary>
		private static readonly ImmutableHashSet<string> DefaultKnownImmutableTypes = new HashSet<string> {
			"count4net.IRateCounter",
			"count4net.IStatCounter",
			"count4net.IValueCounter",
			"count4net.Writers.DurationCounter",
			"log4net.ILog",
			"Newtonsoft.Json.JsonSerializer",
			"System.Collections.Generic.KeyValuePair",
			"System.ComponentModel.TypeConverter",
			"System.DateTime",
			"System.Drawing.Size", // only safe because it's a struct with primitive fields
			"System.Guid",
			"System.Reflection.ConstructorInfo",
			"System.Reflection.FieldInfo",
			"System.Reflection.MemberInfo",
			"System.Reflection.MethodInfo",
			"System.Reflection.PropertyInfo",
			"System.Text.RegularExpressions.Regex",
			"System.TimeSpan",
			"System.Type",
			"System.Uri",
			"System.String",
			"System.Version",
			"System.Workflow.ComponentModel.DependencyProperty",
			"System.Xml.Serialization.XmlSerializer"
		}.ToImmutableHashSet();

		/// <summary>
		/// A list of known immutable types defined for the assembly.
		/// </summary>
		private readonly ImmutableHashSet<string> DeclaredKnownImmutableTypes;

		internal KnownImmutableTypes( ImmutableHashSet<string> declaredKnownImmutableTypes ) {
			DeclaredKnownImmutableTypes = declaredKnownImmutableTypes;
		}

		internal KnownImmutableTypes( IAssemblySymbol a )
			: this( LoadFromAssembly( a ) ) { }

		internal bool IsTypeKnownImmutable( ITypeSymbol type ) {
			if( type.IsPrimitive() ) {
				return true;
			}

			string typeName = type.GetFullTypeName();

			if( DeclaredKnownImmutableTypes.Contains( typeName ) ) {
				return true;
			}

			if( DefaultKnownImmutableTypes.Contains( typeName ) ) {
				return true;
			}

			return false;
		}

		private static ImmutableHashSet<string> LoadFromAssembly( IAssemblySymbol a ) {
			var knownImmutableTypesAttribute = a.GetAttributes()
				.Where( attr => attr.AttributeClass.Name == "KnownImmutableTypes" )
				.SingleOrDefault();

			if( knownImmutableTypesAttribute == null ) {
				return ImmutableHashSet<string>.Empty;
			}

			var knownImmutableTypes = knownImmutableTypesAttribute
				.ConstructorArguments
				.SelectMany( GetTypeConstantValues<string> );

			return knownImmutableTypes
				.Select( t => t.ToString() )
				.ToImmutableHashSet();
		}

		private static ISet<T> GetTypeConstantValues<T>( TypedConstant c ) {
			var values = new HashSet<T>();

			if( c.Value != null ) {
				values.Add( (T) c.Value );
			}

			if( !c.Values.IsDefaultOrEmpty ) {
				var nestedValues = c.Values.SelectMany( GetTypeConstantValues<T> );
				foreach( var nestedValue in nestedValues ) {
					values.Add( nestedValue );
				}
			}

			return values;
		}
	}
}
