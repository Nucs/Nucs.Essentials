using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using EnumsNET;
using Nucs.Collections;
using Nucs.Collections.Structs;

namespace Nucs.Configuration {
    // A KeyValuePair holds a key and a value from a dictionary.
    // It is used by the IEnumerable<T> implementation for both IDictionary<TKey, TValue>
    // and IReadOnlyDictionary<TKey, TValue>.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct ConfigEntry {
        public readonly string Key;
        public readonly ConfigNode Value;

        public ConfigEntry(string key, ConfigNode value) {
            Key = key;
            Value = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out string key, out ConfigNode value) {
            key = Key;
            value = Value;
        }
    }

    /// <summary>
    ///     Class for reading parameters from xml files
    /// </summary>
    public partial class XmlConfig {
        private static List<(string, string)> s_emptyList = new List<(string, string)>(0);

        /// <summary>
        ///     A key representing this XmlConfig
        /// </summary>
        public string FileKey { get; }

        /// <summary>
        ///     A path to xml file containing this config
        /// </summary>
        public string File { get; }

        /// <summary>
        ///     The class this xml inherits all its properties from.
        /// </summary>
        public string? Inheriting { get; internal set; }

        /// <summary>
        ///     The class this xml inherits all its properties from.
        /// </summary>
        public string? ClassedParent { get; internal set; }

        /// <summary>
        ///     If true, this config is a child element in the xml config (subconfig).
        /// </summary>
        public bool IsSubConfig { get; }

        /// <summary>
        ///     The Keys from <see cref="Entries"/>.Keys
        /// </summary>
        public Enumerable<Dictionary<string, ConfigNode>.KeyCollection.Enumerator, string> Keys => SelectKeys();

        /// <summary>
        ///     The Keys from <see cref="Entries"/>.Keys
        /// </summary>
        public Enumerable<Dictionary<string, ConfigNode>.ValueCollection.Enumerator, ConfigNode> Values => SelectValues();

        /// <summary>
        ///     A key to ConfigNode
        /// </summary>
        public Dictionary<string, ConfigNode> Entries { get; }

        /// <summary>
        ///     Finds key in <see cref="Entries"/>. Otherwise throws <see cref="KeyNotFoundException"/>
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public ConfigNode this[string key] => Entries[key];

        /// <summary>
        ///     Creates a deep copy of this config object.
        /// </summary>
        public XmlConfig Copy() {
            return Copy(new Dictionary<string, ConfigNode>(Entries));
        }

        /// <summary>
        ///     Creates a deep copy of this config object.
        /// </summary>
        public XmlConfig Copy(Dictionary<string, ConfigNode> externalEntries) {
            return new XmlConfig(this.FileKey, File, IsSubConfig, externalEntries) { Inheriting = Inheriting, ClassedParent = ClassedParent };
        }

        public XmlConfig(string fileKey, string file, bool isSubConfig, Dictionary<string, ConfigNode> entries) {
            FileKey = fileKey;
            File = file;
            IsSubConfig = isSubConfig;
            Entries = entries;
        }

        public XmlConfig(FileInfo file) : this(file.FullName, Path.GetFileNameWithoutExtension(file.FullName), SystemHelper.ReadAllText(file.FullName)) { }

        public XmlConfig(string filePath, string? fileKey, string xmlText) {
            if (fileKey != null)
                FileKey = string.Intern(fileKey);
            File = string.Intern(filePath);
            if (string.IsNullOrEmpty(xmlText)) {
                Entries = new Dictionary<string, ConfigNode>(0);
                return;
            }

            ConfigNode configNode;
            XDocument root;
            try {
                root = XDocument.Parse(xmlText);
            } catch (XmlException e) {
                SystemHelper.Logger?.Error($"Unable to parse xml at '{filePath}' for fileKey '{FileKey}'. Error: \n{e}");
                throw;
            }

            //figure root element
            XElement? rootNode = null;
            foreach (var node in root.Nodes()) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                rootNode = (XElement?) node;
                if (rootNode?.Attribute("inherits") != null || rootNode?.Attribute("parent") != null) {
                    //it inherits, filekey must match first element
                    if (!rootNode.Name.ToString().Equals(fileKey))
                        throw new ConfigurationException($"The file name must match root (first) element's name in the xml. FileName: {fileKey}, Root element name: {rootNode.Name.ToString()}.\nFile Path: {filePath}");
                }

                break;
            }

            if (FileKey == null)
                FileKey = rootNode?.Name.ToString() ?? throw new ConfigurationException("Could not resolve root element and no FileKey specified");

            if (rootNode?.HasAttributes == true) {
                Inheriting = rootNode.Attribute("inherits")?.Value;
            }

            var lists = new Dictionary<string, List<(string Name, string Value)>>();
            var nodes = new Dictionary<string, ConfigNode>(32);
            var keys = new StructList<string>(16);
            List<string>? recentXmlList = new List<string>(32);
            foreach (XElement node in root.Descendants()) {
                object? value = null;
                XAttribute? attr = null;
                if (node.HasAttributes) {
                    if (AbstractInfrastructure.IsProduction) {
                        var val = (attr = node.Attribute("prod"))?.Value;
                        if (val != null) {
                            value = val;
                            goto _found;
                        }
                    }

                    if (AbstractInfrastructure.IsResearch) {
                        var val = (attr = node.Attribute("research"))?.Value;
                        if (val != null) {
                            value = val;
                            goto _found;
                        }
                    }

                    attr = node.Attribute("value");
                    //try for file
                    if (attr == null) {
                        attr = node.Attribute("file");
                        if (attr != null) {
                            var task = Task.Factory.StartNew(node => {
                                foreach (var fallbackAttr in ((XElement) node!).Attributes()) {
                                    if (!fallbackAttr.Name.LocalName.StartsWith("file"))
                                        continue;
                                    if (AbstractInfrastructure.CachedFile.Exists(fallbackAttr.Value))
                                        return fallbackAttr.Value;
                                }

                                return null;
                            }, node);
                            value = new Func<object>(() => task.GetAwaiter().GetResult()!);
                        }
                    }

                    //try for directory
                    if (attr == null) {
                        attr = node.Attribute("directory");
                        if (attr != null) {
                            var task = Task.Factory.StartNew(node => {
                                foreach (var fallbackAttr in ((XElement) node!).Attributes()) {
                                    if (!fallbackAttr.Name.LocalName.StartsWith("directory"))
                                        continue;
                                    if (AbstractInfrastructure.CachedFile.ExistsDirectory(fallbackAttr.Value))
                                        return fallbackAttr.Value;
                                }

                                return null;
                            }, node);
                            value = new Func<object>(() => task.GetAwaiter().GetResult()!);
                        }
                    }

                    //resolve value or alias
                    if (value == null) {
                        if (attr != null) {
                            value = attr.Value;
                        } else if ((attr = node.Attribute("alias")) != null) {
                            value = $"alias::{attr.Value}";
                        }
                    }
                }

                _found:
                //compute keys
                keys.Clear();
                var target = node;
                do {
                    keys.Add(target.Name.ToString());
                } while ((target = target.Parent) != null);

                Array.Reverse(keys.InternalArray, 0, keys.Count);

                switch (node.Attribute("type")?.Value) {
                    case null: break;
                    case "list": {
                        var key = string.Join(".", keys.InternalArray, 0, keys.Count);
                        {
                            var list = new List<(string Name, string Value)>(8);
                            if (value != null)
                                list.Add((key, (string) value));
                            configNode = new ConfigNode(key: key, value: list); //we all keys except for the last one f
                        }

                        nodes.Add(configNode.Key, configNode);
                        continue;
                    }
                    case "xmllist": {
                        var key = string.Join(".", keys.InternalArray, 0, keys.Count);
                        if (!recentXmlList.Contains(key + "."))
                            recentXmlList.Add(key + ".");
                        configNode = new ConfigNode(key: key, value: value = new PendingXmlDictionary(this, key)); //we all keys except for the last one f
                        nodes.Add(configNode.Key, configNode);
                        continue;
                    }
                }

                var path = string.Join(".", keys.InternalArray, 0, keys.Count);

                if (node.Parent != null && node.Parent.HasAttributes && node.Parent.Attribute("type")?.Value == "list") {
                    var parentKey = string.Join(".", keys.InternalArray, 0, keys.Count - 1);
                    if (!lists.TryGetValue(parentKey, out var list))
                        lists[parentKey] = list = nodes[parentKey].NodeList;

                    value ??= attr?.Value ?? node.FirstAttribute?.Value;
                    list.Add((string.Intern(path), (string) value));
                    continue;
                }

                if (recentXmlList.Count > 0 && node.Parent != null) {
                    bool found = false;
                    for (int i = 0; i < recentXmlList.Count; i++) {
                        if (path.StartsWith(recentXmlList[i])) {
                            var attrs = new StructList<XAttribute>(node.Attributes().ToArray());
                            if (attrs.Count == 0) {
                                path += ".value";
                                nodes.Add(path, new ConfigNode(path, string.Empty));
                            } else {
                                foreach (var nodeAttrs in attrs.Select(attr => (AttrName: attr.Name.ToString(), AttrValue: attr.Value))) {
                                    var attrKey = path + "." + nodeAttrs.AttrName;
                                    nodes.Add(attrKey, new ConfigNode(attrKey, nodeAttrs.AttrValue));
                                }
                            }

                            found = true;
                        }
                    }

                    if (found) {
                        continue;
                    }

                    configNode = new ConfigNode(
                        key: path, //we all keys except for the last one f
                        value: value);

                    if (configNode.IsEmpty)
                        continue;

                    nodes.Add(configNode.Key, configNode);
                    continue;
                }

                if (value == null) //no value and not related to nested xmllist
                    continue;

                configNode = new ConfigNode(
                    key: path, //we all keys except for the last one f
                    value: value);

                if (configNode.IsEmpty)
                    continue;

                nodes.Add(configNode.Key, configNode);
            }

            Entries = nodes;
        }

        public XmlConfig(string fileKey, string file, Dictionary<string, ConfigNode> entries, bool isSubConfig = false) {
            FileKey = string.Intern(fileKey);
            File = string.Intern(file);
            Entries = entries;
            IsSubConfig = isSubConfig;
        }

        public XmlConfig SubConfig(string key, string? newFileKey = null) {
            key = string.Intern(key);
            if (newFileKey != null)
                newFileKey = string.Intern(newFileKey);

            var startsWith = newFileKey ?? key;
            Dictionary<string, ConfigNode> nodes = new Dictionary<string, ConfigNode>(16);
            foreach (var kv in Entries) {
                if (kv.Key.StartsWith(startsWith) && kv.Key.Length > startsWith.Length && kv.Key[startsWith.Length] == '.') {
                    // ReSharper disable once ReplaceSubstringWithRangeIndexer
                    string subKey = string.Intern(kv.Key.Substring(startsWith.Length + 1));
                    var kvValue = kv.Value.ObjectValue;
                    //handle list
                    if (kvValue is List<(string Name, string Value)> srcListArr) {
                        var copy = new List<(string Name, string Value)>(srcListArr.Count);
                        var len = srcListArr.Count;
                        var subStringLen = startsWith.Length + 1 + subKey.Length + 1;
                        for (var i = 0; i < len; i++) {
                            var item = srcListArr[i];
                            copy.Add((item.Name.Length == subStringLen - 1 ? string.Empty : item.Name.Substring(subStringLen), item.Value));
                        }

                        kvValue = copy;
                    }

                    nodes.Add(subKey, new ConfigNode(subKey, kvValue));
                }
            }

            return new XmlConfig(newFileKey ?? FileKey, File, nodes, true) { Inheriting = Inheriting, ClassedParent = ClassedParent };
        }

        public XmlConfig SubConfig(Func<KeyValuePair<string, ConfigNode>, bool> filter, Func<string, string>? renameKey = null, string? newFileKey = null) {
            if (newFileKey != null)
                newFileKey = string.Intern(newFileKey);
            var nodes = new Dictionary<string, ConfigNode>(16);
            if (renameKey == null) {
                foreach (var kv in Entries) {
                    if (filter(kv)) {
                        nodes.Add(kv.Key, kv.Value);
                    }
                }
            } else {
                foreach (var kv in Entries) {
                    if (filter(kv)) {
                        nodes.Add(renameKey(kv.Key), kv.Value);
                    }
                }
            }

            return new XmlConfig(newFileKey ?? FileKey, File, nodes, true) { Inheriting = Inheriting, ClassedParent = ClassedParent };
        }

        public FileInfo GetFile(string key) {
            if (TryGetValue(key, out var path)) {
                if (AbstractInfrastructure.CachedFile.Exists(path))
                    return new FileInfo(path);
                else {
                    //check for other attributes
                    if (!TryGetValue(key + "-Fallback", out var fallbackPath) || !AbstractInfrastructure.CachedFile.Exists(fallbackPath)) {
                        if (!TryGetValue(key + "-FallbackDrive", out var fallbackPathDrive) || !AbstractInfrastructure.CachedFile.Exists(fallbackPathDrive + Path.GetFullPath(path).Substring(1))) {
                            throw new ConfigurationException($"Configuration '{key}' not found because '{path};{fallbackPath}' both do not exist.");
                        }

                        return new FileInfo(fallbackPathDrive + Path.GetFullPath(path).Substring(1));
                    }

                    return new FileInfo(fallbackPath);
                }
            } else {
                throw new ConfigurationException($"Configuration '{key}' not found.");
            }
        }

        public DirectoryInfo GetDirectory(string key) {
            if (TryGetValue(key, out var path)) {
                if (AbstractInfrastructure.CachedFile.ExistsDirectory(path))
                    return new DirectoryInfo(path);
                else {
                    if (!TryGetValue(key + "-Fallback", out var fallbackPath) || !AbstractInfrastructure.CachedFile.ExistsDirectory(fallbackPath)) {
                        if (!TryGetValue(key + "-FallbackDrive", out var fallbackPathDrive) || !AbstractInfrastructure.CachedFile.ExistsDirectory(fallbackPathDrive + Path.GetFullPath(path).Substring(1))) {
                            throw new ConfigurationException($"Configuration '{key}' not found because '{path};{fallbackPath}' both do not exist.");
                        }

                        return new DirectoryInfo(fallbackPathDrive + Path.GetFullPath(path).Substring(1));
                    }

                    return new DirectoryInfo(fallbackPath);
                }
            } else {
                throw new ConfigurationException($"Configuration '{key}' not found.");
            }
        }

        public DirectoryInfo GetOrCreateDirectory(string key) {
            if (TryGetValue(key, out var path)) {
                if (Directory.Exists(path))
                    return new DirectoryInfo(path);
                else {
                    if (!TryGetValue(key + "-Fallback", out var fallbackPath) || !Directory.Exists(fallbackPath)) {
                        Directory.CreateDirectory(path);
                        return new DirectoryInfo(path);
                    }

                    return new DirectoryInfo(fallbackPath);
                }
            } else {
                throw new ConfigurationException($"Configuration '{key}' not found.");
            }
        }

        public DirectoryInfo GetOptionalAndTryCreateDirectory(string key, DirectoryInfo @default = null) {
            if (TryGetValue(key, out var path)) {
                if (Directory.Exists(path))
                    return new DirectoryInfo(path);
                else {
                    if (!TryGetValue(key + "-Fallback", out var fallbackPath) || !Directory.Exists(fallbackPath)) {
                        try {
                            Directory.CreateDirectory(path);
                        } catch (Exception) {
                            return @default;
                        }

                        return new DirectoryInfo(path);
                    }

                    return new DirectoryInfo(fallbackPath);
                }
            } else {
                return @default;
            }
        }

        public string GetString(string key) {
            return GetValue(key);
        }

        public List<T> GetListOf<T>(string key) {
            if (!TryGetNode(key, out var value))
                return new List<T>(0);
            return value.GetListOf<T>();
        }

        public T[] GetArrayOf<T>(string key) {
            if (!TryGetNode(key, out var value))
                return Array.Empty<T>();
            return value.GetArrayOf<T>();
        }

        public IList GetListOf(Type underlyingType, string key) {
            TryGetNode(key, out var value); //GetListOf handles default return
            return value.GetListOf(underlyingType);
        }

        public Array GetArrayOf(Type underlyingType, string key) {
            TryGetNode(key, out var value); //GetArrayOf handles default return
            return value.GetArrayOf(underlyingType);
        }

        public List<List<T>> GetListOfLists<T>(string key, char listDelimiter = '~', char delimiter = ',') {
            if (!TryGetNode(key, out var value))
                return new List<List<T>>(0);
            return value.GetListOfLists<T>();
        }

        public T[][] GetArrayOfArrays<T>(string key, char listDelimiter = '~', char delimiter = ',') {
            if (!TryGetNode(key, out var value))
                return Array.Empty<T[]>();
            return value.GetArrayOfArrays<T>(listDelimiter, delimiter);
        }

        public PriceBucket<TBucket, TValue> GetBucket<TBucket, TValue>(string key)
            where TBucket : unmanaged, IComparable<TBucket> {
            return new PriceBucket<TBucket, TValue>(GetArrayOf<TBucket>(key + ".Buckets"), GetArrayOf<TValue>(key + ".Values"));
        }

        public PriceBucket<TBucket, TValue>? GetOptionalBucket<TBucket, TValue>(string key, Func<PriceBucket<TBucket, TValue>>? factory = null)
            where TBucket : unmanaged, IComparable<TBucket> {
            if (!TryGetString(key, out _))
                return factory?.Invoke();

            return new PriceBucket<TBucket, TValue>(GetArrayOf<TBucket>(key + ".Buckets"), GetArrayOf<TValue>(key + ".Values"));
        }

        public DateTime GetDateTime(string key) {
            return ConfigParsers.DateTime(GetValue(key));
        }

        public DateTime? GetOptionalDateTime(string key, DateTime? @default = null) {
            if (!TryGetValue(key, out var value))
                return @default;

            if (string.IsNullOrEmpty(value))
                return @default;

            return ConfigParsers.Safe.DateTime(value) ?? @default;
        }

        public DateTime GetOptionalDateTime(string key, DateTime @default = default) {
            if (!TryGetValue(key, out var value))
                return @default;

            if (string.IsNullOrEmpty(value))
                return @default;

            return ConfigParsers.Safe.DateTime(value) ?? @default;
        }

        public TimeSpan GetTimeSpan(string key) {
            return ConfigParsers.TimeSpan(GetValue(key));
        }

        public TimeSpan? GetOptionalTimeSpan(string key, TimeSpan? @default = null) {
            try {
                return ConfigParsers.Safe.TimeSpan(GetValue(key));
            } catch (ConfigurationException) {
                return @default;
            }
        }

        public TimeSpan GetOptionalTimeSpan(string key, TimeSpan @default = default) {
            try {
                return ConfigParsers.Safe.TimeSpan(GetValue(key)) ?? @default;
            } catch (ConfigurationException) {
                return @default;
            }
        }

        public double GetDouble(string key) {
            var val = GetValue(key);
            if (val[^1] == '%')
                return double.Parse(val.AsSpan().TrimEnd('%'), NumberStyles.Float | NumberStyles.AllowThousands) / 100d;

            return double.Parse(val, NumberStyles.Float | NumberStyles.AllowThousands);
        }

        public float GetFloat(string key) {
            return float.Parse(GetValue(key), AbstractInfrastructure.DefaultCulture);
        }

        public float GetOptionalFloat(string key, float @default = default) {
            if (float.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public float? GetOptionalFloat(string key, float? @default = default) {
            if (float.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public float GetDictionary(string key) {
            return float.Parse(GetValue(key), AbstractInfrastructure.DefaultCulture);
        }

        public bool GetBoolean(string key) {
            return bool.Parse(GetValue(key));
        }

        public long GetLong(string key) {
            return long.Parse(GetValue(key), AbstractInfrastructure.DefaultCulture);
        }

        public int GetInt(string key) {
            return int.Parse(GetValue(key), AbstractInfrastructure.DefaultCulture);
        }

        public double GetOptionalDouble(string key, double @default = default) {
            if (double.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public double? GetOptionalDouble(string key, double? @default = default) {
            if (double.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public int GetOptionalInt(string key, int @default = default) {
            if (int.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public List<(string Name, string Value)> GetList(string key) {
            if (!TryGetList(key, out var list))
                throw new ConfigurationException($"Configuration list at path '{key}' was not found.");

            return list;
        }

        public List<(string XmlKey, StructList<(string AttrName, string AttrValue)> Attributes)> GetXmlList(string key) {
            if (!TryGetXmlList(key, out var list))
                throw new ConfigurationException($"Configuration list at path '{key}' was not found.");

            return list;
        }

        public T GetEnum<T>(string key, bool ignoreCase = true) where T : Enum {
            return (T) Enum.Parse(typeof(T), GetValue(key), ignoreCase: ignoreCase);
        }

        public T? GetOptionalEnum<T>(string key, T? @default = default, bool ignoreCase = true) where T : Enum {
            if (TryGetValue(key, out var value) && Enums.TryParseUnsafe<T>(value, ignoreCase: ignoreCase, out var @result))
                return @result;

            return @default;
        }

        public bool? GetOptionalBoolean(string key, bool? @default = null) {
            if (bool.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public bool GetOptionalBoolean(string key, bool @default = default) {
            if (bool.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public long GetOptionalLong(string key, long @default = default) {
            if (long.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public long? GetOptionalLong(string key, long? @default = default) {
            if (long.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public int? GetOptionalInt(string key, int? @default = default) {
            if (int.TryParse(GetValue(key), out var value))
                return value;
            return @default;
        }

        public string? GetOptionalString(string key, string @default = null) {
            TryGetValue(key, out var value, @default);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetValue(string key) {
            if (!TryGetValue(key, out var value))
                throw new ConfigurationException($"Configuration '{key}' not found at {File}.");

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetValue(string key, out string value, string @default = default) {
            /*if (key.StartsWith(FileKey + "."))
                key = key.Substring(FileKey.Length + 1);*/
            foreach (var kv in Entries) {
                var entry = kv.Value;
                if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)) {
                    value = entry.Value;
                    if (value != null && value.StartsWith("alias::", StringComparison.OrdinalIgnoreCase)) {
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (AbstractInfrastructure.ConfigProvider is null)
                            return false;
                        throw new ConfigurationException("Alias not supported");
                    }

                    return true;
                }
            }

            value = @default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNode(string key, out ConfigNode outEntry, ConfigNode? @default = default) {
            foreach (var kv in Entries) {
                var entry = kv.Value;
                if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)) {
                    outEntry = entry;
                    if (outEntry.Value != null && outEntry.Value.StartsWith("alias::", StringComparison.OrdinalIgnoreCase))
                        throw new ConfigurationException("Alias not supported");

                    return true;
                }
            }

            outEntry = @default ?? default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetList(string key, out List<(string Name, string Value)> list, List<(string Name, string Value)> @default = default) {
            /*if (key.StartsWith(FileKey + "."))
                key = key.Substring(FileKey.Length + 1);*/
            foreach (var kv in Entries) {
                var entry = kv.Value;
                if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)) {
                    var value = entry.Value;
                    if (value != null && value.StartsWith("alias::", StringComparison.OrdinalIgnoreCase))
                        throw new ConfigurationException("Alias not supported");

                    list = entry.NodeList ?? throw new ConfigurationException($"Config node '{key}' has no list.");
                    return true;
                }
            }

            list = @default;
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetXmlList(string key, out List<(string Name, StructList<(string AttrName, string AttrValue)> Attributes)> list, List<(string Key, StructList<(string AttrName, string AttrValue)> Attributes)>? @default = default) {
            /*if (key.StartsWith(FileKey + "."))
                key = key.Substring(FileKey.Length + 1);*/
            foreach (KeyValuePair<string, ConfigNode> kv in Entries) {
                ConfigNode entry = kv.Value;
                if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)) {
                    string? value = entry.Value;
                    if (value != null && value.StartsWith("alias::", StringComparison.OrdinalIgnoreCase))
                        throw new ConfigurationException("Alias not supported");
                    list = entry.XmlList ?? throw new ConfigurationException($"Config node '{key}' has no list.");
                    return true;
                }
            }

            list = @default;
            return false;
        }

        public bool TryGetString(string key, out string value, string @default = null) {
            return TryGetValue(key, out value, @default);
        }

        /// <summary>
        ///     Will use current XmlConfig to override any xmls matching keys found on <see cref="baseCfg"/>.
        ///     If no changes were applied, the original is returned.
        /// </summary>
        /// <param name="baseCfg">The base cfg that has this XML inherited</param>
        /// <returns>A copy of baseCfg that has the values of the matching keys here.</returns>
        public XmlConfig Overrides(XmlConfig existingCfg) {
            try {
                XmlConfig? existingCopy = null;
                foreach (var overriding in Entries) {
                    foreach (var existingEntry in existingCfg.Entries) {
                        if (string.Equals(overriding.Key, existingEntry.Key, StringComparison.Ordinal)) {
                            existingCopy ??= existingCfg.Copy();
                            existingCopy.Entries[existingEntry.Key] = overriding.Value;
                            break;
                        }
                    }
                }

                return existingCopy ?? existingCfg;
            } catch (Exception e) {
                SystemHelper.Logger?.Error($"Unable to override '{this.File ?? FileKey}' onto existing '{existingCfg.File ?? existingCfg.FileKey}'\n {e}");
                throw;
            }
        }

        /// <summary>
        ///     Will use current XmlConfig as child while merging <paramref name="parent"/> onto a copy of this instance.
        ///     If no changes were applied, the original is returned.
        /// </summary>
        /// <param name="parent">The base cfg that has this XML inherited</param>
        /// <returns>A copy of this with values from parent merged.</returns>
        public XmlConfig Merge(XmlConfig parent) {
            try {
                XmlConfig newChild = this.Copy(new Dictionary<string, ConfigNode>(parent.Entries));

                var key = parent.Inheriting ?? parent.FileKey;
                foreach (var childEntry in this.Entries) {
                    //copy parent's value because child doesn't have it.
                    var childKey = string.Intern(key + childEntry.Key.Substring(this.FileKey.Length));
                    newChild.Entries[childKey] = childEntry.Value;
                }

                return newChild;
            } catch (Exception e) {
                SystemHelper.Logger?.Error($"Unable to merge child '{this.File ?? FileKey} ({FileKey})' with parent '{parent.File ?? parent.FileKey} ({parent.FileKey})'\n {e}");
                throw;
            }
        }

        /// <summary>
        ///     A simple fluent delegate calling wrapper for executing logic in constructor when passing to base.
        /// </summary>
        /// <returns>this, not a new xml.</returns>
        public XmlConfig Manipulate(Action<XmlConfig> @delegate) {
            @delegate(this);
            return this;
        }
    }
}