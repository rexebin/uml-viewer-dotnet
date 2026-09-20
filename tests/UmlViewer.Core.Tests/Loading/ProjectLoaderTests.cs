using Microsoft.CodeAnalysis;
using UmlViewer.Core.Loading;

namespace UmlViewer.Core.Tests.Loading;

public class ProjectLoaderTests
{
    private static string SampleProjectPath => FindFixture("fixtures/SampleProject/SampleProject.csproj");

    [Fact]
    public async Task LoadAsync_WithSampleProject_ReturnsCompilationWithNoErrorDiagnostics()
    {
        var loader = new ProjectLoader();

        var loaded = await loader.LoadAsync(SampleProjectPath);

        var errors = loaded.Compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public async Task LoadAsync_WithSampleProject_IncludesEveryHandWrittenSourceFileAsASyntaxTree()
    {
        var loader = new ProjectLoader();

        var loaded = await loader.LoadAsync(SampleProjectPath);

        var expectedFiles = Directory.GetFiles(
            Path.GetDirectoryName(SampleProjectPath)!, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToList();

        var syntaxTreeFilePaths = loaded.Compilation.SyntaxTrees
            .Select(t => t.FilePath)
            .ToHashSet();

        Assert.All(expectedFiles, file => Assert.Contains(file, syntaxTreeFilePaths));
    }

    [Fact]
    public async Task LoadAsync_WithNonExistentProjectFile_ThrowsProjectLoadException()
    {
        var loader = new ProjectLoader();
        var missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.csproj");

        await Assert.ThrowsAsync<ProjectLoadException>(() => loader.LoadAsync(missingPath));
    }

    [Fact]
    public async Task LoadAsync_WithMalformedProjectFile_ThrowsProjectLoadExceptionCarryingWorkspaceFailureMessage()
    {
        var loader = new ProjectLoader();
        var malformedPath = FindFixture("fixtures/MalformedProject/Malformed.csproj");

        var ex = await Assert.ThrowsAsync<ProjectLoadException>(() => loader.LoadAsync(malformedPath));

        Assert.Contains($"Failed to load project '{malformedPath}'", ex.Message);
    }

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
