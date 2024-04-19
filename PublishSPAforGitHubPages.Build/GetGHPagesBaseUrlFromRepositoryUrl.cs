using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace PublishSPAforGHPages;

public class GetGHPagesBaseUrlFromRepositoryUrl : Microsoft.Build.Utilities.Task
{
    [Required]
    public string WorkFolder { get; set; }

    [Output]
    public string BaseUrl { get; set; } = "";

    public override bool Execute()
    {
        var gitDir = this.FindGitDir();
        if (string.IsNullOrEmpty(gitDir)) return true;

        var gitConfigPath = Path.Combine(gitDir, "config");
        var regexMatchRepoUrl = File.ReadLines(gitConfigPath)
            .SkipWhile(line => line != "[remote \"origin\"]")
            .Select(line => Regex.Match(line, @"^\s*url\s*=\s*((https://github.com/(?<owner>[^/]+)/(?<project>.+))|(git@github.com:(?<owner>[^/]+)/(?<project>.+)))\s*$"))
            .FirstOrDefault(m => m.Success);

        if (regexMatchRepoUrl != null)
        {
            var owner = regexMatchRepoUrl.Groups["owner"].Value;
            var project = regexMatchRepoUrl.Groups["project"].Value;
            if (project.EndsWith(".git")) project = project.Substring(0, project.Length - 4);
            if (owner + ".github.io" == project)
                this.BaseUrl = "/";
            else
                this.BaseUrl = "/" + project + "/";
        }

        return true;
    }

    private string FindGitDir()
    {
        for (var baseDir = this.WorkFolder; baseDir != null; baseDir = Path.GetDirectoryName(baseDir))
        {
            var gitDir = Path.Combine(baseDir, ".git");
            if (Directory.Exists(gitDir)) return gitDir;
        }
        return null;
    }

    private void LogMessage(string message)
    {
        if (this.BuildEngine != null) this.Log.LogMessage(MessageImportance.High, message);
    }
}
