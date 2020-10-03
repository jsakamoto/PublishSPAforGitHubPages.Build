using System;
using System.IO;

namespace PublishSPAforGitHubPages.Build.Test.Internals
{
    internal static class WorkDir
    {
        internal static DisposableDir SetupWorkDir(params string[] subDirs)
        {
            var srcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", Path.Combine(subDirs));
            return SetupWorkDirCore(srcDir);
        }

        internal static DisposableDir SetupWorkDir(string siteType, string protocol)
        {
            var srcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", siteType, protocol);
            return SetupWorkDirCore(srcDir);
        }

        private static DisposableDir SetupWorkDirCore(string srcDir)
        {
            var workDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString("N"));

            Shell.XcopyDir(srcDir, workDir);

            var gitDir = Path.Combine(workDir, "(.git)");
            if (Directory.Exists(gitDir))
            {
                Directory.Move(gitDir, Path.Combine(workDir, ".git"));
            }

            return new DisposableDir(workDir);
        }
    }
}
