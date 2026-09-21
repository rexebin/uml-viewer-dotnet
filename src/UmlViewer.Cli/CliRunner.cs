using UmlViewer.Core.Loading;
using UmlViewer.Core.Scanning;

namespace UmlViewer.Cli;

public static class CliRunner
{
    private const int Success = 0;
    private const int Failure = 1;

    public static async Task<int> RunAsync(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length is 0 or > 2)
        {
            await stderr.WriteLineAsync("Usage: uml-viewer <csproj-path> [output-json-path]");
            return Failure;
        }

        var csprojPath = args[0];
        var outputPath = args.Length == 2 ? args[1] : null;

        string content;
        try
        {
            content = await ScanAsync(csprojPath, outputPath);
        }
        catch (ProjectLoadException ex)
        {
            await stderr.WriteLineAsync(ex.Message);
            return Failure;
        }

        if (outputPath is null)
        {
            await stdout.WriteLineAsync(content);
        }
        else
        {
            await File.WriteAllTextAsync(outputPath, content);
        }

        return Success;
    }

    private static Task<string> ScanAsync(string csprojPath, string? outputPath)
    {
        var isHtmlOutput = outputPath is not null && outputPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
        return isHtmlOutput
            ? DiagramScanner.ScanToHtmlAsync(csprojPath)
            : ProjectScanner.ScanToJsonAsync(csprojPath);
    }
}
