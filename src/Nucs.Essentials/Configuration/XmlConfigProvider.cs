using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nucs.DependencyInjection;

namespace Nucs.Configuration {
    public delegate void KeyPathResolver(string key, out string xmlPath, out string fileKey);

    /// <summary>
    ///     Provides XML configuration from Live/Binary/Config directory by default.
    /// </summary>
    public class XmlConfigProvider {
        private static readonly string loneLiveXmls = @"(\<[\d\w]+\s)live(\s?=\s?[\""\'][\S\d\w]+[\""\']\s*\/\>)";
        private static readonly string loneLiveXmls_ConfirmNoMatches = @"[\d\w]\slive(\s?=\s?[\""\'][\S\d\w]+[\""\']\s*\/\>)";
        private static readonly string loneLiveXmlsReplacement = @"$1value$2";
        private static readonly StringComparison StringComparison = StringComparison.OrdinalIgnoreCase;
        public static KeyPathResolver DefaultKeyResolver = Default;
        public static Dictionary<string, string> InheritanceMap = new();


        protected internal readonly KeyPathResolver PathResolver;

        public XmlConfigProvider(KeyPathResolver pathResolver) {
            PathResolver = pathResolver;
        }

        public XmlConfigProvider() : this(DefaultKeyResolver) { }

        public XmlConfig this[string key] => Config(key);

        public bool LoadingConfiguration { get; set; }

        public virtual XmlConfig Config(string key) {
            try {
                PathResolver(key, out string filePath, out string fileKey);
                return ReadConfigInternal(filePath, fileKey);
            } catch (Exception e) {
                SystemHelper.Logger?.Error(e);
                throw;
            }
        }

        public virtual XmlConfig Config(FileInfo file) {
            try {
                var filePath = file.FullName;
                var fileKey = Path.GetFileNameWithoutExtension(filePath);
                return ReadConfigInternal(filePath, fileKey);
            } catch (Exception e) {
                SystemHelper.Logger?.Error(e);
                throw;
            }
        }

        protected virtual XmlConfig ReadConfigInternal(string filePath, string fileKey) {
            LoadingConfiguration = true;
            try {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 131072);
                using var sr = new StreamReader(fs, Encoding.UTF8);

                if (Path.GetExtension(filePath).Trim('.', ' ') != "xml")
                    throw new NotSupportedException($"Unable to parse configuration from {filePath}");
                XmlConfig cfg = new XmlConfig(filePath, fileKey, sr.ReadToEnd());
                XmlConfig baseTarget = cfg;

                string? lastKnownParent = null;
                //perform inheritance on non cs classes
                if (!string.IsNullOrEmpty(baseTarget.Inheriting) && (lastKnownParent == null || !Types.Known.ContainsKey(baseTarget.Inheriting))) {
                    InheritanceMap.TryAdd(baseTarget.FileKey, baseTarget.Inheriting);
                    lastKnownParent = baseTarget.Inheriting;
                    baseTarget = Config(baseTarget.Inheriting); //Config takes care of the entire parent call-pipe incl caching
                    cfg = cfg.Merge(baseTarget);
                }

                if (lastKnownParent != null) {
                    cfg.Inheriting = lastKnownParent;
                    if (Types.Known.ContainsKey(lastKnownParent)) {
                        cfg.ClassedParent = lastKnownParent;
                    } else {
                        var inherited = lastKnownParent;
                        var inheriting = inherited;
                        while (inheriting != null && InheritanceMap.TryGetValue(inherited!, out inheriting)) {
                            if (string.IsNullOrEmpty(inheriting))
                                break;
                            inherited = inheriting;
                        }

                        if (!string.IsNullOrEmpty(inherited))
                            cfg.ClassedParent = inherited;
                    }
                }

                return cfg;
            } catch (Exception e) {
                throw;
            } finally {
                LoadingConfiguration = false;
            }
        }

        public virtual XmlConfig SubConfig(FileInfo file, string key, string? newFileKey = null) {
            try {
                var filePath = file.FullName;
                var fileKey = Path.GetFileNameWithoutExtension(filePath);
                return ReadConfigInternal(filePath, fileKey).SubConfig(key, newFileKey);
            } catch (Exception e) {
                SystemHelper.Logger?.Error(e);
                throw;
            }
        }

        public virtual XmlConfig SubConfig(string key, string? newFileKey = null) {
            try {
                PathResolver(key, out string filePath, out string fileKey);
                return ReadConfigInternal(filePath, fileKey).SubConfig(key, newFileKey);
            } catch (Exception e) {
                SystemHelper.Logger?.Error(e);
                throw;
            }
        }

        private static void Default(string key, out string? xmlPath, out string fileKey) {
            var index = key.IndexOf('.', 2);
            fileKey = index == -1 ? key : key.Substring(0, index);
            xmlPath = SystemHelper.TryGetSettingsFile($"{fileKey}.xml");
        }
    }
}