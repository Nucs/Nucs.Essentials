using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using EnumsNET;
using Nucs.Configuration;
using static Nucs.AbstractInfrastructure;
using XmlAttribute = System.Xml.XmlAttribute;

namespace Nucs {
    /// <summary>
    /// Represents a Configuration Node in the XML file
    /// </summary>
    [SuppressMessage("ReSharper", "RedundantCaseLabel")]
    public readonly struct ConfigNode {
        private static readonly char[] separators = { '/', '\\' };
        private static readonly char[] numberChars = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '_' };

        public bool IsEmpty => node == null;

        /// <summary>
        /// The node from the XMLDocument, which it describes
        /// </summary>
        private readonly XmlNode node;

        /// <summary>
        /// Creates an instance of the class
        /// </summary>
        /// <param name="node">
        /// the XmlNode to describe
        /// </param>
        public ConfigNode(XmlNode node) {
            this.node = node ?? throw new SourceException("Node parameter can NOT be null!");
        }

        /// <summary>
        /// The Name of the element it describes
        /// </summary>
        /// <remarks>Read only property</remarks>        
        public string Name => node.Name;

        /// <summary>
        /// Gets the number of children of the specific node
        /// </summary>
        /// <param name="unique">
        /// If true, get only the number of children with distinct names.
        /// So if it has two nodes with "foo" name, and three nodes
        /// named "bar", the return value will be 2. In the same case, if unique
        /// was false, the return value would have been 2 + 3 = 5
        /// </param>
        /// <returns>
        /// The number of (uniquely named) children
        /// </returns>
        public int ChildCount(bool unique) {
            IList<string> names = ChildrenNames(unique);
            if (names != null)
                return names.Count;
            else
                return 0;
        }

        /// <summary>
        /// Gets the names of children of the specific node
        /// </summary>
        /// <param name="unique">
        /// If true, get only distinct names.
        /// So if it has two nodes with "foo" name, and three nodes
        /// named "bar", the return value will be {"bar","foo"} . 
        /// In the same case, if unique was false, the return value 
        /// would have been {"bar","bar","bar","foo","foo"}
        /// </param>
        /// <returns>
        /// An IList object with the names of (uniquely named) children
        /// </returns>
        public IList<string> ChildrenNames(bool unique) {
            if (node.ChildNodes.Count == 0)
                return null;
            List<string> stringlist = new List<string>();

            foreach (XmlNode achild in node.ChildNodes) {
                string name = achild.Name;
                if ((!unique) || (!stringlist.Contains(name)))
                    stringlist.Add(name);
            }

            stringlist.Sort();
            return stringlist;
        }

        /// <summary>
        /// Return true if the string is present as as child node
        /// </summary>
        public bool ContainsChild(string key) {
            IList<string> childrenNames = ChildrenNames(true);

            foreach (string s in childrenNames) {
                if (s == key)
                    return true;
            }

            return false;
        }


        /// <summary>
        /// An IList compatible object describing each and every child node
        /// </summary>
        /// <remarks>Read only property</remarks>
        public IList<ConfigNode> Children() {
            if (ChildCount(false) == 0)
                return null;
            List<ConfigNode> list = new List<ConfigNode>();
            list.AddRange(node.ChildNodes.Cast<XmlNode>().Select(c => new ConfigNode(c)));

            return list;
        }

        /// <summary>
        /// Get all children with the same name, specified in the name parameter
        /// </summary>
        /// <param name="name">
        /// An alphanumerical string, containing the name of the child nodes to return
        /// </param>
        /// <returns>
        /// An array with the child nodes with the specified name, or null 
        /// if no childs with the specified name exist
        /// </returns>
        public IList<ConfigNode> GetNamedChildren(string name) {
            foreach (char c in name)
                if (!char.IsLetterOrDigit(c) && c != '_')
                    throw new SourceException("Name MUST be alphanumerical!");
            XmlNodeList xmlnl = node.SelectNodes(name);
            List<ConfigNode> css = new List<ConfigNode>();
            foreach (XmlNode achild in xmlnl) {
                css.Add(new ConfigNode(achild));
            }

            return css;
        }

