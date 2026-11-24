using System.IO;
using Microsoft.Win32;
using OpenQA.Selenium.Chrome;

namespace MutualPulse.Desktop.Services;

public enum ChromeCheckResultKind
{
    AVAILABLE,
    NOT_INSTALLED,
    CANNOT_START
}

public sealed record ChromeCheckResult(ChromeCheckResultKind Kind, string? BrowserPath, string Message);

/// <summary>Distinguishes an absent browser from a ChromeDriver startup failure.</summary>
public sealed class ChromeInstallationService
{
    public ChromeCheckResult FindChrome()
    {
        foreach (var path in FindChromeCandidates())
        {
            if (File.Exists(path))
                return new ChromeCheckResult(ChromeCheckResultKind.AVAILABLE, path,
                    "Google Chrome обнаружен. Проверяем запуск через Selenium…");
        }
        return new ChromeCheckResult(ChromeCheckResultKind.NOT_INSTALLED, null,
            "Google Chrome не найден. Установите браузер и повторите проверку.");
    }

    public async Task<ChromeCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var located = FindChrome();
        if (located.Kind != ChromeCheckResultKind.AVAILABLE || located.BrowserPath is null)
            return located;

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var profile = Path.Combine(Path.GetTempPath(), "MutualPulse-ChromeCheck-" + Guid.NewGuid().ToString("N"));
            ChromeDriver? driver = null;
            try
            {
                var options = new ChromeOptions { BinaryLocation = located.BrowserPath };
                options.AddArgument("--headless=new");
                options.AddArgument("--no-first-run");
                options.AddArgument("--no-default-browser-check");
                options.AddArgument($"--user-data-dir={profile}");
                driver = new ChromeDriver(options);
                driver.Navigate().GoToUrl("about:blank");
                cancellationToken.ThrowIfCancellationRequested();
                return new ChromeCheckResult(ChromeCheckResultKind.AVAILABLE, located.BrowserPath,
                    "Google Chrome установлен и успешно запущен через Selenium.");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception) when (exception is OpenQA.Selenium.WebDriverException or
                IOException or UnauthorizedAccessException)
            {
                // Never describe a Selenium Manager/driver problem as 'Chrome not installed'.
                return new ChromeCheckResult(ChromeCheckResultKind.CANNOT_START,
                    located.BrowserPath,
                    "Chrome установлен, но Selenium не смог его запустить. " + exception.Message);
            }
            finally
            {
                if (driver is not null)
                {
                    try { driver.Quit(); }
                    catch (OpenQA.Selenium.WebDriverException) { /* Chrome may already have exited. */ }
                    driver.Dispose();
                }
                try { if (Directory.Exists(profile)) Directory.Delete(profile, recursive: true); }
                catch (IOException) { /* Temporary profile may still be locked. */ }
                catch (UnauthorizedAccessException) { /* OS may still hold temp files. */ }
            }
        }, cancellationToken);
    }

    private static IEnumerable<string> FindChromeCandidates()
    {
        foreach (var baseDirectory in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        })
        {
            if (!string.IsNullOrWhiteSpace(baseDirectory))
                yield return Path.Combine(baseDirectory, "Google", "Chrome", "Application", "chrome.exe");
        }

        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var root = RegistryKey.OpenBaseKey(hive, view);
            using var key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
            var raw = key?.GetValue(null) as string;
            if (!string.IsNullOrWhiteSpace(raw))
                yield return raw.Trim('"');
        }

        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), "chrome.exe");
            yield return candidate;
        }
    }
}
