using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using EnumsNET;
using Nucs.Extensions;

namespace Nucs.Configuration {
    public readonly partial struct ConfigNode {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetValue() {
            return Value;
        }

        public readonly FileInfo GetFile() {
            if (AbstractInfrastructure.CachedFile.Exists(Value))
                return new FileInfo(Value);
            else
                throw new ConfigurationException($"Configuration '{Key}' not found because '{Value}' both do not exist.");
        }

        public readonly DirectoryInfo GetDirectory() {
            if (AbstractInfrastructure.CachedFile.ExistsDirectory(Value))
                return new DirectoryInfo(Value);
            else {
                throw new ConfigurationException($"Configuration '{Key}' not found because '{Value};' both do not exist.");
            }
        }

        public readonly DirectoryInfo GetOrCreateDirectory() {
            if (!string.IsNullOrEmpty(Value)) {
                if (!Directory.Exists(Value))
                    Directory.CreateDirectory(Value);
                return new DirectoryInfo(Value);
            } else {
                throw new ConfigurationException($"Configuration '{Key}' not found.");
            }
        }

        public readonly DirectoryInfo GetOptionalAndTryCreateDirectory(DirectoryInfo @default = null) {
            if (!string.IsNullOrEmpty(Value)) {
                if (Directory.Exists(Value))
                    return new DirectoryInfo(Value);
                else {
                    try {
                        Directory.CreateDirectory(Value);
                    } catch (Exception) {
                        return @default;
                    }

                    return new DirectoryInfo(Value);
                }
            } else {
                return @default;
            }
        }

        public readonly string GetString() {
            return Value;
        }

        public readonly List<T> GetListOf<T>() {
            if (string.IsNullOrEmpty(Value))
                return new List<T>(0);
            var line = new LineReader(Value);
            var result = new List<T>(line.CountItems(','));
            var converter = (ConverterDelegate<T>) ConfigParsers.Converters[typeof(T)];
            while (line.HasNext)
                result.Add(converter(line.Next()));
            return result;
        }

        public readonly T[] GetArrayOf<T>() {
            if (string.IsNullOrEmpty(Value))
                return Array.Empty<T>();
            var line = new LineReader(Value);
            var result = new T[line.CountItems(',')];
            var converter = (ConverterDelegate<T>) ConfigParsers.Converters[typeof(T)];
            for (int i = 0; line.HasNext; i++) {
                result[i] = converter(line.Next());
            }

            return result;
        }

        public readonly List<List<T>> GetListOfLists<T>(char listDelimiter = '~', char delimiter = ',') {
            if (string.IsNullOrEmpty(Value))
                return new List<List<T>>(0);
            var firstLine = new LineReader(Value);
            var collection = new List<List<T>>(firstLine.CountItems(listDelimiter));
            while (firstLine.HasNext) {
                var line = new LineReader(firstLine.Next(listDelimiter));
                var result = new List<T>(line.CountItems(delimiter));
                var converter = (ConverterDelegate<T>) ConfigParsers.Converters[typeof(T)];
                while (line.HasNext)
                    result.Add(converter(line.Next(delimiter)));
                collection.Add(result);
            }

            return collection;
        }

        public readonly T[][] GetArrayOfArrays<T>(char listDelimiter = '~', char delimiter = ',') {
            if (string.IsNullOrEmpty(Value))
                return Array.Empty<T[]>();
            var firstLine = new LineReader(Value);
            var collection = new List<T[]>(firstLine.CountItems(listDelimiter));
            while (firstLine.HasNext) {
                var line = new LineReader(firstLine.Next(listDelimiter));
                var result = new T[line.CountItems(delimiter)];
                var converter = (ConverterDelegate<T>) ConfigParsers.Converters[typeof(T)];
                int i = 0;
                while (line.HasNext)
                    result[i++] = converter(line.Next(delimiter));
                collection.Add(result);
            }

            return collection.ToArray();
        }

        public readonly IList GetListOf(Type underlyingType) {
            if (string.IsNullOrEmpty(Value))
                return (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(underlyingType))!;
            var line = new LineReader(Value);
            IList list = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(underlyingType), new object[] {line.CountItems(',')})!;
            if (underlyingType.IsEnum) {
                while (line.HasNext)
                    list.Add(Enums.Parse(underlyingType, line.Next()));
                return list;
            } else {
                var converter = ConfigParsers.BoxedConverters[underlyingType];
                while (line.HasNext)
                    list.Add(converter(line.Next()));
                return list;
            }
        }

