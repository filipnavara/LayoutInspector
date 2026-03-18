using Schaumamal.Models.Platform;
using Schaumamal.Models.Repository;
using Schaumamal.ViewModels.Notifications;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Schaumamal.Models.Dumper;

public class Dumper
{
    private readonly TimeSpan _shortTimeout = TimeSpan.FromSeconds(12);
    private readonly TimeSpan _dumpTimeout = TimeSpan.FromSeconds(40);
    private const string RemoteDumpFilePath = "/sdcard/dump.xml";
    private static string RemoteScreenshotFilePath(string name) => $"/sdcard/{name}";
    private readonly string _appDirectoryPath;
    private readonly NicknameProvider _nicknameProvider;

    public Dumper(PlatformInformationProvider platformInformationProvider, NicknameProvider nicknameProvider)
    {
        _appDirectoryPath = platformInformationProvider.GetAppDirectoryPath();
        _nicknameProvider = nicknameProvider;
    }

    public async Task<DumpResult> DumpAsync(string? lastNickname, string tempDirectoryName, DumpProgressHandler progressHandler)
    {
        using var cts = new CancellationTokenSource(_dumpTimeout);
        try { return await DumpInternalAsync(lastNickname, tempDirectoryName, progressHandler, cts.Token); }
        catch (OperationCanceledException)
        {
            return new DumpResult.Error(new Notification("Dump Timeout",
                $"Dump process took too long (more than {_dumpTimeout.TotalSeconds}s). Try again.",
                NotificationSeverity.Error, ExitStrategy: new NotificationExitStrategy.Timeout(TimeSpan.FromSeconds(10))));
        }
    }

    private async Task<DumpResult> DumpInternalAsync(string? lastNickname, string tempDirectoryName,
        DumpProgressHandler progressHandler, CancellationToken ct)
    {
        progressHandler.ReportStartingDump();

        var adbCheck = await RunAdbAsync("devices", _shortTimeout, ct);
        if (adbCheck == null) return AdbError("ADB Session Error",
            "Could not establish ADB connection. Please check that ADB is installed and that the usual commands work.");

        if (!HasConnectedDevice(adbCheck)) return AdbError("No Device Connected",
            "Cannot find a device that is reachable through ADB. Connect to a device or start an emulator.", 8);

        var timeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nextNickname = _nicknameProvider.GetNext(lastNickname);

        await RunAdbAsync("root", _shortTimeout, ct);
        await Task.Delay(500, ct);

        var tempDir = Path.Combine(_appDirectoryPath, tempDirectoryName);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        progressHandler.ReportPreDumpSetupFinished();

        // Dump UI
        var dumpOut = await RunAdbAsync($"shell uiautomator dump --windows {RemoteDumpFilePath}", _shortTimeout, ct);
        if (dumpOut == null) return AdbError("XML Dump Timeout",
            "The XML dump ran into a timeout. Try executing \"adb shell uiautomator dump\" to debug, or try again.");

        // Pull dump file
        var dumpFileName = $"dump_{UniqueIdUtils.Hash()}.xml";
        var localDump = Path.Combine(tempDir, dumpFileName);
        var pull = await RunAdbAsync($"pull {RemoteDumpFilePath} \"{localDump}\"", _shortTimeout, ct);
        if (pull == null) return AdbError("Dump File Error", "Could not pull the dump file from the device. Try again.");

        await RunAdbAsync($"shell rm {RemoteDumpFilePath}", _shortTimeout, ct);
        progressHandler.ReportXmlDumpFinished();

        // API level
        var apiOut = await RunAdbAsync("shell getprop ro.build.version.sdk", _shortTimeout, ct);
        if (apiOut == null || !int.TryParse(apiOut.Trim(), out var api))
            return AdbError("API Level Error", "Could not retrieve device API level.");

        // SurfaceFlinger
        var flingerOut = await RunAdbAsync("shell dumpsys SurfaceFlinger --displays", _shortTimeout, ct);
        if (flingerOut == null) return AdbError("SurfaceFlinger Error", "Could not retrieve display IDs. Try again.");

        // Display IDs
        var displaysOut = await RunAdbAsync("shell cmd display get-displays", _shortTimeout, ct);
        if (displaysOut == null) return AdbError("Display IDs Error", "Could not retrieve display IDs. Try again.");

        var dumpXml = await File.ReadAllTextAsync(localDump, ct);
        var resolved = ResolveDisplays(api, flingerOut, displaysOut, dumpXml);
        if (resolved == null) return new DumpResult.Error(new Notification("Unsupported API",
            $"Devices with API {api} are not supported.", NotificationSeverity.Error));

        var valid = resolved.Where(d => d.ScreenshotId != null).ToList();
        progressHandler.SetExpectedScreenshotCount(Math.Max(valid.Count, 1));

        var displays = new List<DisplayInfo>();
        foreach (var rd in valid)
        {
            var scrName = $"scr_{UniqueIdUtils.Hash()}.png";
            var remotePath = RemoteScreenshotFilePath(scrName);
            var scrResult = await RunAdbAsync($"shell screencap -d {rd.ScreenshotId} {remotePath}", _shortTimeout, ct);
            if (scrResult == null) continue;

            var localScr = Path.Combine(tempDir, scrName);
            var pullScr = await RunAdbAsync($"pull {remotePath} \"{localScr}\"", _shortTimeout, ct);
            if (pullScr == null) continue;

            await RunAdbAsync($"shell rm {remotePath}", _shortTimeout, ct);
            progressHandler.ReportScreenshotTaken();
            displays.Add(new DisplayInfo(rd.DumpId, scrName));
        }

        return new DumpResult.Success(new DumpData("", nextNickname, timeMs, dumpFileName, displays));
    }

