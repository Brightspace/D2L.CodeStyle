using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.UnsafeStaticCounter {
	internal class CountingVisitor : SymbolVisitor {

		private const string UnauditedAttributeName = "Unaudited";
		private const string DefaultCause = "none";

		internal readonly ConcurrentBag<AnalyzedStatic> AnalyzedStatics = new ConcurrentBag<AnalyzedStatic>();

		public override void VisitField( IFieldSymbol symbol ) {
			var unauditedAttribute = symbol
				.GetAttributes()
				.FirstOrDefault( a => a.AttributeClass.MetadataName == UnauditedAttributeName );
			if( unauditedAttribute == null ) {
				return;
			}

			var cause = DefaultCause;
			if( unauditedAttribute.ConstructorArguments.Length > 0 ) {
				cause = unauditedAttribute.ConstructorArguments[ 0 ].Value.ToString();
			}
			AnalyzedStatics.Add( new AnalyzedStatic( symbol, cause ) );
		}

		public override void VisitProperty( IPropertySymbol symbol ) {
			var unauditedAttribute = symbol
				.GetAttributes()
				.FirstOrDefault( a => a.AttributeClass.MetadataName == UnauditedAttributeName );
			if( unauditedAttribute == null ) {
				return;
			}

			var cause = DefaultCause;
			if( unauditedAttribute.ConstructorArguments.Length > 0 ) {
				cause = unauditedAttribute.ConstructorArguments[ 0 ].Value.ToString();
			}
			AnalyzedStatics.Add( new AnalyzedStatic( symbol, cause ) );
		}
	}
}
