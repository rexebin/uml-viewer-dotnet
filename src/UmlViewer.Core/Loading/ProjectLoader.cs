using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace UmlViewer.Core.Loading;

/// <summary>
/// Loads a single <c>.csproj</c> via <see cref="MSBuildWorkspace"/> and produces the
/// Roslyn <see cref="Compilation"/> that structural extraction consumes.
/// </summary>
public sealed class ProjectLoader
{
    public async Task<LoadedProject> LoadAsync(string csprojPath)
    {
        if (!File.Exists(csprojPath))
        {
            throw new ProjectLoadException($"Project file not found: '{csprojPath}'.");
        }

        MsBuildEnvironment.EnsureRegistered();
        return await OpenAsync(csprojPath);
    }

    private static async Task<LoadedProject> OpenAsync(string csprojPath)
    {
        using var workspace = MSBuildWorkspace.Create();

        var failureMessages = new List<string>();
        using var _ = workspace.RegisterWorkspaceFailedHandler(e =>
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                failureMessages.Add(e.Diagnostic.Message);
            }
        });

        Project project;
        try
        {
            project = await workspace.OpenProjectAsync(csprojPath);
        }
        catch (Exception ex) when (ex is not ProjectLoadException)
        {
            throw new ProjectLoadException($"Failed to open project '{csprojPath}'.", ex);
        }

        if (failureMessages.Count > 0)
        {
            throw new ProjectLoadException(
                $"Failed to load project '{csprojPath}': {string.Join("; ", failureMessages)}");
        }

        var compilation = await project.GetCompilationAsync();
        if (compilation is null)
        {
            throw new ProjectLoadException(
                $"Project '{csprojPath}' does not support compilation (not a C#/VB project?).");
        }

        return new LoadedProject(project, compilation);
    }
}
