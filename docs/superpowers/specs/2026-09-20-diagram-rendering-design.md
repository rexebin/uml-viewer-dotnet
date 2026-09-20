# Design: diagram rendering

Follows [Design: JSON schema for structural model (#3)](https://github.com/rexebin/uml-viewer-dotnet/issues/3)
and the association discovery work ([#10](https://github.com/rexebin/uml-viewer-dotnet/issues/10)). Renders
the existing `StructuralModel` JSON ([docs/design/json-schema.md](../../design/json-schema.md)) as a class
diagram, built in-house rather than integrating with `unclebob/uml-viewer` (decision recorded in project
memory `uml-viewer-own-renderer`).

## Goal

`uml-viewer <csproj-path> diagram.html` produces one self-contained HTML file showing a full class diagram
(all types, one diagram, not split by namespace) of the scanned project: types as nodes, inheritance/interface
implementation/association edges. Clicking a node shows its member details in a side panel, including a
"view source" link that opens the file directly in the reader's editor.

## Out of scope for this ticket

- Metrics overlay (CRAP/mutation score coloring or badges) — a separate future ticket once this rendering
  core is proven out.
- Composition/aggregation/dependency distinction between association edges — the schema records one flat
  association kind; this ticket draws all of them as plain association arrows.
- Namespace-scoped diagrams or single-type neighborhood views — full-model diagram only, for now.
- Any interactive local server / live source-snippet embedding — see "Drill-to-code" below.

## Architecture

Two new pure, independently-testable components in `UmlViewer.Core`, composed by a new scanner, mirroring
the existing `ProjectScanner` / `StructuralModelJsonWriter` split:

- **`MermaidClassDiagramGenerator.ToMermaid(StructuralModel model) -> string`** — translates the structural
  model into Mermaid `classDiagram` syntax. Pure function, no I/O.
- **`DiagramHtmlWriter.ToHtml(StructuralModel model, string mermaidSource) -> string`** — wraps the Mermaid
  source and the full model JSON into one self-contained HTML page (CDN-loaded `mermaid.js`, inline styles,
  inline click-handler script). Pure function, no I/O.
- **`DiagramScanner.ScanToHtmlAsync(string csprojPath) -> Task<string>`** — composes `ProjectLoader` +
  `StructuralExtractor` + the two components above, the same shape as `ProjectScanner.ScanToJsonAsync`.

`CliRunner` gains one branch: when an output path is given and ends in `.html` (case-insensitive), it calls
`DiagramScanner.ScanToHtmlAsync` and writes the result instead of the JSON path. Stdout-only invocation (no
output path argument) is unchanged — it always produces JSON; HTML-to-stdout is not a supported case.

## Mermaid generation details

- Each `TypeEntry` becomes one Mermaid `class` block, named by a **sanitized node ID** derived from
  `namespace + "." + name` (Mermaid class names can't reliably contain dots or generic-style brackets). A
  node-ID map is built once per generation pass so same-simple-name types in different namespaces never
  collide. The type's full namespace is not shown on the diagram face — it's available in the click-detail
  panel — the node label is just the simple name (kind-prefixed where Mermaid supports it, e.g.
  `<<interface>>`).
- **Inheritance**: a non-null `baseType` emits a Mermaid inheritance arrow (`<|--`) from the base to the
  derived node.
- **Interface implementation**: each entry in `implementedInterfaces` emits a Mermaid realization arrow
  (`<|..`) from the interface to the implementing/extending type.
- **Associations**: each `AssociationEntry` emits a plain association arrow (`-->`) from `from` to `to`,
  labeled with `memberName`. `isCollection` does not change the arrow style for this pass (multiplicity
  annotations are a possible future refinement, not required for v1).
- **Generics**: a generic type renders under its simple name only. `genericParameters` and any
  `typeArguments` on base types/interfaces are omitted from the diagram face and surfaced only in the
  click-detail panel — keeping the diagram readable was the explicit reason for choosing name-only nodes
  over full Mermaid member-listing syntax.
- A `click <nodeId> call umlViewerNodeClick("<nodeId>")` directive is emitted for every node.
- Types outside the compiled assembly are never nodes (the model already excludes them from `types`, and
  `associations`' target-filtering already excludes BCL/external types — nothing new to filter here).

## Drill-to-code

No local server, no live filesystem reads at view time — the generated HTML is fully self-contained and
works from a file:// URL or shared as a static artifact.

- The full `StructuralModel` JSON is embedded verbatim in a `<script type="application/json">` block.
- The click handler (`umlViewerNodeClick`) looks up the clicked node's `TypeEntry` by ID in that embedded
  JSON and renders a side panel: kind, namespace, generic parameters, base type, implemented interfaces,
  and one link per entry in `sourceFiles`.
- Each source-file link is a `vscode://file{absolutePath}` deep link. This works without any path
  resolution because `FileEntry.path` (and therefore every `sourceFiles` entry) is already an absolute path
  — confirmed by inspecting current CLI output, since `StructuralExtractor` derives it from the Roslyn
  syntax tree's `FilePath`. Readers without VS Code installed will see the link fail silently (browser-level
  behavior for unregistered custom protocols) — no special handling needed; this is an accepted limitation,
  not a bug to fix in this ticket.

## HTML page shape

One file, three parts:

1. `<script src>` pointing at a CDN-hosted `mermaid.js` build (no bundler, no build step — consistent with
   this being a single generated artifact, not a built frontend project).
2. Inline `<style>` for a minimal side-panel layout (diagram takes the main viewport; panel slides in on
   node click).
3. Inline `<script>` containing: the embedded model JSON, `mermaid.initialize(...)`, and the
   `umlViewerNodeClick` handler.

## Testing

Standard TDD, one behavior per test, output-based (pure functions in, string out — no browser needed to
verify Mermaid syntax or HTML shape):

- `MermaidClassDiagramGeneratorTests`: one test per edge kind (inheritance arrow present for a type with a
  base type; realization arrow present per implemented interface; association arrow present and labeled
  for an `AssociationEntry`; node emitted per type; click directive emitted per node; same-simple-name
  types in different namespaces get distinct node IDs).
- `DiagramHtmlWriterTests`: asserts the returned HTML contains the given Mermaid source verbatim and the
  serialized model JSON (reusing `StructuralModelJsonWriter`'s camelCase convention for consistency, so the
  embedded JSON matches what a consumer already expects from the `.json` output mode).
- `DiagramScannerTests`: one integration test against the real `SampleProject` fixture, mirroring
  `ProjectScannerTests` (asserts the returned HTML string contains an expected type name), plus a
  non-existent-project-path case asserting `ProjectLoadException` propagates unchanged.
- `CliRunnerTests`: one new case — an `.html` output path produces a file whose content looks like HTML
  (e.g. contains `<html` and the Mermaid CDN script tag) rather than raw JSON.

No new fixture types are needed. `SampleProject` already has classes, interfaces, enums, records, and
(after #10) associations — every edge kind this ticket needs to exercise already exists there.

## Error handling

No new failure modes. `DiagramScanner.ScanToHtmlAsync` propagates `ProjectLoadException` from
`ProjectLoader` exactly as `ProjectScanner.ScanToJsonAsync` already does — `CliRunner`'s existing catch
block handles both output modes identically.

## CRAP / mutation verification

As established in this project's workflow (see #8, #10), the new C# code (`MermaidClassDiagramGenerator`,
`DiagramHtmlWriter`, `DiagramScanner`, the new `CliRunner` branch) should pass a `crap4csharp` run
(threshold ≤8.0) and a `mutate4csharp` run (no surviving mutants) before this ticket is considered done,
same as every prior ticket in this repo.
