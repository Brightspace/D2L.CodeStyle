using System.Collections.Generic;
using System.Collections.Immutable;
using D2L.CodeStyle.Analyzers.Extensions;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Immutability {
	internal sealed class KnownImmutableTypes {
		internal static readonly KnownImmutableTypes Default = new KnownImmutableTypes( ImmutableHashSet<string>.Empty );

		/// <summary>
		/// A list of known immutable types.
		/// </summary>
		private static readonly ImmutableHashSet<string> DefaultKnownImmutableTypes = new HashSet<string> {
			"D2L.UtcDateTime",
			"D2L.LP.Users.UserId",
			"D2L.LP.OrgUnits.OrgId",
			"D2L.LP.OrgUnits.OrgUnitId",

			"count4net.IRateCounter",
			"count4net.IStatCounter",
			"count4net.IValueCounter",
			"count4net.IDurationCounter",
			"count4net.Writers.DurationCounter",
			"log4net.ILog",
			"Newtonsoft.Json.JsonSerializer",
			"Newtonsoft.Json.JsonConverter",
			"System.Collections.Generic.KeyValuePair",
			"System.ComponentModel.TypeConverter",
			"System.DateTime",
			"System.Drawing.Imaging.ImageFormat",
			"System.Drawing.Size", // only safe because it's a struct with primitive fields
			"System.Guid",
			"System.Reflection.ConstructorInfo",
			"System.Reflection.FieldInfo",
			"System.Reflection.MemberInfo",
			"System.Reflection.MethodInfo",
			"System.Reflection.PropertyInfo",
			"System.Security.Cryptography.RNGCryptoServiceProvider",
			"System.Text.RegularExpressions.Regex",
			"System.Threading.ReaderWriterLockSlim",
			"System.TimeSpan",
			"System.Type",
			"System.Uri",
			"System.String",
			"System.Version",
			"System.Workflow.ComponentModel.DependencyProperty",
			"System.Xml.Serialization.XmlSerializer",
			"System.Xml.Linq.XName",
			"System.Xml.Linq.XNamespace"
		}.ToImmutableHashSet();

		/// <summary>
		/// A list of known immutable special types.
		/// </summary
		private readonly static ImmutableArray<SpecialType> ImmutableSpecialTypes = ImmutableArray.Create(
			SpecialType.System_Enum,
			SpecialType.System_Boolean,
			SpecialType.System_Char,
			SpecialType.System_SByte,
			SpecialType.System_Byte,
			SpecialType.System_Int16,
			SpecialType.System_UInt16,
			SpecialType.System_Int32,
			SpecialType.System_UInt32,
			SpecialType.System_Int64,
			SpecialType.System_UInt64,
			SpecialType.System_Decimal,
			SpecialType.System_Single,
			SpecialType.System_Double,
			SpecialType.System_String,
			SpecialType.System_IntPtr,
			SpecialType.System_UIntPtr
		);
		
		/// <summary>
		/// A list of known immutable types defined for the assembly.
		/// </summary>
		private readonly ImmutableHashSet<string> DeclaredKnownImmutableTypes;

		internal KnownImmutableTypes( ImmutableHashSet<string> declaredKnownImmutableTypes ) {
			DeclaredKnownImmutableTypes = declaredKnownImmutableTypes;
		}

		internal KnownImmutableTypes( IAssemblySymbol a )
			: this( LoadFromAssembly( a ).ToImmutableHashSet() ) { }

		internal bool IsTypeKnownImmutable( ITypeSymbol type ) {
			if( ImmutableSpecialTypes.Contains( type.SpecialType ) ) {
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

		private static IEnumerable<string> LoadFromAssembly( IAssemblySymbol a ) {
			var typesAttributes = Attributes.Types.Audited.GetAll( a );

			foreach( var attribute in typesAttributes ) {
				var typeofArgument = attribute.ConstructorArguments[0];
				var value = typeofArgument.Value as INamedTypeSymbol;
				if( value == null ) {
					// unable to extract the type, continue safely
					continue;
				}

				yield return value.GetFullTypeName();
			}
		}
	}
}
