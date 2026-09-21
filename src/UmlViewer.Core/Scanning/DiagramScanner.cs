using UmlViewer.Core.Extraction;
using UmlViewer.Core.Loading;
using UmlViewer.Core.Rendering;

namespace UmlViewer.Core.Scanning;

/// <summary>
/// Runs the full pipeline for a single <c>.csproj</c>: load -> extract -> render as a self-contained HTML diagram.
/// </summary>
public static class DiagramScanner
{
    public static async Task<string> ScanToHtmlAsync(string csprojPath)
    {
        var loader = new ProjectLoader();
        var loaded = await loader.LoadAsync(csprojPath);
        var model = StructuralExtractor.Extract(loaded.Compilation);
        var mermaidSource = MermaidClassDiagramGenerator.ToMermaid(model);
        return DiagramHtmlWriter.ToHtml(model, mermaidSource);
    }
}
