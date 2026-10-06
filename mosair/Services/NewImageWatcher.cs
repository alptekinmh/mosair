using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace mosair.Services
{
    // Watches the Downloads and Desktop folders (not their subfolders) for new JPEG and PNG files, so the window
    // can offer to open them. A browser first writes "x.jpg.crdownload" / "x.jpg.part" and renames it when the
    // download ends, so renames count as well; the event fires once the file can be read and its size is steady.
    public sealed class NewImageWatcher : IDisposable
    {
        public enum Place { Downloads, Desktop }

        // Raised on a worker thread with the file's full path.
        public event Action<string, Place>? ImageArrived;

        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly ConcurrentDictionary<string, DateTime> _seen = new(StringComparer.OrdinalIgnoreCase);

        // Files mosair writes itself (export, screenshot) are not offered back.
        private static readonly ConcurrentDictionary<string, DateTime> Ignored = new(StringComparer.OrdinalIgnoreCase);

        public static void Ignore(string path)
        {
            try { Ignored[Path.GetFullPath(path)] = DateTime.UtcNow; } catch (Exception) { }
        }

        public void Start()
        {
            if (_watchers.Count > 0) return;
            Add(DownloadsFolder(), Place.Downloads);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.Equals(desktop, DownloadsFolder(), StringComparison.OrdinalIgnoreCase))
                Add(desktop, Place.Desktop);
        }

        public void Stop()
        {
            foreach (var w in _watchers) w.Dispose();
            _watchers.Clear();
        }

        public void Dispose() => Stop();

        private void Add(string dir, Place place)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            try
            {
                var w = new FileSystemWatcher(dir)
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName
                };
                w.Created += (_, e) => Seen(e.FullPath, place);
                w.Renamed += (_, e) => Seen(e.FullPath, place);
                w.EnableRaisingEvents = true;
                _watchers.Add(w);
            }
            catch (Exception)
            {
                // A folder that cannot be watched (permissions, network drive) is skipped.
            }
        }

        public static bool IsImage(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png";
        }

        private void Seen(string path, Place place)
        {
            if (!IsImage(path)) return;
            // Each file is offered once while mosair runs: the same file is often reported several times (created,
            // renamed, rewritten by the browser or by a copy). Only a file that never became readable may come again.
            if (!_seen.TryAdd(path, DateTime.UtcNow)) return;
            _ = Task.Run(async () =>
            {
                if (!await WaitUntilReadyAsync(path)) { _seen.TryRemove(path, out _); return; }
                if (Ignored.TryGetValue(path, out var mine) && DateTime.UtcNow - mine < TimeSpan.FromMinutes(2)) return;
                ImageArrived?.Invoke(path, place);
            });
        }

        // Up to 30 s for the writer to finish: the file opens for reading and its size is the same twice.
        private static async Task<bool> WaitUntilReadyAsync(string path)
        {
            long lastSize = -1;
            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(500);
                try
                {
                    if (!File.Exists(path)) return false;
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    long size = fs.Length;
                    if (size > 0 && size == lastSize) return true;
                    lastSize = size;
                }
                catch (IOException)
                {
                    // Still being written.
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            }
            return false;
        }

        // The user's Downloads folder (also when it was moved to another drive on Windows).
        public static string DownloadsFolder()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var id = new Guid("374DE290-123F-4565-9164-39C4925E467B");   // FOLDERID_Downloads
                    if (SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out IntPtr p) == 0)
                    {
                        string? path = Marshal.PtrToStringUni(p);
                        Marshal.FreeCoTaskMem(p);
                        if (!string.IsNullOrEmpty(path)) return path;
                    }
                }
                catch (Exception) { }
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetKnownFolderPath(ref Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
    }
}
