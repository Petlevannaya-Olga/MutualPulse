using System.ComponentModel;
using System.Runtime.CompilerServices;
using MutualPulse.Desktop.Settings;

namespace MutualPulse.Desktop.ViewModels;

public enum InstagramConnectionStage
{
    NOT_OPENED,
    READY_TO_VERIFY,
    VERIFIED
}

/// <summary>
/// Instagram setup: open the browser, confirm the session, then explicitly finish.
/// Name validation only checks syntax; it never proves that the user is signed in.
/// </summary>
public sealed class InstagramAccountSetupViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private InstagramConnectionStage _stage = InstagramConnectionStage.NOT_OPENED;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;

            _name = value;
            // A result verified for the old username must not be reused for a new one.
            _stage = InstagramConnectionStage.NOT_OPENED;
            OnPropertyChanged();
            NotifyStateChanged();
        }
    }

    public InstagramConnectionStage Stage => _stage;

    public bool IsNameEmpty => string.IsNullOrWhiteSpace(Name);

    public bool IsNameValid
    {
        get
        {
            if (IsNameEmpty || Name.StartsWith('@') || Name.Any(char.IsWhiteSpace))
                return false;

            try
            {
                InstagramUserName.Normalize(Name);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    public bool IsNameInvalid => !IsNameEmpty && !IsNameValid;

    public string ValidationMessage => IsNameInvalid
        ? "Имя: до 30 символов, латинские буквы, цифры, точка и _. Без @, пробелов и ссылок."
        : string.Empty;

    public bool ShowOpen => Stage == InstagramConnectionStage.NOT_OPENED;
    public bool ShowVerify => Stage == InstagramConnectionStage.READY_TO_VERIFY;
    public bool ShowContinue => Stage == InstagramConnectionStage.VERIFIED;
    public bool ShowReset => Stage == InstagramConnectionStage.VERIFIED;

    public bool CanOpen => ShowOpen && IsNameValid;
    public bool CanVerify => ShowVerify && IsNameValid;
    public bool CanContinue => ShowContinue && IsNameValid;

    public void MarkBrowserOpened()
    {
        if (!CanOpen)
            throw new InvalidOperationException("Сначала укажите корректное имя аккаунта.");

        SetStage(InstagramConnectionStage.READY_TO_VERIFY);
    }

    public void MarkSessionVerified()
    {
        if (!CanVerify)
            throw new InvalidOperationException("Перед проверкой необходимо открыть Instagram.");

        SetStage(InstagramConnectionStage.VERIFIED);
    }

    // При восстановлении не доверяем прежней проверке авторизации:
    // сохранённый профиль позволяет повторно проверить сессию без нового входа.
    public void RestoreConnection(string? accountName, bool loginStarted)
    {
        Name = accountName ?? string.Empty;
        SetStage(loginStarted && IsNameValid
            ? InstagramConnectionStage.READY_TO_VERIFY
            : InstagramConnectionStage.NOT_OPENED);
    }

    public void ResetFlow() => SetStage(InstagramConnectionStage.NOT_OPENED);

    private void SetStage(InstagramConnectionStage stage)
    {
        if (_stage == stage)
            return;

        _stage = stage;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(Stage));
        OnPropertyChanged(nameof(IsNameEmpty));
        OnPropertyChanged(nameof(IsNameValid));
        OnPropertyChanged(nameof(IsNameInvalid));
        OnPropertyChanged(nameof(ValidationMessage));
        OnPropertyChanged(nameof(ShowOpen));
        OnPropertyChanged(nameof(ShowVerify));
        OnPropertyChanged(nameof(ShowContinue));
        OnPropertyChanged(nameof(ShowReset));
        OnPropertyChanged(nameof(CanOpen));
        OnPropertyChanged(nameof(CanVerify));
        OnPropertyChanged(nameof(CanContinue));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
