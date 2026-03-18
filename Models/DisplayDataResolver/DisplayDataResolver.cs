using Schaumamal.Models.Parser;
using Schaumamal.Models.Platform;
using Schaumamal.Models.Repository;

namespace Schaumamal.Models.DisplayDataResolver;

public class DisplayDataResolver
{
    private readonly string _appDirectoryPath;
    private readonly XmlParser _xmlParser;

    public DisplayDataResolver(PlatformInformationProvider platformInformationProvider, XmlParser xmlParser)
    {
        _appDirectoryPath = platformInformationProvider.GetAppDirectoryPath();
        _xmlParser = xmlParser;
    }

    public List<DisplayData> Resolve(string dumpsDirectoryName, DumpData selectedDump)
    {
        var dumpDir = Path.Combine(_appDirectoryPath, dumpsDirectoryName, selectedDump.DirectoryName);
        var dumpFilePath = Path.Combine(dumpDir, selectedDump.XmlTreeFileName);
        var displayNodes = _xmlParser.ParseSystem(dumpFilePath);

        return selectedDump.Displays.Select(display => new DisplayData(
            Path.Combine(dumpDir, display.ScreenshotFileName),
            displayNodes.First(dn => dn.Id == display.Id)
        )).ToList();
    }

    public Dictionary<DumpData, string> ResolveThumbnails(string dumpsDirectoryName, List<DumpData> dumps)
    {
        return dumps.ToDictionary(d => d,
            d => Path.Combine(_appDirectoryPath, dumpsDirectoryName, d.DirectoryName, d.Displays[0].ScreenshotFileName));
    }
}
