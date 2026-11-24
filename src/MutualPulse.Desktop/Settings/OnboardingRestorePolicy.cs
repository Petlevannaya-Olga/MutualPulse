namespace MutualPulse.Desktop.Settings;

public static class OnboardingRestorePolicy
{
    public static OnboardingStep Resolve(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.IsOnboardingCompleted &&
            !string.IsNullOrWhiteSpace(settings.InstagramAccountName) &&
            !string.IsNullOrWhiteSpace(settings.UserName))
            return OnboardingStep.WORKSPACE;

        var step = settings.CurrentOnboardingStep;
        if (!Enum.IsDefined(step)) return OnboardingStep.WELCOME;
        if (step == OnboardingStep.WORKSPACE)
            step = OnboardingStep.INSTAGRAM;
        if ((step is OnboardingStep.CHROME_CHECK or OnboardingStep.INSTAGRAM) &&
            string.IsNullOrWhiteSpace(settings.UserName))
            return OnboardingStep.PROFILE;
        return step;
    }
}
