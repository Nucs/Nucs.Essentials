using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Nucs.Extensions {
    public static class EnumHelper<T> where T : Enum {
        public static readonly IDictionary<T, string> Map = new Dictionary<T, string>();
    }

    public static class EnumHelper {
        public static string GetDescription<T>(this T enumValue) where T : Enum {
            if (EnumHelper<T>.Map.TryGetValue(enumValue, out var value))
                return value;

            var memInfo = typeof(T).GetMember(enumValue.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            value = ((DescriptionAttribute) attributes[0]).Description;

            EnumHelper<T>.Map.Add(enumValue, value);
            return value;
        }

        public static T FromDisplayName<T>(string displayName) where T : struct, IConvertible {
            List<T> items = GetItems<T>(false);
            // Check for a display name match
            foreach (T item in items) {
                string itemDisplayName = GetEnumDisplayName(item as Enum);
                if (itemDisplayName == displayName) {
                    return item;
                }
            }

            // Check for a ordinary name match
            foreach (T item in items) {
                if (item.ToString() == displayName) {
                    return item;
                }
            }

            return GetDefault<T>();
        }

        public static T GetDefault<T>() {
            if (IsDefined<T>("None")) {
                return (T) Enum.Parse(typeof(T), "None", true);
            } else if (IsDefined<T>(0)) {
                return (T) Enum.ToObject(typeof(T), 0);
            } else {
                throw new ArgumentException("No default value found for type " + typeof(T).FullName);
            }
        }

        /// <summary>
        /// Define the enum description as follows and the specified description attribute is what will get returned from
        /// this method, otherwise, just the text of the enum name itself.
        ///
        /// public enum MyEnum
        /// {
        ///   [Description("Empty")]
        ///   None,
        ///   [Description("First Item")]
        ///   Item1,
        ///   [Description("Second Item")]
        ///   Item2
        /// }
        /// </summary>
        public static string GetEnumDescription(Enum value) {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[]) fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes != null && attributes.Length > 0) {
                return attributes[0].Description;
            } else {
                return value.ToString();
            }
        }

        /// <summary>
        /// Define the enum display name as follows.
        ///
        /// public enum MyEnum
        /// {
        ///   [Display(Name = "None")]
        ///   None,
        ///   [Display(Name = "Item1")]
        ///   Item1,
        ///   [Display(Name = "Item2")]
        ///   Item2
        /// }
        /// </summary>
        public static string GetEnumDisplayName(Enum value) {
            DisplayAttribute att = value.GetType().GetCustomAttribute<DisplayAttribute>();
            if (att != null) {
                return att.Name;
            } else {
                return value.ToString();
            }
        }

        public static List<object> GetItems(Type enumType, bool excludeNone = false) {
            if (!enumType.IsEnum) {
                throw new ArgumentException("Type must be an enum type.");
            }

            int noneValue = -1;
            if (excludeNone) {
                noneValue = GetNoneValue(enumType);
            }

            int[] items = (int[]) Enum.GetValues(enumType);
            List<object> output = new List<object>();
            foreach (int val in items) {
                if (noneValue > -1 && noneValue == val) {
                    continue;
                }

                output.Add(val);
            }

            return output;
        }

        /// <summary>
        /// Gets a list of all instances of enum items within the specified enum.
        /// </summary>
        public static List<T> GetItems<T>(bool excludeNone = false) where T : struct, IConvertible {
            if (!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enum type.");
            }

            int noneValue = -1;
            if (excludeNone) {
                noneValue = GetNoneValue<T>();
            }

            List<T> list = new List<T>();
            foreach (T value in Enum.GetValues(typeof(T))) {
                if (noneValue > -1 && noneValue == GetValue<T>(value)) {
                    continue;
                }

                list.Add(value);
            }

            //if (excludeNone)
            //{
            //    if (Enum.TryParse("None", true, out T noneValue))
            //    {
            //        list.Remove(noneValue);
            //    }
            //}

            return list;
        }

        public static int GetNoneValue<T>() where T : struct, IConvertible {
            if (!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enum type.");
            }

            if (Enum.TryParse("None", true, out T noneValue)) {
                return GetValue<T>(noneValue);
            } else {
                return -1;
            }
        }

        public static int GetNoneValue(Type enumType) {
            if (!enumType.IsEnum) {
                throw new ArgumentException("Type must be an enum type.");
            }

            string[] names = Enum.GetNames(enumType);
            int indexOfNone = -1;
            for (int i = 0; i < names.Length; i++) {
                if (names[i] == "None") {
                    indexOfNone = i;
                    break;
                }
            }

            if (indexOfNone > -1) {
                int[] values = (int[]) Enum.GetValues(enumType);
                return values[indexOfNone];
            } else {
                return -1;
            }
        }

        public static int GetValue<T>(T value) where T : struct, IConvertible {
            Type t = typeof(T);
            if (!t.IsEnum) {
                throw new ArgumentException("T must be an enum type.");
            }

            return value.ToInt32(CultureInfo.InvariantCulture.NumberFormat);
        }

        public static int GetValue(object enumValue) {
            Type t = enumValue.GetType();
            if (!t.IsEnum) {
                throw new ArgumentException("T must be an enum type.");
            }

            return ((IConvertible) enumValue).ToInt32(CultureInfo.InvariantCulture.NumberFormat);
        }

        public static List<int> GetValues(Type enumType, bool excludeNone = false) {
            int noneValue = -1;
            if (excludeNone) {
                noneValue = GetNoneValue(enumType);
            }

            Array items = Enum.GetValues(enumType);
            List<int> intValues = new List<int>();
            foreach (var en in items) {
                if (excludeNone && (int) en == noneValue) {
                    continue;
                }

                intValues.Add((int) en);
            }

            return intValues;
        }

        public static List<int> GetValues<T>(bool excludeNone = false) {
            return GetValues(typeof(T), excludeNone);
        }

        /// <summary>
        /// Gets a sorted dictionary of the integer and string representations of each item in the enumeration.  The
        /// keys are integers and the values are strings.
        /// </summary>
        public static SortedDictionary<int, string> GetValues<T>() where T : struct, IConvertible {
            if (!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enum type.");
            }

            SortedDictionary<int, string> sortedList = new SortedDictionary<int, string>();
            Type enumType = typeof(T);
            foreach (T value in Enum.GetValues(enumType)) {
                FieldInfo fi = enumType.GetField(value.ToString());
                DescriptionAttribute[] attributes = (DescriptionAttribute[]) fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes != null && attributes.Length > 0) {
                    DescriptionAttribute description = (DescriptionAttribute) attributes[0];
                    sortedList.Add(value.ToInt32(CultureInfo.CurrentCulture.NumberFormat), description.Description);
                } else {
                    sortedList.Add(value.ToInt32(CultureInfo.CurrentCulture.NumberFormat), value.ToString());
                }
            }

            return sortedList;
        }

        public static bool IsDefined<T>(string strEnumText) {
            string s = strEnumText.Trim().ToUpper();
            string[] names = Enum.GetNames(typeof(T));
            foreach (string n in names) {
                if (string.Equals(n, s, StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsDefined<T>(int iEnumValue) {
            int[] vals = (int[]) Enum.GetValues(typeof(T));
            foreach (int i in vals) {
                if (iEnumValue == i) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns default if it can't parse.  If no default found, then
        /// throws exception.
        /// </summary>
        public static T Parse<T>(int iEnumValue) {
            if (IsDefined<T>(iEnumValue)) {
                T rt = (T) Enum.ToObject(typeof(T), iEnumValue);
                return rt;
            } else {
                return GetDefault<T>();
            }
        }

        /// <summary>
        /// Returns default if it can't parse.  If no default found, then
        /// throws exception.
        /// </summary>
        public static T Parse<T>(string strEnumText) where T : struct {
            if (IsDefined<T>(strEnumText)) {
                T pm = (T) Enum.Parse(typeof(T), strEnumText, true);
                return pm;
            } else {
                return GetDefault<T>();
            }
        }

        public static bool TryParse(Type enumType, string value, out object objectEnum) {
            objectEnum = null;
            try {
                objectEnum = Enum.Parse(enumType, value);
                return true;
            } catch {
                return false;
            }
        }

        public static bool TryParse<T>(string text, out T enumValue) where T : struct {
            if (IsDefined<T>(text)) {
                enumValue = (T) Enum.Parse(typeof(T), text, true);
                return true;
            } else {
                enumValue = GetDefault<T>();
                return false;
            }
        }
    }
}