using System.Text.Json;
using UmlViewer.Core.Extraction;
using UmlViewer.Core.Output;

namespace UmlViewer.Core.Tests.Output;

public class StructuralModelJsonWriterTests
{
    [Fact]
    public void WritesFilesAndTypesWithCamelCaseFieldNames()
    {
        var model = new StructuralModel(
            Files: [new FileEntry("src/Foo.cs", ["System"])],
            Types:
            [
                new TypeEntry(
                    Kind: "class",
                    Namespace: "MyApp.Models",
                    Name: "Foo",
                    GenericParameters: [],
                    BaseType: null,
                    ImplementedInterfaces: [],
                    ContainingType: null,
                    SourceFiles: ["src/Foo.cs"],
                    UnderlyingType: null,
                    Members: null)
            ]);

        var json = StructuralModelJsonWriter.ToJson(model);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var file = root.GetProperty("files")[0];
        Assert.Equal("src/Foo.cs", file.GetProperty("path").GetString());
        Assert.Equal("System", file.GetProperty("usings")[0].GetString());

        var type = root.GetProperty("types")[0];
        Assert.Equal("class", type.GetProperty("kind").GetString());
        Assert.Equal("MyApp.Models", type.GetProperty("namespace").GetString());
        Assert.Equal("Foo", type.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, type.GetProperty("baseType").ValueKind);
        Assert.Equal("src/Foo.cs", type.GetProperty("sourceFiles")[0].GetString());
    }
}
