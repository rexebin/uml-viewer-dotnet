using UmlViewer.Core.Loading;
using UmlViewer.Core.Scanning;

namespace UmlViewer.Core.Tests.Scanning;

public class DiagramScannerTests
{
    private static string SampleProjectPath => FindFixture("fixtures/SampleProject/SampleProject.csproj");

    [Fact]
    public async Task ScanToHtmlAsync_WithSampleProject_ReturnsHtmlContainingExpectedTypeName()
    {
        var html = await DiagramScanner.ScanToHtmlAsync(SampleProjectPath);

        Assert.Contains("Animal", html);
    }

    [Fact]
    public async Task ScanToHtmlAsync_WithNonExistentProjectFile_ThrowsProjectLoadException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.csproj");

        await Assert.ThrowsAsync<ProjectLoadException>(() => DiagramScanner.ScanToHtmlAsync(missingPath));
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
