using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UmlViewer.Core.Extraction;

public static class StructuralExtractor
{
    public static StructuralModel Extract(Compilation compilation)
    {
        var declaredTypes = WalkNamespace(compilation.GlobalNamespace).ToList();

        var types = declaredTypes
            .Select(ToTypeEntry)
            .ToList();

        var files = compilation.SyntaxTrees
            .Select(ToFileEntry)
            .ToList();

        var associations = declaredTypes
            .SelectMany(type => AssociationsOf(type, compilation.Assembly))
            .ToList();

        return new StructuralModel(Files: files, Types: types, Associations: associations);
    }

    private static FileEntry ToFileEntry(SyntaxTree tree)
    {
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var usings = root.Usings
            .Concat(root.Members.OfType<BaseNamespaceDeclarationSyntax>().SelectMany(n => n.Usings))
            .Where(u => u.Alias is null && !u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            .Select(u => u.Name!.ToString())
            .ToList();

        return new FileEntry(tree.FilePath, usings);
    }

    private static IEnumerable<INamedTypeSymbol> WalkNamespace(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (IsSourceDeclared(type))
            {
                foreach (var t in WalkType(type))
                {
                    yield return t;
                }
            }
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            foreach (var t in WalkNamespace(child))
            {
                yield return t;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> WalkType(INamedTypeSymbol type)
    {
        yield return type;

        foreach (var nested in type.GetTypeMembers())
        {
            if (IsSourceDeclared(nested))
            {
                foreach (var t in WalkType(nested))
                {
                    yield return t;
                }
            }
        }
    }

    private static bool IsSourceDeclared(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences.Length > 0;

    private static TypeEntry ToTypeEntry(INamedTypeSymbol type) =>
        new(
            Kind: KindOf(type),
            Namespace: type.ContainingNamespace.ToDisplayString(),
            Name: type.Name,
            GenericParameters: type.TypeParameters.Select(ToGenericParameter).ToList(),
            BaseType: BaseTypeOf(type),
            ImplementedInterfaces: type.Interfaces.Select(ToTypeReference).ToList(),
            ContainingType: type.ContainingType is { } containingType
                ? new TypeRef(containingType.ContainingNamespace.ToDisplayString(), containingType.Name)
                : null,
            SourceFiles: type.DeclaringSyntaxReferences
                .Select(r => r.SyntaxTree.FilePath)
                .Distinct()
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList(),
            UnderlyingType: type.TypeKind == TypeKind.Enum ? SimpleName(type.EnumUnderlyingType!) : null,
            Members: type.TypeKind == TypeKind.Enum ? EnumMembersOf(type) : null);

    private static List<EnumMember> EnumMembersOf(INamedTypeSymbol enumType) =>
        [.. enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.ConstantValue is not null)
            .Select(f => new EnumMember(f.Name, Convert.ToInt64(f.ConstantValue)))];

    private static TypeReference? BaseTypeOf(INamedTypeSymbol type)
    {
        var baseType = type.BaseType;
        if (baseType is null || baseType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
        {
            return null;
        }

        return ToTypeReference(baseType);
    }

    private static GenericParameter ToGenericParameter(ITypeParameterSymbol parameter)
    {
        List<string> constraints = [.. parameter.ConstraintTypes.Select(SimpleName)];

        if (parameter.HasReferenceTypeConstraint)
        {
            constraints.Add("class");
        }

        if (parameter.HasValueTypeConstraint)
        {
            constraints.Add("struct");
        }

        if (parameter.HasUnmanagedTypeConstraint)
        {
            constraints.Add("unmanaged");
        }

        if (parameter.HasNotNullConstraint)
        {
            constraints.Add("notnull");
        }

        if (parameter.HasConstructorConstraint)
        {
            constraints.Add("new()");
        }

        return new GenericParameter(parameter.Name, constraints);
    }

    private static string SimpleName(ITypeSymbol type) =>
        type.SpecialType != SpecialType.None
            ? type.ToDisplayString()
            : type is INamedTypeSymbol named ? named.Name : type.ToDisplayString();

    private static TypeReference ToTypeReference(INamedTypeSymbol type) =>
        new(
            Namespace: type.ContainingNamespace.ToDisplayString(),
            Name: type.Name,
            TypeArguments: type.TypeArguments.Select(SimpleName).ToList());

    private static IEnumerable<AssociationEntry> AssociationsOf(INamedTypeSymbol type, IAssemblySymbol compilationAssembly)
    {
        var from = new TypeRef(type.ContainingNamespace.ToDisplayString(), type.Name);

        foreach (var member in type.GetMembers())
        {
            if (member.IsImplicitlyDeclared)
            {
                continue;
            }

            var memberType = member switch
            {
                IFieldSymbol { IsConst: false } field => field.Type,
                IPropertySymbol { IsIndexer: false } property => property.Type,
                _ => null,
            };

            if (memberType is null)
            {
                continue;
            }

            var (target, isCollection) = ResolveAssociationTarget(memberType);

            if (target is null || !SymbolEqualityComparer.Default.Equals(target.ContainingAssembly, compilationAssembly))
            {
                continue;
            }

            yield return new AssociationEntry(
                From: from,
                To: new TypeRef(target.ContainingNamespace.ToDisplayString(), target.Name),
                MemberName: member.Name,
                IsCollection: isCollection);
        }
    }

    private static (INamedTypeSymbol? Target, bool IsCollection) ResolveAssociationTarget(ITypeSymbol memberType)
    {
        var elementType = EnumerableElementTypeOf(memberType);
        if (elementType is not null)
        {
            return (elementType as INamedTypeSymbol, true);
        }

        return (memberType as INamedTypeSymbol, false);
    }

    private static ITypeSymbol? EnumerableElementTypeOf(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        var enumerableInterface = SelfAndInterfaces(type)
            .FirstOrDefault(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        return enumerableInterface?.TypeArguments[0];
    }

    private static IEnumerable<INamedTypeSymbol> SelfAndInterfaces(ITypeSymbol type) =>
        type is INamedTypeSymbol named ? [named, .. named.AllInterfaces] : type.AllInterfaces;

    private static string KindOf(INamedTypeSymbol type) => type.TypeKind switch
    {
        TypeKind.Enum => "enum",
        TypeKind.Interface => "interface",
        TypeKind.Struct => type.IsRecord ? "recordStruct" : "struct",
        TypeKind.Class => type.IsRecord ? "record" : "class",
        _ => throw new NotSupportedException($"Unsupported type kind '{type.TypeKind}' for {type}"),
    };
}
