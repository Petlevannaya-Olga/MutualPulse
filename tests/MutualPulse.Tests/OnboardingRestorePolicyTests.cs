using MutualPulse.Desktop.Settings;
using Xunit;

namespace MutualPulse.Tests;

public sealed class OnboardingRestorePolicyTests
{
    [Fact]
    public void FreshInstallStartsAtWelcome()
    {
        Assert.Equal(OnboardingStep.WELCOME, OnboardingRestorePolicy.Resolve(new AppSettings()));
    }

    [Fact]
    public void StepCannotSkipName()
    {
        Assert.Equal(OnboardingStep.PROFILE, OnboardingRestorePolicy.Resolve(new AppSettings
        {
            CurrentOnboardingStep = OnboardingStep.INSTAGRAM
        }));
    }

    [Fact]
    public void CanResumeInstagramSetup()
    {
        Assert.Equal(OnboardingStep.INSTAGRAM, OnboardingRestorePolicy.Resolve(new AppSettings
        {
            UserName = "Мария", CurrentOnboardingStep = OnboardingStep.INSTAGRAM
        }));
    }

    [Fact]
    public void CompletedSetupResumesWorkspace()
    {
        Assert.Equal(OnboardingStep.WORKSPACE, OnboardingRestorePolicy.Resolve(new AppSettings
        {
            UserName = "Мария", InstagramAccountName = "example_account",
            IsOnboardingCompleted = true, CurrentOnboardingStep = OnboardingStep.WORKSPACE
        }));
    }

    [Fact]
    public void UnfinishedWorkspaceCannotBypassInstagram()
    {
        Assert.Equal(OnboardingStep.INSTAGRAM, OnboardingRestorePolicy.Resolve(new AppSettings
        {
            UserName = "Мария", CurrentOnboardingStep = OnboardingStep.WORKSPACE
        }));
    }
}
