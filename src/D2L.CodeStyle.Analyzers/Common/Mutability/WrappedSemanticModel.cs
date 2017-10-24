using Microsoft.CodeAnalysis;

namespace D2L.CodeStyle.Analyzers.Common.Mutability {
	internal sealed class WrappedSemanticModel : ISemanticModel {
		private readonly SemanticModel m_model;
		
		public WrappedSemanticModel( SemanticModel model ) {
			m_model = model;
		}

		public IAssemblySymbol Assembly() {
			return m_model.Compilation.Assembly;
		}

		public ITypeSymbol GetTypeSymbol( SyntaxNode node ) {
			return m_model.GetTypeInfo( node ).Type;
		}
	}
}
