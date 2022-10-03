using System.Collections.Generic;
using System.Diagnostics;
using Nucs.Collections.Structs;

namespace Nucs.Configuration {
    public readonly partial struct ConfigNode {
        public readonly string Key;
        public readonly object? ObjectValue;
        public string? Value => ObjectValue as string;
        public List<(string Name, string Value)>? NodeList => ObjectValue as List<(string Name, string Value)>;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public List<(string Key, StructList<(string AttrName, string AttrValue)> Attributes)>? XmlList => _xmlListChild?.Computed;

        private PendingXmlDictionary? _xmlListChild => ObjectValue as PendingXmlDictionary;

        public bool IsEmpty => ObjectValue == null;

        public ConfigNode(string key, object? value) {
            Key = key != null ? string.Intern(key) : null;
            ObjectValue = value is string val ? string.Intern(val) : value;
        }

        public ConfigNode(ConfigNode other) {
            Key = other.Key;
            ObjectValue = other.Value;
        }

        public ConfigNode(ConfigNode other, string key) {
            Key = string.Intern(key);
            ObjectValue = other.ObjectValue;
        }
    }
}