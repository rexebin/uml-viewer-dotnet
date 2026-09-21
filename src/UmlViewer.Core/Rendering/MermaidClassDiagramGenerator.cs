using System.Text;
using UmlViewer.Core.Extraction;

namespace UmlViewer.Core.Rendering;

public static class MermaidClassDiagramGenerator
{
    public static string ToMermaid(StructuralModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("classDiagram");

        foreach (var type in model.Types)
        {
            var nodeId = NodeId(type.Namespace, type.Name);
            sb.AppendLine($"    class {nodeId}");
            if (type.Kind == "interface")
            {
                sb.AppendLine($"    <<interface>> {nodeId}");
            }

            sb.AppendLine($"    click {nodeId} call umlViewerNodeClick(\"{nodeId}\")");

            if (type.BaseType is not null)
            {
                sb.AppendLine($"    {NodeId(type.BaseType.Namespace, type.BaseType.Name)} <|-- {nodeId}");
            }

            foreach (var iface in type.ImplementedInterfaces)
            {
                sb.AppendLine($"    {NodeId(iface.Namespace, iface.Name)} <|.. {nodeId}");
            }
        }

        foreach (var association in model.Associations)
        {
            var from = NodeId(association.From.Namespace, association.From.Name);
            var to = NodeId(association.To.Namespace, association.To.Name);
            sb.AppendLine($"    {from} --> {to} : {association.MemberName}");
        }

        return sb.ToString();
    }

    private static string NodeId(string ns, string name) =>
        $"{ns}.{name}".Replace('.', '_');
}
