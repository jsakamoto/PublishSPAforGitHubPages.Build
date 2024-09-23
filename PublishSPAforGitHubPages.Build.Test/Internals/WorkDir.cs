using Toolbelt;

namespace PublishSPAforGitHubPages.Build.Test.Internals;

internal class WorkDir : IDisposable
{
    private readonly WorkDirectory _workDir;

    public string FixtureDir { get; }

    public string ProjectDir { get; }

    public string PublicDir { get; }

    public string PublishDir { get; }

    public WorkDir(string fixtureDir, string srcDir, string? projectLocation)
    {
        this._workDir = WorkDirectory.CreateCopyFrom(srcDir, null);
        this.FixtureDir = fixtureDir;
        this.ProjectDir = string.IsNullOrEmpty(projectLocation) ? this._workDir : Path.Combine(this._workDir, projectLocation);
        this.PublicDir = Path.Combine(this.ProjectDir, "public");
        this.PublishDir = Path.Combine(this.PublicDir, "wwwroot");
    }

    internal static WorkDir SetupWorkDir(SiteType siteType, OriginUrlType protocol, string? projectName = null, string? projectLocation = null)
    {
        var testProjectDir = FileIO.FindContainerDirToAncestor("PublishSPAforGitHubPages.Build.Test.csproj");
        var fixtureDir = Path.Combine(testProjectDir, "Fixtures");
        var srcDir = Path.Combine(fixtureDir, "GitHub Page Types", siteType.ToString(), protocol.ToString());

        var workDir = new WorkDir(fixtureDir, srcDir, projectLocation);

        var gitDir = Path.Combine(workDir, "(.git)");
        Directory.Move(gitDir, Path.Combine(workDir, ".git"));

        if (projectName != null)
        {
            var projectSrcDir = Path.Combine(testProjectDir, "Fixtures", "Sample Projects", projectName);
            FileIO.XcopyDir(projectSrcDir, workDir.ProjectDir);
        }

        return workDir;
    }

    public static implicit operator string(WorkDir workDirectory) => workDirectory._workDir.Path;

    public void Dispose() => this._workDir.Dispose();
}
