namespace UmlViewer.Core.Loading;

/// <summary>
/// Thrown when an MSBuildWorkspace-based project load fails: the project file doesn't
/// exist, MSBuild reports a load failure diagnostic, or the loaded project doesn't
/// produce a compilation.
/// </summary>
public sealed class ProjectLoadException : Exception
{
    public ProjectLoadException(string message)
        : base(message)
    {
    }

    public ProjectLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