    private List<ResolvedDisplay>? ResolveDisplays(int api, string flingerOutput, string getDisplaysOutput, string dumpOutput)
    {
        var dumpDisplays = ExtractAll(dumpOutput, new Regex(@"<display id=""(\d+)"">", RegexOptions.Singleline),
            m => new DumpDisplay(m.Groups[1].Value));

        return api switch
        {
            35 or 36 => ResolveApi35(flingerOutput, getDisplaysOutput, dumpDisplays),
            33 or 34 => ResolveApi33(getDisplaysOutput, dumpDisplays),
            31 or 32 => dumpDisplays.Select(d => new ResolvedDisplay(d.DumpId, d.DumpId)).ToList(),
            _ => null
        };
    }

    private List<ResolvedDisplay> ResolveApi35(string flingerOutput, string getDisplaysOutput, List<DumpDisplay> dumpDisplays)
    {
        var flingerDisplays = ExtractAll(flingerOutput, new Regex(@"(\w*)\s?Display (\d+)\s", RegexOptions.Singleline),
            m => new FlingerDisplay(m.Groups[1].Value.Equals("Virtual", StringComparison.OrdinalIgnoreCase), m.Groups[2].Value));
        var cmdDisplays = ExtractAll(getDisplaysOutput,
            new Regex(@"Display id (\d+).*?type (\w+).*?uniqueId "".*?:(\d+)""", RegexOptions.Singleline),
            m => new CmdDisplay(m.Groups[1].Value, m.Groups[2].Value.Equals("VIRTUAL", StringComparison.OrdinalIgnoreCase), m.Groups[3].Value));

        var fV = flingerDisplays.Where(f => f.IsVirtual).ToList();
        var cV = cmdDisplays.Where(c => c.IsVirtual).ToList();
        var virtualMap = new Dictionary<string, string>();
        if (fV.Count == cV.Count)
            for (int i = 0; i < cV.Count; i++) virtualMap[cV[i].DumpId] = fV[i].ScreenshotId;

        var map = cmdDisplays.Where(c => !c.IsVirtual).ToDictionary(c => c.DumpId, c => c.ScreenshotId);
        foreach (var kv in virtualMap) map[kv.Key] = kv.Value;

        return dumpDisplays.Select(d => new ResolvedDisplay(map.GetValueOrDefault(d.DumpId), d.DumpId)).ToList();
    }

    private List<ResolvedDisplay> ResolveApi33(string getDisplaysOutput, List<DumpDisplay> dumpDisplays)
    {
        var cmdDisplays = ExtractAll(getDisplaysOutput,
            new Regex(@"Display id (\d+).*?type (\w+).*?uniqueId "".*?:(\d+)""", RegexOptions.Singleline),
            m => new CmdDisplay(m.Groups[1].Value, m.Groups[2].Value.Equals("VIRTUAL", StringComparison.OrdinalIgnoreCase), m.Groups[3].Value));
        var map = cmdDisplays.Where(c => !c.IsVirtual).ToDictionary(c => c.DumpId, c => c.ScreenshotId);
        return dumpDisplays.Select(d => new ResolvedDisplay(map.GetValueOrDefault(d.DumpId), d.DumpId)).ToList();
    }

    private static List<T> ExtractAll<T>(string input, Regex pattern, Func<Match, T> transform)
        => pattern.Matches(input).Select(transform).ToList();

    private static bool HasConnectedDevice(string output)
        => output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Any(l => l.Contains("device") && !l.StartsWith("List"));

    private static async Task<string?> RunAdbAsync(string arguments, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            using var tCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            tCts.CancelAfter(timeout);
            await process.WaitForExitAsync(tCts.Token);
            return output;
        }
        catch { return null; }
    }

    private static DumpResult.Error AdbError(string title, string desc, int timeoutSec = 5) => new(new Notification(
        title, desc, NotificationSeverity.Error,
        ExitStrategy: new NotificationExitStrategy.Timeout(TimeSpan.FromSeconds(timeoutSec))));
}
