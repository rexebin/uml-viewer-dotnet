using UmlViewer.Core.Extraction;
using UmlViewer.Core.Output;

namespace UmlViewer.Core.Rendering;

public static class DiagramHtmlWriter
{
    public static string ToHtml(StructuralModel model, string mermaidSource)
    {
        var modelJson = StructuralModelJsonWriter.ToJson(model);

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8">
              <title>UML Diagram</title>
              <script src="https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js"></script>
              <style>
                body { margin: 0; font-family: sans-serif; }
                #diagram { padding: 1rem; }
                #detail-panel { position: fixed; top: 0; right: 0; width: 320px; height: 100%;
                  background: #fff; border-left: 1px solid #ccc; padding: 1rem; overflow: auto;
                  transform: translateX(100%); transition: transform 0.2s ease-out; }
                #detail-panel.open { transform: translateX(0); }
              </style>
            </head>
            <body>
              <div id="diagram" class="mermaid">
            {{mermaidSource}}
              </div>
              <div id="detail-panel"></div>
              <script type="application/json" id="model-data">
            {{modelJson}}
              </script>
              <script>
                const model = JSON.parse(document.getElementById('model-data').textContent);
                mermaid.initialize({ startOnLoad: true });

                function umlViewerNodeClick(nodeId) {
                  const type = model.types.find(t => (t.namespace + '.' + t.name).replace(/\./g, '_') === nodeId);
                  if (!type) return;
                  const panel = document.getElementById('detail-panel');
                  panel.innerHTML = '<h2>' + type.name + '</h2>'
                    + '<p>' + type.kind + ' in ' + type.namespace + '</p>'
                    + (type.sourceFiles || []).map(f => '<a href="vscode://file' + f + '">' + f + '</a>').join('<br>');
                  panel.classList.add('open');
                }
              </script>
            </body>
            </html>
            """;
    }
}
