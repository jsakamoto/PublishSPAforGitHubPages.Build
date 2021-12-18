using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using NUnit.Framework;
using PublishSPAforGitHubPages.Build.Test.Internals;
using static PublishSPAforGitHubPages.Build.Test.Internals.Shell;

namespace PublishSPAforGitHubPages.Build.Test
{
    [Parallelizable(ParallelScope.Children)]
    public class PublishTest
    {
        private readonly HtmlParser _Parser = new();

        public static readonly IEnumerable<object[]> TestPattern = new[] {
            new object[]{"HTTPS", ""},
            new object[]{"HTTPS", "WorkDir"},
            //new object[]{"HTTPS.git", ""},
            //new object[]{"HTTPS.git", "WorkDir"},
            //new object[]{"SSH", ""},
            //new object[]{"SSH", "WorkDir"},
            //new object[]{"SSH.git", ""},
            //new object[]{"SSH.git", "WorkDir"},
        };

        private string GetBaseHref(string indexHtmlPath)
        {
            using var indexHtmlDoc = this._Parser.ParseDocument(File.ReadAllText(indexHtmlPath));
            return indexHtmlDoc.Head.Children.OfType<IHtmlBaseElement>().First().Href;
        }

        [Test]
        [TestCaseSource(nameof(TestPattern))]
        public void Publish_ProjectSite_Test(string protocol, string subDir)
        {
            using var workDir = WorkDir.SetupWorkDir(siteType: "Project", protocol);
            var projectSrcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "SampleApp");
            var projectDir = Path.Combine(workDir, subDir);
            XcopyDir(projectSrcDir, projectDir);

            var publishedFilesDir = Path.Combine(projectDir, "public", "wwwroot");
            var addedFiles = new[] { ".nojekyll", "404.html", ".gitattributes", "decode.min.js", "brotliloader.min.js" }
                .ToDictionary(f => f, f => Path.Combine(publishedFilesDir, f));
            var publishedIndexHtmlPath = Path.Combine(publishedFilesDir, "index.html");
            var published404HtmlPath = addedFiles["404.html"];

            // At first, normal publishing doesn't contain any additional files.
            Run(projectDir, "dotnet", "publish", "-c:Release", "-o:public").ExitCode.Is(0);
            addedFiles.Values.Any(f => File.Exists(f)).IsFalse();

            // and, the base URL is not rewrited.
            this.GetBaseHref(publishedIndexHtmlPath).Is("/foo/");

            // Second, "GHPages" enabled publishing contain additional files for GitHub pages.
            Delete(Path.Combine(projectDir, "public"));
            Run(projectDir, "dotnet", "publish", "-c:Release", "-o:public", "-p:GHPages=true").ExitCode.Is(0);
            addedFiles.Values.All(f => File.Exists(f)).IsTrue();

            // and, the base URL is rewrited to project sub path.
            this.GetBaseHref(publishedIndexHtmlPath).Is("/fizz.buzz/");

            // Validate that the "404.html" is a copy of the "index.html".
            var indexHtmlBytes = File.ReadAllBytes(publishedIndexHtmlPath);
            var _404HtmlBytes = File.ReadAllBytes(published404HtmlPath);
            _404HtmlBytes.Is(indexHtmlBytes);

            // Validate recompression static files.
            ValidateRecompression(publishedIndexHtmlPath, indexHtmlBytes);
            ValidateRecompression(published404HtmlPath, _404HtmlBytes);
        }

        [Test]
        [TestCaseSource(nameof(TestPattern))]
        public void Publish_UserSite_Test(string protocol, string subDir)
        {
            using var workDir = WorkDir.SetupWorkDir(siteType: "User", protocol);
            var projectSrcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "SampleApp");
            var projectDir = Path.Combine(workDir, subDir);
            XcopyDir(projectSrcDir, projectDir);

