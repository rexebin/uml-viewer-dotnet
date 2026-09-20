using Microsoft.Build.Locator;

namespace UmlViewer.Core.Loading;

/// <summary>
/// Registers the ambient MSBuild toolset with <see cref="MSBuildLocator"/>.
/// </summary>
/// <remarks>
/// <see cref="EnsureRegistered"/> must be called before any code touches
/// <c>Microsoft.Build.*</c> or <c>Microsoft.CodeAnalysis.MSBuild.*</c> types, and this
/// method itself must not reference those types: the JIT resolves every type a method
/// touches when the method is first entered, so registering and using MSBuild in the
/// same method can try to load MSBuild assemblies before the locator has hooked assembly
/// resolution. Keep this method's body limited to <see cref="MSBuildLocator"/> members.
/// </remarks>
public static class MsBuildEnvironment
{
    public static void EnsureRegistered()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}
