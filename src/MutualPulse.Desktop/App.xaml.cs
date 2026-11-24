using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MutualPulse.Application;
using MutualPulse.Desktop.Services;
using MutualPulse.Desktop.Settings;
using MutualPulse.Desktop.Views.Dialogs;
using MutualPulse.Infrastructure;

namespace MutualPulse.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private ApplicationInstanceLock? _instanceLock;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            var rootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MutualPulse");
            var dataDirectory = Path.Combine(rootDirectory, "Data");
            Directory.CreateDirectory(dataDirectory);
            if (!ApplicationInstanceLock.TryAcquire(dataDirectory, out _instanceLock))
            {
                new AlreadyRunningDialog().ShowDialog();
                Shutdown();
                return;
            }

            var builder = Host.CreateApplicationBuilder();
            builder.Logging.AddDebug();
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(Path.Combine(dataDirectory, "mutualpulse.db"));
            builder.Services.AddSingleton<ISettingsService, JsonSettingsService>();
            builder.Services.AddSingleton<ChromeInstallationService>();
            builder.Services.AddSingleton<InstagramSessionService>();
            builder.Services.AddSingleton<MainWindow>();
            _host = builder.Build();
            await _host.StartAsync();

            var window = _host.Services.GetRequiredService<MainWindow>();
            await window.InitializeAsync();
            MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
        }
        catch (Exception exception)
        {
            var rootCause = exception.GetBaseException();
            var details = ReferenceEquals(rootCause, exception)
                ? exception.Message
                : $"{exception.Message}{Environment.NewLine}Причина: {rootCause.Message}";

            try
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MutualPulse");
                Directory.CreateDirectory(directory);
                var logPath = Path.Combine(directory, "startup-error.log");
                File.WriteAllText(logPath, exception.ToString());
                details += $"{Environment.NewLine}{Environment.NewLine}Подробности: {logPath}";
            }
            catch (IOException)
            {
                // Запись диагностического файла не должна мешать закрытию приложения.
            }
            catch (UnauthorizedAccessException)
            {
                // При отсутствии прав исходная ошибка всё равно показывается пользователю.
            }

            MessageBox.Show($"Не удалось запустить MutualPulse:{Environment.NewLine}{details}",
                "MutualPulse — Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _host?.StopAsync().GetAwaiter().GetResult(); }
        finally
        {
            _host?.Dispose();
            _instanceLock?.Dispose();
            base.OnExit(e);
        }
    }
}
