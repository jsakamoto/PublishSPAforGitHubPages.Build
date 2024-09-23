namespace PublishSPAforGitHubPages.Build.Test.Internals;

/// <summary>
/// A URL types to connect GitHub remote repository as an origin.
/// </summary>
public enum OriginUrlType
{
    /// <summary>
    /// Represents a "https" protocol, such as "https://github.com/{account}/{repository}".
    /// </summary>
    HTTPS,

    /// <summary>
    /// Represents a "https" protocol, such as "https://github.com/{account}/{repository}.git".
    /// </summary>
    HTTPS_git,

    /// <summary>
    /// Represents a "git" protocol, such as "git@github.com:{account}/{repository}".
    /// </summary>
    SSH,

    /// <summary>
    /// Represents a "git" protocol, such as "git@github.com:{account}/{repository}.git".
    /// </summary>
    SSH_git
}
