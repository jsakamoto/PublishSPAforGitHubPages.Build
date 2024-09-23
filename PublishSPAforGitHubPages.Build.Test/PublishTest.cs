using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using NUnit.Framework;
using PublishSPAforGitHubPages.Build.Test.Internals;
using PublishSPAforGitHubPages.Build.Test.Internals.Models;
using static Toolbelt.FileIO;

namespace PublishSPAforGitHubPages.Build.Test;

[Parallelizable(ParallelScope.Children)]
public class PublishTest
{
    private readonly HtmlParser _Parser = new();

    public static readonly IEnumerable<object[]> TestPattern =
        from urlType in Enum.GetValues<OriginUrlType>()
        from subDir in new[] { "", "WorkDir" }
        select new object[] { urlType, subDir };

    private string GetBaseHref(string indexHtmlPath)
    {
        using var indexHtmlDoc = this._Parser.ParseDocument(File.ReadAllText(indexHtmlPath));
        var href = indexHtmlDoc.Head?.Children.OfType<IHtmlBaseElement>().FirstOrDefault()?.Href;
        href.IsNotNull();
        return href;
    }

    [TestCaseSource(nameof(TestPattern))]
    public async Task Publish_PWA_ProjectSite_Test(OriginUrlType originUrlType, string subDir)
    {
        // Given
        using var workDir = WorkDir.SetupWorkDir(SiteType.ProjectSite, originUrlType, projectName: "SamplePWA", projectLocation: subDir);
        var addedFiles = new[] { ".nojekyll", "404.html", ".gitattributes", "decode.min.js", "brotliloader.min.js" }
            .ToDictionary(f => f, f => Path.Combine(workDir.PublishDir, f));
        var publishedIndexHtmlPath = Path.Combine(workDir.PublishDir, "index.html");

        // When: At first, normal publishing... 
        await TestAssert.RunAsync("dotnet", "publish -c:Release -o:public", workDir.ProjectDir);

        // Then: doesn't contain any additional files,
        addedFiles.ToList().ForEach(x => File.Exists(x.Value).IsFalse(message: $"{x.Key} shouldn't exist, but there is."));

        // and, the base URL is not rewrited.
        this.GetBaseHref(publishedIndexHtmlPath).Is("/foo/");

        // Given
        DeleteDir(workDir.PublicDir);

        // When: Second, "GHPages" enabled publishing...
        await TestAssert.RunAsync("dotnet", "publish -c:Release -o:public -p:GHPages=true", workDir.ProjectDir);

        // Then: contain additional files for GitHub pages.
        addedFiles.ToList().ForEach(x => File.Exists(x.Value).IsTrue(message: $"{x.Key} should exist, but not."));

        // and, the base URL is rewrited to project sub path.
        this.GetBaseHref(publishedIndexHtmlPath).Is("/fizz.buzz/");

        // Validate that the "404.html" is a copy of the "index.html".
        TestAssert.FilesAreEquals(
            actualFilePath: addedFiles["404.html"],
            expectedFilePath: publishedIndexHtmlPath);
    }

    [TestCaseSource(nameof(TestPattern))]
    public async Task Publish_PWA_UserSite_Test(OriginUrlType originUrlType, string subDir)
    {
        // Given
        using var workDir = WorkDir.SetupWorkDir(SiteType.UserSite, originUrlType, projectName: "SamplePWA", projectLocation: subDir);
        var addedFiles = new[] { ".nojekyll", "404.html", ".gitattributes", "decode.min.js", "brotliloader.min.js" }
            .ToDictionary(f => f, f => Path.Combine(workDir.PublishDir, f));
        var publishedIndexHtmlPath = Path.Combine(workDir.PublishDir, "index.html");

        // When: At first, normal publishing ...
        await TestAssert.RunAsync("dotnet", "publish -c:Release -o:public", workDir.ProjectDir);

        // Then: doesn't contain any additional files.
        addedFiles.ToList().ForEach(x => File.Exists(x.Value).IsFalse(message: $"{x.Key} shouldn't exist, but there is."));

        // and, the base URL is not rewrited.
        this.GetBaseHref(publishedIndexHtmlPath).Is("/foo/");

        // Given
        DeleteDir(workDir.PublicDir);

        // When: Second, "GHPages" enabled publishing...
        await TestAssert.RunAsync("dotnet", "publish -c:Release -o:public -p:GHPages=true", workDir.ProjectDir);

        // Then: contain additional files for GitHub pages.
        addedFiles.ToList().ForEach(x => File.Exists(x.Value).IsTrue(message: $"{x.Key} should exist, but not."));

        // and, the base URL is rewrited to root path.
        this.GetBaseHref(publishedIndexHtmlPath).Is("/");

        // Validate that the "404.html" is a copy of the "index.html".
        TestAssert.FilesAreEquals(
            actualFilePath: addedFiles["404.html"],
            expectedFilePath: publishedIndexHtmlPath);
    }

