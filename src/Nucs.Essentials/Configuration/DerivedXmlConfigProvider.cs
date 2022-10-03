using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Nucs.Configuration {
    public class DerivedXmlConfigProvider : XmlConfigProvider {
        private readonly ConcurrentDictionary<string, XmlConfig> _configs = new();
        private readonly ConcurrentDictionary<string, XmlConfig> _configsSubs = new();

        public IReadOnlyDictionary<string, XmlConfig> OverridenConfigs => _configs;
        public IReadOnlyDictionary<string, XmlConfig> OverridenSubConfigs => _configsSubs;

        private readonly XmlConfigProvider _parent;
        public XmlConfigProvider Parent => _parent;

        public DerivedXmlConfigProvider(KeyPathResolver pathResolver, XmlConfigProvider parent) : base(pathResolver) {
            _parent = parent;
        }

        public DerivedXmlConfigProvider(XmlConfigProvider parent) : base(parent.PathResolver) {
            _parent = parent;
        }

        /// <summary>
        ///     Creates a deep copy of this config object provider.
        /// </summary>
        public DerivedXmlConfigProvider Copy() {
            var ret = new DerivedXmlConfigProvider(PathResolver, _parent);
            foreach (var cfg in _configs) {
                ret._configs[cfg.Key] = cfg.Value.Copy();
            }

            foreach (var cfg in _configsSubs) {
                ret._configsSubs[cfg.Key] = cfg.Value.Copy();
            }

            return ret;
        }

        public virtual void AssignOverrideXml(XmlConfig xml) {
            _configs[xml.FileKey] = xml;
        }

        public virtual void AssignOverrideSubXml(XmlConfig xml) {
            _configsSubs[xml.FileKey] = xml;
        }

        public virtual XmlConfig OverrideXml(string key) {
            if (_configs.TryGetValue(key, out var overriden))
                return overriden;
            var copy = _parent.Config(key).Copy();

            foreach (var valueEntry in copy.Entries) {
                if (valueEntry.Value.ObjectValue is IRefersXmlConfig reference) {
                    reference.Config = copy;
                    copy.Entries[valueEntry.Key] = new ConfigNode(valueEntry.Key, reference);
                }
            }

            _configs[key]= copy;
            return copy;
        }

        public virtual XmlConfig OverrideSubXml(string key) {
            if (_configsSubs.TryGetValue(key, out var overriden))
                return overriden;
            var copy = _parent.SubConfig(key).Copy();

            foreach (var valueEntry in copy.Entries) {
                if (valueEntry.Value.ObjectValue is IRefersXmlConfig reference) {
                    reference.Config = copy;
                    copy.Entries[valueEntry.Key] = new ConfigNode(valueEntry.Key, reference);
                }

                if (valueEntry.Value.ObjectValue is IRefersXmlKey keyed) {
                    copy.Entries[valueEntry.Key] = new ConfigNode(valueEntry.Key, keyed.CloneSub(copy, key));
                }
            }

            _configsSubs[key] = copy;
            return copy;
        }

        public override XmlConfig Config(FileInfo file) {
            var filePath = file.FullName;
            var fileKey = Path.GetFileNameWithoutExtension(filePath);
            if (_configs.TryGetValue(fileKey, out var result))
                return result;

            var cfg = _parent.Config(file);

            foreach (var valueEntry in cfg.Entries) {
                if (valueEntry.Value.ObjectValue is IRefersXmlConfig reference) {
                    reference.Config = cfg;
                    cfg.Entries[valueEntry.Key] = new ConfigNode(valueEntry.Key, reference);
                }
            }

            _configs[fileKey] = cfg;
            return cfg;
        }

        public override XmlConfig SubConfig(FileInfo file, string key, string? newFileKey = null) {
            var filePath = file.FullName;
            var fileKey = Path.GetFileNameWithoutExtension(filePath);
            if (_configsSubs.TryGetValue(fileKey, out var result))
                return result;
            var cfg = _parent.SubConfig(file, key, newFileKey).Copy();
            foreach (var valueEntry in cfg.Entries) {
                if (valueEntry.Value.ObjectValue is IRefersXmlConfig reference) {
                    reference.Config = cfg;
                    cfg.Entries[valueEntry.Key] = new ConfigNode(valueEntry.Key, reference);
                }

                if (valueEntry.Value.ObjectValue is IRefersXmlKey keyed) {
                    cfg.Entries[valueEntry.Key] = new ConfigNode(valueEntry.Key, keyed.CloneSub(cfg, key));
                }
            }

            _configsSubs[fileKey] = cfg;
            return cfg;
        }

        public override XmlConfig Config(string key) {
            try {
                PathResolver(key, out string? filePath, out string fileKey);
                // ReSharper disable once VariableHidesOuterVariable
                if (_configs.TryGetValue(fileKey, out var result))
                    return result;

                var cfg = _parent.Config(key).Copy();
                foreach (var valueEntry in cfg.Entries) {
                    if (valueEntry.Value.ObjectValue is IRefersXmlConfig reference) {
                        reference.Config = cfg;
                        cfg.Entries[valueEntry.Key] = new ConfigNode(valueEntry.Key, reference);
                    }
                }

                _configs[fileKey] = cfg;
                return cfg;
            } catch (Exception e) {
                SystemHelper.Logger?.Error(e);
                throw;
            }
        }

        public override XmlConfig SubConfig(string key, string? newFileKey = null) {
            try {
                PathResolver(key, out string? filePath, out string fileKey);
                // ReSharper disable once VariableHidesOuterVariable
                if (_configsSubs.TryGetValue(key, out var result))
                    return result;
                var cfg = _parent.SubConfig(key, newFileKey).Copy();
                foreach (var valueEntry in cfg.Entries) {
                    if (valueEntry.Value.ObjectValue is IRefersXmlConfig reference) {
                        reference.Config = cfg;
                        cfg.Entries[valueEntry.Key] = new ConfigNode(valueEntry.Key, reference);
                    }

                    if (valueEntry.Value.ObjectValue is IRefersXmlKey keyed) {
                        cfg.Entries[valueEntry.Key] = new ConfigNode(valueEntry.Key, keyed.CloneSub(cfg, key));
                    }
                }

                _configsSubs[key] = cfg;
                return cfg;
            } catch (Exception e) {
                SystemHelper.Logger?.Error(e);
                throw;
            }
        }
    }
}