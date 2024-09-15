using NUnit.Framework;
using PublishSPAforGHPages;
using PublishSPAforGitHubPages.Build.Test.Internals;

namespace PublishSPAforGitHubPages.Build.Test;

public class RewriteIndexHtmlTest
{
    public static IEnumerable<object[]> TestPattern = [
        ["index.html"],
        ["index - no autostart.html"],
        ["index - autostart is true.html"],
    ];

    [TestCaseSource(nameof(TestPattern))]
    public void InjectBrotliLoader_Test(string caseFileName)
    {
        // Given
        using var workDir = WorkDir.SetupWorkDir("StaticFiles");
        var sourceDir = Path.Combine(workDir, "Source");
        var rewritedDir = Path.Combine(workDir, "Rewrited");
        var targetIndexHtmlPath = Path.Combine(sourceDir, "index.html");

        if (caseFileName != "index.html") File.Copy(Path.Combine(sourceDir, caseFileName), targetIndexHtmlPath, overwrite: true);
        var exceptIndexHtmlFiles = Directory.GetFiles(sourceDir, "index*.html").Where(path => Path.GetFileName(path) != "index.html").ToArray();
        foreach (var exceptIndexHtmlFile in exceptIndexHtmlFiles) { File.Delete(exceptIndexHtmlFile); }

        // When
        var task = new RewriteIndexHtml
        {
            WebRootPath = sourceDir,
            FileSearchPatterns = "*.html",
            InjectBrotliLoader = true,
            BaseHref = "/foo/bar/",
            Recursive = true,
        };

        task.Execute().IsTrue();

        // Then
        var filesToCheckUp = new[] { "index.html", "fetchdata/index.html", "counter.html" };
        foreach (var fileToCheckUp in filesToCheckUp)
        {
            var expectedPath = Path.Combine(rewritedDir, fileToCheckUp);
            var actualPath = Path.Combine(sourceDir, fileToCheckUp);
            File.ReadAllLines(actualPath).Is(File.ReadAllLines(expectedPath));
        }
    }

    [Test]
    public void NotInjectedBrotliLoader_Test()
    {
        using var workDir = WorkDir.SetupWorkDir("StaticFiles");
        var sourceDir = Path.Combine(workDir, "Source");
        var targetIndexHtmlPath = Path.Combine(sourceDir, "index.html");

        var task = new RewriteIndexHtml
        {
            WebRootPath = sourceDir,
            FileSearchPatterns = "index.html",
            InjectBrotliLoader = false,
            BaseHref = "/",
            Recursive = true,
        };

        var original = File.ReadAllText(targetIndexHtmlPath);

        task.Execute().IsTrue();

        var rewrited = File.ReadAllText(targetIndexHtmlPath);

        rewrited.Is(original);
    }

    [Test]
    public void NotInjectedBrotliLoader_bue_to_its_not_Blazor_Test()
    {
        using var workDir = WorkDir.SetupWorkDir("StaticFiles");
        var sourceDir = Path.Combine(workDir, "Source");
        var rewritedDir = Path.Combine(workDir, "Rewrited");

        var task = new RewriteIndexHtml
        {
            WebRootPath = sourceDir,
            FileSearchPatterns = "*.html",
            InjectBrotliLoader = true,
            BaseHref = "/foo/bar/",
            Recursive = true,
        };

        task.Execute().IsTrue();
        var actual = File.ReadAllText(Path.Combine(sourceDir, "index - no blazor.html"));
        var expected = File.ReadAllText(Path.Combine(rewritedDir, "index - no blazor.html"));
        actual.Is(expected);
    }
}
