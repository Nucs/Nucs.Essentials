using System;
using System.IO;

namespace Nucs.Essentials.UnitTests;

public class TempFile : IDisposable {
    public string Path { get; }

    public TempFile() {
        Path = System.IO.Path.GetTempFileName();
    }

    public TempFile(string extension) : this() {
        Path = System.IO.Path.ChangeExtension(Path, extension);
    }

    public FileInfo FileInfo => new FileInfo(Path);

    public void Dispose() {
        File.Delete(Path);
    }

    public static implicit operator FileInfo(TempFile file) =>
        file.FileInfo;

    public override string ToString() {
        return Path;
    }
}