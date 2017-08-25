﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace D2L.CodeStyle.Analyzers.Common {

	[Flags]
	public enum MutabilityInspectionFlags {
		Default = 0,
		AllowUnsealed = 1,
		IgnoreImmutabilityAttribute = 2,
	}

	internal sealed class MutabilityInspector {

		/// <summary>
		/// A list of temporarily allowed external types. This is here so that we can get a fix in for 
		/// requiring external types to be marked with [Objects.Immutable]; we will burn this list down
		/// and will not add to it!
		/// </summary>
		private static readonly ImmutableHashSet<string> TemporarilyAllowedExternalTypes = new HashSet<string> {
			"D2L.AW.ETL.Utility.Logger",
			"D2L.Core.Caching.OptimisticPermanentCache",
			"D2L.Core.Comparisons.EqualityComparerAdapter",
			"D2L.Core.Comparisons.NullableEqualityComparerWrapper",
			"D2L.Core.Contracts.Plugins.Contract",
			"D2L.Core.Data.MsSql.RowVersion",
			"D2L.Core.JobManagement.Legacy.JobManager",
			"D2L.Core.JobManagement.Legacy.JobStatusType",
			"D2L.Core.Reflection.AssemblyQualifiedNameFormatter",
			"D2L.Core.Security.Encryption.D2LEncryptionFactory",
			"D2L.Core.Security.Encryption.StringHasher",
			"D2L.Core.Xml.ArrayXmlFormatter",
			"D2L.Core.Xml.DictionaryXmlFormatter",
			"D2L.Custom.ReportingFramework.Domain.Entities.DataSetFilter",
			"D2L.HoldingTank.Batch.DSL.HoldingTankCommandHelpText",
			"D2L.LOR.Search.Field",
			"D2L.LP.Caching.Providers.PermanentCache",
			"D2L.LP.Caching.SimpleLifetimeCache",
			"D2L.LP.Diagnostics.Performance.Metric",
			"D2L.LP.Extensibility.TypeResolution.CustomType",
			"D2L.LP.Files.Domain.Default.InMemoryFile",
			"D2L.LP.Globalization.Timezones.Domain.ServerTimezone",
			"D2L.LP.Globalization.Timezones.Domain.UtcTimezone",
			"D2L.LP.Hypermedia.Constants.CanonicalDomainName",
			"D2L.LP.LayeredArch.Data.CompositeDTOFactory",
			"D2L.LP.LayeredArch.Data.CompositeDTOFactoryBase",
			"D2L.LP.LayeredArch.Data.Objects.ExpressionFieldMapper",
			"D2L.LP.LayeredArch.Data.Searching.MsSql.MsSqlSearchData",
			"D2L.LP.LayeredArch.LoadMoreSortInfo",
			"D2L.LP.OrgUnits.OrgId",
			"D2L.LP.OrgUnits.OrgUnitId",
			"D2L.LP.OrgUnits.WorkQueue.Workers.HandlerType",
			"D2L.LP.Resources.GlobalTypeIdentifier",
			"D2L.LP.Text.LangTerm",
			"D2L.LP.Users.UserId",
			"D2L.LP.Validation.Validators.GreaterThanValidator",
			"D2L.LP.Validation.Validators.TrueValidator",
			"D2L.LP.Web.Mvc.RouteLocation",
			"D2L.LP.Web.UI.Common.Controls.Container.SetIsVisibleFunction",
			"D2L.LP.Web.UI.Common.Controls.Form.SubmitRpcFunction",
			"D2L.LP.Web.UI.Common.Controls.Hidden.SetValueFunction",
			"D2L.LP.Web.UI.Desktop.Controls.DateTimeSelector.SetIsEnabledFunction",
			"D2L.LP.Web.UI.Desktop.Controls.Grid.ReloadFunction",
			"D2L.LP.Web.UI.Desktop.Controls.RadioList.SetIsAllEnabledFunction",
			"D2L.LP.Web.UI.Html.JavaScript.CompareBooleanFunction",
			"D2L.LP.Web.UI.Html.JavaScript.CompareStringFunction",
			"D2L.LP.Web.UI.Html.JavaScript.Expressions.IntExpression",
			"D2L.LP.Web.UI.Html.JavaScript.IfShortHandJavaScriptFunction",
			"D2L.LP.Web.UI.Html.JavaScript.JavaScriptFunction",
			"D2L.LP.Web.UI.Html.JavaScript.JavaScriptFunctionPipeline",
			"D2L.LP.Web.UI.Html.JavaScript.NavigateFunction",
			"D2L.LP.Web.UI.Html.Scoping.ScopeName",
			"D2L.LP.Web.UI.Html.Style.Background.BackgroundHorizontalPosition",
			"D2L.LP.Web.UI.Html.Style.Background.BackgroundStyleFactory",
			"D2L.LP.Web.UI.Html.Style.Background.BackgroundVerticalPosition",
			"D2L.LP.Web.UI.Html.Style.Borders.Border",
			"D2L.LP.Web.UI.Html.Style.Borders.BorderFactory",
			"D2L.LP.Web.UI.Html.Style.Borders.BorderRadius",
			"D2L.LP.Web.UI.Html.Style.Spacing.SpacingFactory",
			"D2L.LP.WebExtensibility.Paging.LoadMorePagingInfo",
			"D2L.LP.WebExtensibility.Paging.LoadMorePagingResultSet",
			"D2L.LP.WebExtensibility.Paging.PageSizeCalculator",
			"D2L.LP.WebExtensibility.Versioning.RestWebServiceVersion",
			"D2L.Lms.Domain.Security.Right",
			"D2L.LocalDate",
			"D2L.LocalDateTime",
			"D2L.LocalTime",
			"D2L.Paths.Web.AbsoluteVirtualPath",
			"D2L.Services.Monitoring.HealthCheck.Contract.Model.ApplicationIdentifier",
			"D2L.Services.Monitoring.HealthCheck.Contract.Model.Status",
			"D2L.Services.Monitoring.HealthCheck.Contract.Status",
			"D2L.UtcDateTime",
			"System.Drawing.Imaging.ImageFormat",
			"System.Random",
			"System.Security.Cryptography.RNGCryptoServiceProvider",
			"System.Text.Encoding",
			"System.Text.ASCIIEncoding",
			"System.Text.UTF8Encoding",
			"System.Threading.ReaderWriterLockSlim",
			"System.Xml.Linq.XDeclaration",
			"System.Xml.Linq.XName",
			"System.Xml.Linq.XNamespace",
			"System.Xml.XmlWriterSettings"
		}.ToImmutableHashSet();

		/// <summary>
		/// A list of marked immutable types owned externally.
		/// </summary>
		private static readonly ImmutableHashSet<string> MarkedImmutableTypes = new HashSet<string> {
			"System.StringComparer"
		}.ToImmutableHashSet();

		/// <summary>
		/// A list of immutable container types (i.e., types that hold other types)
		/// </summary>
		private static readonly ImmutableDictionary<string, string[]> ImmutableContainerTypes = new Dictionary<string, string[]> {
			[ "D2L.LP.Utilities.DeferredInitializer" ] = new[] { "Value" },
			[ "System.Collections.Immutable.ImmutableArray" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableDictionary" ] = new[] { "[].Key", "[].Value" },
			[ "System.Collections.Immutable.ImmutableHashSet" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableList" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableQueue" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableSortedDictionary" ] = new[] { "[].Key", "[].Value" },
			[ "System.Collections.Immutable.ImmutableSortedSet" ] = new[] { "[]" },
			[ "System.Collections.Immutable.ImmutableStack" ] = new[] { "[]" },
			[ "System.Collections.Generic.IReadOnlyCollection" ] = new[] { "[]" },
			[ "System.Collections.Generic.IReadOnlyList" ] = new[] { "[]" },
			[ "System.Collections.Generic.IReadOnlyDictionary" ] = new[] { "[].Key", "[].Value" },
			[ "System.Collections.Generic.IEnumerable" ] = new[] { "[]" },
			[ "System.Lazy" ] = new[] { "Value" },
			[ "System.Nullable" ] = new[] { "Value" },
			[ "System.Tuple" ] = new[] { "Item1", "Item2", "Item3", "Item4", "Item5", "Item6" }
		}.ToImmutableDictionary();

		private readonly KnownImmutableTypes _knownImmutableTypes;

		internal MutabilityInspector(
			KnownImmutableTypes knownImmutableTypes
		) {
			_knownImmutableTypes = knownImmutableTypes;
		}

		/// <summary>
		/// Determine if a given type is mutable.
		/// </summary>
		/// <param name="type">The type to determine mutability for.</param>
		/// <returns>Whether the type is mutable.</returns>
		public MutabilityInspectionResult InspectType(
			ITypeSymbol type,
			IAssemblySymbol rootAssembly,
			MutabilityInspectionFlags flags = MutabilityInspectionFlags.Default
		) {
			var typesInCurrentCycle = new HashSet<ITypeSymbol>();
			var result = InspectTypeRecursive( 
				type, 
				rootAssembly, 
				flags, 
				typesInCurrentCycle
			);
			return result;
		}

		private MutabilityInspectionResult InspectTypeRecursive(
			ITypeSymbol type,
			IAssemblySymbol rootAssembly,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {
			if( type is IErrorTypeSymbol ) {
				// This only happens for code that otherwise won't compile. Our
				// analyzer doesn't need to validate these types. It only needs
				// to be strict for valid code.
				return MutabilityInspectionResult.NotMutable();
			}

			if( type == null ) {
				throw new Exception( "Type cannot be resolved. Please ensure all dependencies "
					+ "are referenced, including transitive dependencies." );
			}

			if( _knownImmutableTypes.IsTypeKnownImmutable( type ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			switch( type.TypeKind ) {
				case TypeKind.Array:
					// Arrays are always mutable because you can rebind the
					// individual elements.
					return MutabilityInspectionResult.MutableType(
						type,
						MutabilityCause.IsAnArray
					);

				case TypeKind.Delegate:
					// Delegates can hold state so are mutable in general.
					return MutabilityInspectionResult.MutableType(
						type,
						MutabilityCause.IsADelegate
					);

				case TypeKind.Dynamic:
					// Dynamic types are always mutable
					return MutabilityInspectionResult.MutableType(
						type,
						MutabilityCause.IsDynamic
					);

				case TypeKind.Enum:
					// Enums are just fancy ints.
					return MutabilityInspectionResult.NotMutable();

				case TypeKind.Class:
				case TypeKind.Interface:
				case TypeKind.Struct: // equivalent to TypeKind.Structure
				case TypeKind.TypeParameter:
					return InspectClassStructOrInterfaceOrTypeParameter(
						type,
						rootAssembly,
						flags,
						typeStack
					);

				case TypeKind.Error:
					// This only happens when the build is failing for other
					// (more fundamental) reasons. We only need to be strict
					// for otherwise-successful builds, so we bail analysis in
					// this case.
					return MutabilityInspectionResult.NotMutable();

				case TypeKind.Unknown:
					// Looking at the Roslyn source this doesn't appear to
					// happen outside their tests. It is value 0 in the enum so
					// it may just be a safety guard.
					throw new NotImplementedException();

				default:
					// not handled: Module, Pointer, Submission.
					throw new NotImplementedException(
						$"TypeKind.{type.Kind} not handled by analysis"
					);
			}
		}

		private MutabilityInspectionResult InspectBaseType(
			ITypeSymbol type,
			IAssemblySymbol rootAssembly,
			HashSet<ITypeSymbol> typeStack
		) {
			if ( type.BaseType == null ) {
				return MutabilityInspectionResult.NotMutable();
			}

			if ( type.BaseType is IErrorTypeSymbol ) {
				return MutabilityInspectionResult.NotMutable();
			}

			return InspectClassStructOrInterfaceOrTypeParameter( type.BaseType, rootAssembly, MutabilityInspectionFlags.AllowUnsealed, typeStack );
		}

		private MutabilityInspectionResult InspectClassStructOrInterfaceOrTypeParameter(
			ITypeSymbol type,
			IAssemblySymbol rootAssembly,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {
			// If we're verifying immutability, then carry on; otherwise, bailout
			if( !flags.HasFlag( MutabilityInspectionFlags.IgnoreImmutabilityAttribute ) && IsTypeMarkedImmutable( type ) ) {
				return MutabilityInspectionResult.NotMutable();
			}

			if( typeStack.Contains( type ) ) {
				// We have a cycle. If we're here, it means that either some read-only member causes the cycle (via IsMemberMutableRecursive), 
				// or a generic parameter to a type causes the cycle (via IsTypeMutableRecursive). This is safe if the checks above have 
				// passed and the remaining members are read-only immutable. So we can skip the current check, and allow the type to continue 
				// to be evaluated.
				return MutabilityInspectionResult.NotMutable();
			}

			typeStack.Add( type );
			try {

				if( ImmutableContainerTypes.ContainsKey( type.GetFullTypeName() ) ) {
					var namedType = type as INamedTypeSymbol;
					for( int i = 0; i < namedType.TypeArguments.Length; i++ ) {
						var arg = namedType.TypeArguments[ i ];
						var result = InspectTypeRecursive( arg, rootAssembly, MutabilityInspectionFlags.Default, typeStack );

						if( result.IsMutable ) {
							if( result.Target == MutabilityTarget.Member ) {

								// modify the result to prefix with container member.
								var prefix = ImmutableContainerTypes[ type.GetFullTypeName() ];
								result = result.WithPrefixedMember( prefix[i] );

							} else {

								// modify the result to target the type argument if the target is not a member
								result = result.WithTarget( MutabilityTarget.TypeArgument );

							}
							return result;
						}
					}
					return MutabilityInspectionResult.NotMutable();
				}

				if( type.TypeKind == TypeKind.Interface ) {
					return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsAnInterface );
				}

				if( !flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed )
						&& type.TypeKind == TypeKind.Class
						&& !type.IsSealed
					) {
					return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsNotSealed );
				}

				// System.Object is safe if we can allow unsealed types (i.e., the type is the concrete type). 
				// For example, `private readonly object m_lock = new object();` is fine. 
				// There is no state, so we finish early.
				if( flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed ) && type.SpecialType == SpecialType.System_Object ) {
					return MutabilityInspectionResult.NotMutable();
				}

				// System.ValueType is the base class of all value types (obscure detail)
				if ( flags.HasFlag( MutabilityInspectionFlags.AllowUnsealed ) && type.SpecialType == SpecialType.System_ValueType ) {
					return MutabilityInspectionResult.NotMutable();
				}

				// We have a type that is not marked immutable, is not an interface, is not an immutable container, etc..
				// If it is defined in a different assembly, we might not have the metadata to correctly analyze it; so we fail.
				if( type.ContainingAssembly != rootAssembly ) {
					if( !TemporarilyAllowedExternalTypes.Contains( type.GetFullTypeName() ) ) {
						// this if-block is temporary!
						return MutabilityInspectionResult.MutableType( type, MutabilityCause.IsAnExternalUnmarkedType );
					}
				}

				foreach( ISymbol member in type.GetExplicitNonStaticMembers() ) {
					var result = InspectMemberRecursive( member, rootAssembly, MutabilityInspectionFlags.Default, typeStack );
					if( result.IsMutable ) {
						return result;
					}
				}

				// We descend into the base class last
				var baseResult = InspectBaseType( type, rootAssembly, typeStack );
				if ( baseResult.IsMutable ) {
					return baseResult;
				}

				return MutabilityInspectionResult.NotMutable();

			} finally {
				typeStack.Remove( type );
			}
		}

		private MutabilityInspectionResult InspectMemberRecursive(
			ISymbol symbol,
			IAssemblySymbol rootAssembly,
			MutabilityInspectionFlags flags,
			HashSet<ITypeSymbol> typeStack
		) {

			MutabilityInspectionResult result;

			switch( symbol.Kind ) {

				case SymbolKind.Property:

					var prop = (IPropertySymbol)symbol;

					var sourceTree = prop.DeclaringSyntaxReferences.FirstOrDefault();
					var declarationSyntax = sourceTree?.GetSyntax() as PropertyDeclarationSyntax;
					if( declarationSyntax != null && !declarationSyntax.IsAutoImplemented() ) {
						// non-auto properties never hold state themselves
						return MutabilityInspectionResult.NotMutable();
					}

					if( !prop.IsReadOnly ) {
						return MutabilityInspectionResult.MutableProperty( prop, MutabilityCause.IsNotReadonly );
					}

					result = InspectTypeRecursive( prop.Type, rootAssembly, flags, typeStack );
					if( result.IsMutable ) {
						return result.WithPrefixedMember( prop.Name );
					}

					return MutabilityInspectionResult.NotMutable();

				case SymbolKind.Field:

					var field = (IFieldSymbol)symbol;

					if( !field.IsReadOnly ) {
						return MutabilityInspectionResult.MutableField( field, MutabilityCause.IsNotReadonly );
					}

					result = InspectTypeRecursive( field.Type, rootAssembly, flags, typeStack );
					if( result.IsMutable ) {
						return result.WithPrefixedMember( field.Name );
					}

					return MutabilityInspectionResult.NotMutable();

				case SymbolKind.Method:
				case SymbolKind.NamedType:
					// ignore these symbols, because they do not contribute to immutability
					return MutabilityInspectionResult.NotMutable();

				default:
					// we've got a member (event, etc.) that we can't currently be smart about, so fail
					return MutabilityInspectionResult.PotentiallyMutableMember( symbol );
			}
		}

		public bool IsTypeMarkedImmutable( ITypeSymbol symbol ) {
			if( MarkedImmutableTypes.Contains( symbol.GetFullTypeName() ) ) {
				return true;
			}
			if( symbol.GetAttributes().Any( a => a.AttributeClass.Name == "Immutable" ) ) {
				return true;
			}
			if( symbol.Interfaces.Any( IsTypeMarkedImmutable ) ) {
				return true;
			}
			if( symbol.BaseType != null && IsTypeMarkedImmutable( symbol.BaseType ) ) {
				return true;
			}
			return false;
		}
	}
}
