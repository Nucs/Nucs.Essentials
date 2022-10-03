using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nucs.Collections.Structs;

namespace Nucs.Configuration {
    /// <summary>
    ///     An xml list returning a list of key + attributes.keys + attributes.values. 
    /// </summary>
    public sealed class PendingXmlDictionary : IRefersXmlKey, IRefersXmlConfig {
        /// <summary>
        ///     Get or create the list.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public List<(string Key, StructList<(string AttrName, string AttrValue)> Attributes)> Computed => _list ?? Compute();

        private List<(string Key, StructList<(string AttrName, string AttrValue)> Attributes)>? _list;
        private XmlConfig _config;

        /// <summary>
        ///     The config this list exists in.
        /// </summary>
        public XmlConfig Config {
            get => _config;
            set {
                _config = value;
                _list = null; //recompute later
            }
        }

        public string Key { get; set; }

        public object CloneSub(XmlConfig cfg, string key) {
            //if (key.Equals(Key, StringComparison.Ordinal))
            //    return new PendingXmlList(cfg, Key);

            if (Key.StartsWith(key) && Key.Length > key.Length && Key[key.Length] == '.') 
                return new PendingXmlDictionary(cfg, Key.Substring(key.Length + 1));
            else if (Key.StartsWith(cfg.ClassedParent) && Key.Length > cfg.ClassedParent.Length && Key[cfg.ClassedParent.Length] == '.') 
                return new PendingXmlDictionary(cfg, Key.Substring(cfg.ClassedParent.Length + 1));
            else if (Key.StartsWith(cfg.Inheriting) && Key.Length > cfg.Inheriting.Length && Key[cfg.Inheriting.Length] == '.') 
                return new PendingXmlDictionary(cfg, Key.Substring(cfg.Inheriting.Length + 1));
            else 
                throw new ConfigurationException($"Unable to subkey \"{Key}\" with \"{key}\"");
        }

        public PendingXmlDictionary(XmlConfig config, string key) {
            Config = config;
            Key = key;
        }

        private List<(string Key, StructList<(string AttrName, string AttrValue)> Attributes)> Compute() {
            List<(string Key, StructList<(string AttrName, string AttrValue)> Attributes)>? list = _list;
            if (list != null)
                return list;
            lock (this) {
                list = _list;
                if (list != null)
                    return list;

                list = new List<(string Key, StructList<(string AttrName, string AttrValue)> Attributes)>(8);
                foreach (var entry in Config.Entries) {
                    if (entry.Key.StartsWith(Key) && entry.Key.Length > Key.Length && entry.Key[Key.Length] == '.') {
                        //it is a sub to given key
                        var klen = entry.Key.LastIndexOf('.');
                        var attrKey = entry.Key.Substring(0, klen == -1 ? entry.Key.Length : klen);
                        bool noAttr = attrKey == Key;
                        int i;
                        for (i = 0; i < list.Count; i++) {
                            if (list[i].Key == attrKey)
                                goto found;
                        }

                        //not found
                        noAttr |= list.Any(f => f.Key == attrKey);
                        list.Add((noAttr ? entry.Key : attrKey, new StructList<(string AttrName, string AttrValue)>(4)));
                        i = list.Count - 1;

                        found:
                        if (noAttr)
                            continue;
                        var (key, attributes) = list[i];
                        attributes.Add((entry.Key.Substring(attrKey.Length + 1), entry.Value.Value)!);

                        list[i] = (Key: key, Attributes: attributes);
                    }
                }

                list.Capacity = list.Count; //trim end.
                _list = list;
                return list;
            }
        }
    }
}