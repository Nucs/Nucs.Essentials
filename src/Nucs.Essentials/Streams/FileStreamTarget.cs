using System;
using System.IO;
using System.Text;

namespace Nucs.FileSystem {
    public class FileStreamTarget : IDisposable {
        private readonly FileStream _fileStream;
        public readonly StreamWriter Writer;

        public FileStreamTarget(string targetFile, bool @override = true) {
            if (string.IsNullOrEmpty(targetFile)) throw new ArgumentException("Value cannot be null or empty.", nameof(targetFile));
            targetFile = Path.GetFullPath(targetFile);
            if (@override && File.Exists(targetFile)) {
                File.Delete(targetFile);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            _fileStream = new FileStream(targetFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            Writer = new StreamWriter(_fileStream, Encoding.UTF8, 8064, leaveOpen: true);
            Writer.AutoFlush = true;
        }

        public void Dispose() {
            Writer.Flush();
            Writer.Dispose();
            _fileStream.Flush();
            _fileStream?.Dispose();
        }
    }

    public struct FileStreamTargetStruct : IDisposable {
        private readonly FileStream _fileStream;
        public readonly StreamWriter Writer;

        public FileStreamTargetStruct(string targetFile, bool @override = true, bool autoFlush = true, int bufferSize = 8064) {
            if (string.IsNullOrEmpty(targetFile)) throw new ArgumentException("Value cannot be null or empty.", nameof(targetFile));
            targetFile = Path.GetFullPath(targetFile);
            if (@override && File.Exists(targetFile)) {
                File.Delete(targetFile);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            _fileStream = new FileStream(targetFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            Writer = new StreamWriter(_fileStream, Encoding.UTF8, bufferSize, leaveOpen: true) { AutoFlush = autoFlush };
        }

        public void Dispose() {
            Writer.Flush();
            Writer.Dispose();
            _fileStream.Flush();
            _fileStream?.Dispose();
        }
    }

    public class SafeFileStreamWriter : IDisposable {
        private FileStream _fileStream;
        public readonly StreamWriter Writer;

        public SafeFileStreamWriter(string targetFile, bool @override = true, bool autoFlush = true, int bufferSize = 8064) {
            if (string.IsNullOrEmpty(targetFile)) throw new ArgumentException("Value cannot be null or empty.", nameof(targetFile));
            targetFile = Path.GetFullPath(targetFile).Trim('\\', '/');
            if (@override && File.Exists(targetFile)) {
                Retry.Do(() => { File.Delete(targetFile); }, TimeSpan.FromMilliseconds(100), 300);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            Retry.Do(() => { _fileStream = new FileStream(targetFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read); }, TimeSpan.FromMilliseconds(100), 300);
            Writer = new StreamWriter(_fileStream, Encoding.UTF8, bufferSize, leaveOpen: true) { AutoFlush = autoFlush };
        }

        public void Dispose() {
            Writer.Flush();
            Writer.Dispose();
            _fileStream.Flush();
            _fileStream?.Dispose();
        }
    }
}