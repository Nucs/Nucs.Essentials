using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Nucs.Collections;

namespace Nucs.Configuration {
    public delegate bool SelectDelegate(KeyValuePair<string, ConfigNode> kv);
    public delegate bool SelectValueDelegate(ConfigNode kv);
    public delegate bool SelectKeyDelegate(string kv);

    public partial class XmlConfig {
        /// <summary>
        ///     Enumerates all keys in this xml config
        /// </summary>
        public Enumerable<Dictionary<string, ConfigNode>.KeyCollection.Enumerator, string> SelectKeys() {
            return new(Entries.Keys.GetEnumerator());
        }

        /// <summary>
        ///     Enumerates all keys in this xml config filtered by regex
        /// </summary>
        public Enumerable<RegexKeyEnumerator, string> SelectKeys(string regex, RegexOptions opts = RegexOptions.IgnoreCase | RegexOptions.Compiled) {
            return SelectKeys(new Regex(regex, opts));
        }

        /// <summary>
        ///     Enumerates all keys in this xml config filtered by regex
        /// </summary>
        public Enumerable<RegexKeyEnumerator, string> SelectKeys(Regex regex) {
            return new Enumerable<RegexKeyEnumerator, string>(new RegexKeyEnumerator(Entries.Keys.GetEnumerator(), regex));
        }

        /// <summary>
        ///     Enumerates all keys in this xml config filtered by regex
        /// </summary>
        public Enumerable<DelegateKeyEnumerator, string> SelectKeys(SelectKeyDelegate filter) {
            return new Enumerable<DelegateKeyEnumerator, string>(new DelegateKeyEnumerator(Entries.Keys.GetEnumerator(), filter));
        }

        /// <summary>
        ///     Enumerates all Values in this xml config
        /// </summary>
        public Enumerable<Dictionary<string, ConfigNode>.ValueCollection.Enumerator, ConfigNode> SelectValues() {
            return new(Entries.Values.GetEnumerator());
        }

        /// <summary>
        ///     Enumerates all Values in this xml config filtered by regex
        /// </summary>
        public Enumerable<RegexValueEnumerator, ConfigNode> SelectValues(Regex regex) {
            return new Enumerable<RegexValueEnumerator, ConfigNode>(new RegexValueEnumerator(Entries.Values.GetEnumerator(), regex));
        }

        /// <summary>
        ///     Enumerates all values in this xml config
        /// </summary>
        public Enumerable<RegexValueEnumerator, ConfigNode> SelectValues(string regex, RegexOptions opts = RegexOptions.IgnoreCase | RegexOptions.Compiled) {
            return SelectValues(new Regex(regex, opts));
        }

        /// <summary>
        ///     Enumerates all values in this xml config filtered by delegate
        /// </summary>
        public Enumerable<DelegateValueEnumerator, ConfigNode> SelectValues(SelectValueDelegate filter) {
            return new Enumerable<DelegateValueEnumerator, ConfigNode>(new DelegateValueEnumerator(Entries.Values.GetEnumerator(), filter));
        }

        /// <summary>
        ///     Enumerates all keys in this xml config filtered by regex
        /// </summary>
        public Enumerable<RegexEnumerator, KeyValuePair<string, ConfigNode>> Select(string regex, RegexOptions opts = RegexOptions.IgnoreCase | RegexOptions.Compiled) {
            return Select(new Regex(regex, opts));
        }

        /// <summary>
        ///     Enumerates all key-values in this xml config filtered by regex
        /// </summary>
        public Enumerable<RegexEnumerator, KeyValuePair<string, ConfigNode>> Select(Regex regex) {
            return new Enumerable<RegexEnumerator, KeyValuePair<string, ConfigNode>>(new RegexEnumerator(Entries.GetEnumerator(), regex));
        }

        /// <summary>
        ///     Enumerates all key-values in this xml config filtered by delegate
        /// </summary>
        public Enumerable<DelegateEnumerator, KeyValuePair<string, ConfigNode>> Select(SelectDelegate filter) {
            return new Enumerable<DelegateEnumerator, KeyValuePair<string, ConfigNode>>(new DelegateEnumerator(Entries.GetEnumerator(), filter));
        }

        /// <summary>
        ///     Enumerates all key-values in this xml config
        /// </summary>
        public Enumerable<Dictionary<string, ConfigNode>.Enumerator, KeyValuePair<string, ConfigNode>> Select() {
            return new Enumerable<Dictionary<string, ConfigNode>.Enumerator, KeyValuePair<string, ConfigNode>>(Entries.GetEnumerator());
        }

        public struct RegexEnumerator : IEnumerator<KeyValuePair<string, ConfigNode>> {
            private Dictionary<string, ConfigNode>.Enumerator _enumerator;
            private readonly Regex _filter;

            public RegexEnumerator(Dictionary<string, ConfigNode>.Enumerator enumerator, Regex filter) {
                _enumerator = enumerator;
                _filter = filter;
            }

            public bool MoveNext() {
                while (_enumerator.MoveNext()) {
                    if (!_filter.IsMatch(_enumerator.Current.Key))
                        continue;
                    return true;
                }

                return false;
            }

