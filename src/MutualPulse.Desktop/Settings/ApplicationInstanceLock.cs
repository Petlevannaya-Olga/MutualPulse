using System.IO;

namespace MutualPulse.Desktop.Settings;

/// <summary>
/// Prevents two MutualPulse processes (including other Windows sessions of the same
/// user) from editing the same local MutualPulse settings and browser profile directory.
/// The OS releases the file handle after a crash; the empty file may remain.
/// </summary>
public sealed class ApplicationInstanceLock : IDisposable
{
    private readonly FileStream _stream;

    private ApplicationInstanceLock(FileStream stream) => _stream = stream;

    public static bool TryAcquire(string dataDirectory, out ApplicationInstanceLock? instance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        Directory.CreateDirectory(dataDirectory);
        try
        {
            instance = new ApplicationInstanceLock(new FileStream(
                Path.Combine(dataDirectory, "mutualpulse.instance.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
            return true;
        }
        catch (IOException)
        {
            instance = null;
            return false;
        }
    }

    public void Dispose() => _stream.Dispose();
}
