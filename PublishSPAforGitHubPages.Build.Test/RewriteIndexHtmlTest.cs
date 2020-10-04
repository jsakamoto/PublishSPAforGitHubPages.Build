using System.Collections.Generic;
using System.IO;
using PublishSPAforGHPages;
using PublishSPAforGitHubPages.Build.Test.Internals;
using Xunit;

namespace PublishSPAforGitHubPages.Build.Test
{
    public class RewriteIndexHtmlTest
    {
        public static IEnumerable<object[]> TestPattern = new[] {
            new object[]{ "index - no autostart.html" },
            new object[]{ "index - autostart is true.html" },
        };

        [Theory]
        [MemberData(nameof(TestPattern))]
        public void InjectBrotliLoader_Test(string fileName)
        {
            using var workDir = WorkDir.SetupWorkDir("StaticFiles");
            var task = new RewriteIndexHtml
            {
                File = Path.Combine(workDir, "Source", fileName),
                InjectBrotliLoader = true,
                BaseHref = "/foo/bar/"
            };

            task.Execute().IsTrue();

            var expectedPath = Path.Combine(workDir, "Rewrited", "index.html");
            File.ReadAllLines(task.File).Is(File.ReadAllLines(expectedPath));
        }
    }
}
