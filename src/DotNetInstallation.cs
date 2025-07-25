using System.Runtime.Versioning;
using Microsoft.Win32;
using NuGet.Versioning;

namespace UnityRoslynUpdater;

public sealed class DotNetInstallation
{
    public string Location { get; }

    public static DotNetInstallation Current { get; } = GetCurrentInstallation();

    public DotNetInstallation(string location)
    {
        ArgumentNullException.ThrowIfNull(location);
        Location = location;
    }

    public IEnumerable<DotNetSdk> EnumerateSDKs()
    {
        foreach (string directory in Directory.EnumerateDirectories(Path.Combine(Location, "sdk")))
        {
            var directoryName = Path.GetFileName(directory);

            if (!SemanticVersion.TryParse(directoryName, out SemanticVersion version))
                continue;

            yield return new DotNetSdk(Path.GetFullPath(directory), version);
        }
    }

    private static DotNetInstallation GetCurrentInstallation()
    {
        var dotnetExe = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        var location = Environment.GetEnvironmentVariable("DOTNET_ROOT");

        if (string.IsNullOrEmpty(location))
        {
            var systemPath = Environment.GetEnvironmentVariable("PATH")!.Split(Path.PathSeparator);
            foreach (var path in systemPath)
            {
                var dotnetPath = Path.Combine(path, dotnetExe);
                if (!IsValidDotnetRoot(dotnetPath, dotnetExe)) continue;
                location = Path.GetDirectoryName(dotnetPath)!;
                break;
            }
        }

        if (OperatingSystem.IsWindows()
            && !IsValidDotnetRoot(location, dotnetExe))
            location = GetWindowsRegistryDotNetInstallLocation();

        if (!IsValidDotnetRoot(location, dotnetExe))
            location = GetDefaultInstallationLocation();

        if (string.IsNullOrEmpty(location))
            throw new PlatformNotSupportedException("Could not find a valid .NET installation.");

        return new DotNetInstallation(location);
    }

    private static bool IsValidDotnetRoot(string? location, string dotnetExe)
    {
        return !string.IsNullOrEmpty(location)
               && File.Exists(Path.Combine(location, dotnetExe))
               && Directory.Exists(Path.Combine(location, "sdk"));
    }

    private static string GetDefaultInstallationLocation()
    {
        if (OperatingSystem.IsWindows() && Environment.Is64BitOperatingSystem)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            return "/usr/local/share/dotnet";

        throw new PlatformNotSupportedException();
    }

    [SupportedOSPlatform("windows")]
    private static string? GetWindowsRegistryDotNetInstallLocation()
    {
        return Registry.LocalMachine.GetValue(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\InstallLocation", null) as string;
    }
}