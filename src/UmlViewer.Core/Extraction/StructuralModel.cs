namespace UmlViewer.Core.Extraction;

public sealed record StructuralModel(
    IReadOnlyList<FileEntry> Files,
    IReadOnlyList<TypeEntry> Types,
    IReadOnlyList<AssociationEntry> Associations);

public sealed record FileEntry(
    string Path,
    IReadOnlyList<string> Usings);

public sealed record TypeEntry(
    string Kind,
    string Namespace,
    string Name,
    IReadOnlyList<GenericParameter> GenericParameters,
    TypeReference? BaseType,
    IReadOnlyList<TypeReference> ImplementedInterfaces,
    TypeRef? ContainingType,
    IReadOnlyList<string> SourceFiles,
    string? UnderlyingType,
    IReadOnlyList<EnumMember>? Members);

public sealed record GenericParameter(
    string Name,
    IReadOnlyList<string> Constraints);

public sealed record TypeReference(
    string Namespace,
    string Name,
    IReadOnlyList<string> TypeArguments);

public sealed record TypeRef(
    string Namespace,
    string Name);

public sealed record EnumMember(
    string Name,
    long Value);

public sealed record AssociationEntry(
    TypeRef From,
    TypeRef To,
    string MemberName,
    bool IsCollection);
