using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using Schaumamal.Models.Platform;
using Schaumamal.Models.Repository;
using Schaumamal.ViewModels.Notifications;
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

        AdbClient client;
        IEnumerable<DeviceData> devices;
        try
        {
            var server = new AdbServer();
            if (!server.GetStatus().IsRunning)
                server.StartServer("adb", false);

            client = new AdbClient();
            devices = await client.GetDevicesAsync(ct);
        }
        catch
        {
            return AdbError("ADB Session Error",
                "Could not establish ADB connection. Please check that ADB is installed and that the usual commands work.");
        }

        var device = devices.FirstOrDefault(d => d.State == DeviceState.Online);
        if (device == null) return AdbError("No Device Connected",
            "Cannot find a device that is reachable through ADB. Connect to a device or start an emulator.", 8);

        var timeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nextNickname = _nicknameProvider.GetNext(lastNickname);

        try { await client.RootAsync(device, ct); } catch { /* not all devices support root */ }
        await Task.Delay(500, ct);

        var tempDir = Path.Combine(_appDirectoryPath, tempDirectoryName);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        progressHandler.ReportPreDumpSetupFinished();

        // Dump UI
        var dumpOut = await RunShellAsync(client, device, $"uiautomator dump --windows {RemoteDumpFilePath}", _shortTimeout, ct);
        if (dumpOut == null) return AdbError("XML Dump Timeout",
            "The XML dump ran into a timeout. Try executing \"adb shell uiautomator dump\" to debug, or try again.");

        // Pull dump file
        var dumpFileName = $"dump_{UniqueIdUtils.Hash()}.xml";
        var localDump = Path.Combine(tempDir, dumpFileName);
        if (!await PullFileAsync(client, device, RemoteDumpFilePath, localDump, ct))
            return AdbError("Dump File Error", "Could not pull the dump file from the device. Try again.");

        await RunShellAsync(client, device, $"rm {RemoteDumpFilePath}", _shortTimeout, ct);
        progressHandler.ReportXmlDumpFinished();

        // API level
        var apiOut = await RunShellAsync(client, device, "getprop ro.build.version.sdk", _shortTimeout, ct);
        if (apiOut == null || !int.TryParse(apiOut.Trim(), out var api))
            return AdbError("API Level Error", "Could not retrieve device API level.");

        // SurfaceFlinger
        var flingerOut = await RunShellAsync(client, device, "dumpsys SurfaceFlinger --displays", _shortTimeout, ct);
        if (flingerOut == null) return AdbError("SurfaceFlinger Error", "Could not retrieve display IDs. Try again.");

        // Display IDs
        var displaysOut = await RunShellAsync(client, device, "cmd display get-displays", _shortTimeout, ct);
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
            var scrResult = await RunShellAsync(client, device, $"screencap -d {rd.ScreenshotId} {remotePath}", _shortTimeout, ct);
            if (scrResult == null) continue;

            var localScr = Path.Combine(tempDir, scrName);
            if (!await PullFileAsync(client, device, remotePath, localScr, ct)) continue;

            await RunShellAsync(client, device, $"rm {remotePath}", _shortTimeout, ct);
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

    private static async Task<string?> RunShellAsync(AdbClient client, DeviceData device, string command,
        TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            using var tCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            tCts.CancelAfter(timeout);
            var receiver = new ConsoleOutputReceiver();
            await client.ExecuteRemoteCommandAsync(command, device, receiver, tCts.Token);
            return receiver.ToString();
        }
        catch { return null; }
    }

    private static async Task<bool> PullFileAsync(AdbClient client, DeviceData device, string remotePath,
        string localPath, CancellationToken ct)
    {
        try
        {
            using var syncService = new SyncService(client, device);
            using var stream = File.Create(localPath);
            await syncService.PullAsync(remotePath, stream, null, false, ct);
            return true;
        }
        catch { return false; }
    }

    private static DumpResult.Error AdbError(string title, string desc, int timeoutSec = 5) => new(new Notification(
        title, desc, NotificationSeverity.Error,
        ExitStrategy: new NotificationExitStrategy.Timeout(TimeSpan.FromSeconds(timeoutSec))));
}
