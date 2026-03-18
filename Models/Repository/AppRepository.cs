using System.Text.Json;
using Schaumamal.Models.Platform;
using Schaumamal.ViewModels.Notifications;

namespace Schaumamal.Models.Repository;

public class AppRepository
{
    public string AppDirectoryPath { get; }
    private readonly string _contentJsonFilePath;
    private readonly string _settingsFilePath;
    private static readonly AppRepositoryJsonContext JsonContext = AppRepositoryJsonContext.Default;

    public AppRepository(PlatformInformationProvider platformInformationProvider)
    {
        AppDirectoryPath = platformInformationProvider.GetAppDirectoryPath();
        _contentJsonFilePath = Path.Combine(AppDirectoryPath, "content.json");
        _settingsFilePath = Path.Combine(AppDirectoryPath, "settings.json");
    }

    public bool ExistsContentJson() => File.Exists(_contentJsonFilePath);
    public Content ReadContentJson() => JsonSerializer.Deserialize(File.ReadAllText(_contentJsonFilePath), JsonContext.Content)!;
    public void WriteContentJson(Content content) => File.WriteAllText(_contentJsonFilePath, JsonSerializer.Serialize(content, JsonContext.Content));
    public bool ExistsSettingsJson() => File.Exists(_settingsFilePath);
    public Settings ReadSettingsJson() => JsonSerializer.Deserialize(File.ReadAllText(_settingsFilePath), JsonContext.Settings)!;
    public void WriteSettingsJson(Settings settings) => File.WriteAllText(_settingsFilePath, JsonSerializer.Serialize(settings, JsonContext.Settings));
    public void CreateAppDirectory() => Directory.CreateDirectory(AppDirectoryPath);

    public void CreateContentDirectories(Content content)
    {
        Directory.CreateDirectory(Path.Combine(AppDirectoryPath, content.TempDirectoryName));
        Directory.CreateDirectory(Path.Combine(AppDirectoryPath, content.DumpsDirectoryName));
    }

    public DumpRegisterResult RegisterNewDump(DumpData dump, Content content, int maxDumps)
    {
        var tempDirectoryPath = Path.Combine(AppDirectoryPath, content.TempDirectoryName);
        var currentDumpCount = content.Dumps.Count;
        var maxDumpsReached = currentDumpCount >= maxDumps;

        var destinationDirectoryPath = maxDumpsReached
            ? Path.Combine(AppDirectoryPath, content.DumpsDirectoryName, content.Dumps[^1].DirectoryName)
            : Path.Combine(AppDirectoryPath, content.DumpsDirectoryName, (currentDumpCount + 1).ToString());

        try { MoveContents(tempDirectoryPath, destinationDirectoryPath); }
        catch
        {
            return new DumpRegisterResult.Error(new Notification(
                Title: "Register Error", Description: "Failed to register dump. Try again.",
                Severity: NotificationSeverity.Error,
                ExitStrategy: new NotificationExitStrategy.Timeout(TimeSpan.FromSeconds(5))));
        }

        var updatedDump = dump with { DirectoryName = Path.GetFileName(destinationDirectoryPath) };
        var updatedDumps = new List<DumpData> { updatedDump };
        updatedDumps.AddRange(maxDumpsReached ? content.Dumps.Take(content.Dumps.Count - 1) : content.Dumps);
        return new DumpRegisterResult.Success(content with { Dumps = updatedDumps });
    }

    private static void MoveContents(string source, string destination)
    {
        if (Directory.Exists(destination)) Directory.Delete(destination, true);
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Move(file, Path.Combine(destination, Path.GetFileName(file)));
        if (Directory.Exists(source)) Directory.Delete(source, true);
        Directory.CreateDirectory(source);
    }
}
