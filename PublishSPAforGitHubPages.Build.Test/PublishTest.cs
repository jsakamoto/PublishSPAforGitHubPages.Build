using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using NUnit.Framework;
using PublishSPAforGHPages.Models;
using PublishSPAforGitHubPages.Build.Test.Internals;
using static Toolbelt.Diagnostics.XProcess;
using static Toolbelt.FileIO;

namespace PublishSPAforGitHubPages.Build.Test;

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
        var href = indexHtmlDoc.Head?.Children.OfType<IHtmlBaseElement>().FirstOrDefault()?.Href;
        href.IsNotNull();
        return href;
    }

    [TestCaseSource(nameof(TestPattern))]
    public async Task Publish_ProjectSite_Test(string protocol, string subDir)
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
        (await Start("dotnet", "publish -c:Release -o:public", projectDir).WaitForExitAsync()).ExitCode.Is(0);
        addedFiles.Values.Any(f => File.Exists(f)).IsFalse();

        // and, the base URL is not rewrited.
        this.GetBaseHref(publishedIndexHtmlPath).Is("/foo/");

        // Second, "GHPages" enabled publishing contain additional files for GitHub pages.
        DeleteDir(Path.Combine(projectDir, "public"));
        (await Start("dotnet", "publish -c:Release -o:public -p:GHPages=true", projectDir).WaitForExitAsync()).ExitCode.Is(0);
        addedFiles.Values.All(f => File.Exists(f)).IsTrue();

        // and, the base URL is rewrited to project sub path.
        this.GetBaseHref(publishedIndexHtmlPath).Is("/fizz.buzz/");

        // Validate that the "404.html" is a copy of the "index.html".
        var indexHtmlBytes = File.ReadAllBytes(publishedIndexHtmlPath);
        var _404HtmlBytes = File.ReadAllBytes(published404HtmlPath);
        _404HtmlBytes.Is(indexHtmlBytes);
    }

    [TestCaseSource(nameof(TestPattern))]
    public async Task Publish_UserSite_Test(string protocol, string subDir)
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
        (await Start("dotnet", "publish -c:Release -o:public", projectDir).WaitForExitAsync()).ExitCode.Is(0);
        addedFiles.Values.Any(f => File.Exists(f)).IsFalse();

        // and, the base URL is not rewrited.
        this.GetBaseHref(publishedIndexHtmlPath).Is("/foo/");

        // Second, "GHPages" enabled publishing contain additional files for GitHub pages.
        DeleteDir(Path.Combine(projectDir, "public"));
        (await Start("dotnet", "publish -c:Release -o:public -p:GHPages=true", projectDir).WaitForExitAsync()).ExitCode.Is(0);
        addedFiles.Values.All(f => File.Exists(f)).IsTrue();

        // and, the base URL is rewrited to root path.
        this.GetBaseHref(publishedIndexHtmlPath).Is("/");

        // Validate that the "404.html" is a copy of the "index.html".
        var indexHtmlBytes = File.ReadAllBytes(publishedIndexHtmlPath);
        var _404HtmlBytes = File.ReadAllBytes(Path.Combine(publishedFilesDir, "404.html"));
        _404HtmlBytes.Is(indexHtmlBytes);
    }

    [Test]
    public async Task Publish_DisableComprression_Test()
    {
        using var workDir = WorkDir.SetupWorkDir(siteType: "Project", protocol: "HTTPS");
        var projectSrcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "SampleApp");
        var projectDir = Path.Combine(workDir, "WorkDir");
        XcopyDir(projectSrcDir, projectDir);

        (await Start("dotnet", "publish -c:Release -o:public -p:BlazorEnableCompression=false -p:GHPages=true", projectDir)
            .WaitForExitAsync())
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

        using var sha256 = SHA256.Create();
        var expectedIndexHtmlBytes = File.ReadAllBytes(expectedPublishedFiles["index.html"]);
        var hash = "sha256-" + Convert.ToBase64String(sha256.ComputeHash(expectedIndexHtmlBytes));

        // Verify the file hash in the service worker assets manifest.
        var serviceWorkerAssetsJs = File.ReadAllText(expectedPublishedFiles["my-assets.js"]);
        serviceWorkerAssetsJs = Regex.Replace(serviceWorkerAssetsJs, @"^self\.assetsManifest\s*=\s*", "");
        serviceWorkerAssetsJs = Regex.Replace(serviceWorkerAssetsJs, ";\\s*$", "");
        var assetsManifestFile = JsonSerializer.Deserialize<AssetsManifestFile>(serviceWorkerAssetsJs);

        assetsManifestFile.IsNotNull();
        assetsManifestFile.assets.IsNotNull();
        var assetManifestEntry = assetsManifestFile.assets.FirstOrDefault(a => a.url == "index.html");
        assetManifestEntry.IsNotNull();
        assetManifestEntry.hash.Is(hash);

        // Verify the assets manifest doesn't include compressed file path such as ".dll.br" or ".dll.bz".
        assetsManifestFile.assets.Any(a => a.url.EndsWith(".br") || a.url.EndsWith(".bz")).IsFalse();
    }

    [Test]
    public async Task Publish_NonPWA_Test()
    {
        using var workDir = WorkDir.SetupWorkDir(siteType: "Project", protocol: "HTTPS");
        var projectSrcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "SampleAppNonPWA");
        var projectDir = Path.Combine(workDir, "WorkDir");
        XcopyDir(projectSrcDir, projectDir);

        var dotnetCLI = Start("dotnet", "publish -c:Release -o:public -p:BlazorEnableCompression=false -p:GHPages=true", projectDir);
        await dotnetCLI.WaitForExitAsync();
        dotnetCLI.ExitCode.Is(0, message: dotnetCLI.Output);
    }
}
