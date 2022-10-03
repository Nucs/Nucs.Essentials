using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Nucs.Configuration {
    public class SharedXmlConfigProvider : CachedXmlConfigProvider {
        public SharedXmlConfigProvider(string directory, SearchOption opts, KeyPathResolver pathResolver) : base(directory, opts, pathResolver) { }
        public SharedXmlConfigProvider(KeyPathResolver pathResolver) : base(pathResolver) { }
        public SharedXmlConfigProvider() { }
    }

    public class CachedXmlConfigProvider : XmlConfigProvider {
        private readonly ConcurrentDictionary<string, XmlConfig> _configs = new();
        private readonly ConcurrentDictionary<string, XmlConfig> _configsSubs = new();

        public CachedXmlConfigProvider(string directory, SearchOption opts, KeyPathResolver pathResolver) : base(pathResolver) {
            LoadDirectory(directory, opts);
        }

        public CachedXmlConfigProvider(KeyPathResolver pathResolver) : base(pathResolver) { }
        public CachedXmlConfigProvider() : base(DefaultKeyResolver) { }

        public void LoadDirectory(string directory, SearchOption opts = SearchOption.AllDirectories) {
            foreach (var file in Directory.GetFiles(directory, "*.xml", opts)) {
                var fileKey = Path.GetFileNameWithoutExtension(file);
                _configs[fileKey] = Config(fileKey);
            }
        }

        public void LoadFiles(IList<string> paths) {
            foreach (var file in paths) {
                if (!file.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                var fileKey = Path.GetFileNameWithoutExtension(file);
                _configs[fileKey] = Config(fileKey);
            }
        }

        public override XmlConfig Config(FileInfo file) {
            var filePath = file.FullName;
            var fileKey = Path.GetFileNameWithoutExtension(filePath);
            return _configs.GetOrAdd(fileKey, static (fileKey
                                                      , p) => {
                var cfg = p.Item1.ReadConfigInternal(p.filePath, fileKey);
                return cfg;
            }, (this, filePath));
        }

        public override XmlConfig SubConfig(FileInfo file, string key, string? newFileKey = null) {
            var filePath = file.FullName;
            var fileKey = Path.GetFileNameWithoutExtension(filePath);
            // ReSharper disable once VariableHidesOuterVariable
            return _configsSubs.GetOrAdd(key, static (_, p) => {
                return p._configs.GetOrAdd(p.fileKey, static (fileKey, p) => {
                             var cfg = p.Item1.ReadConfigInternal(p.filePath, fileKey);
                             p._configs[fileKey] = cfg;
                             return cfg;
                         }, (p.Item1, p.filePath, p._configs))
                        .SubConfig(p.key, p.newFileKey ?? p.key);
            }, (this, key, fileKey, newFileKey, filePath, _configs));
        }

        public override XmlConfig Config(string key) {
            try {
                PathResolver(key, out string filePath, out string fileKey);
                // ReSharper disable once VariableHidesOuterVariable
                return _configs.GetOrAdd(fileKey, static (fileKey
                                                          , p) => {
                    var cfg = p.Item1.ReadConfigInternal(p.filePath, fileKey);
                    return cfg;
                }, (this, filePath));
            } catch (Exception e) {
                SystemHelper.Logger?.Error(e);
                throw;
            }
        }

        public override XmlConfig SubConfig(string key, string? newFileKey = null) {
            try {
                PathResolver(key, out string filePath, out string fileKey);
                // ReSharper disable once VariableHidesOuterVariable
                return _configsSubs.GetOrAdd(key, static (_, p) => {
                    return p._configs.GetOrAdd(p.fileKey, static (fileKey, p) => {
                                 var cfg = p.Item1.ReadConfigInternal(p.filePath, fileKey);
                                 p._configs[fileKey] = cfg;
                                 return cfg;
                             }, (p.Item1, p.filePath, p._configs))
                            .SubConfig(p.key, p.newFileKey ?? p.key);
                }, (this, key, fileKey, newFileKey, filePath, _configs));
            } catch (Exception e) {
                SystemHelper.Logger?.Error(e);
                throw;
            }
        }
    }
}