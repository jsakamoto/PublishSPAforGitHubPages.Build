using NUnit.Framework;
using PublishSPAforGHPages;
using PublishSPAforGitHubPages.Build.Test.Internals;

namespace PublishSPAforGitHubPages.Build.Test;

public class GetGHPagesBaseUrlFromRepositoryUrlTest
{
    public static IEnumerable<object[]> TestPattern = [
        ["HTTPS", ""],
        ["HTTPS", "WorkDir"],
        ["HTTPS.git", ""],
        ["HTTPS.git", "WorkDir"],
        ["SSH", ""],
        ["SSH", "WorkDir"],
        ["SSH.git", ""],
        ["SSH.git", "WorkDir"],
    ];

    [TestCaseSource(nameof(TestPattern))]
    public void GetBaseUrl_ProjectSite_Test(string protocol, string subDir)
    {
        using var workDir = WorkDir.SetupWorkDir(siteType: "Project", protocol);
        var task = new GetGHPagesBaseUrlFromRepositoryUrl
        {
            WorkFolder = Path.Combine(workDir, subDir)
        };

        task.Execute().IsTrue();
        task.BaseUrl.Is("/fizz.buzz/");
    }

    [TestCaseSource(nameof(TestPattern))]
    public void GetBaseUrl_UserSite_Test(string protocol, string subDir)
    {
        using var workDir = WorkDir.SetupWorkDir(siteType: "User", protocol);
        var task = new GetGHPagesBaseUrlFromRepositoryUrl
        {
            WorkFolder = Path.Combine(workDir, subDir)
        };

        task.Execute().IsTrue();
        task.BaseUrl.Is("/");
    }
}
