using System.IO;
using NUnit.Framework;
using PublishSPAforGHPages;
using Toolbelt;

namespace PublishSPAforGitHubPages.Build.Test
{
    public class RewriteServiceWorkerAssetsManifestJsTest
    {
        [Test]
        public void RewriteHashOfIndex_Test()
        {
            // Given
            var solutionDir = FileIO.FindContainerDirToAncestor("*.sln");
            var fixtureDir = Path.Combine(solutionDir, "PublishSPAforGitHubPages.Build.Test", "Fixtures");
            var templateDir = Path.Combine(fixtureDir, "StaticFiles", "Source");
            using var workDir = WorkDirectory.CreateCopyFrom(templateDir, null);

            // When
            var task = new RewriteServiceWorkerAssetsManifestJs
            {
                WebRootPath = workDir,
                IndexHtml = Path.Combine(workDir, "index.html"),
                ServiceWorkerAssetsManifestJs = Path.Combine(workDir, "service-worker-assets.js"),
                InjectBrotliLoader = false
            };
            task.Execute().IsTrue();

            // Then
            var actualAssetsText = File.ReadAllText(task.ServiceWorkerAssetsManifestJs).TrimEnd('\r', '\n');
            var expectedAssetsText = File.ReadAllText(Path.Combine(fixtureDir, "StaticFiles", "Rewrited", "service-worker-assets (compression disabled).js")).TrimEnd('\r', '\n');
            actualAssetsText.Is(expectedAssetsText);
        }

        [Test]
        public void Rewrite_when_BrotliLoader_is_enabled_Test()
        {
            // Given
            var solutionDir = FileIO.FindContainerDirToAncestor("*.sln");
            var fixtureDir = Path.Combine(solutionDir, "PublishSPAforGitHubPages.Build.Test", "Fixtures");
            var templateDir = Path.Combine(fixtureDir, "StaticFiles", "Source");
            using var workDir = WorkDirectory.CreateCopyFrom(templateDir, null);

            // When
            var task = new RewriteServiceWorkerAssetsManifestJs
            {
                WebRootPath = workDir,
                IndexHtml = Path.Combine(workDir, "index.html"),
                ServiceWorkerAssetsManifestJs = Path.Combine(workDir, "service-worker-assets.js"),
                InjectBrotliLoader = true
            };
            task.Execute().IsTrue();

            // Then
            var actualAssetsText = File.ReadAllText(task.ServiceWorkerAssetsManifestJs).TrimEnd('\r', '\n');
            var expectedAssetsText = File.ReadAllText(Path.Combine(fixtureDir, "StaticFiles", "Rewrited", "service-worker-assets (compression enabled).js")).TrimEnd('\r', '\n');
            actualAssetsText.Is(expectedAssetsText);
        }
    }
}
