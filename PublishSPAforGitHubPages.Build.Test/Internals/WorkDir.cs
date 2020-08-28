using System;
using System.IO;

namespace PublishSPAforGitHubPages.Build.Test.Internals
{
    internal static class WorkDir
    {
        internal static DisposableDir SetupWorkDir(string siteType, string protocol)
        {
            var srcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", siteType, protocol);
            var workDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString("N"));
            Shell.XcopyDir(srcDir, workDir);
            Directory.Move(Path.Combine(workDir, "(.git)"), Path.Combine(workDir, ".git"));
            return new DisposableDir(workDir);
        }
    }
}