        /// <summary>
        /// Gets the number of childs with the specified name
        /// </summary>
        /// <param name="name">
        /// An alphanumerical string with the name of the nodes to look for
        /// </param>
        /// <returns>
        /// An integer with the count of the nodes
        /// </returns>
        public int GetNamedChildrenCount(string name) {
            foreach (char c in name)
                if (!char.IsLetterOrDigit(c) && c != '_')
                    throw new SourceException("Name MUST be alphanumerical!");
            return node.SelectNodes(name).Count;
        }

        public readonly struct XmlAttributeResolve {
            public readonly string Group;
            public readonly XmlAttribute MainAttribute;
            public readonly XmlAttribute[] Fallbacks;

            public bool IsEmpty => MainAttribute == null;

            public XmlAttributeResolve(string @group, XmlAttribute mainAttribute) {
                MainAttribute = mainAttribute;
                Group = @group;
                Fallbacks = Array.Empty<XmlAttribute>();
            }

            public XmlAttributeResolve(string @group, XmlAttribute mainAttribute, XmlAttribute[] fallbacks) {
                MainAttribute = mainAttribute;
                Fallbacks = fallbacks;
                Group = @group;
            }
        }

        /// <summary>
        ///    Returns enumeration of attributes falling back 
        /// </summary>
        public static XmlAttributeResolve ResolveValueAttribute(XmlNode node) {
            if (node == null)
                throw new InvalidOperationException($"This XmlNode is null");
            var attrs = node.Attributes ?? throw new NullReferenceException($"node.Attributes == null");
            if (attrs.Count > 0) {
                XmlAttribute attr;
                if (IsProduction) {
                    attr = attrs["prod"];
                    if (attr != null)
                        return new XmlAttributeResolve("prod", attr);
                }                
                
                if (IsResearch) {
                    attr = attrs["research"];
                    if (attr != null)
                        return new XmlAttributeResolve("research", attr);
                }

                attr = attrs["value"];

                //special groups
                XmlAttribute[] fallbacks = Array.Empty<XmlAttribute>();
                string group = "value";

                //try for file
                if (attr == null) {
                    attr = attrs["file"] ?? attrs["file-optional"];
                    if (attr != null) {
                        group = "file";
                        fallbacks = attrs.Cast<XmlAttribute>().Where(attr => attr.Name.StartsWith("file-fallback") || attr.Name.StartsWith("file-optional-fallback")).ToArray();
                    }
                }

                //try for directory
                if (attr == null) {
                    attr = attrs["directory"] ?? attrs["directory-optional"];
                    if (attr != null) {
                        group = "directory";
                        fallbacks = attrs.Cast<XmlAttribute>().Where(attr => attr.Name.StartsWith("directory-fallback") || attr.Name.StartsWith("directory-optional-fallback")).ToArray();
                    }
                }

                return new XmlAttributeResolve(group, attr, fallbacks);
            }

            return default;
        }

