// analyzer: D2L.CodeStyle.Analyzers.ApiUsage.Serialization.SerializationFrameworkAnalyzer

using System;
using D2L.LP.Serialization

namespace D2L.LP.Serialization {

	public sealed class SerializationFrameworkAttribute : Attribute { }

	[SerializationFramework]
	public class DangerousExplicitClass : ISerializer, ITrySerializer, ISerializationWriter {
		public void Foo() { }	
		void ISerializer.Serialize() { }
		bool ITrySerializer.TrySerialize() { return true; }
		void ISerializationWriter.Write() { }
	}

	[SerializationFramework]
	public class DangerousImplicitClass : ISerializer, ITrySerializer, ISerializationWriter {
		public void Serialize() { }
		public bool TrySerialize() { return true; }
		public void Write() { }
	}

	public interface ISerializer {
		public void Serialize();
	}

	public interface ITrySerializer {
		public bool TrySerialize();
	}

	public interface ISerializationWriter {
		public void Write();
	}

	[SerializationFramework]
	public static class ISerializationWriterExtensions {
		public static void Write( this ISerializationWriter writer, string _ ) {
			writer.Write();
		}
	}

	[SerializationFramework]
	public static class SerializationFactory {
		public static ISerializer Serializer {
			get { return new DangerousExplicitClass(); }
		}
		public static ITrySerializer TrySerializer {
			get { return new DangerousExplicitClass(); }
		}
		public static ISerializer SerializationWriter {
			get { return new DangerousExplicitClass(); }
		}
	}

}

namespace D2L.CodeStyle.Analyzers.SerializationFrameworkAnalyzer.Examples {
	using D2L.LP.Serialization;

	public sealed class BadClass {

		private DangerousExplicitClass /* DangerousSerializationTypeReference */ m_dangerousExplicitClass /**/;
		private DangerousImplicitClass /* DangerousSerializationTypeReference */ m_dangerousImplicitClass /**/;

		public void Uses_ISerializer() {
			ISerializer explicitSerializer = SerializationFactory.Serializer;
			/* DangerousSerializationTypeReference */
			explicitSerializer.Serialize() /**/ ;
		}

		public void Uses_ITrySerializer() {
			ITrySerializer explicitTrySerializer = SerializationFactory.TrySerializer;
			/* DangerousSerializationTypeReference */
			explicitTrySerializer.TrySerialize() /**/ ;
		}

		public void Uses_ISerializationWriter() {
			ISerializationWriter explicitWriter = SerializationFactory.SerializationWriter;
			/* DangerousSerializationTypeReference */
			explicitWriter.Write() /**/ ;
		}

		public void Uses_ISerializationWriterExtensions() {
			ISerializationWriter explicitWriter = SerializationFactory.SerializationWriter;
			/* DangerousSerializationTypeReference */
			explicitWriter.Write( "foo" ) /**/ ;
		}

		public void Uses_DangerousImplicitReferences() {
			DangerousImplicitClass dangerousClass = new DangerousImplicitClass();
			/* DangerousSerializationTypeReference */
			dangerousClass.Serialize() /**/ ;
			/* DangerousSerializationTypeReference */
			dangerousClass.TrySerialize() /**/ ;
		}

		public void Uses_DangerousImplementingType() {
			DangerousExplicitClass dangerousClass = new DangerousExplicitClass();
			/* DangerousSerializationTypeReference */
			dangerousClass.Foo() /**/ ;
		}

	}

	[SerializationFramework]
	public sealed class SerializationFrameworkClass {

		private readonly ISerializer m_serializer;
		private readonly ITrySerializer m_trySerializer;
		private readonly ISerializationWriter m_serializationWriter;

		public SerializationFrameworkClass(
			ISerializer serializer,
			ITrySerializer trySerializer,
			ISerializationWriter serializationWriter
		) {
			m_serializer = serializer;
			m_trySerializer = trySerializer;
			m_serializationWriter = serializationWriter;
		}

		public void InSerializationClass() {
			ISerializer ok = SerializationFactory.Serializer;
			ok.Serialize();

			ITrySerializer alsoOk = SerializationFactory.TrySerializer;
			alsoOk.TrySerialize();

			ISerializationWriter okayToo = SerializationFactory.SerializationWriter;
			okayToo.Write();
		}

		public ISerializer Serializer {
			get { return m_serializer; }
		}
	}

	[SerializationFramework]
	public sealed class SerializationFrameworkUsageInNestedClass {
		private static class Nested {
			public static void Usage() {
				ISerializer ok = SerializationFactory.Serializer;
				ok.Serialize();

				ITrySerializer alsoOk = SerializationFactory.TrySerializer;
				alsoOk.TrySerialize();

				ISerializationWriter okayToo = SerializationFactory.SerializationWriter;
				okayToo.Write();
			}
		}
	}


}
