using System.Runtime.InteropServices;

namespace Core;

public record EnvironmentReport(
    string OsDescription,
    string FrameworkDescription,
    string ProcessArchitecture,
    string DetectedRid,
    string ReportedRid,
    string BaseDirectory,
    string BuildNote
);

public static class EnvironmentInfo
{
#if NET10_0_OR_GREATER
    private const string BuildNote = "збірка під net10.0";
#else
    private const string BuildNote = "збірка під net8.0";
#endif

    public static EnvironmentReport Collect()
    {
        string architecture = RuntimeInformation.ProcessArchitecture.ToString();

        string detectedRid = architecture switch
        {
            "Arm64" when OperatingSystem.IsMacOS() => "osx-arm64",
            "X64" when OperatingSystem.IsMacOS() => "osx-x64",
            "Arm64" when OperatingSystem.IsLinux() => "linux-arm64",
            "X64" when OperatingSystem.IsLinux() => "linux-x64",
            "Arm64" when OperatingSystem.IsWindows() => "win-arm64",
            "X64" when OperatingSystem.IsWindows() => "win-x64",
            _ => "unknown"
        };

        string reportedRid = RuntimeInformation.RuntimeIdentifier;

        return new EnvironmentReport(
            RuntimeInformation.OSDescription,
            RuntimeInformation.FrameworkDescription,
            architecture,
            detectedRid,
            reportedRid,
            AppContext.BaseDirectory,
            BuildNote
        );
    }
}