        /// <summary>
        /// String value of the specific Configuration Node
        /// </summary>
        public string Value {
            get {
                XmlAttributeResolve resolve = ResolveValueAttribute(node);

                if (resolve.IsEmpty)
                    return "";

                string result;
                bool optional = resolve.MainAttribute.Name.EndsWith("optional");

                switch (resolve.Group) {
                    case "file-optional":
                    case "file": {
                        int i = 0;
                        var mainPath = result = resolve.MainAttribute.Value;
                        bool exists;
                        while (!(exists = AbstractInfrastructure.CachedFile.Exists(result)) && i < resolve.Fallbacks.Length) {
                            var fb = resolve.Fallbacks[i++];
                            optional = optional || fb.Name.Contains("");
                            switch (fb.Name.TrimEnd(numberChars)) {
                                case "file-optional-fallback-drive":
                                case "file-fallback-drive":
                                    result = fb.Value + Path.GetFullPath(result).Substring(1);
                                    break;
                                case "file-fallback":
                                default:
                                    result = fb.Value;
                                    break;
                            }
                        }

                        if (!exists) {
                            if (optional)
                                return string.Empty;
                            string fallbacks = resolve.Fallbacks.Length > 0 ? "\nwith fallbacks:\n" + string.Join("\n", resolve.Fallbacks.Select(f => f.Value.Length == 1 ? "Drive: " + f.Value : f.Value)) : "";
                            throw new ConfigurationException($"Configuration for node '{node.Name}' file not found:\n\"{mainPath}\"{fallbacks}");
                        }

                        return result;
                    }
                    case "directory-optional":
                    case "directory": {
                        int i = 0;
                        var mainPath = result = resolve.MainAttribute.Value;
                        bool exists;
                        while (!(exists = AbstractInfrastructure.CachedFile.ExistsDirectory(result)) && i < resolve.Fallbacks.Length) {
                            var fb = resolve.Fallbacks[i++];
                            optional = optional || fb.Name.Contains("");
                            switch (fb.Name.TrimEnd(numberChars)) {
                                case "optional-directory-fallback-drive":
                                case "directory-fallback-drive":
                                    result = fb.Value + Path.GetFullPath(result).Substring(1);
                                    break;
                                case "directory-fallback":
                                default:
                                    result = fb.Value;
                                    break;
                            }
                        }

                        if (!exists) {
                            if (optional)
                                return string.Empty;

                            string fallbacks = resolve.Fallbacks.Length > 0 ? "\nwith fallbacks:\n" + string.Join("\n", resolve.Fallbacks.Select(f => f.Value.Length == 1 ? "Drive: " + f.Value : f.Value)) : "";
                            throw new ConfigurationException($"Configuration for node '{node.Name}' directory not found:\n\"{mainPath}\"{fallbacks}");
                        }

                        return result;
                    }
                    default:
                        result = resolve.MainAttribute.Value;
                        if (resolve.Fallbacks.Length <= 0)
                            return result;

                        for (int i = 0; i < resolve.Fallbacks.Length && string.IsNullOrEmpty(result); i++) {
                            result = resolve.Fallbacks[i].Value;
                        }

                        return result;
                }
            }
            private set {
                XmlAttributeResolve resolve = ResolveValueAttribute(node);
                XmlAttribute attr = null;

                //if the attribute doesnt exist, create it
                if (resolve.IsEmpty)
                    attr = node.Attributes.Append(node.OwnerDocument.CreateAttribute(resolve.Group));
                else
                    attr = resolve.MainAttribute;

                if (string.IsNullOrEmpty(value))
                    node.Attributes.RemoveNamedItem(resolve.Group);
                else
                    attr.Value = value;
            }
        }

        public void SetValue(string val) {
            Value = val;
        }

        /// <summary>
        /// long value of the specific Configuration Node
        /// </summary>
        public long longValue {
            get => long.Parse(Value);
            set => Value = value.ToString();
        }

        public long? tryLongValue {
            get {
                if (long.TryParse(Value, NumberStyles.Integer | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(CultureInfo.InvariantCulture), out var val))
                    return val;
                return null;
            }
        }

        /// <summary>
        /// int value of the specific Configuration Node
        /// </summary>
        public int intValue {
            get => int.Parse(Value);
            set => Value = value.ToString();
        }

        public int? tryIntValue {
            get {
                if (int.TryParse(Value, NumberStyles.Integer | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(CultureInfo.InvariantCulture), out var val))
                    return val;
                return null;
            }
        }

        /// <summary>
        /// bool value of the specific Configuration Node
        /// </summary>
        public bool boolValue {
            get => bool.Parse(Value);
            set => Value = value.ToString();
        }

        public bool? tryBoolValue {
            get {
                if (bool.TryParse(Value, out var val))
                    return val;
                return null;
            }
        }

        /// <summary>
        /// float value of the specific Configuration Node
        /// </summary>
        public float floatValue {
            //get { float f; float.TryParse(Value, out f); return f; }
            get => float.Parse(Value, CultureInfo.InvariantCulture);
            set => Value = value.ToString();
        }

        public float? tryFloatValue {
            get {
                if (float.TryParse(Value, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(CultureInfo.InvariantCulture), out var val))
                    return val;
                return null;
            }
        }

        /// <summary>
        /// double value of the specific Configuration Node
        /// </summary>
        public double doubleValue {
            get {
                string value = Value;
                if (value.EndsWith('%'))
                    return double.Parse(value.TrimEnd('%')) / 100d;

                return double.Parse(value);
            }
            set => Value = value.ToString();
        }

        public double? tryDoubleValue {
            get {
                if (double.TryParse(Value, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(CultureInfo.InvariantCulture), out var val))
                    return val;
                return null;
            }
        }

