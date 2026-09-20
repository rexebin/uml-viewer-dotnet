using UmlViewer.Core.Extraction;
using UmlViewer.Core.Loading;
using UmlViewer.Core.Output;

namespace UmlViewer.Core.Scanning;

/// <summary>
/// Runs the full pipeline for a single <c>.csproj</c>: load -> extract -> serialize.
/// </summary>
public static class ProjectScanner
{
    public static async Task<string> ScanToJsonAsync(string csprojPath)
    {
        var loader = new ProjectLoader();
        var loaded = await loader.LoadAsync(csprojPath);
        var model = StructuralExtractor.Extract(loaded.Compilation);
        return StructuralModelJsonWriter.ToJson(model);
    }
}
