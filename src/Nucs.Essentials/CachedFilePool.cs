using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nucs {
    public sealed class CachedFilePool {
        public static TimeSpan? DefaultTimeout;

        internal readonly ConcurrentDictionary<string, Access> _cachedText = new ConcurrentDictionary<string, Access>();
        internal readonly ConcurrentDictionary<string, Access> _cachedLines = new ConcurrentDictionary<string, Access>();
        internal readonly ConcurrentDictionary<string, Access> _cachedBytes = new ConcurrentDictionary<string, Access>();
        internal readonly ConcurrentDictionary<string, bool> _cachedExists = new ConcurrentDictionary<string, bool>();
        internal readonly ConcurrentDictionary<string, long> _cachedLengths = new ConcurrentDictionary<string, long>();

        /// <summary>
        ///     Will print the results of all accesses.
        /// </summary>
        public bool Verbose { get; set; }

        public void FlushExistsCache() {
            _cachedExists.Clear();
        }

        public void FlushAll() {
            _cachedText.Clear();
            _cachedLines.Clear();
            _cachedBytes.Clear();
            _cachedExists.Clear();
        }

        public bool Exists(string path) {
            if (string.IsNullOrEmpty(path))
                return false;
            return _cachedExists.GetOrAdd(new FileInfo(path).FullName, _fileFactory);
        }

        public bool Exists(FileInfo path) {
            return _cachedExists.GetOrAdd(path.FullName, _fileFactory);
        }

        public bool ExistsDirectory(string path) {
            if (string.IsNullOrEmpty(path))
                return false;

            return _cachedExists.GetOrAdd(new DirectoryInfo(path).FullName, _directoryFactory);
        }

        public bool ExistsDirectory(DirectoryInfo path) {
            return _cachedExists.GetOrAdd(path.FullName, _directoryFactory);
        }

        private bool _fileFactory(string s) {
            var result = File.Exists(s);
            if (Verbose)
                SystemHelper.Logger?.Trace($"File exists: {result}, '{s}'");
            return result;
        }

        private bool _directoryFactory(string s) {
            var result = Directory.Exists(s);
            if (Verbose)
                SystemHelper.Logger?.Trace($"Directory exists: {result}, '{s}'");
            return result;
        }

        public string ReadAllText(string path, TimeSpan? timeout = null) {
            return _cachedText.GetOrAdd(new FileInfo(path).FullName, s => new Access(this, s, timeout ?? DefaultTimeout) { _type = 1 })
                              .ReadAllText();
        }

        public string[] ReadAllLines(string path, TimeSpan? timeout = null) {
            return _cachedLines.GetOrAdd(new FileInfo(path).FullName, s => new Access(this, s, timeout ?? DefaultTimeout) { _type = 2 })
                               .ReadAllLines();
        }

        public byte[] ReadAllBytes(string path, TimeSpan? timeout = null) {
            return _cachedBytes.GetOrAdd(new FileInfo(path).FullName, s => new Access(this, s, timeout ?? DefaultTimeout) { _type = 3 })
                               .ReadAllBytes();
        }

        public long Length(string path) {
            return _cachedLengths.GetOrAdd(new FileInfo(path).FullName, s => new FileInfo(s).Length);
        }

        public sealed class Access {
            internal byte _type;
            private int _accesses;
            private int _accessesWhenScheduled;
            private TimeSpan? ExpireAfter;
            private object _cache;
            private readonly CachedFilePool _pool;

            public readonly string Path;

            public Access(CachedFilePool pool, string path, TimeSpan? expireAfter) {
                _pool = pool;
                Path = new FileInfo(path).FullName;
                ExpireAfter = expireAfter;
                if (expireAfter.HasValue && ExpireAfter.Value != TimeSpan.Zero) {
                    Task.Delay(expireAfter.Value).ContinueWith(_ => OnExpiring());
                }
            }

            private void OnExpiring() {
                if (_accessesWhenScheduled == _accesses) {
                    lock (Path) {
                        //expired
                        switch (_type) {
                            case 1:
                                _pool._cachedText.TryRemove(Path, out _);
                                break;
                            case 2:
                                _pool._cachedLines.TryRemove(Path, out _);
                                break;
                            case 3:
                                _pool._cachedBytes.TryRemove(Path, out _);
                                break;
                            default:
                                break;
                        }

                        _cache = null; //release
                        return;
                    }
                }

                //it was accessed during wait
                _accessesWhenScheduled = _accesses;
                Task.Delay(ExpireAfter!.Value).ContinueWith(_ => OnExpiring());
            }

            public string ReadAllText() {
                Interlocked.Increment(ref _accesses);
                // ReSharper disable once InconsistentlySynchronizedField
                var content = _cache;
                if (content != null)
                    return (string) content;

                lock (Path) {
                    if (_cache != null)
                        return (string) _cache;

                    return (string) (_cache = SystemHelper.ReadAllText(Path));
                }
            }

            public string[] ReadAllLines() {
                Interlocked.Increment(ref _accesses);
                // ReSharper disable once InconsistentlySynchronizedField
                var content = _cache;
                if (content != null)
                    return (string[]) content;

                lock (Path) {
                    if (_cache != null)
                        return (string[]) _cache;

                    return (string[]) (_cache = SystemHelper.ReadAllLines(Path));
                }
            }

            public byte[] ReadAllBytes() {
                Interlocked.Increment(ref _accesses);
                // ReSharper disable once InconsistentlySynchronizedField
                var content = _cache;
                if (content != null)
                    return (byte[]) content;

                lock (Path) {
                    if (_cache != null)
                        return (byte[]) _cache;

                    return (byte[]) (_cache = SystemHelper.ReadAllBytes(Path));
                }
            }
        }
    }
}