        /// <summary>
        /// double value of the specific Configuration Node rounded to 2 decimals
        /// </summary>
        public double doubleValue2Decimails {
            get => Math.Round(double.Parse(Value), 2);
            set => Value = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Date Value of the Config Node - yyyyMMdd
        /// </summary>
        public DateTime dateValue {
            get => ConfigParsers.DateTime(Value);
            set => Value = value.ToString(AbstractInfrastructure.DateFormat);
        }

        public DateTime? tryDateTimeValue => ConfigParsers.Safe.DateTime(Value, null);

        /// <summary>
        /// TimeSpan value of the specific Configuration Node
        /// </summary>
        public TimeSpan timeSpanValue {
            get => ConfigParsers.TimeSpan(Value);
            set => Value = value.ToString(TimeFormat);
        }

        public TimeSpan? tryTimeSpan => ConfigParsers.Safe.TimeSpan(Value, null);

        /// <summary>
        /// List<string> value of the specific Configuration Node
        /// The List of strings are seperated by ","
        /// </summary>
        public List<string> listString {
            get {
                var value = Value;
                if (string.IsNullOrEmpty(value))
                    throw new SourceException($"List: {value} not in correct Format");

                var list = new List<string>(value.Split(','));

                if (list == null || list.Count == 0)
                    throw new SourceException($"List: {value} not in correct Format");

                return list;
            }
            set => Value = string.Join(",", value);
        }

        /// <summary>
        /// List<string> value of the specific Configuration Node
        /// The List of strings are seperated by ","
        /// </summary>
        public List<T> listEnum<T>() where T : struct, Enum {
            var value = Value;
            if (string.IsNullOrEmpty(value))
                throw new SourceException($"List: {value} not in correct Format");

            var list = new List<T>(value.Split(',').Select(s => Enums.Parse<T>(s, true)));

            if (list == null || list.Count == 0)
                throw new SourceException($"List: {value} not in correct Format");

            return list;
        }

        /// <summary>
        /// List<string> value of the specific Configuration Node
        /// The List of strings are seperated by ","
        /// </summary>
        public IList listEnum(Type enumType) {
            var stringValue = Value;
            if (string.IsNullOrEmpty(stringValue))
                throw new SourceException($"List: {stringValue} not in correct Format");
            var list = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(enumType));

            foreach (var value in stringValue.Split(',').Select(s => Enums.Parse(enumType, s, true))) {
                list.Add(value);
            }

            if (list == null || list.Count == 0)
                throw new SourceException($"List: {stringValue} not in correct Format");

            return list;
        }

        /// <summary>
        /// List<int> value of the specific Configuration Node
        /// The List of ints are seperated by ","
        /// </summary>
        public List<int> listInt {
            get => listString.Select(int.Parse).ToList();
            set => Value = string.Join(",", value.Select(ts => ts.ToString(DefaultCulture)));
        }

        /// List<int> value of the specific Configuration Node
        /// The List of uints are seperated by ","
        /// </summary>
        public List<uint> listUInt {
            get => listString.Select(uint.Parse).ToList();
            set => Value = string.Join(",", value.Select(ts => ts.ToString(DefaultCulture)));
        }

        /// List<int> value of the specific Configuration Node
        /// The List of uints are seperated by ","
        /// </summary>
        public List<long> listLong {
            get => listString.Select(long.Parse).ToList();
            set => Value = string.Join(",", value.Select(ts => ts.ToString(DefaultCulture)));
        }

        /// List<int> value of the specific Configuration Node
        /// The List of uints are seperated by ","
        /// </summary>
        public List<ulong> listULong {
            get => listString.Select(ulong.Parse).ToList();
            set => Value = string.Join(",", value.Select(ts => ts.ToString(DefaultCulture)));
        }


        /// <summary>
        /// List<bool> value of the specific Configuration Node
        /// The List of bool are seperated by ","
        /// </summary>
        public List<bool> listBool {
            get => listString.Select(bool.Parse).ToList();
            set => Value = string.Join(",", value.Select(ts => ts.ToString(DefaultCulture)));
        }

        /// <summary>
        /// List<float> value of the specific Configuration Node
        /// The List of float are seperated by ","
        /// </summary>
        public List<float> listFloat {
            get => listString.Select(float.Parse).ToList();
            set => Value = Value = string.Join(",", value.Select(ts => ts.ToString(DefaultCulture)));
        }


