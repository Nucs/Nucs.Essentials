using System;

namespace Nucs.Extensions {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class OptionalAttribute : Attribute { }
}