﻿using System.Collections.Generic;
using System.IO;
using PublishSPAforGHPages;
using PublishSPAforGitHubPages.Build.Test.Internals;
using NUnit.Framework;

namespace PublishSPAforGitHubPages.Build.Test
{
    public class RewriteIndexHtmlTest
    {
        public static IEnumerable<object[]> TestPattern = new[] {
            new object[]{ "index - no autostart.html" },
            new object[]{ "index - autostart is true.html" },
        };

        [Test]
        [TestCaseSource(nameof(TestPattern))]
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

        [Test]
        public void NotInjectedBrotliLoader_Test()
        {
            using var workDir = WorkDir.SetupWorkDir("StaticFiles");
            var task = new RewriteIndexHtml
            {
                File = Path.Combine(workDir, "Source", "index.html"),
                InjectBrotliLoader = false,
                BaseHref = "/"
            };

            var original = File.ReadAllText(task.File);

            task.Execute().IsTrue();

            var rewrited = File.ReadAllText(task.File);

            rewrited.Is(original);
        }

        [Test]
        public void NotInjectedBrotliLoader_bue_to_its_not_Blazor_Test()
        {
            using var workDir = WorkDir.SetupWorkDir("StaticFiles");
            var task = new RewriteIndexHtml
            {
                File = Path.Combine(workDir, "Source", "index - no blazor.html"),
                InjectBrotliLoader = true,
                BaseHref = "/foo/bar/"
            };

            task.Execute().IsTrue();
            var rewrited = File.ReadAllText(task.File);
            var expected = File.ReadAllText(Path.Combine(workDir, "Rewrited", "index - no blazor.html"));
            rewrited.Is(expected);
        }
    }
}