        /// <summary>
        /// List<double> value of the specific Configuration Node
        /// The List of double are seperated by ","
        /// </summary>
        public List<double> listDouble {
            get => listString.Select(s => {
                if (s.EndsWith('%')) {
                    return double.Parse(s.TrimEnd('%')) / 100d;
                }

                return double.Parse(s);
            }).ToList();
            set => Value = Value = string.Join(",", value.Select(ts => ts.ToString(DefaultCulture)));
        }

        /// <summary>
        /// List<TimeSpan> value of the specific Configuration Node
        /// The List of double are seperated by ","
        /// </summary>
        public List<TimeSpan> listTimeSpan {
            get {
                List<string> arrVal = listString;
                List<TimeSpan> arrTimeSpan = new List<TimeSpan>();

                foreach (string s in arrVal) {
                    arrTimeSpan.Add(TimeSpan.Parse(s));
                }

                return arrTimeSpan;
            }
            set => Value = string.Join(",", value.Select(ts => ts.ToString(TimeFormat)));
        }

        /// <summary>
        /// Get a specific child node
        /// </summary>
        /// <param name="path">
        /// The path to the specific node. Can be either only a name, or a full path separated by '/' or '\'
        /// </param>
        /// <example>
        /// <code>
        /// XmlConfig conf = new XmlConfig("configuration.xml");
        /// screenname = conf.Settings["screen"].Value;
        /// height = conf.Settings["screen/height"].IntValue;
        ///  // OR
        /// height = conf.Settings["screen"]["height"].IntValue;
        /// </code>
        /// </example>
        /// <returns>
        /// The specific child node
        /// </returns>
        public ConfigNode this[string path] {
            get {
                string[] pathParts = path.Split(separators);

                XmlNode selectedNode = node ?? throw new SourceException($"node is null");

                foreach (string part in pathParts) {
                    string nodename;
                    int nodeposition;
                    int indexofdiez = part.IndexOf('#');

                    if (indexofdiez == -1) // No position defined, take the first one by default
                    {
                        nodename = part;
                        nodeposition = 1;
                    } else {
                        nodename = part.Substring(0, indexofdiez); // Node name is before the diez character
                        var nodePosStr = part.Substring(indexofdiez + 1);
                        if (nodePosStr == "#") // Double diez means he wants to create a new node
                            nodeposition = GetNamedChildrenCount(nodename) + 1;
                        else
                            nodeposition = int.Parse(nodePosStr);
                    }

                    selectedNode = selectedNode.SelectSingleNode($"{nodename}[{nodeposition}]");
                }

                if (null == selectedNode)
                    throw new SourceException($"Check Config File '{Name}' spelling of Path: {path}");

                return new ConfigNode(selectedNode);
            }
        }

