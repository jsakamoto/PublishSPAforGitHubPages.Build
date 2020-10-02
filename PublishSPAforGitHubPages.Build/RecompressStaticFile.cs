using System.IO;
using System.IO.Compression;
using Brotli;
//using BrotliSharpLib;
using Microsoft.Build.Framework;

namespace PublishSPAforGHPages
{
    public class RecompressStaticFile : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string File { get; set; }

        public bool EnableGzip { get; set; } = true;

        public bool EnableBrotli { get; set; } = true;

        public override bool Execute()
        {
            using var sourceStream = System.IO.File.OpenRead(this.File);

            if (EnableGzip)
            {
                using var outputStream = System.IO.File.Create(this.File + ".gz");
                using var compressingStream = new GZipStream(outputStream, CompressionLevel.Optimal);
                sourceStream.Seek(0, SeekOrigin.Begin);
                sourceStream.CopyTo(compressingStream);
            }

            if (EnableBrotli)
            {
                using var outputStream = System.IO.File.Create(this.File + ".br");
                using var compressingStream = new BrotliStream(outputStream, CompressionMode.Compress);
                compressingStream.SetQuality(11);
                compressingStream.SetWindow(22);
                sourceStream.Seek(0, SeekOrigin.Begin);
                sourceStream.CopyTo(compressingStream);
            }

            return true;
        }
    }
}
