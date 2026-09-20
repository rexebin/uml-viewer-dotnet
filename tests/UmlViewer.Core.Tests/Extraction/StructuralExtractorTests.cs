using UmlViewer.Core.Extraction;

namespace UmlViewer.Core.Tests.Extraction;

[Collection(SampleProjectCollection.Name)]
public class StructuralExtractorTests(SampleProjectCompilationFixture fixture)
{
    private StructuralModel Extract() => StructuralExtractor.Extract(fixture.Compilation);

    private static TypeEntry FindType(StructuralModel model, string ns, string name) =>
        Assert.Single(model.Types, t => t.Namespace == ns && t.Name == name);

    [Fact]
    public void Extract_PlainInterface_HasNoBaseTypeNoImplementedInterfacesNoContainingType()
    {
        var type = FindType(Extract(), "SampleProject.Common", "IIdentifiable");

        Assert.Equal("interface", type.Kind);
        Assert.Null(type.BaseType);
        Assert.Empty(type.ImplementedInterfaces);
        Assert.Null(type.ContainingType);
        Assert.Equal(["fixtures/SampleProject/Common/IIdentifiable.cs"], NormalizedSourceFiles(type));
    }

    [Fact]
    public void Extract_InterfaceExtendingInterface_ListsExtendedInterfaceAsImplementedInterface()
    {
        var type = FindType(Extract(), "SampleProject.Common", "IFeedable");

        var extended = Assert.Single(type.ImplementedInterfaces);
        Assert.Equal("SampleProject.Common", extended.Namespace);
        Assert.Equal("IIdentifiable", extended.Name);
        Assert.Empty(extended.TypeArguments);
    }

    [Fact]
    public void Extract_GenericInterfaceWithTypeConstraint_CapturesGenericParameterConstraint()
    {
        var type = FindType(Extract(), "SampleProject.Common", "IRepository");

        var parameter = Assert.Single(type.GenericParameters);
        Assert.Equal("T", parameter.Name);
        Assert.Equal(["IIdentifiable"], parameter.Constraints);
    }

    [Fact]
    public void Extract_Enum_HasUnderlyingTypeAndMembersWithValues()
    {
        var type = FindType(Extract(), "SampleProject.Animals", "AnimalKind");

        Assert.Equal("enum", type.Kind);
        Assert.Equal("byte", type.UnderlyingType);
        Assert.NotNull(type.Members);
        Assert.Equal(
            [("Mammal", 0L), ("Bird", 1L), ("Reptile", 2L)],
            type.Members!.Select(m => (m.Name, m.Value)));
    }

    [Fact]
    public void Extract_ClassImplementingInterface_ListsInterfaceButNoBaseType()
    {
        var type = FindType(Extract(), "SampleProject.Animals", "Animal");

        Assert.Equal("class", type.Kind);
        Assert.Null(type.BaseType);
        var implemented = Assert.Single(type.ImplementedInterfaces);
        Assert.Equal("IFeedable", implemented.Name);
    }

    [Fact]
    public void Extract_DerivedClass_HasBaseTypeAndNoOwnImplementedInterfaces()
    {
        var type = FindType(Extract(), "SampleProject.Animals", "Lion");

        Assert.NotNull(type.BaseType);
        Assert.Equal("Animal", type.BaseType!.Name);
        Assert.Empty(type.ImplementedInterfaces);
    }

    [Theory]
    [InlineData("SampleProject.Animals", "AnimalRecord", "record")]
    [InlineData("SampleProject.Animals", "Coordinates", "recordStruct")]
    [InlineData("SampleProject.Animals", "Enclosure", "struct")]
    public void Extract_TypeKind_ClassifiesAsExpectedKind(string ns, string name, string expectedKind)
    {
        var type = FindType(Extract(), ns, name);

        Assert.Equal(expectedKind, type.Kind);
    }

    [Fact]
    public void Extract_StructImplementingInterface_ListsImplementedInterface()
    {
        var type = FindType(Extract(), "SampleProject.Animals", "Enclosure");

        var implemented = Assert.Single(type.ImplementedInterfaces);
        Assert.Equal("IIdentifiable", implemented.Name);
    }

    [Fact]
    public void Extract_GenericClassWithReferenceTypeConstraint_CapturesClassConstraint()
    {
        var type = FindType(Extract(), "SampleProject.Common", "ReferenceTypeConstrained");

        var parameter = Assert.Single(type.GenericParameters);
        Assert.Equal(["class"], parameter.Constraints);
    }

