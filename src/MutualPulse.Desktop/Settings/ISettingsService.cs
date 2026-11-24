namespace MutualPulse.Desktop.Settings;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveOnboardingStepAsync(OnboardingStep step, CancellationToken cancellationToken = default);
    Task SaveUserNameAndStepAsync(string userName, OnboardingStep step,
        CancellationToken cancellationToken = default);
    Task SaveInstagramLoginStartedAsync(string instagramAccount, CancellationToken cancellationToken = default);
    Task ClearInstagramConnectionAsync(CancellationToken cancellationToken = default);
    Task CompleteOnboardingAsync(string verifiedInstagramAccount,
        CancellationToken cancellationToken = default);
}
