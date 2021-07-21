using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RadiantPi.Core.Utility {

    public static class AssemblyEx {

        //--- Extension Methods ---
        public static string ReadManifestResource(this System.Reflection.Assembly assembly, string resourceName, bool convertLineEndings = true) {

            // load resource stream
            using var resource = assembly.GetManifestResourceStream(resourceName) ?? throw new ApplicationException($"unable to locate embedded resource: '{resourceName}'");

            // check if resource stream has to be decompressed
            using var stream = resourceName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                ? new GZipStream(resource, CompressionMode.Decompress)
                : resource;

            // parse resource stream into a string
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var result = reader.ReadToEnd();

            // optionally remove carriage return characters
            if(convertLineEndings) {
                result = result.Replace("\r", "");
            }
            return result;
        }
    }
}