using System;
using System.IO;
using Toolbelt;

namespace PublishSPAforGitHubPages.Build.Test.Internals
{
    internal static class WorkDir
    {
        internal static WorkDirectory SetupWorkDir(params string[] subDirs)
        {
            var srcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", Path.Combine(subDirs));
            return SetupWorkDirCore(srcDir);
        }

        internal static WorkDirectory SetupWorkDir(string siteType, string protocol)
        {
            var srcDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", siteType, protocol);
            return SetupWorkDirCore(srcDir);
        }

        private static WorkDirectory SetupWorkDirCore(string srcDir)
        {
            var workDir = Toolbelt.WorkDirectory.CreateCopyFrom(srcDir, null);
            var gitDir = Path.Combine(workDir, "(.git)");
            if (Directory.Exists(gitDir))
            {
                Directory.Move(gitDir, Path.Combine(workDir, ".git"));
            }

            return workDir;
        }
    }
}
