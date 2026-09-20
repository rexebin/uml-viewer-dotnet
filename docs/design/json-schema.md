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

## Out of scope for this schema

- Any field/property/parameter referencing another type (associations) — see [CONTEXT.md](../../CONTEXT.md#language).
- Accessibility modifiers (public/internal/private) — not part of the recorded MVP scope; add only if a later ticket surfaces a need.
