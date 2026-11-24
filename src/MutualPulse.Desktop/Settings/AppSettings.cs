namespace MutualPulse.Desktop.Settings;

public enum OnboardingStep
{
    WELCOME,
    PROFILE,
    CHROME_CHECK,
    INSTAGRAM,
    WORKSPACE
}

/// <summary>Local settings only. Instagram password and browser cookies are never serialized here.</summary>
public sealed class AppSettings
{
    public string? UserName { get; set; }
    public string? InstagramAccountName { get; set; }
    // Фиксируется после успешного открытия браузера; не означает успешную авторизацию.
    public bool InstagramLoginStarted { get; set; }
    public OnboardingStep CurrentOnboardingStep { get; set; } = OnboardingStep.WELCOME;
    public bool IsOnboardingCompleted { get; set; }
}
