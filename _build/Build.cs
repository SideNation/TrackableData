using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Parameter("NuGet API key for publishing packages")]
    [Secret]
    readonly string NuGetApiKey;

    [Parameter("NuGet source URL - Default is nuget.org")]
    readonly string NuGetSource = "https://api.nuget.org/v3/index.json";

    [Parameter("Package version override")]
    readonly string Version;

    [Solution]
    readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    string[] PackableProjects => new[]
    {
        "TrackableData.Core",
        "TrackableData.Generator",
        "TrackableData.MemoryPack",
        "TrackableData.MongoDB",
        "TrackableData.PostgreSql",
        "TrackableData.Redis",
    };

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetFilter("FullyQualifiedName~TrackableData.Tests" +
                           "|FullyQualifiedName~Generator.Tests" +
                           "|FullyQualifiedName~MemoryPack.Tests"));
        });

    Target TestAll => _ => _
        .DependsOn(Compile)
        .Description("Run all tests including integration tests (requires DB connections)")
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild());
        });

    Target Pack => _ => _
        .DependsOn(Test)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();

            foreach (var name in PackableProjects)
            {
                var projectPath = SourceDirectory / name / (name + ".csproj");
                DotNetPack(s =>
                {
                    s = s
                        .SetProject(projectPath)
                        .SetConfiguration("Release")
                        .SetOutputDirectory(ArtifactsDirectory);
                    if (Version != null)
                        s = s.SetVersion(Version);
                    return s;
                });
            }
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            ArtifactsDirectory.GlobFiles("*.nupkg")
                .Where(x => !x.ToString().EndsWith(".symbols.nupkg"))
                .ForEach(package =>
                {
                    DotNetNuGetPush(s => s
                        .SetTargetPath(package)
                        .SetSource(NuGetSource)
                        .SetApiKey(NuGetApiKey)
                        .EnableSkipDuplicate());
                });
        });
}
