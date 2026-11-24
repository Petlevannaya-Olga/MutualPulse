using System.IO;
using MutualPulse.Desktop.Settings;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace MutualPulse.Desktop.Services;

/// <summary>Visible, locally persistent Chrome session. Login is performed by the user.</summary>
public sealed class InstagramSessionService : IDisposable
{
    private readonly ChromeInstallationService _chrome;
    private readonly string _profileDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ChromeDriver? _driver;
    private bool _disposed;

    public InstagramSessionService(ChromeInstallationService chrome) : this(chrome,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MutualPulse", "InstagramBrowser")) { }

    public InstagramSessionService(ChromeInstallationService chrome, string profileDirectory)
    {
        _chrome = chrome;
        _profileDirectory = Path.GetFullPath(profileDirectory);
    }

    public async Task OpenLoginAsync(CancellationToken cancellationToken = default)
    {
        await RunAsync(driver => driver.Navigate().GoToUrl("https://www.instagram.com/"), cancellationToken);
    }

    /// <summary>
    /// The session cookie is necessary but not sufficient. Confirms the specified
    /// account through Instagram's own profile navigation, never through post links.
    /// Returns false if the evidence is absent or ambiguous.
    /// </summary>
    public async Task<bool> VerifySessionAsync(string userName, CancellationToken cancellationToken = default)
    {
        var expected = InstagramUserName.Normalize(userName);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return await Task.Run(() =>
            {
                var driver = GetDriver();
                driver.Navigate().GoToUrl("https://www.instagram.com/");
                for (var i = 0; i < 24; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (driver.Manage().Cookies.GetCookieNamed("sessionid") is not null &&
                        HasOwnProfileNavigation(driver, expected))
                        return true;
                    Thread.Sleep(300);
                }
                if (driver.Manage().Cookies.GetCookieNamed("sessionid") is null)
                    return false;
                return ConfirmByOpeningProfileControl(driver, expected, cancellationToken);
            }, cancellationToken);
        }
        catch (WebDriverException) { return false; }
        finally { _gate.Release(); }
    }

    public async Task ClearSessionAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await Task.Run(() =>
            {
                CloseDriver();
                if (Directory.Exists(_profileDirectory))
                    Directory.Delete(_profileDirectory, recursive: true);
            }, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    private async Task RunAsync(Action<ChromeDriver> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await Task.Run(() => action(GetDriver()), cancellationToken);
        }
        finally { _gate.Release(); }
    }

    private ChromeDriver GetDriver()
    {
        if (_driver is not null)
        {
            try { _ = _driver.WindowHandles; return _driver; }
            catch (WebDriverException) { CloseDriver(); }
        }
        var found = _chrome.FindChrome();
        if (found.Kind != ChromeCheckResultKind.AVAILABLE || found.BrowserPath is null)
            throw new InvalidOperationException("Google Chrome не установлен.");
        Directory.CreateDirectory(_profileDirectory);
        var options = new ChromeOptions { BinaryLocation = found.BrowserPath };
        options.AddArgument($"--user-data-dir={_profileDirectory}");
        options.AddArgument("--profile-directory=Default");
        options.AddArgument("--no-first-run");
        _driver = new ChromeDriver(options);
        return _driver;
    }

    private static bool HasOwnProfileNavigation(ChromeDriver driver, string expected)
    {
        try
        {
            foreach (var link in driver.FindElements(By.CssSelector("a[href]")))
            {
                if (!link.Displayed || !IsProfileUrl(link.GetAttribute("href"), expected))
                    continue;
                var inNavigation = (bool)((IJavaScriptExecutor)driver).ExecuteScript(
                    "return !!arguments[0].closest('nav,aside,[role=\"navigation\"]');", link);
                if (inNavigation) return true;
                var labels = new[] { link.GetAttribute("aria-label"), link.GetAttribute("title"), link.Text };
                if (labels.Any(IsProfileLabel)) return true;
                var iconLabels = link.FindElements(By.CssSelector(
                    "svg[aria-label], [role='img'][aria-label], img[alt]"));
                if (iconLabels.Any(icon => IsProfileLabel(icon.GetAttribute("aria-label")) ||
                                         IsProfileLabel(icon.GetAttribute("alt"))))
                    return true;
            }
        }
        catch (StaleElementReferenceException) { /* Instagram updated the DOM during inspection. */ }
        catch (WebDriverException) { /* Browser closed or refreshed; session evidence unavailable. */ }
        return false;
    }

    private static bool ConfirmByOpeningProfileControl(ChromeDriver driver, string expected,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = ((IJavaScriptExecutor)driver).ExecuteScript("""
                const names = new Set(['profile', 'профиль']);
                const controls = document.querySelectorAll('a, button, [role="button"]');
                for (const control of controls) {
                    const rect = control.getBoundingClientRect();
                    if (!rect.width || !rect.height) continue;
                    const labels = [control.getAttribute('aria-label'), control.getAttribute('title'),
                      ...[...control.querySelectorAll('[aria-label],img[alt]')].map(el =>
                        el.getAttribute('aria-label') || el.getAttribute('alt')),
                      ...[...control.querySelectorAll('span')].map(el => el.textContent)];
                    if (labels.some(value => names.has((value || '').trim().toLowerCase())))
                      return control;
                }
                return null;
                """) as IWebElement;
            if (profile is null) return false;
            profile.Click();
            for (var i = 0; i < 20; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (IsProfileUrl(driver.Url, expected)) return true;
                Thread.Sleep(250);
            }
        }
        catch (StaleElementReferenceException) { /* Instagram updated the DOM during inspection. */ }
        catch (WebDriverException) { /* Browser closed or refreshed; session evidence unavailable. */ }
        return false;
    }

    private static bool IsProfileLabel(string? text)
        => text?.Trim().Equals("Profile", StringComparison.OrdinalIgnoreCase) == true ||
           text?.Trim().Equals("Профиль", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsProfileUrl(string? url, string userName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !uri.Host.Equals("www.instagram.com", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.Equals("instagram.com", StringComparison.OrdinalIgnoreCase))
            return false;
        return uri.AbsolutePath.Trim('/').Equals(userName, StringComparison.OrdinalIgnoreCase);
    }

    private void CloseDriver()
    {
        var driver = _driver;
        _driver = null;
        if (driver is null) return;
        try { driver.Quit(); }
        catch (WebDriverException) { /* Browser closed or refreshed; session evidence unavailable. */ }
        finally { driver.Dispose(); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _gate.Wait();
        try
        {
            if (_disposed) return;
            _disposed = true;
            CloseDriver();
        }
        finally { _gate.Release(); }
    }
}
