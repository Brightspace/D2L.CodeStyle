using System;

// ReSharper disable once CheckNamespace
namespace D2L.CodeStyle.Annotations {
    public static partial class Objects {
        /// <summary>
        /// Indicates that a static variable is safe in a multi-tenant process
        /// </summary>
        [AttributeUsage( validOn: AttributeTargets.Class )]
        public sealed class Immutable : Attribute {

        }
    }
}
