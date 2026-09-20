using System.Text.Encodings.Web;
using System.Text.Json;
using UmlViewer.Core.Extraction;

namespace UmlViewer.Core.Output;

public static class StructuralModelJsonWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string ToJson(StructuralModel model) =>
        JsonSerializer.Serialize(model, Options);
}
