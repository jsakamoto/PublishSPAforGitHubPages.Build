using System.Collections.Generic;
using System.IO;
using PublishSPAforGHPages;
using PublishSPAforGitHubPages.Build.Test.Internals;
using Xunit;

namespace PublishSPAforGitHubPages.Build.Test
{
    public class GetGHPagesBaseUrlFromRepositoryUrlTest
    {
        public static IEnumerable<object[]> TestPattern = new[] {
            new object[]{"HTTPS", ""},
            new object[]{"HTTPS", "WorkDir"},
            new object[]{"HTTPS.git", ""},
            new object[]{"HTTPS.git", "WorkDir"},
            new object[]{"SSH", ""},
            new object[]{"SSH", "WorkDir"},
            new object[]{"SSH.git", ""},
            new object[]{"SSH.git", "WorkDir"},
        };

        [Theory]
        [MemberData(nameof(TestPattern))]
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

        [Theory]
        [MemberData(nameof(TestPattern))]
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
}
