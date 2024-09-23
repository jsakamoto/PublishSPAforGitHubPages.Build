using NUnit.Framework;
using PublishSPAforGHPages;
using PublishSPAforGitHubPages.Build.Test.Internals;

namespace PublishSPAforGitHubPages.Build.Test;

public class GetGHPagesBaseUrlFromRepositoryUrlTest
{
    public static IEnumerable<object[]> TestPattern =
        from urlType in Enum.GetValues<OriginUrlType>()
        from subDir in new[] { "", "WorkDir" }
        select new object[] { urlType, subDir };

    [TestCaseSource(nameof(TestPattern))]
    public void GetBaseUrl_ProjectSite_Test(OriginUrlType originUrlType, string subDir)
    {
        // Given
        using var workDir = WorkDir.SetupWorkDir(SiteType.ProjectSite, originUrlType);

        // When
        var task = new GetGHPagesBaseUrlFromRepositoryUrl
        {
            WorkFolder = Path.Combine(workDir, subDir)
        };
        task.Execute().IsTrue();

        // Then
        task.BaseUrl.Is("/fizz.buzz/");
    }

    [TestCaseSource(nameof(TestPattern))]
    public void GetBaseUrl_UserSite_Test(OriginUrlType originUrlType, string subDir)
    {
        // Given
        using var workDir = WorkDir.SetupWorkDir(SiteType.UserSite, originUrlType);

        // When
        var task = new GetGHPagesBaseUrlFromRepositoryUrl
        {
            WorkFolder = Path.Combine(workDir, subDir)
        };
        task.Execute().IsTrue();

        // Then
        task.BaseUrl.Is("/");
    }
}