    [Test]
    public async Task Publish_PWA_DisableComprression_Test()
    {
        // Given
        using var workDir = WorkDir.SetupWorkDir(SiteType.ProjectSite, OriginUrlType.HTTPS, projectName: "SamplePWA", projectLocation: "WorkDir");

        // When
        await TestAssert.RunAsync("dotnet", "publish -c:Release -o:public -p:CompressionEnabled=false -p:GHPages=true", workDir.ProjectDir);

        // Then...

        // compression files are not exists. (Only added files are exists in publish dir.)
        var actualPublishedFiles = Directory.GetFiles(workDir.PublishDir).OrderBy(name => name).ToArray();
        var expectedPublishedFiles = new[] { ".nojekyll", "404.html", ".gitattributes", "index.html", "favicon.ico", "manifest.json", "service-worker.js", "my-assets.js" }
            .ToDictionary(name => name, name => Path.Combine(workDir.PublishDir, name));
        actualPublishedFiles.Is(expectedPublishedFiles.Values.OrderBy(name => name));

        // Brotli loader was not injected.
        TestAssert.FilesAreEquals(
            actualFilePath: expectedPublishedFiles["index.html"],
            expectedFilePath: Path.Combine(workDir.FixtureDir, "Expected", "index of PWA - brotli loader is not injected.html"));

        // Verify the file hash in the service worker assets manifest.
        using var sha256 = SHA256.Create();
        var expectedIndexHtmlBytes = File.ReadAllBytes(expectedPublishedFiles["index.html"]);
        var hash = "sha256-" + Convert.ToBase64String(sha256.ComputeHash(expectedIndexHtmlBytes));

        var serviceWorkerAssetsJs = File.ReadAllText(expectedPublishedFiles["my-assets.js"]);
        serviceWorkerAssetsJs = Regex.Replace(serviceWorkerAssetsJs, @"^self\.assetsManifest\s*=\s*", "");
        serviceWorkerAssetsJs = Regex.Replace(serviceWorkerAssetsJs, ";\\s*$", "");
        var assetsManifestFile = JsonSerializer.Deserialize<AssetsManifestFile>(serviceWorkerAssetsJs);

        assetsManifestFile.IsNotNull();
        assetsManifestFile.assets.IsNotNull();
        var assetManifestEntry = assetsManifestFile.assets.FirstOrDefault(a => a.url == "index.html");
        assetManifestEntry.IsNotNull().hash.Is(hash);

        // Verify the assets manifest doesn't include compressed file path such as ".dll.br" or ".dll.bz".
        assetsManifestFile.assets.Any(a => a.url?.EndsWith(".br") == true || a.url?.EndsWith(".bz") == true).IsFalse();
    }

    [Test]
    public async Task Publish_NonPWA_Test()
    {
        // Given
        using var workDir = WorkDir.SetupWorkDir(SiteType.ProjectSite, OriginUrlType.HTTPS, projectName: "SampleApp", projectLocation: "WorkDir");

        // When & Then
        await TestAssert.RunAsync("dotnet", "publish -c:Release -o:public -p:CompressionEnabled=false -p:GHPages=true", workDir.ProjectDir);
    }

    [Test]
    public async Task Publish_PWA_For_NonGHPages_Test()
    {
        // Given
        using var workDir = WorkDir.SetupWorkDir(SiteType.ProjectSite, OriginUrlType.HTTPS, projectName: "SamplePWA", projectLocation: "WorkDir");

        // When
        await TestAssert.RunAsync("dotnet", "publish -c:Release -o:public -p:GHPagesBase=\"/foo/\"", workDir.ProjectDir);

        // Then
        var actualIndexHtmlPath = Path.Combine(workDir.PublishDir, "index.html");
        var expectedIndexHtmlPath = Path.Combine(workDir.FixtureDir, "Expected", "index of PWA - no changed.html");
        TestAssert.FilesAreEquals(actualIndexHtmlPath, expectedIndexHtmlPath);
    }

    [Test]
    public async Task Publish_NonPWA_For_NonGHPages_Test()
    {
        // Given
        using var workDir = WorkDir.SetupWorkDir(SiteType.ProjectSite, OriginUrlType.HTTPS, projectName: "SampleApp", projectLocation: "WorkDir");

        // When
        await TestAssert.RunAsync("dotnet", "publish -c:Release -o:public -p:GHPagesBase=\"/foo/\"", workDir.ProjectDir);

        // Then
        var actualIndexHtmlPath = Path.Combine(workDir.PublishDir, "index.html");
        var expectedIndexHtmlPath = Path.Combine(workDir.FixtureDir, "Expected", "index of NonPWA - no changed.html");
        TestAssert.FilesAreEquals(actualIndexHtmlPath, expectedIndexHtmlPath);
    }
}
