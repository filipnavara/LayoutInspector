using System.Runtime.InteropServices;

namespace Schaumamal.Shared;

public static class AppValues
{
    public static readonly int ScrollZoomFactor = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 10 : 50;
    public const float KeyboardZoomFactor = 1.1f;
    public const float MinScreenshotScale = 0.1f;
    public const float MaxScreenshotScale = 20f;
    public const float ScreenshotLayerWidthPercentage = 0.6f;
}
