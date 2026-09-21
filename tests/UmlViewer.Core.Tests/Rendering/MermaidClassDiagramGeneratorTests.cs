using UmlViewer.Core.Extraction;
using UmlViewer.Core.Rendering;

namespace UmlViewer.Core.Tests.Rendering;

public class MermaidClassDiagramGeneratorTests
{
    [Fact]
    public void ToMermaid_WithOneType_EmitsClassDiagramBlockNamedBySanitizedNamespaceAndName()
    {
        var model = new StructuralModel(
            Files: [],
            Types: [Type("class", "Zoo.Animals", "Animal")],
            Associations: []);

        var mermaid = MermaidClassDiagramGenerator.ToMermaid(model);

        Assert.Contains("class Zoo_Animals_Animal", mermaid);
    }

    [Fact]
    public void ToMermaid_WithInterfaceType_AnnotatesNodeWithInterfaceStereotype()
    {
        var model = new StructuralModel(
            Files: [],
            Types: [Type("interface", "Zoo.Common", "IFeedable")],
            Associations: []);

        var mermaid = MermaidClassDiagramGenerator.ToMermaid(model);

        Assert.Contains("<<interface>> Zoo_Common_IFeedable", mermaid);
    }

    [Fact]
    public void ToMermaid_WithOneType_EmitsClickDirectiveForNode()
    {
        var model = new StructuralModel(
            Files: [],
            Types: [Type("class", "Zoo.Animals", "Animal")],
            Associations: []);

        var mermaid = MermaidClassDiagramGenerator.ToMermaid(model);

        Assert.Contains("click Zoo_Animals_Animal call umlViewerNodeClick(\"Zoo_Animals_Animal\")", mermaid);
    }

    [Fact]
    public void ToMermaid_WithBaseType_EmitsInheritanceArrowFromBaseToDerived()
    {
        var model = new StructuralModel(
            Files: [],
            Types:
            [
                Type("class", "Zoo.Animals", "Animal"),
                Type(
                    "class",
                    "Zoo.Animals",
                    "Lion",
                    baseType: new TypeReference("Zoo.Animals", "Animal", [])),
            ],
            Associations: []);

        var mermaid = MermaidClassDiagramGenerator.ToMermaid(model);

        Assert.Contains("Zoo_Animals_Animal <|-- Zoo_Animals_Lion", mermaid);
    }

    [Fact]
    public void ToMermaid_WithImplementedInterface_EmitsRealizationArrowFromInterfaceToType()
    {
        var model = new StructuralModel(
            Files: [],
            Types:
            [
                Type("interface", "Zoo.Common", "IFeedable"),
                Type(
                    "class",
                    "Zoo.Animals",
                    "Lion",
                    implementedInterfaces: [new TypeReference("Zoo.Common", "IFeedable", [])]),
            ],
            Associations: []);

        var mermaid = MermaidClassDiagramGenerator.ToMermaid(model);

        Assert.Contains("Zoo_Common_IFeedable <|.. Zoo_Animals_Lion", mermaid);
    }

    [Fact]
    public void ToMermaid_WithAssociation_EmitsLabeledAssociationArrow()
    {
        var model = new StructuralModel(
            Files: [],
            Types:
            [
                Type("class", "Zoo.Habitats", "Zoo"),
                Type("class", "Zoo.Habitats", "ZooKeeper"),
            ],
            Associations:
            [
                new AssociationEntry(
                    From: new TypeRef("Zoo.Habitats", "Zoo"),
                    To: new TypeRef("Zoo.Habitats", "ZooKeeper"),
                    MemberName: "Keeper",
                    IsCollection: false),
            ]);

        var mermaid = MermaidClassDiagramGenerator.ToMermaid(model);

        Assert.Contains("Zoo_Habitats_Zoo --> Zoo_Habitats_ZooKeeper : Keeper", mermaid);
    }

    [Fact]
    public void ToMermaid_WithSameSimpleNameInDifferentNamespaces_AssignsDistinctNodeIds()
    {
        var model = new StructuralModel(
            Files: [],
            Types:
            [
                Type("class", "Zoo.Habitats", "Cage"),
                Type("class", "Zoo.Storage", "Cage"),
            ],
            Associations: []);

        var mermaid = MermaidClassDiagramGenerator.ToMermaid(model);

        Assert.Contains("class Zoo_Habitats_Cage", mermaid);
        Assert.Contains("class Zoo_Storage_Cage", mermaid);
    }

    private static TypeEntry Type(
        string kind,
        string ns,
        string name,
        TypeReference? baseType = null,
        IReadOnlyList<TypeReference>? implementedInterfaces = null) =>
        new(
            Kind: kind,
            Namespace: ns,
            Name: name,
            GenericParameters: [],
            BaseType: baseType,
            ImplementedInterfaces: implementedInterfaces ?? [],
            ContainingType: null,
            SourceFiles: [],
            UnderlyingType: null,
            Members: null);
}
