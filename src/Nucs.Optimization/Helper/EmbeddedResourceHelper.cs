using Nucs.Optimization.Analyzer;

namespace Nucs.Optimization.Helper;

public static class EmbeddedResourceHelper {
    public static string? ReadEmbeddedResource(string resourceName) {
        var assembly = typeof(ParametersAnalyzer<>).Assembly;
        var resource = assembly.GetManifestResourceNames();
        var exactResourceName = resource.FirstOrDefault(r => r.EndsWith(resourceName));
        if (exactResourceName == null) return null;
        using (var stream = assembly.GetManifestResourceStream(exactResourceName))
        using (var reader = new StreamReader(stream))
            return reader.ReadToEnd();
    }
}