            public readonly KeyValuePair<string, ConfigNode> Current => _enumerator.Current;

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                ((IEnumerator) _enumerator).Reset();
            }

            public void Dispose() {
                _enumerator.Dispose();
            }
        }

        public struct DelegateEnumerator : IEnumerator<KeyValuePair<string, ConfigNode>> {
            private Dictionary<string, ConfigNode>.Enumerator _enumerator;
            private readonly SelectDelegate _filter;

            public DelegateEnumerator(Dictionary<string, ConfigNode>.Enumerator enumerator, SelectDelegate filter) {
                _enumerator = enumerator;
                _filter = filter;
            }

            public bool MoveNext() {
                while (_enumerator.MoveNext()) {
                    if (!_filter(_enumerator.Current))
                        continue;
                    return true;
                }

                return false;
            }

            public readonly KeyValuePair<string, ConfigNode> Current => _enumerator.Current;

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                ((IEnumerator) _enumerator).Reset();
            }

            public void Dispose() {
                _enumerator.Dispose();
            }
        }

        public struct RegexKeyEnumerator : IEnumerator<string> {
            private Dictionary<string, ConfigNode>.KeyCollection.Enumerator _enumerator;
            private readonly Regex _filter;

            public RegexKeyEnumerator(Dictionary<string, ConfigNode>.KeyCollection.Enumerator enumerator, Regex filter) {
                _enumerator = enumerator;
                _filter = filter;
            }

            public bool MoveNext() {
                while (_enumerator.MoveNext()) {
                    if (!_filter.IsMatch(_enumerator.Current))
                        continue;
                    return true;
                }

                return false;
            }

            public readonly string Current => _enumerator.Current;

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                ((IEnumerator) _enumerator).Reset();
            }

            public void Dispose() {
                _enumerator.Dispose();
            }
        }

        public struct DelegateKeyEnumerator : IEnumerator<string> {
            private Dictionary<string, ConfigNode>.KeyCollection.Enumerator _enumerator;
            private readonly SelectKeyDelegate _filter;

            public DelegateKeyEnumerator(Dictionary<string, ConfigNode>.KeyCollection.Enumerator enumerator, SelectKeyDelegate filter) {
                _enumerator = enumerator;
                _filter = filter;
            }

            public bool MoveNext() {
                while (_enumerator.MoveNext()) {
                    if (!_filter(_enumerator.Current))
                        continue;
                    return true;
                }

                return false;
            }

            public readonly string Current => _enumerator.Current;

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                ((IEnumerator) _enumerator).Reset();
            }

            public void Dispose() {
                _enumerator.Dispose();
            }
        }

        public struct KeyEnumerator : IEnumerator<string> {
            private Dictionary<string, ConfigNode>.KeyCollection.Enumerator _enumerator;
            private readonly SelectKeyDelegate _filter;

            public KeyEnumerator(Dictionary<string, ConfigNode>.KeyCollection.Enumerator enumerator, SelectKeyDelegate filter) {
                _enumerator = enumerator;
                _filter = filter;
            }

            public bool MoveNext() {
                while (_enumerator.MoveNext()) {
                    if (!_filter(_enumerator.Current))
                        continue;
                    return true;
                }

                return false;
            }

            public readonly string Current => _enumerator.Current;

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                ((IEnumerator) _enumerator).Reset();
            }

            public void Dispose() {
                _enumerator.Dispose();
            }
        }

        public struct RegexValueEnumerator : IEnumerator<ConfigNode> {
            private Dictionary<string, ConfigNode>.ValueCollection.Enumerator _enumerator;
            private readonly Regex _filter;

            public RegexValueEnumerator(Dictionary<string, ConfigNode>.ValueCollection.Enumerator enumerator, Regex filter) {
                _enumerator = enumerator;
                _filter = filter;
            }

            public bool MoveNext() {
                while (_enumerator.MoveNext()) {
                    if (!_filter.IsMatch(_enumerator.Current.Key))
                        continue;
                    return true;
                }

                return false;
            }

            public readonly ConfigNode Current => _enumerator.Current;

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                ((IEnumerator) _enumerator).Reset();
            }

            public void Dispose() {
                _enumerator.Dispose();
            }
        }

        public struct DelegateValueEnumerator : IEnumerator<ConfigNode> {
            private Dictionary<string, ConfigNode>.ValueCollection.Enumerator _enumerator;
            private readonly SelectValueDelegate _filter;

            public DelegateValueEnumerator(Dictionary<string, ConfigNode>.ValueCollection.Enumerator enumerator, SelectValueDelegate filter) {
                _enumerator = enumerator;
                _filter = filter;
            }

            public bool MoveNext() {
                while (_enumerator.MoveNext()) {
                    if (!_filter(_enumerator.Current))
                        continue;
                    return true;
                }

                return false;
            }

            public readonly ConfigNode Current => _enumerator.Current;

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                ((IEnumerator) _enumerator).Reset();
            }

            public void Dispose() {
                _enumerator.Dispose();
            }
        }
    }
}