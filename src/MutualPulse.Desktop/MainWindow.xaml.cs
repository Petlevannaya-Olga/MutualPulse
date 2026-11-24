using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using MutualPulse.Desktop.Services;
using MutualPulse.Desktop.Settings;
using MutualPulse.Desktop.Views.Dialogs;

namespace MutualPulse.Desktop;

public partial class MainWindow : Window
{
    private readonly ISettingsService _settings;
    private readonly ChromeInstallationService _chrome;
    private readonly InstagramSessionService _instagram;
    private readonly ILogger<MainWindow> _logger;
    private OnboardingStep _currentStep = OnboardingStep.WELCOME;
    private bool _initialized;
    private bool _loaded;
    private bool _busy;
    private bool _isCompleted;
    private bool _isExitDialogOpen;
    private bool _isCloseConfirmed;

    public MainWindow(ISettingsService settings, ChromeInstallationService chrome,
        InstagramSessionService instagram, ILogger<MainWindow> logger)
    {
        InitializeComponent();
        _settings = settings;
        _chrome = chrome;
        _instagram = instagram;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var saved = await _settings.LoadAsync(cancellationToken);
        ProfileScreen.RestoreName(saved.UserName);
        InstagramScreen.RestoreConnection(saved.InstagramAccountName, saved.InstagramLoginStarted);
        WorkspaceScreen.ShowAccount(saved.InstagramAccountName);
        _isCompleted = saved.IsOnboardingCompleted;
        ShowStep(OnboardingRestorePolicy.Resolve(saved));
        _initialized = true;
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs args)
    {
        if (!_initialized || _loaded) return;
        _loaded = true;
        // Завершённый онбординг не проверяем автоматически через Selenium:
        // проверка открывает Chrome и может дать ложный отрицательный результат
        // при изменениях интерфейса Instagram. Повторная проверка — вручную.
        if (_currentStep == OnboardingStep.CHROME_CHECK)
            await RunChromeCheckAsync();
        else if (_currentStep == OnboardingStep.INSTAGRAM &&
                 _chrome.FindChrome().Kind != ChromeCheckResultKind.AVAILABLE)
        {
            // При восстановлении незавершённого шага Instagram не возвращаем
            // на проверку Chrome без причины.
            await _settings.SaveOnboardingStepAsync(OnboardingStep.CHROME_CHECK);
            await RunChromeCheckAsync();
        }
    }

    private void ShowStep(OnboardingStep step)
    {
        _currentStep = step;
        WelcomeScreen.Visibility = step == OnboardingStep.WELCOME ? Visibility.Visible : Visibility.Collapsed;
        ProfileScreen.Visibility = step == OnboardingStep.PROFILE ? Visibility.Visible : Visibility.Collapsed;
        ChromeScreen.Visibility = step == OnboardingStep.CHROME_CHECK ? Visibility.Visible : Visibility.Collapsed;
        InstagramScreen.Visibility = step == OnboardingStep.INSTAGRAM ? Visibility.Visible : Visibility.Collapsed;
        WorkspaceScreen.Visibility = step == OnboardingStep.WORKSPACE ? Visibility.Visible : Visibility.Collapsed;
        Title = step switch
        {
            OnboardingStep.PROFILE => "MutualPulse — Настройка: шаг 1",
            OnboardingStep.CHROME_CHECK => "MutualPulse — Настройка: шаг 2",
            OnboardingStep.INSTAGRAM => "MutualPulse — Настройка: шаг 3",
            OnboardingStep.WORKSPACE => "MutualPulse — Главная",
            _ => "MutualPulse — Поиск людей для взаимных подписок"
        };
    }

    private async Task NavigateAsync(OnboardingStep step)
    {
        try
        {
            await _settings.SaveOnboardingStepAsync(step);
            _isCompleted = false;
            if (step == OnboardingStep.INSTAGRAM)
            {
                var saved = await _settings.LoadAsync();
                InstagramScreen.RestoreConnection(saved.InstagramAccountName, saved.InstagramLoginStarted);
            }
            ShowStep(step);
            if (step == OnboardingStep.CHROME_CHECK)
                await RunChromeCheckAsync();
        }
        catch (Exception exception)
        {
            ShowError(exception, "Не удалось сохранить состояние настройки.");
        }
    }

    private async Task RunChromeCheckAsync()
    {
        if (_busy) return;
        _busy = true;
        ShowStep(OnboardingStep.CHROME_CHECK);
        ChromeScreen.SetBusy();
        try
        {
            var result = await _chrome.CheckAsync();
            // Показываем результат на втором шаге; переход только по явному нажатию «Далее».
            ChromeScreen.ShowResult(result);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Ошибка проверки Google Chrome");
            ChromeScreen.ShowResult(new ChromeCheckResult(ChromeCheckResultKind.CANNOT_START,
                null, "Проверить запуск Chrome не удалось: " + exception.Message));
        }
        finally { _busy = false; }
    }

    private async void Welcome_OnStart(object sender, EventArgs args)
        => await NavigateAsync(OnboardingStep.PROFILE);
    private async void Profile_OnBack(object sender, EventArgs args)
        => await NavigateAsync(OnboardingStep.WELCOME);

    private async void Profile_OnContinue(object sender, EventArgs args)
    {
        if (_busy) return;
        try
        {
            await _settings.SaveUserNameAndStepAsync(ProfileScreen.DisplayName, OnboardingStep.CHROME_CHECK);
            _isCompleted = false;
            await RunChromeCheckAsync();
        }
        catch (Exception exception) { ShowError(exception, "Не удалось сохранить имя."); }
    }

    private async void Chrome_OnBack(object sender, EventArgs args)
        => await NavigateAsync(OnboardingStep.PROFILE);
    private async void Chrome_OnRetry(object sender, EventArgs args)
        => await RunChromeCheckAsync();

