using System;

namespace PublishSPAforGitHubPages.Build.Test.Internals
{
    public class DisposableDir : IDisposable
    {
        private string DirPath { get; }

        public DisposableDir(string dirPath)
        {
            DirPath = dirPath;
        }

        public override string ToString() => DirPath;

        public void Dispose()
        {
            Shell.Delete(DirPath);
        }

        public static implicit operator string(DisposableDir dir) { return dir.DirPath; }
    }
}