            var publishedFilesDir = Path.Combine(projectDir, "public", "wwwroot");
            var addedFiles = new[] { ".nojekyll", "404.html", ".gitattributes", "decode.min.js", "brotliloader.min.js" }
                .ToDictionary(f => f, f => Path.Combine(publishedFilesDir, f));
            var publishedIndexHtmlPath = Path.Combine(publishedFilesDir, "index.html");
            var published404HtmlPath = addedFiles["404.html"];

            // At first, normal publishing doesn't contain any additional files.
            Run(projectDir, "dotnet", "publish", "-c:Release", "-o:public").ExitCode.Is(0);
            addedFiles.Values.Any(f => File.Exists(f)).IsFalse();

            // and, the base URL is not rewrited.
            this.GetBaseHref(publishedIndexHtmlPath).Is("/foo/");

            // Second, "GHPages" enabled publishing contain additional files for GitHub pages.
            Delete(Path.Combine(projectDir, "public"));
            Run(projectDir, "dotnet", "publish", "-c:Release", "-o:public", "-p:GHPages=true").ExitCode.Is(0);
            addedFiles.Values.All(f => File.Exists(f)).IsTrue();

            // and, the base URL is rewrited to root path.
            this.GetBaseHref(publishedIndexHtmlPath).Is("/");

            // Validate that the "404.html" is a copy of the "index.html".
            var indexHtmlBytes = File.ReadAllBytes(publishedIndexHtmlPath);
            var _404HtmlBytes = File.ReadAllBytes(Path.Combine(publishedFilesDir, "404.html"));
            _404HtmlBytes.Is(indexHtmlBytes);

            // Validate recompression static files.
            ValidateRecompression(publishedIndexHtmlPath, indexHtmlBytes);
            ValidateRecompression(published404HtmlPath, _404HtmlBytes);
        }

        [Test]
        public void Publish_DisableComprression_Test()
        {
            using var workDir = WorkDir.SetupWorkDir(siteType: "Project", protocol: "HTTPS");
            var projectSrcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "SampleApp");
            var projectDir = Path.Combine(workDir, "WorkDir");
            XcopyDir(projectSrcDir, projectDir);

            Run(projectDir, "dotnet", "publish", "-c:Release", "-o:public", "-p:BlazorEnableCompression=false", "-p:GHPages=true")
                .ExitCode.Is(0);

            var publishedFilesDir = Path.Combine(projectDir, "public", "wwwroot");

            var actualPublishedFiles = Directory.GetFiles(publishedFilesDir).OrderBy(name => name).ToArray();

            // compression files are not exists.
            var expectedPublishedFiles = new[] { ".nojekyll", "404.html", ".gitattributes", "index.html", "favicon.ico", "manifest.json", "service-worker.js", "my-assets.js" }
                .ToDictionary(name => name, name => Path.Combine(publishedFilesDir, name));
            actualPublishedFiles.Is(expectedPublishedFiles.Values.OrderBy(name => name));

            // Brotli loader was not injected.
            var expectedIndexHtmlContents = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "StaticFiles", "Rewrited", "index - brotli loader is not injected.html"));
            var actualIndexHtmlContents = File.ReadAllText(expectedPublishedFiles["index.html"]);
            actualIndexHtmlContents.Is(expectedIndexHtmlContents);
        }

        private static void ValidateRecompression(string htmlPath, byte[] htmlBytes)
        {
            ValidateRecompression(htmlPath, htmlBytes, ".gz", fileStream => new GZipStream(fileStream, CompressionMode.Decompress));
            ValidateRecompression(htmlPath, htmlBytes, ".br", fileStream => new BrotliStream(fileStream, CompressionMode.Decompress));
        }

        private static void ValidateRecompression(string htmlPath, byte[] htmlBytes, string suffix, Func<Stream, Stream> getDecompressingStream)
        {
            var compressedFilePath = htmlPath + suffix;
            File.Exists(compressedFilePath).IsTrue();
            using var memStream = new MemoryStream();
            using var compressedFileStream = File.OpenRead(compressedFilePath);
            using var decompressingStream = getDecompressingStream(compressedFileStream);
            decompressingStream.CopyTo(memStream);

            memStream.ToArray().Is(htmlBytes);
        }
    }
}