    private async void Chrome_OnContinue(object sender, EventArgs args)
    {
        if (_busy || _currentStep != OnboardingStep.CHROME_CHECK || !ChromeScreen.CanContinue)
            return;

        _busy = true;
        try
        {
            await _settings.SaveOnboardingStepAsync(OnboardingStep.INSTAGRAM);
            _isCompleted = false;
            var saved = await _settings.LoadAsync();
            InstagramScreen.RestoreConnection(saved.InstagramAccountName, saved.InstagramLoginStarted);
            ShowStep(OnboardingStep.INSTAGRAM);
        }
        catch (Exception exception)
        {
            ShowError(exception, "Не удалось перейти к подключению Instagram.");
        }
        finally { _busy = false; }
    }

    private void Chrome_OnDownload(object sender, EventArgs args)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.google.com/chrome/")
                { UseShellExecute = true });
        }
        catch (Exception exception) { ShowError(exception, "Не удалось открыть сайт Google Chrome."); }
    }

    private async void Instagram_OnBack(object sender, EventArgs args)
        => await NavigateAsync(OnboardingStep.CHROME_CHECK);

    private async void Instagram_OnOpen(object sender, EventArgs args)
    {
        if (_busy) return;
        _busy = true;
        InstagramScreen.SetBusy("Открываем Instagram…");
        try
        {
            await _instagram.OpenLoginAsync();
            await _settings.SaveInstagramLoginStartedAsync(InstagramScreen.AccountName);
            InstagramScreen.MarkBrowserOpened();
            InstagramScreen.ShowStatus("Войдите в Instagram в открывшемся Chrome. После входа нажмите «Проверить сессию».");
        }
        catch (Exception exception) { ShowError(exception, "Не удалось открыть Instagram."); }
        finally { InstagramScreen.SetReady(); _busy = false; }
    }

    private async void Instagram_OnVerify(object sender, EventArgs args)
    {
        if (_busy) return;
        string username;
        try { username = InstagramUserName.Normalize(InstagramScreen.AccountName); }
        catch (ArgumentException exception)
        {
            InstagramScreen.ShowStatus(exception.Message);
            return;
        }
        _busy = true;
        InstagramScreen.SetBusy("Проверяем сессию Instagram и имя учётной записи…");
        try
        {
            if (!await _instagram.VerifySessionAsync(username))
            {
                InstagramScreen.ShowStatus("Сессия не подтверждена для @" + username +
                    ". Проверьте, что вы вошли именно в этот аккаунт, и повторите проверку.");
                return;
            }
            InstagramScreen.MarkSessionVerified();
            InstagramScreen.ShowVerifiedStatus(username);
        }
        catch (Exception exception) { ShowError(exception, "Не удалось проверить Instagram-сессию."); }
        finally { InstagramScreen.SetReady(); _busy = false; }
    }

    private async void Instagram_OnContinue(object sender, EventArgs args)
    {
        if (_busy || !InstagramScreen.CanContinue) return;
        _busy = true;
        InstagramScreen.SetBusy("Сохраняем подключение Instagram…");
        try
        {
            var username = InstagramUserName.Normalize(InstagramScreen.AccountName);
            await _settings.CompleteOnboardingAsync(username);
            _isCompleted = true;
            WorkspaceScreen.ShowAccount(username);
            ShowStep(OnboardingStep.WORKSPACE);
        }
        catch (Exception exception)
        {
            ShowError(exception, "Не удалось завершить подключение Instagram.");
        }
        finally { InstagramScreen.SetReady(); _busy = false; }
    }

    private async void Instagram_OnReset(object sender, EventArgs args)
    {
        if (_busy) return;
        if (MessageBox.Show(this,
                "Удалить локальную браузерную сессию MutualPulse? Потребуется заново войти в Instagram. Сессия ChatPulse не затрагивается.",
                "MutualPulse — Сброс сессии", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;
        _busy = true;
        InstagramScreen.SetBusy("Удаляем сессию браузера…");
        try
        {
            await _instagram.ClearSessionAsync();
            await _settings.ClearInstagramConnectionAsync();
            InstagramScreen.RestoreAccountName(string.Empty);
            InstagramScreen.ResetFlow();
            InstagramScreen.ShowStatus("Сессия удалена. Укажите имя аккаунта и откройте Instagram заново.");
        }
        catch (Exception exception) { ShowError(exception, "Не удалось удалить сессию."); }
        finally { InstagramScreen.SetReady(); _busy = false; }
    }

    private async void Workspace_OnReconnect(object sender, EventArgs args)
        => await NavigateAsync(OnboardingStep.INSTAGRAM);

    private void ShowError(Exception exception, string message)
    {
        _logger.LogError(exception, "{Message}", message);
        MessageBox.Show(this, message + " " + exception.Message,
            "MutualPulse — Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ShowDialogOverlay() => DialogOverlay.Visibility = Visibility.Visible;
    private void HideDialogOverlay() => DialogOverlay.Visibility = Visibility.Collapsed;

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isCloseConfirmed || _isCompleted || !_initialized || !IsLoaded || !IsVisible)
        {
            base.OnClosing(e);
            return;
        }
        if (_isExitDialogOpen)
        {
            e.Cancel = true;
            return;
        }
        _isExitDialogOpen = true;
        var dialog = new ExitOnboardingDialog { Owner = this };
        ShowDialogOverlay();
        try
        {
            if (dialog.ShowDialog() == true) _isCloseConfirmed = true;
            else e.Cancel = true;
        }
        finally
        {
            HideDialogOverlay();
            _isExitDialogOpen = false;
            Keyboard.ClearFocus();
        }
        base.OnClosing(e);
    }
}