    [Fact]
    public void Extract_GenericClassWithValueTypeConstraint_CapturesStructConstraint()
    {
        var type = FindType(Extract(), "SampleProject.Common", "ValueTypeConstrained");

        var parameter = Assert.Single(type.GenericParameters);
        Assert.Equal(["struct"], parameter.Constraints);
    }

    [Fact]
    public void Extract_GenericClassWithUnmanagedConstraint_CapturesUnmanagedConstraint()
    {
        var type = FindType(Extract(), "SampleProject.Common", "UnmanagedConstrained");

        var parameter = Assert.Single(type.GenericParameters);
        Assert.Equal(["struct", "unmanaged"], parameter.Constraints);
    }

    [Fact]
    public void Extract_GenericClassWithNotNullConstraint_CapturesNotNullConstraint()
    {
        var type = FindType(Extract(), "SampleProject.Common", "NotNullConstrained");

        var parameter = Assert.Single(type.GenericParameters);
        Assert.Equal(["notnull"], parameter.Constraints);
    }

    [Fact]
    public void Extract_GenericClassWithTypeAndConstructorConstraint_CapturesBothConstraints()
    {
        var type = FindType(Extract(), "SampleProject.Habitats", "Cage");

        var parameter = Assert.Single(type.GenericParameters);
        Assert.Equal("T", parameter.Name);
        Assert.Equal(["Animal", "new()"], parameter.Constraints);
    }

    [Fact]
    public void Extract_BaseTypeWithTypeArguments_CapturesTypeArgumentsAsSimpleNames()
    {
        var type = FindType(Extract(), "SampleProject.Habitats", "LionCage");

        Assert.NotNull(type.BaseType);
        Assert.Equal("Cage", type.BaseType!.Name);
        Assert.Equal(["Lion"], type.BaseType.TypeArguments);
    }

    [Fact]
    public void Extract_NestedClass_HasContainingTypePointer()
    {
        var type = FindType(Extract(), "SampleProject.Habitats", "Ticket");

        Assert.NotNull(type.ContainingType);
        Assert.Equal("SampleProject.Habitats", type.ContainingType!.Namespace);
        Assert.Equal("Zoo", type.ContainingType.Name);
    }

    [Fact]
    public void Extract_NestedEnum_HasContainingTypePointer()
    {
        var type = FindType(Extract(), "SampleProject.Habitats", "Season");

        Assert.NotNull(type.ContainingType);
        Assert.Equal("Zoo", type.ContainingType!.Name);
    }

    [Fact]
    public void Extract_TopLevelType_HasNullContainingType()
    {
        var type = FindType(Extract(), "SampleProject.Habitats", "Zoo");

        Assert.Null(type.ContainingType);
    }

    [Fact]
    public void Extract_PartialClassAcrossFiles_MergesIntoSingleTypeWithAllSourceFiles()
    {
        var model = Extract();
        var matches = model.Types.Where(t => t.Namespace == "SampleProject.Habitats" && t.Name == "ZooKeeper").ToList();

        var type = Assert.Single(matches);
        Assert.Equal(
            [
                "fixtures/SampleProject/Habitats/ZooKeeper.Shifts.cs",
                "fixtures/SampleProject/Habitats/ZooKeeper.cs",
            ],
            NormalizedSourceFiles(type));
    }

    [Fact]
    public void Extract_Files_CapturesPerFileUsingsThatDifferBetweenPartialDeclarations()
    {
        var model = Extract();

        var keeperFile = FindFile(model, "fixtures/SampleProject/Habitats/ZooKeeper.cs");
        var shiftsFile = FindFile(model, "fixtures/SampleProject/Habitats/ZooKeeper.Shifts.cs");

        Assert.Equal(["SampleProject.Common"], keeperFile.Usings);
        Assert.Equal(["System.Collections.Generic"], shiftsFile.Usings);
    }

    private static FileEntry FindFile(StructuralModel model, string relativePath) =>
        Assert.Single(model.Files, f => f.Path.EndsWith(relativePath, StringComparison.Ordinal));

    private static List<string> NormalizedSourceFiles(TypeEntry type) =>
        type.SourceFiles.Select(f => f[(f.IndexOf("fixtures/SampleProject", StringComparison.Ordinal))..]).ToList();
}
