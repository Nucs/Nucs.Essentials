using System;

namespace Nucs.Extensions {
    /// <summary>
    ///     This Property/Field is required, or.. the strings specified
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class RequiredOrAttribute : Attribute {
        public RequiredOrAttribute(string type) { }
        public RequiredOrAttribute(string type, string type1) { }
        public RequiredOrAttribute(string type, string type1, string type2) { }
    }
}