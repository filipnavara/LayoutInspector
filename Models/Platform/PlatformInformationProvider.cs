using System.Runtime.InteropServices;

namespace Schaumamal.Models.Platform;

public abstract class PlatformInformationProvider
{
    public const string RegularLocalApplicationFolder = "Schaumamal";
    public const string HiddenLocalApplicationFolder = ".schaumamal";

    public abstract string GetAppDirectoryPath();

    public static PlatformInformationProvider Current()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return new WindowsInformationProvider();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return new MacosInformationProvider();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return new LinuxInformationProvider();
        throw new PlatformNotSupportedException();
    }
}

public class WindowsInformationProvider : PlatformInformationProvider
{
    public override string GetAppDirectoryPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), RegularLocalApplicationFolder);
}

public class MacosInformationProvider : PlatformInformationProvider
{
    public override string GetAppDirectoryPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", RegularLocalApplicationFolder);
}

public class LinuxInformationProvider : PlatformInformationProvider
{
    public override string GetAppDirectoryPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), HiddenLocalApplicationFolder);
}
