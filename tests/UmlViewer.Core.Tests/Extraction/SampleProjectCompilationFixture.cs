using Microsoft.CodeAnalysis;
using UmlViewer.Core.Loading;

namespace UmlViewer.Core.Tests.Extraction;

/// <summary>
/// Loads fixtures/SampleProject once and shares the resulting Compilation across all
/// extraction tests in the collection — MSBuildWorkspace loads are expensive per-project.
/// </summary>
public sealed class SampleProjectCompilationFixture : IAsyncLifetime
{
    public Compilation Compilation { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var loader = new ProjectLoader();
        var loaded = await loader.LoadAsync(FindFixture("fixtures/SampleProject/SampleProject.csproj"));
        Compilation = loaded.Compilation;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string FindFixture(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate fixture '{relativePath}' above {AppContext.BaseDirectory}");
    }
}

[CollectionDefinition(Name)]
public sealed class SampleProjectCollection : ICollectionFixture<SampleProjectCompilationFixture>
{
    public const string Name = "SampleProject compilation";
}