        public readonly Array GetArrayOf(Type underlyingType) {
            if (string.IsNullOrEmpty(Value))
                return Array.CreateInstance(underlyingType, 0);
            var line = new LineReader(Value);
            var result = Array.CreateInstance(underlyingType, line.CountItems(','));
            var converter = ConfigParsers.BoxedConverters[underlyingType];
            if (underlyingType.IsEnum) {
                for (int i = 0; line.HasNext; i++) {
                    result.SetValue(Enums.Parse(underlyingType, line.Next()), i);
                }
            } else {
                for (int i = 0; line.HasNext; i++) {
                    result.SetValue(converter(line.Next()), i);
                }
            }

            return result;
        }

        public readonly DateTime GetDateTime() {
            return ConfigParsers.DateTime(Value);
        }

        public readonly DateTime? GetOptionalDateTime(DateTime? @default = null) {
            if (string.IsNullOrEmpty(Value))
                return @default;

            return ConfigParsers.Safe.DateTime(Value) ?? @default;
        }

        public readonly DateTime GetOptionalDateTime(DateTime @default = default) {
            if (string.IsNullOrEmpty(Value))
                return @default;

            return ConfigParsers.Safe.DateTime(Value) ?? @default;
        }

        public readonly TimeSpan GetTimeSpan() {
            return ConfigParsers.TimeSpan(Value);
        }

        public readonly TimeSpan? GetOptionalTimeSpan(TimeSpan? @default = null) {
            try {
                return ConfigParsers.Safe.TimeSpan(Value);
            } catch (ConfigurationException) {
                return @default;
            }
        }

        public readonly TimeSpan GetOptionalTimeSpan(TimeSpan @default = default) {
            try {
                return ConfigParsers.Safe.TimeSpan(Value) ?? @default;
            } catch (ConfigurationException) {
                return @default;
            }
        }

        public readonly double GetDouble() {
            return double.Parse(Value, AbstractInfrastructure.DefaultCulture);
        }

        public readonly float GetFloat() {
            return float.Parse(Value, AbstractInfrastructure.DefaultCulture);
        }

        public readonly float GetOptionalFloat(float @default = default) {
            if (float.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly float? GetOptionalFloat(float? @default = default) {
            if (float.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly float GetDictionary() {
            return float.Parse(Value, AbstractInfrastructure.DefaultCulture);
        }

        public readonly bool GetBoolean() {
            return bool.Parse(Value);
        }

        public readonly long GetLong() {
            return long.Parse(Value, AbstractInfrastructure.DefaultCulture);
        }

        public readonly int GetInt() {
            return int.Parse(Value, AbstractInfrastructure.DefaultCulture);
        }

        public readonly double GetOptionalDouble(double @default = default) {
            if (double.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly double? GetOptionalDouble(double? @default = default) {
            if (double.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly int GetOptionalInt(int @default = default) {
            if (int.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly T GetEnum<T>(bool ignoreCase = true) where T : Enum {
            return (T) Enum.Parse(typeof(T), Value, ignoreCase: ignoreCase);
        }

        public readonly T? GetOptionalEnum<T>(T? @default = default, bool ignoreCase = true) where T : Enum {
            if (Enums.TryParseUnsafe<T>(Value, ignoreCase: ignoreCase, out var @result))
                return @result;

            return @default;
        }

        public readonly bool? GetOptionalBoolean(bool? @default = null) {
            if (bool.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly bool GetOptionalBoolean(bool @default = default) {
            if (bool.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly long GetOptionalLong(long @default = default) {
            if (long.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly long? GetOptionalLong(long? @default = default) {
            if (long.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly int? GetOptionalInt(int? @default = default) {
            if (int.TryParse(Value, out var value))
                return value;
            return @default;
        }

        public readonly string? GetOptionalString(string @default = null) {
            return Value ?? @default;
        }

        public readonly object ToBoxedValue(Type type) {
            string stringValue;
            if (ObjectValue is Func<object> t) { //lazy loading
                stringValue = (string?) t();
            } else {
                stringValue = Value;
            }

            if (ConfigParsers.BoxedConverters.TryGetValue(type, out var converter)) {
                if (string.IsNullOrEmpty(stringValue))
                    return default!;
                return converter(stringValue);
            } else if (type.IsEnum) {
                return Enums.Parse(type, stringValue);
            } else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
                return GetListOf(type.GetGenericArguments()[0]);
            } else if (type.IsArray) {
                return GetArrayOf(type.GetElementType()!);
            } else {
                throw new ConfigurationException();
            }
        }
    }
}