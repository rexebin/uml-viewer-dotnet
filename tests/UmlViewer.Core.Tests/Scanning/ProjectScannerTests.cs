using System.Text.Json;
using UmlViewer.Core.Loading;
using UmlViewer.Core.Scanning;

namespace UmlViewer.Core.Tests.Scanning;

public class ProjectScannerTests
{
    private static string SampleProjectPath => FindFixture("fixtures/SampleProject/SampleProject.csproj");

    [Fact]
    public async Task ScanToJsonAsync_WithSampleProject_ReturnsJsonContainingExtractedTypes()
    {
        var json = await ProjectScanner.ScanToJsonAsync(SampleProjectPath);

        using var document = JsonDocument.Parse(json);
        var typeNames = document.RootElement.GetProperty("types")
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("Animal", typeNames);
    }

    [Fact]
    public async Task ScanToJsonAsync_WithNonExistentProjectFile_ThrowsProjectLoadException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.csproj");

        await Assert.ThrowsAsync<ProjectLoadException>(() => ProjectScanner.ScanToJsonAsync(missingPath));
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
