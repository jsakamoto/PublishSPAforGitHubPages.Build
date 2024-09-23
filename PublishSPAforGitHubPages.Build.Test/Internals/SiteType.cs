namespace PublishSPAforGitHubPages.Build.Test.Internals;

/// <summary>
/// A site type of GitHub Pages.
/// </summary>
public enum SiteType
{
    /// <summary>
    /// Represents a Project site of GitHub Pages that will have a URL like "http://{account}.github.io/{project}".
    /// </summary>
    ProjectSite,

    /// <summary>
    /// Represents a User site of GitHub Pages that will have a URL like "http://{account}.github.io/".
    /// </summary>
    UserSite
}
