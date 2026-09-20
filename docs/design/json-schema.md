# Structural model JSON schema

Resolves [Design: JSON schema for structural model (#3)](https://github.com/rexebin/uml-viewer-dotnet/issues/3).

## Shape

```jsonc
{
  "files": [
    {
      "path": "src/Foo.cs",
      "usings": ["System", "System.Collections.Generic"]
    }
  ],
  "types": [
    {
      "kind": "class",           // "class" | "interface" | "record" | "struct" | "recordStruct" | "enum"
      "namespace": "MyApp.Models",
      "name": "Foo",
      "genericParameters": [
        { "name": "T", "constraints": ["IFoo"] }
      ],
      "baseType": {
        "namespace": "MyApp.Models",
        "name": "Bar",
        "typeArguments": ["int"]
      },
      "implementedInterfaces": [
        { "namespace": "System", "name": "IDisposable", "typeArguments": [] }
      ],
      "containingType": null,     // { "namespace": ..., "name": ... } | null
      "sourceFiles": ["src/Foo.cs", "src/Foo.Extra.cs"],

      // enum only:
      "underlyingType": null,     // e.g. "int", null for non-enums
      "members": null             // [{ "name": "Red", "value": 0 }], null for non-enums
    }
  ],
  "associations": [
    {
      "from": { "namespace": "MyApp", "name": "Zoo" },
      "to": { "namespace": "MyApp", "name": "Animal" },
      "memberName": "Animals",
      "isCollection": true
    }
  ]
}
```

## Field notes

- **Root is flat**: `files` and `types` are both flat lists, not nested trees. `namespace` is a plain string field on each type; `containingType` points at a parent type by `{ namespace, name }` rather than nesting the child inline. Namespaces in C# aren't strictly hierarchical containers (multiple namespaces per file, `namespace A.B.C` as one declaration), so a tree would force a modeling decision the language itself doesn't make. Flat + fields lets any consumer group client-side.
- **Type identity**: `namespace` + `name` together are the de-facto key. No synthetic ID for MVP.
- **`kind`** includes `enum` alongside class/interface/record/struct/recordStruct — added during grilling since its absence would be a conspicuous gap the moment this is pointed at real code.
- **Generics**: a type's own `genericParameters` capture name + constraints (as strings, e.g. `"IFoo"`, `"struct"`, `"new()"`). A base type or implemented interface that's itself generic (`Bar<int>`) records its own identity plus `typeArguments` as strings — not resolved as full type references. Resolving those arguments as references is association-tracking, out of scope for this MVP.
- **`baseType`** is single and nullable (C# single inheritance for classes; records/structs have no base type in this model beyond object). **`implementedInterfaces`** is a list — covers a class's/struct's/record's implemented interfaces and an interface's own extended interfaces.
- **Partial types merge**: one type entry per fully-qualified name regardless of how many `partial` declarations exist. `sourceFiles` lists every file the declarations came from (single-file types get a one-element list).
- **Usings live with files, not types**: top-level `files` array of `{ path, usings }`, separate from `types`. A type's `sourceFiles` cross-reference into it by path. Chosen over a type-level usings field (types don't own usings, files do) and over a separate flat `path -> usings` map (redundant second top-level structure keyed the same way as `files`).
- **Enum members**: `name` plus the explicit or compiler-resolved integer `value`. No real design trade-off — enums have no other extraction data.
- **`associations`**: a flat root-level list, alongside `files`/`types`, reusing the existing `TypeRef` (`{ namespace, name }`) shape for both `from` and `to`. One entry per declared (non-inherited, non-compiler-generated) field or property whose type resolves to another project-declared type — method parameters are out of scope for this pass. `from` is the declaring type; `to` is the resolved target type; `memberName` is the field/property name; `isCollection` is `true` when the member's type is an array or implements `IEnumerable<T>` (the association then points at the unwrapped element type `T`, not the collection type itself). Only one flat association kind is recorded — no composition/aggregation/dependency distinction in this pass. A target is only emitted when its containing assembly matches the compilation's own assembly (i.e. it's one of this scan's `types` entries); this is what filters out `string`, `DateTime`, `List<T>` itself, and other BCL/external types.
- **Known limitation — multi-type-argument collections**: `Dictionary<K,V>` (and similar multi-argument collection types) resolve via `IEnumerable<KeyValuePair<K,V>>`, whose single type argument is `KeyValuePair<K,V>` — not `K` or `V` individually, and `KeyValuePair<K,V>` itself is a BCL type. This means a `Dictionary<K,V>`-typed member will not produce a clean association edge to `K` or `V`. Accepted as a known gap for this pass rather than special-cased.

## Out of scope for this schema

- Method-parameter-based associations, and any composition/aggregation/dependency distinction between associations — see [CONTEXT.md](../../CONTEXT.md#language).
- Accessibility modifiers (public/internal/private) — not part of the recorded MVP scope; add only if a later ticket surfaces a need.
