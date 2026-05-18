#if BROWSER_WEB_HOST
using System.Runtime.InteropServices.JavaScript;

namespace BgfXna.Samples;

internal static partial class BrowserDiagnostics
{
    [JSImport("globalThis.BgfXna_showManagedError")]
    internal static partial void ShowManagedError(string message);
}
#endif
