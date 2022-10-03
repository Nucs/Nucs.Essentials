using System;

namespace Nucs.DependencyInjection {
    /// <summary>
    ///     This type will be mapped for DI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class BindAttribute : Attribute {
        public BindAttribute() { }
    }

    /// <summary>
    ///     This type and all the types inherited will be mapped for DI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    public class BindInheritedAttribute : BindAttribute {
        public BindInheritedAttribute() { }
    }

    /// <summary>
    ///     This module will be mapped for DI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ModuleAttribute : BindAttribute {
        public ModuleAttribute() { }
    }

    /// <summary>
    ///     This module config will be mapped for DI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ModuleConfigAttribute : BindAttribute {
        public ModuleConfigAttribute() { }
    }
}