using Microsoft.CodeAnalysis;

namespace UmlViewer.Core.Loading;

/// <summary>
/// A successfully loaded MSBuild project paired with the Roslyn compilation produced
/// from it, ready for structural extraction to walk.
/// </summary>
public sealed record LoadedProject(Project Project, Compilation Compilation);
