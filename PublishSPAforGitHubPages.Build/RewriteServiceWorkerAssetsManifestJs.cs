using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Build.Framework;
using PublishSPAforGHPages.Models;

namespace PublishSPAforGHPages
{
    public class RewriteServiceWorkerAssetsManifestJs : Microsoft.Build.Utilities.Task
    {
        [Required] public string WebRootPath { get; set; }

        [Required] public string ServiceWorkerAssetsManifestJs { get; set; }

        public bool InjectBrotliLoader { get; set; } = true;

        public override bool Execute()
        {
            var jsonSerializer = new DataContractJsonSerializer(typeof(AssetsManifestFile));

            // Load a "service-worker-assets.js" to a model object.
            if (!this.TryLoadAssetsManifestFile(jsonSerializer, out var assetsManifestFile)) return false;

            using var sha256 = SHA256.Create();

            // Update a hash code of a "index.html".
            var indexHtmlAssets = assetsManifestFile.assets.Where(a => a.url.EndsWith("/index.html") || a.url == "index.html");
            foreach (var indexHtmlAsset in indexHtmlAssets)
            {
                indexHtmlAsset.hash = GetHashCode(sha256, this.GetFullPath(indexHtmlAsset));
            }

            // If the Brotli Loader is enabled, update all hash codes of asset entries that are compressed,
            // and add asset entries for brotli loader JavaScript files.
            if (this.InjectBrotliLoader)
            {
                foreach (var asset in assetsManifestFile.assets)
                {
                    // ...but some kinds of files have to exclude.
                    if (asset.url.ToLower().EndsWith(".html")) continue;
                    if (asset.url == "_framework/blazor.webassembly.js") continue;
                    if (Regex.IsMatch(asset.url, @"^_framework/dotnet(\..*)?\.js$")) continue;

                    var path = this.GetFullPath(asset);
                    var compressedPath = path + ".br";
                    if (!File.Exists(compressedPath)) continue;

                    asset.url += ".br";
                    asset.hash = GetHashCode(sha256, compressedPath);
                }

                var brotliLoaderScriptFiles = new[] { "decode.min.js", "brotliloader.min.js" };
                var brotliLoaderScriptEntries = brotliLoaderScriptFiles.Select(name =>
                {
                    return new AssetsManifestFileEntry
                    {
                        url = name,
                        hash = GetHashCode(sha256, Path.Combine(this.WebRootPath, name))
                    };
                });
                assetsManifestFile.assets = assetsManifestFile.assets.Concat(brotliLoaderScriptEntries).ToArray();
            }

            // Write back the model object to the "service-worker-assets.js"
            this.WriteAssetsManifestFile(jsonSerializer, assetsManifestFile);
            return true;
        }

        private bool TryLoadAssetsManifestFile(XmlObjectSerializer serializer, out AssetsManifestFile assetsManifestFile)
        {
            var serviceWorkerAssetsJs = File.ReadAllText(this.ServiceWorkerAssetsManifestJs);
            serviceWorkerAssetsJs = Regex.Replace(serviceWorkerAssetsJs, @"^self\.assetsManifest\s*=\s*", "");
            serviceWorkerAssetsJs = Regex.Replace(serviceWorkerAssetsJs, ";\\s*$", "");
            var serviceWorkerAssetsJsBytes = Encoding.UTF8.GetBytes(serviceWorkerAssetsJs);
            using var jsonReader = JsonReaderWriterFactory.CreateJsonReader(serviceWorkerAssetsJsBytes, XmlDictionaryReaderQuotas.Max);
            assetsManifestFile = serializer.ReadObject(jsonReader) as AssetsManifestFile;
            if (assetsManifestFile == null) return false;
            if (assetsManifestFile.assets == null) return false;
            return true;
        }

        private string GetFullPath(AssetsManifestFileEntry asset)
        {
            return Path.GetFullPath(Path.Combine(this.WebRootPath, asset.url));
        }

        private static string GetHashCode(HashAlgorithm hashAlgorithm, string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return "sha256-" + Convert.ToBase64String(hashAlgorithm.ComputeHash(stream));
        }

        private void WriteAssetsManifestFile(XmlObjectSerializer serializer, AssetsManifestFile assetsManifestFile)
        {
            using var serviceWorkerAssetsStream = File.OpenWrite(this.ServiceWorkerAssetsManifestJs);
            using var streamWriter = new StreamWriter(serviceWorkerAssetsStream, Encoding.UTF8, 50, leaveOpen: true);
            streamWriter.Write("self.assetsManifest = ");
            streamWriter.Flush();
            using var jsonWriter = JsonReaderWriterFactory.CreateJsonWriter(serviceWorkerAssetsStream, Encoding.UTF8, ownsStream: false, indent: true);
            serializer.WriteObject(jsonWriter, assetsManifestFile);
            jsonWriter.Flush();
            streamWriter.WriteLine(";");
        }
    }
}
