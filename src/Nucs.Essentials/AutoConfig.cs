using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using EnumsNET;
using Nucs.Configuration;
using Nucs.Linq;
using Nucs.Reflection;

namespace Nucs {
    public static class AutoConfig {
        private const string BackingField = "_BackingField";
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertiesCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<Type, FieldInfo[]> _fieldsCache = new ConcurrentDictionary<Type, FieldInfo[]>();

        public static PropertyInfo[] StrategyProperties(Type o) {
            return _propertiesCache.GetOrAdd(o, o => o.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).DistinctBy(k => k.Name).ToArray());
        }

        // This contains all the strategy fields
        public static FieldInfo[] StrategyFields(Type o) {
            return _fieldsCache.GetOrAdd(o, o => o.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).DistinctBy(k => k.Name).ToArray());
        }

        public static bool IsValidAutoLoadType(Type type) {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum
                || type == typeof(int)
                || type == typeof(bool)
                || type == typeof(double)
                || type == typeof(float)
                || type == typeof(TimeSpan)
                || type == typeof(string)
                || type == typeof(long)
                || type == typeof(List<int>)
                || type == typeof(List<bool>)
                || type == typeof(List<double>)
                || type == typeof(List<float>)
                || type == typeof(List<TimeSpan>)
                || type == typeof(List<string>)
                || type == typeof(List<long>)
                || (type.GetGenericTypeDefinition() == typeof(List<>) && type.GetGenericArguments()[0].IsEnum))
                return true;

            return false;
        }

        public static void LoadParameters(object target, FileInfo fileInfo) {
            string file;
            if (File.Exists(fileInfo.FullName))
                file = fileInfo.FullName;
            else
                file = SystemHelper.GetSettingsFile(fileInfo.Name);

            var xcfg = new Configuration.XmlConfig(file, Path.GetFileNameWithoutExtension(file), SystemHelper.ReadAllText(file));
            xcfg = xcfg.SubConfig(xcfg.FileKey);
            LoadParameters(target, xcfg);
        }

        public static void LoadParameters(object target, Configuration.XmlConfig xcfg) {
            //handle non-subconfig
            var type = target.GetType();
            using (var enumerator = xcfg.Entries.Keys.GetEnumerator()) {
                if (!enumerator.MoveNext()) {
                    string error = $"AutoConfig of file '{xcfg.File}' did not have any parameters to apply onto {type.Name}";
                    SystemHelper.Logger?.Error(error);
                    throw new StrategyException(error);
                }

                if (enumerator.Current.StartsWith(type.Name) && type.Name.Length + 1 <= enumerator.Current.Length && enumerator.Current[type.Name.Length] == '.') {
                    xcfg = xcfg.SubConfig(type.Name);
                }
            }

            // We will try to set all the parameters that are visible in the Config File - Parameters that do not exist
            // will be logged as errors

            PropertyInfo lastPr = null;
            FieldInfo lastFi = null;
            int applied = 0;

            try {
                foreach (string parameter in xcfg.Keys) {
                    if (parameter == "#comment" || parameter == "#text")
                        continue;
                    bool parameterLoaded = false;

                    foreach (PropertyInfo pi in StrategyProperties(type)) {
                        if (!pi.CanWrite || pi.Name != parameter)
                            continue;

                        if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) {
                            parameterLoaded = true;
                            break;
                        }

                        if (!IsValidAutoLoadType(pi.PropertyType))
                            continue;

                        var cfg = xcfg[pi.Name];
                        if (SetValue(target, pi, ref cfg, xcfg.File))
                            applied++;

                        parameterLoaded = true;
                        break;
                    }

                    lastPr = null;
                    // If we found the paramter lets continue to the next parameter
                    if (parameterLoaded)
                        continue;

                    foreach (FieldInfo fi in StrategyFields(type)) {
                        if (fi.Name != parameter)
                            continue;

                        lastFi = fi;
                        if (fi.GetCustomAttribute<XmlIgnoreAttribute>() != null) {
                            parameterLoaded = true;
                            break;
                        }

                        if (!IsValidAutoLoadType(fi.FieldType))
                            continue;

                        var val = xcfg[fi.Name];
                        if (SetValue(target, fi, ref val, xcfg.File))
                            applied++;
                        parameterLoaded = true;
                        break;
                    }

                    lastFi = null;
                    if (parameterLoaded == false && !parameter.Contains('.') && !parameter.Contains("-Fallback")) {
                        string parameterNotFound = "Parameter: " + parameter + " Not found in the Config File: " + xcfg.File;
                        SystemHelper.Logger?.Error(parameterNotFound);
                        throw new StrategyException(parameterNotFound);
                    }
                }
            } catch (StrategyException ex) {
                var prop = lastFi?.Name ?? lastPr?.Name ?? "irrelevant";
                string error = $"AutoConfig of file '{xcfg.File}' failed for '{target?.GetType().Name}' for prop/field '{prop}':\n\t{ex}";
                SystemHelper.Logger?.Error(error);
                throw new ConfigurationException(error, ex);
            } catch (Exception ex) {
                var prop = lastFi?.Name ?? lastPr?.Name ?? "irrelevant";
                string error = $"AutoConfig of file '{xcfg.File}' failed for '{target?.GetType().Name}' for prop/field '{prop}':\n\t{ex}";
                SystemHelper.Logger?.Error(error);
                throw new StrategyException(error);
            }

            if (applied == 0) {
                string error = $"AutoConfig of file '{xcfg.File}' did not apply any changes onto {target.ToString()} object";
                SystemHelper.Logger?.Error(error);
                throw new StrategyException(error);
            }
        }

        public static object ParseValue(object o, Type type, ref Configuration.ConfigNode node, string file) {
            try {
                return node.ToBoxedValue(type);
            } catch (Exception ex) {
                // Program should crash on invalid values
                string error = "Object Type: " + type + " type: " + type.FullName + " Error in the File: " + file + " Exception: " + ex;
                SystemHelper.Logger?.Error(error);
                throw new StrategyException(error);
            }

            return null;
        }

        public static bool SetValue(object o, PropertyInfo pi, ref Configuration.ConfigNode node, string file) {
            var underlying = Nullable.GetUnderlyingType(pi.PropertyType);
            bool isNullable = underlying != null;
            var type = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;

            try {
                pi.SetValue(o, ParseValue(o, pi.PropertyType, ref node, file));
            } catch (Exception ex) {
                // Program should crash on invalid values
                string error = "Object Type: " + type + " pi.Name: " + pi.Name + " Error in the File: " + file + " Exception: " + ex;
                SystemHelper.Logger?.Error(error);
                throw new StrategyException(error);
            }

            return true;
        }

        public static bool SetValue(object o, FieldInfo fi, ref Configuration.ConfigNode node, string file) {
            try {
                fi.SetValue(o, ParseValue(o, fi.FieldType, ref node, file));
            } catch (Exception ex) {
                // Program should crash on invalid values
                string error = "Object Type: " + (Nullable.GetUnderlyingType(fi.FieldType) ?? fi.FieldType) + " fi.Name: " + fi.Name + " node not exist in the File: " + file + " Exception: " + ex;
                SystemHelper.Logger?.Error(error);
                throw new StrategyException(error);
            }

            return true;
        }

        public static string GetPropertyValue(object o, PropertyInfo pi, char delimiter = ',') {
            var underlying = Nullable.GetUnderlyingType(pi.PropertyType);
            bool isNullable = underlying != null;
            var type = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;

            try {
                if (isNullable) {
                    return pi.GetValue(o, null)?.ToString() ?? "NULL";
                } else if (ConfigParsers.BoxedToStringConverters.TryGetValue(type, out var converter)) {
                    return converter(pi.GetValue(o)!);
                } else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && type.GetGenericArguments()[0].IsEnum) {
                    IList values = (IList) pi.GetValue(o, null);
                    var genericArgument = type.GetGenericArguments()[0];
                    return (values == null || values.Count == 0) ? string.Empty : string.Join(delimiter, values.Cast<object>().Select(o => Enums.AsString(genericArgument, o)));
                }

                return pi.GetValue(o, null)?.ToString() ?? "NULL";
            } catch (Exception ex) {
                string error = "Failed to GetPropertyValue: " + pi.Name + " Propety:  " + pi + " Exception: " + ex;
                SystemHelper.Logger?.Error(error);
                throw new StrategyException(error);
            }
        }

        public static string GetFieldValue(object o, FieldInfo fi, char delimiter = ',') {
            var underlying = Nullable.GetUnderlyingType(fi.FieldType);
            bool isNullable = underlying != null;
            var type = Nullable.GetUnderlyingType(fi.FieldType) ?? fi.FieldType;

            try {
                if (isNullable) {
                    return fi.GetValue(o)?.ToString() ?? "NULL";
                } else if (ConfigParsers.BoxedToStringConverters.TryGetValue(type, out var converter)) {
                    return converter(fi.GetValue(o)!);
                } else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && type.GetGenericArguments()[0].IsEnum) {
                    IList values = (IList) fi.GetValue(o);
                    var genericArgument = type.GetGenericArguments()[0];
                    return (values == null || values.Count == 0) ? string.Empty : string.Join(delimiter, values.Cast<object>().Select(o => Enums.AsString(genericArgument, o)));
                }

                return fi.GetValue(o)?.ToString() ?? "NULL";
            } catch (Exception ex) {
                string error = "Failed to GetFieldValue: " + fi.Name + " Field:  " + fi + " Exception: " + ex;
                SystemHelper.Logger?.Error(error);
                throw new StrategyException(error);
            }
        }

        public static void LogParameters(object o) {
            try {
                var type = o.GetType();
                string logParameters = "*****************************************************************";
                SystemHelper.Logger?.Trace(logParameters);

                logParameters = "Logging Parameters for Strategy: " + type.Name;
                SystemHelper.Logger?.Trace(logParameters);

                foreach (PropertyInfo pi in StrategyProperties(type)) {
                    if (!IsValidAutoLoadType(pi.PropertyType))
                        continue;
                    string propertyLog = "Property: " + pi.Name + " : " + GetPropertyValue(o, pi);
                    SystemHelper.Logger?.Trace(propertyLog);
                }

                foreach (FieldInfo fi in StrategyFields(type)) {
                    if (fi.Name.EndsWith(BackingField) || !IsValidAutoLoadType(fi.FieldType))
                        continue;
                    string fieldLog = "Field: " + fi.Name + " : " + GetFieldValue(o, fi);
                    SystemHelper.Logger?.Trace(fieldLog);
                }

                logParameters = "*****************************************************************";
                SystemHelper.Logger?.Trace(logParameters);
            } catch (Exception ex) {
                string error = "Error Logging Parameters: " + ex;
                SystemHelper.Logger?.Error(error);
                throw new StrategyException(error);
            }
        }

        public static string GetMemberValue(object o, string memberName, char delimiter = ',') {
            if (memberName.Contains('\t')) {
                memberName = memberName.Replace("\t", String.Empty);
            }

            foreach (PropertyInfo pi in StrategyProperties(o.GetType())) {
                if (pi.Name != memberName) continue;
                return GetPropertyValue(o, pi, delimiter);
            }

            foreach (FieldInfo field in StrategyFields(o.GetType())) {
                if (field.Name != memberName) continue;
                return GetFieldValue(o, field, delimiter);
            }

            string error = "memberName not found: " + memberName;
            SystemHelper.Logger?.Error(error);
            throw new ConfigurationException(error);
        }

        public static string GetPreloadedValue<T>(T o, string memberName, char delimiter = ',') {
            if (!PreloadedPropertyGetter<T>.ToStringGetters.TryGetValue(memberName, out var d)) {
                return GetMemberValue(o, memberName, delimiter);
            }

            return ((Func<T, string>) d)(o);
        }

        public static object GetPropertyValue(object o, string propertyName) {
            var coll = StrategyProperties(o.GetType());
            var len = coll.Length;
            for (int i = 0; i < len; i++) {
                if (coll[i].Name == propertyName)
                    return coll[i]?.GetValue(o, null);
            }

            return null;
        }

        public static bool ContainsProperty(object o, string propertyName) {
            var coll = StrategyProperties(o.GetType());
            var len = coll.Length;
            for (int i = 0; i < len; i++) {
                if (coll[i].Name == propertyName)
                    return true;
            }

            return false;
        }
    } // end of Class AutoConfig
}