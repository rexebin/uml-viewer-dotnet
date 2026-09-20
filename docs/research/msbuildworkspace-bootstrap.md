# Research: MSBuildWorkspace + Microsoft.Build.Locator bootstrap for .NET 10

- **Issue:** [rexebin/uml-viewer-dotnet#2](https://github.com/rexebin/uml-viewer-dotnet/issues/2) (child of wayfinder map #1)
- **Date:** 2026-09-20
- **Scope:** How to bootstrap `MSBuildWorkspace` in a .NET 10 console/library app to reliably open a single `.csproj` and get a `Compilation` / `SemanticModel`.

## 1. Minimal correct setup sequence

**NuGet packages** (as of research date; check NuGet.org before pinning in the actual project):

| Package | Latest version seen | Notes |
|---|---|---|
| [`Microsoft.Build.Locator`](https://www.nuget.org/packages/Microsoft.Build.Locator/) | 1.11.2 (published 2025-11-25) | Targets `net8.0` + `net462`; NuGet reports computed compatibility with `net9.0`/`net10.0`. |
| [`Microsoft.CodeAnalysis.Workspaces.MSBuild`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Workspaces.MSBuild) | 5.9.0 (published 2026-08-17) | Now explicitly targets **`net10.0`** and `net472`+ — good sign for .NET 10 support. |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | (match the Workspaces.MSBuild major/minor) | Needed to get the C# `SemanticModel`/`CSharpCompilation` surface via the workspace APIs. |

Sequence (per Microsoft's own docs and the `MSBuildLocator` maintainers' guidance):

1. Reference `Microsoft.Build.Locator` (do **not** exclude runtime assets — unlike raw `Microsoft.Build*` package refs, which should use `ExcludeAssets=runtime` if referenced directly).
2. At process startup, call `MSBuildLocator.RegisterDefaults()` (or `RegisterInstance`/`RegisterMSBuildPath` for a specific SDK) **before anything else touches MSBuild or Roslyn's MSBuild workspace types**.
3. Only after that call returns, construct `MSBuildWorkspace.Create()`.
4. `await workspace.OpenProjectAsync(csprojPath)` → `Project`.
5. `await project.GetCompilationAsync()` → `Compilation`.
6. For each `Document` in `project.Documents`: `var tree = await document.GetSyntaxTreeAsync(); var model = compilation.GetSemanticModel(tree);`.

Minimal code (pattern confirmed by Microsoft docs + the community reference gist by a Roslyn team member, Dustin Campbell):

```csharp
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

MSBuildLocator.RegisterDefaults(); // MUST run first, in its own method (see pitfall #1)

using var workspace = MSBuildWorkspace.Create();
workspace.WorkspaceFailed += (_, e) => Console.Error.WriteLine(e.Diagnostic.Message);

Project project = await workspace.OpenProjectAsync(csprojPath);

if (workspace.Diagnostics.Any(d => d.Kind == WorkspaceDiagnosticKind.Failure))
{
    // Don't silently proceed — see pitfall #3.
}

Compilation? compilation = await project.GetCompilationAsync();
foreach (Document doc in project.Documents)
{
    SyntaxTree? tree = await doc.GetSyntaxTreeAsync();
    if (tree is null) continue;
    SemanticModel model = compilation!.GetSemanticModel(tree);
    // walk model / tree here
}
```

Source: [Find and use a version of MSBuild](https://learn.microsoft.com/visualstudio/msbuild/find-and-use-msbuild-versions?view=visualstudio) (Microsoft Learn), [MSBuildLocator README](https://github.com/microsoft/MSBuildLocator) (GitHub), community reference gist [Using MSBuildWorkspace](https://gist.github.com/DustinCampbell/32cd69d04ea1c08a16ae5c4cd21dd3a3).

## 2. `RegisterDefaults()` ordering — the core gotcha

This is the single most important rule, stated directly by Microsoft's own MSBuild docs:

> "You can't reference any MSBuild types (from the `Microsoft.Build` namespace) in the method that calls MSBuildLocator." — [Find and use a version of MSBuild § Register instance before calling MSBuild](https://learn.microsoft.com/visualstudio/msbuild/find-and-use-msbuild-versions?view=visualstudio#register-instance-before-calling-msbuild)

Why: the JIT resolves all types referenced by a method (including ones only used later in that same method body) when the method is first entered, not lazily line-by-line. If `RegisterDefaults()` and a call into `Microsoft.Build.*` / `Microsoft.CodeAnalysis.MSBuild.*` types live in the *same* method, the JIT may try to resolve/load the MSBuild assemblies before `RegisterDefaults()` has had a chance to register the correct assembly-resolution hooks — causing a `FileNotFoundException`/`Could not load file or assembly 'Microsoft.Build...'` even though the call to `RegisterDefaults()` textually appears first.

**Practical rule:** put `MSBuildLocator.RegisterDefaults()` in `Main` (or the very first method executed), and put all `MSBuildWorkspace`/`Project`/`Compilation` usage in a *separate method* called afterward. Do not inline them in one function body.

```csharp
static async Task Main(string[] args)
{
    MSBuildLocator.RegisterDefaults();   // OK: this method touches nothing else MSBuild-related
    await RunAnalysis(args[0]);          // separate method — safe to use MSBuild/Roslyn types here
}

static async Task RunAnalysis(string csprojPath)
{
    using var workspace = MSBuildWorkspace.Create();
    var project = await workspace.OpenProjectAsync(csprojPath);
    // ...
}
```

This also means: don't let any earlier code path (DI container scanning assemblies, a static initializer, an unrelated `using` that touches `Microsoft.CodeAnalysis.MSBuild`) load first.

## 3. Common pitfalls

1. **JIT/assembly-load-order gotcha (see §2).** Symptom: `System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.Build.Locator, ... '` or similar for `Microsoft.Build.*`. Root cause confirmed in a real .NET 10 source-built SDK bug, [dotnet/roslyn#76797](https://github.com/dotnet/roslyn/issues/76797) — `dotnet-format` crashed with exactly this `FileNotFoundException` for `Microsoft.Build.Locator` because the locator assembly wasn't even deployed alongside the MSBuild build host in that SDK build. Fix pattern there and elsewhere is the same: make sure `Microsoft.Build.Locator` is registered before, and deployed alongside, any code path that touches `Microsoft.CodeAnalysis.MSBuild`.

2. **New .NET 10 SDK layout breaking *ambient* MSBuild resolution.** A concrete 2026 real-world case ([vchelaru/FlatRedBall#2218](https://github.com/vchelaru/FlatRedBall/issues/2218)) shows a project loader failing purely because `MSBuildLocator.RegisterDefaults()` was **not called at all** (commented out) and the app relied on ambient MSBuild resolution that used to work on older SDKs. Failure mode: `Missing SDK: The SDK 'Microsoft.NET.SDK.WorkloadAutoImportPropsLocator' specified could not be found`, pointing at a path like `...\dotnet\sdk\10.0.400\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.ImportWorkloads.props`. Confirmed **not version-specific** to .NET 10 SDK previews — the same repro happened pinned to 8.0.303 via `global.json` once WorkloadAutoImportPropsLocator became resolver-provided. **Takeaway: always call `RegisterDefaults()` explicitly; never rely on "it happened to resolve on this machine."**

3. **`dotnet restore` must have already run.** `MSBuildWorkspace` evaluates the MSBuild project file, which for SDK-style projects requires restored `project.assets.json`/generated `.props`/`.targets` (NuGet SDK resolution, implicit package references, etc.) to exist. If the target `.csproj` hasn't been restored, `OpenProjectAsync` will typically surface this as a `WorkspaceDiagnostic` (missing imports/targets) rather than a hard exception — reinforcing pitfall #4 below (check `Diagnostics`, don't assume success).

4. **Silently swallowed `Diagnostics`.** `OpenProjectAsync`/`OpenSolutionAsync` **do not throw** for most load problems (bad SDK resolution, missing imports, partially-failed project references) — they report them via `workspace.Diagnostics` (a `IEnumerable<WorkspaceDiagnostic>`) and/or the `workspace.WorkspaceFailed` event, while still returning a `Project`/`Compilation` object that may be missing source files, references, or have zero documents. If the calling code doesn't check `workspace.Diagnostics.Any(d => d.Kind == WorkspaceDiagnosticKind.Failure)`, it can silently produce an empty or wrong `Compilation` and misreport "success." Confirmed via Microsoft's own guidance and multiple `dotnet/roslyn` issues surfacing exactly this (e.g. [dotnet/roslyn#44586](https://github.com/dotnet/roslyn/issues/44586), [dotnet/roslyn#58592](https://github.com/dotnet/roslyn/issues/58592)). **Always subscribe to `WorkspaceFailed` and/or inspect `workspace.Diagnostics` after every open call, and fail loudly (throw or report) on `WorkspaceDiagnosticKind.Failure`.**

5. **SDK version mismatches.** The .NET Core build of `Microsoft.Build.Locator` can only see `dotnet.exe`/.NET SDK installations (not Visual Studio/Build Tools installs), and vice versa for the .NET Framework build. On a machine with multiple SDKs, `RegisterDefaults()` picks up the "current" SDK per the search order (`DOTNET_ROOT` → current process path if launched via `dotnet.exe` → `DOTNET_HOST_PATH` → `DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR` → `PATH`, stopping at the first `dotnet` executable found) — that may not be the SDK version the target `.csproj` expects. If a `global.json` pins a specific SDK, `RegisterDefaults()` respects the ambient `dotnet` resolution, so running the tool via `dotnet run`/`dotnet exec` from the correct working directory (so `global.json` is honored) matters. Source: [MSBuildLocator README](https://github.com/microsoft/MSBuildLocator).

## 4. `OpenProjectAsync` → `Compilation` → `SemanticModel`, concretely

- `MSBuildWorkspace.OpenProjectAsync(string projectFilePath, ...)` returns a `Microsoft.CodeAnalysis.Project` — an immutable snapshot of that project's documents, references, and compilation options, loaded by invoking MSBuild evaluation/design-time-build logic under the hood (uses a build host process — this is why locator/assembly-loading correctness matters so much; the RPC to `Microsoft.CodeAnalysis.MSBuild.RemoteBuildHost` is what's crashing in the pitfall #1 scenario).
- `Project.GetCompilationAsync()` returns a `Microsoft.CodeAnalysis.Compilation` (concretely a `CSharpCompilation` for a C# project) — "a representation of everything needed to compile a C#... program, which includes all the assembly references, compiler options, and source files." ([Work with semantics — Microsoft Learn](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/work-with-semantics))
- To get a `SemanticModel` for a specific file: get its `SyntaxTree` (via `Document.GetSyntaxTreeAsync()` on one of `project.Documents`, or `compilation.SyntaxTrees`), then call `compilation.GetSemanticModel(tree)`. The semantic model answers "what does this identifier refer to," resolves types/symbols, and surfaces diagnostics for that tree. ([Get started with semantic analysis — Microsoft Learn](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis))
- Compilations and syntax trees are immutable — same value semantics as the rest of the Workspace API (`Solution`/`Project`/`Document` are all immutable snapshots; mutation = "create a new one from the old one").

## 5. .NET 10 SDK compatibility — current status

- `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.9.0 (Aug 2026) **explicitly targets `net10.0`** — a strong signal that current Roslyn Workspaces are considered .NET-10-ready by the Roslyn team.
- `Microsoft.Build.Locator` 1.11.2 (Nov 2025) still lists `net8.0` as its primary TFM but NuGet reports computed compatibility forward to `net9.0`/`net10.0` (no separate `net10.0`-specific TFM as of this package's latest release) — works via version-agnostic API surface, not a .NET-10-specific build.
- Known live friction points found in GitHub issues, all from 2025–2026 as the .NET 10 SDK rolled out:
  - [dotnet/roslyn#76797](https://github.com/dotnet/roslyn/issues/76797) — .NET 10 source-built SDK's `dotnet-format` failing to load `Microsoft.Build.Locator` at runtime (packaging/deployment issue in that SDK build, not an API-level incompatibility).
  - [vchelaru/FlatRedBall#2218](https://github.com/vchelaru/FlatRedBall/issues/2218) — real-world app broke on .NET 10 SDK specifically because it **never called `RegisterDefaults()`** and previously relied on now-broken ambient resolution; root-caused to the newer SDK's `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator` being resolver-provided.
  - [dotnet/msbuild#12770](https://github.com/dotnet/msbuild/issues/12770) — "Broken Build: MSB4236 on a fresh .NET 10 project" — general SDK-resolution breakage reports around .NET 10 rollout (broader MSBuild issue, not Locator-specific).
- **Conclusion for this project:** no evidence of a fundamental incompatibility between current `Microsoft.Build.Locator`/`Microsoft.CodeAnalysis.Workspaces.MSBuild` and .NET 10 — but .NET 10's SDK layout changes have exposed apps that skipped explicit `RegisterDefaults()` calls or shipped stale/mismatched `Microsoft.Build.Locator` versions. Recommendation: pin to the latest `Microsoft.Build.Locator` (1.11.2+) and latest `Microsoft.CodeAnalysis.Workspaces.MSBuild` (5.9.0+) at implementation time, always call `RegisterDefaults()` explicitly (never rely on ambient resolution), and re-check `microsoft/MSBuildLocator` issues/releases for a `net10.0`-targeted release before implementation, since that repo is actively evolving around this exact rollout.

## Primary sources consulted

- Microsoft Learn: [Find and use a version of MSBuild](https://learn.microsoft.com/visualstudio/msbuild/find-and-use-msbuild-versions?view=visualstudio)
- Microsoft Learn: [MSBuildLocator.RegisterDefaults](https://learn.microsoft.com/dotnet/api/microsoft.build.locator.msbuildlocator.registerdefaults) / [RegisterInstance](https://learn.microsoft.com/dotnet/api/microsoft.build.locator.msbuildlocator.registerinstance) / [RegisterMSBuildPath](https://learn.microsoft.com/dotnet/api/microsoft.build.locator.msbuildlocator.registermsbuildpath)
- Microsoft Learn: [Work with a workspace](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/work-with-workspace), [Work with semantics](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/work-with-semantics), [Get started with semantic analysis](https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis)
- GitHub: [microsoft/MSBuildLocator](https://github.com/microsoft/MSBuildLocator) (README)
- GitHub: [dotnet/roslyn#76797](https://github.com/dotnet/roslyn/issues/76797), [#44586](https://github.com/dotnet/roslyn/issues/44586), [#58592](https://github.com/dotnet/roslyn/issues/58592)
- GitHub: [vchelaru/FlatRedBall#2218](https://github.com/vchelaru/FlatRedBall/issues/2218), [dotnet/msbuild#12770](https://github.com/dotnet/msbuild/issues/12770)
- NuGet.org: [Microsoft.Build.Locator](https://www.nuget.org/packages/Microsoft.Build.Locator/) (1.11.2), [Microsoft.CodeAnalysis.Workspaces.MSBuild](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Workspaces.MSBuild) (5.9.0)
- Community reference (Roslyn team member): [Using MSBuildWorkspace gist](https://gist.github.com/DustinCampbell/32cd69d04ea1c08a16ae5c4cd21dd3a3)
