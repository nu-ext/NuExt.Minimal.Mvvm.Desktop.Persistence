using System;
using System.Reflection;

namespace Minimal.Mvvm.Persistence;

/// <summary>
/// Key that separates application settings sets. Patch releases share the same set,
/// so only the major and minor components of the application version are significant.
/// </summary>
public static class AppVersionKey
{
    /// <summary>
    /// Settings set key of the currently running application version.
    /// </summary>
    public static string Current => Format(AssemblyInfo.Current.Version);

    /// <summary>
    /// Formats the specified version as a settings set key.
    /// </summary>
    /// <param name="version">The application version.</param>
    /// <returns>The major and minor components, or <c>0.0</c> when the version is unknown.</returns>
    public static string Format(Version? version)
    {
        return version is null ? "0.0" : $"{version.Major}.{version.Minor}";
    }
}
