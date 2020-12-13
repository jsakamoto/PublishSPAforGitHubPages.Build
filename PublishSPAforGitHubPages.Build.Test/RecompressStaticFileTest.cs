using System.IO;
using PublishSPAforGHPages;
using PublishSPAforGitHubPages.Build.Test.Internals;
using NUnit.Framework;

namespace PublishSPAforGitHubPages.Build.Test
{
    public class RecompressStaticFileTest
    {
        [Test]
        public void Recompress_Test()
        {
            using var workDir = WorkDir.SetupWorkDir("StaticFiles");
            var task = new RecompressStaticFile
            {
                File = Path.Combine(workDir, "Source", "index.html")
            };

            task.Execute().IsTrue();

            var expectedGzPath = Path.Combine(workDir, "Expected", "index.html.gz");
            var actulaGzPath = Path.Combine(workDir, "Source", "index.html.gz");
            File.Exists(actulaGzPath).IsTrue();
            File.ReadAllBytes(actulaGzPath).Is(File.ReadAllBytes(expectedGzPath));

            var expectedBrPath = Path.Combine(workDir, "Expected", "index.html.br");
            var actulaBrPath = Path.Combine(workDir, "Source", "index.html.br");
            File.Exists(actulaBrPath).IsTrue();
            File.ReadAllBytes(actulaBrPath).Is(File.ReadAllBytes(expectedBrPath));
        }
    }
}
