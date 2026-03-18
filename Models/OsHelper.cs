using System.Runtime.InteropServices;

namespace Schaumamal.Models;

public enum OsType { Windows, MacOS, Linux }

public static class OsHelper
{
    public static readonly OsType Current = GetCurrent();

    private static OsType GetCurrent()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return OsType.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return OsType.MacOS;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return OsType.Linux;
        throw new PlatformNotSupportedException();
    }

    public static T On<T>(T win, T mac, T lin) => Current switch
    {
        OsType.Windows => win,
        OsType.MacOS => mac,
        OsType.Linux => lin,
        _ => throw new PlatformNotSupportedException()
    };
}