        /// <summary>
        /// Get a specific child node
        /// </summary>
        /// <param name="path">
        /// The path to the specific node. Can be either only a name, or a full path separated by '/' or '\'
        /// </param>
        /// <example>
        /// <code>
        /// XmlConfig conf = new XmlConfig("configuration.xml");
        /// screenname = conf.Settings["screen"].Value;
        /// height = conf.Settings["screen/height"].IntValue;
        ///  // OR
        /// height = conf.Settings["screen"]["height"].IntValue;
        /// </code>
        /// </example>
        /// <returns>
        /// The specific child node
        /// </returns>
        public bool Contains(string path) {
            path.Trim(separators);
            string[] pathsection = path.Split(separators);

            XmlNode selectednode = node;
            XmlNode newnode;

            foreach (string asection in pathsection) {
                string nodename, nodeposstr;
                int nodeposition;
                int indexofdiez = asection.IndexOf('#');

                if (indexofdiez == -1) // No position defined, take the first one by default
                {
                    nodename = asection;
                    nodeposition = 1;
                } else {
                    nodename = asection.Substring(0, indexofdiez); // Node name is before the diez character
                    nodeposstr = asection.Substring(indexofdiez + 1);
                    if (nodeposstr == "#") // Double diez means he wants to create a new node
                        nodeposition = GetNamedChildrenCount(nodename) + 1;
                    else
                        nodeposition = int.Parse(nodeposstr);
                }

                // Verify name
                foreach (char c in nodename)
                    // { if ((!Char.IsLetterOrDigit(c))) return null; }
                {
                    if ((!char.IsLetterOrDigit(c) && c != '_')) throw new SourceException($"Check Config File spelling of NodeName: {nodename}");
                }

                newnode = selectednode.SelectSingleNode($"{nodename}[{nodeposition}]");

                selectednode = newnode;
            }

            if (null == selectednode) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a specific child node
        /// </summary>
        /// <param name="path">
        /// The path to the specific node. Can be either only a name, or a full path separated by '/' or '\'
        /// </param>
        /// <example>
        /// <code>
        /// XmlConfig conf = new XmlConfig("configuration.xml");
        /// screenname = conf.Settings["screen"].Value;
        /// height = conf.Settings["screen/height"].IntValue;
        ///  // OR
        /// height = conf.Settings["screen"]["height"].IntValue;
        /// </code>
        /// </example>
        /// <returns>
        /// The specific child node
        /// </returns>
        public ConfigNode TryGet(string path) {
            char[] separators = { '/', '\\' };
            path.Trim(separators);
            string[] pathsection = path.Split(separators);

            XmlNode selectednode = node;
            XmlNode newnode;

            foreach (string asection in pathsection) {
                string nodename, nodeposstr;
                int nodeposition;
                int indexofdiez = asection.IndexOf('#');

                if (indexofdiez == -1) // No position defined, take the first one by default
                {
                    nodename = asection;
                    nodeposition = 1;
                } else {
                    nodename = asection.Substring(0, indexofdiez); // Node name is before the diez character
                    nodeposstr = asection.Substring(indexofdiez + 1);
                    if (nodeposstr == "#") // Double diez means he wants to create a new node
                        nodeposition = GetNamedChildrenCount(nodename) + 1;
                    else
                        nodeposition = int.Parse(nodeposstr);
                }

                // Verify name
                foreach (char c in nodename)
                    // { if ((!Char.IsLetterOrDigit(c))) return null; }
                {
                    if ((!char.IsLetterOrDigit(c) && c != '_')) throw new SourceException($"Check Config File spelling of NodeName: {nodename}");
                }

                newnode = selectednode.SelectSingleNode($"{nodename}[{nodeposition}]");

                selectednode = newnode;
            }

            if (null == selectednode) {
                return default;
            }

            return new ConfigNode(selectednode);
        }

        /// <summary>
        /// Check if the node conforms with the config xml restrictions
        /// 1. No nodes with two children of the same name
        /// 2. Only alphanumerical names
        /// </summary>
        /// <returns>
        /// True on success and false on failiure
        /// </returns>        
        //public bool Validate()
        //{
        //    // Check this node's name for validity
        //    foreach (Char c in this.Name)
        //        if (!Char.IsLetterOrDigit(c))
        //            return false;

        //    // If there are no children, the node is valid.
        //    // If there the node has other children, check all of them for validity
        //    if (ChildCount(false) == 0)
        //        return true;
        //    else
        //    {
        //        foreach (ConfigSetting cs in this.Children())
        //        {
        //            if (!cs.Validate())
        //                return false;
        //        }
        //    }
        //    return true;
        //}
        // 3. allowing comments in between children in xml file
        public bool Validate() {
            // Check this node's name for validity
            foreach (char c in this.Name)
                if (!char.IsLetterOrDigit(c) && c is not '_' and not '.' and not '-')
                    return false;

            // If there are no children, the node is valid.
            // If there the node has other children, check all of them for validity
            if (ChildCount(false) == 0)
                return true;
            else {
                foreach (ConfigNode cs in this.Children()) {
                    if (!cs.Name.Equals("#comment") && !cs.Name.Equals("#text")) {
                        if (!cs.Validate()) {
                            throw new Exception($"Node Name Possible Error: {cs.node.Name}");
                            return false;
                        }
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Removes any empty nodes from the tree, 
        /// that is it removes a node, if it hasn't got any
        /// children, or neither of its children have got a value.
        /// </summary>
        public void Clean() {
            if (ChildCount(false) != 0)
                foreach (ConfigNode cs in this.Children()) {
                    cs.Clean();
                }

            if ((ChildCount(false) == 0) && (this.Value == ""))
                this.Remove();
        }

        /// <summary>
        /// Remove the specific node from the tree
        /// </summary>
        public void Remove() {
            node.ParentNode?.RemoveChild(node);
        }

        /// <summary>
        /// Remove all children of the node, but keep the node itself
        /// </summary>
        public void RemoveChildren() {
            node.RemoveAll();
        }

        public void SetAttribute(string key, string level) {
            node.Attributes[key].Value = level;
        }
    }
}