namespace Schaumamal.Models.Platform;

public abstract class PlatformInformationProvider
{
    public const string RegularLocalApplicationFolder = "Schaumamal";
    public const string HiddenLocalApplicationFolder = ".schaumamal";

    public abstract string GetAppDirectoryPath();

    public static PlatformInformationProvider Current()
    {
        if (OperatingSystem.IsWindows()) return new WindowsInformationProvider();
        if (OperatingSystem.IsMacOS()) return new MacosInformationProvider();
        if (OperatingSystem.IsLinux()) return new LinuxInformationProvider();
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
