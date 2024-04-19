using System.IO;
using System.IO.Compression;
using BrotliSharpLib;
using Microsoft.Build.Framework;

namespace PublishSPAforGHPages
{
    public class RecompressStaticFile : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string File { get; set; }

        public bool EnableGzip { get; set; } = true;

        public bool EnableBrotli { get; set; } = true;

        public bool Recursive { get; set; }

        public override bool Execute()
        {
            var fileName = Path.GetFileName(this.File);
            var baseDir = Path.GetDirectoryName(this.File);
            var searchOption = this.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var targetFiles = Directory.GetFiles(baseDir, fileName, searchOption);
            foreach (var targeFile in targetFiles)
            {
                this.Recompress(targeFile);
            }

            return true;
        }

        private void Recompress(string file)
        {
            using var sourceStream = System.IO.File.OpenRead(file);

            if (this.EnableGzip)
            {
                using var outputStream = System.IO.File.Create(this.File + ".gz");
                using var compressingStream = new GZipStream(outputStream, CompressionLevel.Optimal);
                sourceStream.Seek(0, SeekOrigin.Begin);
                sourceStream.CopyTo(compressingStream);
            }

            if (this.EnableBrotli)
            {
                using var outputStream = System.IO.File.Create(this.File + ".br");
                using var compressingStream = new BrotliStream(outputStream, CompressionMode.Compress);
                compressingStream.SetQuality(11);
                compressingStream.SetWindow(22);
                sourceStream.Seek(0, SeekOrigin.Begin);
                sourceStream.CopyTo(compressingStream);
            }
        }
    }
}
