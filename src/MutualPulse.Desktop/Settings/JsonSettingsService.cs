using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace MutualPulse.Desktop.Settings;

/// <summary>Concurrent-safe JSON settings with an atomic file replacement.</summary>
public sealed class JsonSettingsService : ISettingsService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly string _directory;
    private readonly string _path;
    private int _disposed;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonSettingsService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MutualPulse")) { }

    public JsonSettingsService(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = Path.GetFullPath(directory);
        _path = Path.Combine(_directory, "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await EnterAsync(cancellationToken);
        try { return await LoadInternalAsync(cancellationToken); }
        finally { _gate.Release(); }
    }

    public Task SaveOnboardingStepAsync(OnboardingStep step, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(step)) throw new ArgumentOutOfRangeException(nameof(step));
        return UpdateAsync(settings =>
        {
            settings.CurrentOnboardingStep = step;
            if (step != OnboardingStep.WORKSPACE)
                settings.IsOnboardingCompleted = false;
        }, cancellationToken);
    }

    public Task SaveUserNameAndStepAsync(string userName, OnboardingStep step,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        var name = userName.Trim();
        if (name.Length is < 2 or > 50)
            throw new ArgumentException("Имя должно содержать от 2 до 50 символов.", nameof(userName));
        if (step != OnboardingStep.CHROME_CHECK)
            throw new ArgumentOutOfRangeException(nameof(step));
        return UpdateAsync(settings =>
        {
            settings.UserName = name;
            settings.CurrentOnboardingStep = step;
            settings.IsOnboardingCompleted = false;
        }, cancellationToken);
    }

    public Task SaveInstagramLoginStartedAsync(string instagramAccount,
        CancellationToken cancellationToken = default)
    {
        var account = InstagramUserName.Normalize(instagramAccount);
        return UpdateAsync(settings =>
        {
            if (settings.CurrentOnboardingStep != OnboardingStep.INSTAGRAM)
                throw new InvalidOperationException("Instagram нужно подключать на третьем шаге.");
            settings.InstagramAccountName = account;
            settings.InstagramLoginStarted = true;
            settings.IsOnboardingCompleted = false;
        }, cancellationToken);
    }

    public Task ClearInstagramConnectionAsync(CancellationToken cancellationToken = default)
        => UpdateAsync(settings =>
        {
            settings.InstagramAccountName = null;
            settings.InstagramLoginStarted = false;
            settings.IsOnboardingCompleted = false;
        }, cancellationToken);

    public Task CompleteOnboardingAsync(string verifiedInstagramAccount,
        CancellationToken cancellationToken = default)
    {
        var account = InstagramUserName.Normalize(verifiedInstagramAccount);
        return UpdateAsync(settings =>
        {
            if (settings.UserName is null || settings.UserName.Trim().Length is < 2 or > 50)
                throw new InvalidOperationException("Сначала укажите имя пользователя.");
            if (settings.CurrentOnboardingStep != OnboardingStep.INSTAGRAM)
                throw new InvalidOperationException("Instagram нужно подключить на соответствующем шаге.");
            settings.InstagramAccountName = account;
            settings.InstagramLoginStarted = true;
            settings.CurrentOnboardingStep = OnboardingStep.WORKSPACE;
            settings.IsOnboardingCompleted = true;
        }, cancellationToken);
    }

    private async Task UpdateAsync(Action<AppSettings> modify, CancellationToken cancellationToken)
    {
        await EnterAsync(cancellationToken);
        try
        {
            var settings = await LoadInternalAsync(cancellationToken);
            modify(settings);
            await SaveInternalAsync(settings, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    private async Task<AppSettings> LoadInternalAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path)) return new AppSettings();
        await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, options: FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, _jsonOptions, cancellationToken)
            ?? new AppSettings();
    }

    private async Task SaveInternalAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_directory);
        var tempPath = Path.Combine(_directory, $"settings-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(settings, _jsonOptions),
                new UTF8Encoding(false), cancellationToken);
            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch (IOException) { /* Temporary file cleanup is best effort. */ }
            catch (UnauthorizedAccessException) { /* Atomic settings file remains safe. */ }
        }
    }

    private async Task EnterAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(JsonSettingsService));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _shutdown.Token);
        try { await _gate.WaitAsync(linked.Token); }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        { throw new ObjectDisposedException(nameof(JsonSettingsService)); }
        if (Volatile.Read(ref _disposed) == 0) return;
        _gate.Release();
        throw new ObjectDisposedException(nameof(JsonSettingsService));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            _shutdown.Cancel();
    }
}
