using System;
using System.IO;

namespace Snapr.Services;

/// <summary>Appends unexpected errors to %AppData%\Snapr\snapr.log, keeping the file small.</summary>
public static class ErrorLog
{
    private const long MaxSize = 512 * 1024;
    private static readonly object Gate = new();

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Snapr", "snapr.log");

    public static void Write(Exception exception, string source)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

                // Keep one previous log around instead of growing forever.
                var info = new FileInfo(FilePath);
                if (info.Exists && info.Length > MaxSize)
                    File.Move(FilePath, FilePath + ".old", overwrite: true);

                File.AppendAllText(FilePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never cause a second failure.
        }
    }
}
