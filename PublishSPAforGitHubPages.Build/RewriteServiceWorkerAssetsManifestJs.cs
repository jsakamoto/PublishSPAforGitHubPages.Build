using System;
using System.IO;
using System.Linq;
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
        [Required] public string IndexHtml { get; set; }

        [Required] public string ServiceWorkerAssetsManifestJs { get; set; }

        public bool InjectBrotliLoader { get; set; } = true;

        public override bool Execute()
        {
            var serviceWorkerAssetsJs = File.ReadAllText(this.ServiceWorkerAssetsManifestJs);
            serviceWorkerAssetsJs = Regex.Replace(serviceWorkerAssetsJs, @"^self\.assetsManifest\s*=\s*", "");
            serviceWorkerAssetsJs = Regex.Replace(serviceWorkerAssetsJs, ";\\s*$", "");
            var serviceWorkerAssetsJsBytes = Encoding.UTF8.GetBytes(serviceWorkerAssetsJs);

            var jsonSerializer = new DataContractJsonSerializer(typeof(AssetsManifestFile));

            using var jsonReader = JsonReaderWriterFactory.CreateJsonReader(serviceWorkerAssetsJsBytes, XmlDictionaryReaderQuotas.Max);
            var assetsManifestFile = jsonSerializer.ReadObject(jsonReader) as AssetsManifestFile;
            if (assetsManifestFile == null) return false;
            if (assetsManifestFile.assets == null) return false;

            var indexHtmlName = Path.GetFileName(this.IndexHtml);
            var assetManifestEntry = assetsManifestFile.assets.First(a => Path.GetFileName(a.url) == indexHtmlName);
            if (assetManifestEntry == null) return false;

            using (var indexHtmlStream = File.OpenRead(this.IndexHtml))
            {
                using var sha256 = SHA256.Create();
                assetManifestEntry.hash = "sha256-" + Convert.ToBase64String(sha256.ComputeHash(indexHtmlStream));
            }

            using (var serviceWorkerAssetsStream = File.OpenWrite(this.ServiceWorkerAssetsManifestJs))
            {
                using var streamWriter = new StreamWriter(serviceWorkerAssetsStream, Encoding.UTF8, 50, leaveOpen: true);
                streamWriter.Write("self.assetsManifest = ");
                streamWriter.Flush();
                using var jsonWriter = JsonReaderWriterFactory.CreateJsonWriter(serviceWorkerAssetsStream, Encoding.UTF8, ownsStream: false, indent: true);
                jsonSerializer.WriteObject(jsonWriter, assetsManifestFile);
                jsonWriter.Flush();
                streamWriter.WriteLine(";");
            }
            return true;
        }
    }
}
