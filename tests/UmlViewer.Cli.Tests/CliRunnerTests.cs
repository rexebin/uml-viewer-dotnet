using UmlViewer.Cli;

namespace UmlViewer.Cli.Tests;

public class CliRunnerTests
{
    private static string SampleProjectPath => FindFixture("fixtures/SampleProject/SampleProject.csproj");

    [Fact]
    public async Task RunAsync_WithValidCsprojPath_WritesJsonModelToStdoutAndReturnsZero()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliRunner.RunAsync([SampleProjectPath], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"types\"", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public async Task RunAsync_WithOutputPathArgument_WritesJsonModelToFileNotStdout()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var outputPath = Path.Combine(Path.GetTempPath(), $"uml-viewer-cli-test-{Guid.NewGuid():N}.json");

        try
        {
            var exitCode = await CliRunner.RunAsync([SampleProjectPath, outputPath], stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, stdout.ToString());
            Assert.Contains("\"types\"", await File.ReadAllTextAsync(outputPath));
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RunAsync_WithHtmlOutputPathArgument_WritesHtmlDiagramToFile()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var outputPath = Path.Combine(Path.GetTempPath(), $"uml-viewer-cli-test-{Guid.NewGuid():N}.html");

        try
        {
            var exitCode = await CliRunner.RunAsync([SampleProjectPath, outputPath], stdout, stderr);

            Assert.Equal(0, exitCode);
            var written = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("<html", written);
            Assert.Contains("mermaid", written);
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RunAsync_WithNoArguments_WritesUsageToStderrAndReturnsNonZero()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await CliRunner.RunAsync([], stdout, stderr);

        Assert.NotEqual(0, exitCode);
        Assert.Contains("Usage:", stderr.ToString());
    }

    [Fact]
    public async Task RunAsync_WithNonExistentProjectFile_WritesErrorToStderrAndReturnsNonZero()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.csproj");

        var exitCode = await CliRunner.RunAsync([missingPath], stdout, stderr);

        Assert.NotEqual(0, exitCode);
        Assert.Contains(missingPath, stderr.ToString());
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
