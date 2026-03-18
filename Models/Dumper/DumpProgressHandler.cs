namespace Schaumamal.Models.Dumper;

public class DumpProgressHandler
{
    private readonly Action<float, string> _onProgress;
    private float _currentProgress;
    private int _screenshotCount;
    private int _takenScreenshots;

    public DumpProgressHandler(Action<float, string> onProgress) => _onProgress = onProgress;

    public void ReportStartingDump() => _onProgress(0f, "Dumping the layout");

    public void ReportPreDumpSetupFinished()
    {
        _currentProgress = Math.Min(_currentProgress + 0.15f, 1.0f);
        _onProgress(_currentProgress, "Dumping the layout");
    }

    public void ReportXmlDumpFinished()
    {
        _currentProgress = Math.Min(_currentProgress + 0.25f, 1.0f);
        _onProgress(_currentProgress, "Taking screenshot 1");
    }

    public void SetExpectedScreenshotCount(int count) => _screenshotCount = count;

    public void ReportScreenshotTaken()
    {
        _takenScreenshots++;
        _currentProgress = Math.Min(_currentProgress + 0.6f / _screenshotCount, 1.0f);
        _onProgress(_currentProgress,
            _takenScreenshots < _screenshotCount ? $"Taking screenshot {_takenScreenshots + 1}" : "Dump finished");
    }
}
