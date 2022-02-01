namespace Microsoft.CodeAnalysis.CSharp.Syntax {

	internal static class NameSyntaxExtensions {

		/// <summary>
		/// Returns the unqualified (right-most) part of a qualified or alias-qualified name, or the name itself if already unqualified.
		/// </summary>
		/// <returns>The unqualified (right-most) part of a qualified or alias-qualified name, or the name itself if already unqualified.
		/// If called on an instance of <see cref="AliasQualifiedNameSyntax"/> returns the value of the <see cref="AliasQualifiedNameSyntax.Name"/> property.
		/// If called on an instance of <see cref="QualifiedNameSyntax"/> returns the value of the <see cref="QualifiedNameSyntax.Right"/> property.
		/// If called on an instance of <see cref="SimpleNameSyntax"/> returns the instance itself.
		/// </returns>
		/// <remarks>
		/// Extension method for:
		///
		///    https://github.com/dotnet/roslyn/blob/7f51127d775f9872103bc393d544b71e3e984890/src/Compilers/CSharp/Portable/Syntax/NameSyntax.cs#L29
		///    internal abstract SimpleNameSyntax GetUnqualifiedName();
		///
		/// Requested exposure:
		///
		///    https://github.com/dotnet/roslyn/issues/59175
		///
		/// </remarks>
		public static SimpleNameSyntax GetUnqualifiedName( this NameSyntax name ) {

			/// <remarks>
			/// Covering the immediately derived types similar to Microsoft.CodeAnalysis.CSharp overrides
			///
			/// https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.namesyntax?view=roslyn-dotnet-4.0.1
			/// </remarks>
			return name switch {

				/// <remarks>
				/// See https://github.com/dotnet/roslyn/blob/7f51127d775f9872103bc393d544b71e3e984890/src/Compilers/CSharp/Portable/Syntax/AliasedQualifiedNameSyntax.cs#L14
				/// </remarks>
				AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name,

				/// <remarks>
				/// See https://github.com/dotnet/roslyn/blob/7f51127d775f9872103bc393d544b71e3e984890/src/Compilers/CSharp/Portable/Syntax/QualifiedNameSyntax.cs#L19
				/// </remarks>
				QualifiedNameSyntax qualifiedName => qualifiedName.Right,

				/// <remarks>
				/// See https://github.com/dotnet/roslyn/blob/7f51127d775f9872103bc393d544b71e3e984890/src/Compilers/CSharp/Portable/Syntax/SimpleNameSyntax.cs#L19
				/// </remarks>
				SimpleNameSyntax simpleName => simpleName,

				_ => throw new NotImplementedException( $"{ nameof( GetUnqualifiedName ) } not implemented for { name.GetType().FullName }" ),
			};
		}

		/// <summary>
		/// Returns the unqualified (right-most) part of a qualified or alias-qualified name, or the name itself if already unqualified.
		/// </summary>
		public static string GetUnqualifiedNameAsString( this NameSyntax name ) {
			SimpleNameSyntax unqualifiedName = GetUnqualifiedName( name );
			return unqualifiedName.ToString();
		}
	}
}
