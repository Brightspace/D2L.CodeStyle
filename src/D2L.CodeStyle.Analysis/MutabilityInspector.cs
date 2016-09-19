using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System.Linq;
using D2L.CodeStyle.Annotations;

namespace D2L.CodeStyle.Analysis {

	public sealed class MutabilityInspector {

		/// <summary>
		/// A list of known non-valuetype immutable types
		/// </summary>
		private static readonly ImmutableHashSet<string> KnownImmutableTypes = new HashSet<string> {
			"D2L.Core.Images.IImage",
			"D2L.Core.JobManagement.Legacy.JobStatusType",
			"D2L.LE.Content.Data.OverdueSortOrderField",
			"D2L.LE.CopyCourse.Domain.Logging.Terms.BasicLogTerm",
			"D2L.LE.CopyCourse.Domain.Logging.Terms.SingleParameterLogTerm",
			"D2L.LP.Diagnostics.Performance.Metric",
			"D2L.LP.ImageProcessing.IImage",
			"D2L.LP.ImageProcessing.ImageTerm",
			"D2L.LP.Logging.ILogger",
			"D2L.LP.OrgUnits.OrgId",
			"D2L.LP.OrgUnits.OrgUnitId",
			"D2L.LP.Security.Authorization.Roles.RoleId",
			"D2L.LP.Text.IText",
			"D2L.LP.TextProcessing.IText",
			"D2L.LP.TextProcessing.LangTerm",
			"D2L.LP.Users.UserId",
			"D2L.LP.Web.Http.ILocation",
			"D2L.LP.Web.Mvc.RouteLocation",
			"D2L.LP.Web.Routing.IRoutePattern",
			"D2L.LP.Web.Routing.RoutePattern",
			"D2L.LP.Web.UI.Html.AbsoluteHtmlId",
			"D2L.LP.Web.UI.Html.IHtmlId",
			"D2L.LP.Web.UI.Html.RelativeHtmlId",
			"D2L.LP.Web.UI.Html.Style.Background.IBackgroundStyle",
			"D2L.LP.Web.UI.Html.Style.Borders.IBorderStyle",
			"D2L.LP.Web.UI.Html.Style.Colour.IColour",
			"D2L.LP.Web.UI.Html.Style.Length.ILength",
			"D2L.LP.Web.UI.Html.Style.Spacing.ISpacing",
			"D2L.LP.WebExtensibility.Versioning.RestWebServiceVersion",
			"D2L.Services.Monitoring.HealthCheck.Contract.IStatus",
			"D2L.UtcDateTime",
			"D2L.UtcTimezone",
			"System.DateTime",
			"System.Guid",
			"System.Lazy",
			"System.Reflection.MethodInfo",
			"System.Text.RegularExpressions.Regex",
			"System.TimeSpan",
			"System.Type",
			"System.Uri",
			"System.String",
		}.ToImmutableHashSet();

        private static readonly ImmutableHashSet<string> ImmutableCollectionTypes = new HashSet<string> {
            "System.Collections.Immutable.ImmutableArray",
            "System.Collections.Immutable.ImmutableDictionary",
            "System.Collections.Immutable.ImmutableHashSet",
            "System.Collections.Immutable.ImmutableList",
            "System.Collections.Immutable.ImmutableQueue",
            "System.Collections.Immutable.ImmutableSortedDictionary",
            "System.Collections.Immutable.ImmutableSortedSet",
            "System.Collections.Immutable.ImmutableStack",
            "System.Collections.Generic.IReadOnlyList",
            "System.Collections.Generic.IEnumerable",
        }.ToImmutableHashSet();

        /// <summary>
        /// Determine if a given type is mutable.
        /// </summary>
        /// <param name="type">The type to determine mutability for.</param>
        /// <returns>Whether the type is mutable.</returns>
        public bool IsTypeMutable(
			ITypeSymbol type
		) {
			if( type.IsValueType ) {
				return false;
			}

			if( type.TypeKind == TypeKind.Array ) {
				return true;
			}

			if( KnownImmutableTypes.Contains( type.GetFullTypeName() ) ) {
				return false;
			}

            if( IsTypeMarkedImmutable( type ) ) {
                return false;
            }

            if( ImmutableCollectionTypes.Contains( type.GetFullTypeName() ) ) {
                var elementType = type.GetGenericArgumentOrDefault();
                if( elementType == null ) {
                    return true;
                }
                return IsTypeMutable( elementType );
            }

			foreach( var member in type.GetMembers() ) {
				if( member is IPropertySymbol ) {
					var prop = member as IPropertySymbol;
                    if( IsPropertyMutable( prop ) ) {
                        return true;
                    }
                    if ( !IsTypeMarkedImmutable( prop.Type ) && IsTypeMutable( prop.Type ) ) {
						return true;
					}
					continue;
				}
				if( member is IFieldSymbol ) {
					var field = member as IFieldSymbol;
                    if( IsFieldMutable( field ) ) {
                        return true;
                    }
                    if ( !IsTypeMarkedImmutable( field.Type ) && IsTypeMutable( field.Type ) ) {
						return true;
					}
					continue;
				}
				if( member is IMethodSymbol ) {
					var method = member as IMethodSymbol;
					if( method.MethodKind == MethodKind.Constructor ) {
						// constructors are mutating by definition
						continue;
					}

					// we can't yet be smarter by methods being "pure"
					return true;
				}

				// we've got a member (event, etc.) that we can't currently be smart about, so fail
				return true;
			}

			return false;
		}

		public bool IsTypeMarkedImmutable( ITypeSymbol symbol ) {
			if( symbol.GetAttributes().Any( a => a.AttributeClass.Name == nameof( Objects.Immutable ) ) ) {
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

		/// <summary>
		/// Determine if a property is mutable.
		/// This does not check if the type of the property is also mutable; use <see cref="IsTypeMutable"/> for that.
		/// </summary>
		/// <param name="prop">The property to check for mutability.</param>
		/// <returns>Determines whether the property is mutable.</returns>
		public bool IsPropertyMutable( IPropertySymbol prop ) {
			if( prop.IsReadOnly ) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Determine if a field is mutable.
		/// This does not check if the type of the field is also mutable; use <see cref="IsTypeMutable"/> for that.
		/// </summary>
		/// <param name="field">The field to check for mutability.</param>
		/// <returns>Determines whether the property is mutable.</returns>
		public bool IsFieldMutable( IFieldSymbol field ) {
			if( field.IsReadOnly ) {
				return false;
			}
			return true;
		}

	}
}
