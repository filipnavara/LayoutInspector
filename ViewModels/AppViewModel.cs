using CommunityToolkit.Mvvm.ComponentModel;
using Schaumamal.Models;
using Schaumamal.Models.DisplayDataResolver;
using Schaumamal.Models.Dumper;
using Schaumamal.Models.Parser;
using Schaumamal.Models.Repository;
using Schaumamal.ViewModels.Notifications;

namespace Schaumamal.ViewModels;

public partial class AppViewModel : ObservableObject
{
    public NotificationManager NotificationManager { get; }
    private readonly Dumper _dumper;
    private readonly AppRepository _appRepository;
    private readonly DisplayDataResolver _displayDataResolver;

    [ObservableProperty] private InspectorState _state = InspectorState.Empty;
    [ObservableProperty] private Content _content = Content.DefaultEmpty;
    [ObservableProperty] private Settings _settings = Settings.DefaultEmpty;
    [ObservableProperty] private DumpData _selectedDump = DumpData.Empty;
    [ObservableProperty] private int _displayIndex;
    [ObservableProperty] private int _displayCount;
    [ObservableProperty] private DisplayData _selectedDisplayData = DisplayData.Empty;
    [ObservableProperty] private bool _isNodeSelected;
    [ObservableProperty] private GenericNode _selectedNode = GenericNode.Empty;
    [ObservableProperty] private float _dumpProgress;
    [ObservableProperty] private string _dumpProgressText = "";

    private List<DisplayData> _displayDataList = new();
    private Dictionary<DumpData, string> _resolvedDumpThumbnails = new();
    public Dictionary<DumpData, string> ResolvedDumpThumbnails => _resolvedDumpThumbnails;

    public AppViewModel(NotificationManager notificationManager, Dumper dumper,
        AppRepository appRepository, DisplayDataResolver displayDataResolver)
    {
        NotificationManager = notificationManager;
        _dumper = dumper;
        _appRepository = appRepository;
        _displayDataResolver = displayDataResolver;
        SetupEnvironment();
    }

    private void SetupEnvironment()
    {
        var errors = new List<string>();
        try { _appRepository.CreateAppDirectory(); }
        catch { errors.Add("Could not create folder structure."); NotifyErrors(errors); return; }

        if (_appRepository.ExistsContentJson())
        {
            try
            {
                Content = _appRepository.ReadContentJson();
                if (Content.Dumps.Count > 0)
                {
                    SelectedDump = Content.Dumps[0];
                    State = InspectorState.Populated;
                    RefreshDisplayData();
                    RefreshThumbnails();
                }
            }
            catch { errors.Add("Could not read existing \"content.json\" file."); }
        }
        else
        {
            try { _appRepository.WriteContentJson(Content.DefaultEmpty); }
            catch { errors.Add("Could not create \"content.json\" file."); }
        }

        try { _appRepository.CreateContentDirectories(Content); }
        catch { errors.Add("Could not create content directories."); }

        if (_appRepository.ExistsSettingsJson())
        {
            try { Settings = _appRepository.ReadSettingsJson(); }
            catch { errors.Add("Could not read existing \"settings.json\" file."); }
        }
        else
        {
            try { _appRepository.WriteSettingsJson(Settings.DefaultEmpty); }
            catch { errors.Add("Could not create \"settings.json\" file."); }
        }

        NotifyErrors(errors);
    }

    private void NotifyErrors(List<string> errors)
    {
        foreach (var err in errors)
            NotificationManager.Notify(new Notification("Startup errors",
                $"{err} Consider removing the \"{_appRepository.AppDirectoryPath}\" directory.",
                NotificationSeverity.Error));
    }

    private void RefreshDisplayData()
    {
        if (SelectedDump == DumpData.Empty || SelectedDump.Displays.Count == 0) return;
        try
        {
            _displayDataList = _displayDataResolver.Resolve(Content.DumpsDirectoryName, SelectedDump);
            DisplayCount = _displayDataList.Count;
            if (_displayDataList.Count > 0 && DisplayIndex < _displayDataList.Count)
                SelectedDisplayData = _displayDataList[DisplayIndex];
        }
        catch { /* silently fail */ }
    }

    private void RefreshThumbnails()
    {
        try
        {
            _resolvedDumpThumbnails = _displayDataResolver.ResolveThumbnails(Content.DumpsDirectoryName, Content.Dumps);
            OnPropertyChanged(nameof(ResolvedDumpThumbnails));
        }
        catch { /* silently fail */ }
    }

    partial void OnSelectedDumpChanged(DumpData value) => RefreshDisplayData();

    partial void OnDisplayIndexChanged(int value)
    {
        if (_displayDataList.Count > 0 && value < _displayDataList.Count)
            SelectedDisplayData = _displayDataList[value];
    }

    public async Task ExtractAsync(DumpProgressHandler progressHandler)
    {
        var previousState = State;
        State = InspectorState.Waiting;

        var result = await _dumper.DumpAsync(
            Content.Dumps.Count > 0 ? Content.Dumps[0].Nickname : null,
            Content.TempDirectoryName, progressHandler);

        if (result is DumpResult.Error err) { NotificationManager.Notify(err.Notification); State = previousState; return; }
        var newDump = ((DumpResult.Success)result).Dump;

        var regResult = _appRepository.RegisterNewDump(newDump, Content, Settings.MaxDumps);
        if (regResult is DumpRegisterResult.Error regErr) { NotificationManager.Notify(regErr.Notification); State = previousState; return; }
        var newContent = ((DumpRegisterResult.Success)regResult).Content;

        IsNodeSelected = false;
        SelectedNode = GenericNode.Empty;
        DisplayIndex = 0;
        _appRepository.WriteContentJson(newContent);
        Content = newContent;
        SelectedDump = Content.Dumps[0];
        RefreshThumbnails();
        State = InspectorState.Populated;
    }

    public void SelectNode(GenericNode node) { SelectedNode = node; IsNodeSelected = true; }

    public void SwitchDisplay(Direction direction)
    {
        var changed = false;
        switch (direction)
        {
            case Direction.Previous when DisplayIndex > 0: DisplayIndex--; changed = true; break;
            case Direction.Next when DisplayIndex < DisplayCount - 1: DisplayIndex++; changed = true; break;
        }
        if (changed) { IsNodeSelected = false; SelectedNode = GenericNode.Empty; }
    }

    public void SelectDump(DumpData dump)
    {
        if (dump == SelectedDump) return;
        SelectedDump = dump;
        DisplayIndex = 0;
        IsNodeSelected = false;
        SelectedNode = GenericNode.Empty;
    }

    public void Cleanup() { }
}
