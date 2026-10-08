using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Snapr.Services;

public static class ClipboardHelper
{
    /// <summary>Puts an image on the clipboard, retrying briefly when another app holds it open.</summary>
    public static bool TrySetImage(BitmapSource image)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetImage(image);
                return true;
            }
            catch (COMException)
            {
                Thread.Sleep(50);
            }
        }
        return false;
    }
}
