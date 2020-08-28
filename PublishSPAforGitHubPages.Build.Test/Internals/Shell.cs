using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PublishSPAforGitHubPages.Build.Test.Internals
{
    public static class Shell
    {
        public static bool Exists(string dir, string wildCard)
        {
            return Directory.GetFiles(dir, wildCard, SearchOption.TopDirectoryOnly).Any();
        }

        public static void Delete(string dir)
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }

        public static Process Run(string workDir, params string[] args)
        {
            var pi = new ProcessStartInfo
            {
                WorkingDirectory = workDir,
                FileName = args.First(),
                Arguments = string.Join(" ", args.Skip(1)),
                UseShellExecute = false,
            };
            var process = Process.Start(pi);
            process.WaitForExit();
            return process;
        }

        public static void XcopyDir(string srcDir, string dstDir)
        {
            Directory.CreateDirectory(dstDir);

            var srcFileNames = Directory.GetFiles(srcDir);
            foreach (var srcFileName in srcFileNames)
            {
                var dstFileName = Path.Combine(dstDir, Path.GetFileName(srcFileName));
                File.Copy(srcFileName, dstFileName);
            }

            var srcSubDirs = Directory.GetDirectories(srcDir);
            foreach (var srcSubDir in srcSubDirs)
            {
                var dstSubDir = Path.Combine(dstDir, Path.GetFileName(srcSubDir));
                XcopyDir(srcSubDir, dstSubDir);
            }
        }
    }
}
