using Avalonia.Controls;

namespace Schaumamal.Models.Utils;

public static class SystemUtils
{
    public static bool CopyToClipboard(string text, TopLevel? topLevel)
    {
        try
        {
            var clipboard = topLevel?.Clipboard;
            if (clipboard != null)
            {
                clipboard.SetTextAsync(text).GetAwaiter().GetResult();
                return true;
            }
            return false;
        }
        catch { return false; }
    }
}
