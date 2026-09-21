using UmlViewer.Core.Extraction;
using UmlViewer.Core.Output;
using UmlViewer.Core.Rendering;

namespace UmlViewer.Core.Tests.Rendering;

public class DiagramHtmlWriterTests
{
    [Fact]
    public void ToHtml_ContainsGivenMermaidSourceVerbatim()
    {
        var model = new StructuralModel(Files: [], Types: [], Associations: []);
        const string mermaidSource = "classDiagram\n    class Zoo_Animals_Animal";

        var html = DiagramHtmlWriter.ToHtml(model, mermaidSource);

        Assert.Contains(mermaidSource, html);
    }

    [Fact]
    public void ToHtml_WithInterfaceAnnotationInMermaidSource_EscapesItSoBrowserDoesNotParseItAsATag()
    {
        var model = new StructuralModel(Files: [], Types: [], Associations: []);
        const string mermaidSource = "classDiagram\n    class Foo\n    <<interface>> Foo";

        var html = DiagramHtmlWriter.ToHtml(model, mermaidSource);

        Assert.DoesNotContain("<<interface>>", html);
        Assert.Contains("&lt;&lt;interface&gt;&gt;", html);
    }

    [Fact]
    public void ToHtml_ContainsSerializedModelJsonMatchingJsonOutputMode()
    {
        var model = new StructuralModel(
            Files: [],
            Types: [new TypeEntry("class", "Zoo.Animals", "Animal", [], null, [], null, [], null, null)],
            Associations: []);

        var html = DiagramHtmlWriter.ToHtml(model, "classDiagram");

        Assert.Contains(StructuralModelJsonWriter.ToJson(model), html);
    }
}
