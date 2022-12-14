using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Nucs.Essentials.UnitTests.Resources {
    public static class EmbeddedResourceHelper {
        public static string? ReadEmbeddedResource(string resourceName) {
            var assembly = Assembly.GetExecutingAssembly();
            var resource = assembly.GetManifestResourceNames();
            var exactResourceName = resource.FirstOrDefault(r => r.EndsWith(resourceName));
            if (exactResourceName == null) return null;
            using (var stream = assembly.GetManifestResourceStream(exactResourceName))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}