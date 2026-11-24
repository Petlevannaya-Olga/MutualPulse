using System.IO;
using System.Text.Json;
using MutualPulse.Desktop.Settings;
using Xunit;

namespace MutualPulse.Tests;

public sealed class SettingsTests
{
    [Fact]
    public async Task ProfileAndInstagramPersistAcrossRestart()
    {
        var path = Path.Combine(Path.GetTempPath(), "MutualPulseTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var settings = new JsonSettingsService(path))
            {
                await settings.SaveUserNameAndStepAsync("  Ольга  ", OnboardingStep.CHROME_CHECK);
                await settings.SaveOnboardingStepAsync(OnboardingStep.INSTAGRAM);
                await settings.CompleteOnboardingAsync("@demo.user");
            }
            using var next = new JsonSettingsService(path);
            var saved = await next.LoadAsync();
            Assert.Equal("Ольга", saved.UserName);
            Assert.Equal("demo.user", saved.InstagramAccountName);
            Assert.True(saved.IsOnboardingCompleted);
            Assert.Equal(OnboardingStep.WORKSPACE, saved.CurrentOnboardingStep);
            var text = await File.ReadAllTextAsync(Path.Combine(path, "settings.json"));
            Assert.False(text.Contains("password", StringComparison.OrdinalIgnoreCase));
            Assert.False(text.Contains("sessionid", StringComparison.OrdinalIgnoreCase));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
    }

    [Fact]
    public async Task UnfinishedInstagramLoginIsRestoredAfterApplicationRestart()
    {
        var path = Path.Combine(Path.GetTempPath(), "MutualPulseTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var settings = new JsonSettingsService(path))
            {
                await settings.SaveUserNameAndStepAsync("Ольга", OnboardingStep.CHROME_CHECK);
                await settings.SaveOnboardingStepAsync(OnboardingStep.INSTAGRAM);
                await settings.SaveInstagramLoginStartedAsync("test_account");
            }
            using var restarted = new JsonSettingsService(path);
            var saved = await restarted.LoadAsync();
            Assert.Equal(OnboardingStep.INSTAGRAM, OnboardingRestorePolicy.Resolve(saved));
            Assert.Equal("test_account", saved.InstagramAccountName);
            Assert.True(saved.InstagramLoginStarted);
            Assert.False(saved.IsOnboardingCompleted);
            var text = await File.ReadAllTextAsync(Path.Combine(path, "settings.json"));
            Assert.DoesNotContain("sessionid", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
    }

    [Fact]
    public async Task OnlyExplicitResetClearsInstagramAccountFromSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), "MutualPulseTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var settings = new JsonSettingsService(path);
            await settings.SaveUserNameAndStepAsync("Ольга", OnboardingStep.CHROME_CHECK);
            await settings.SaveOnboardingStepAsync(OnboardingStep.INSTAGRAM);
            await settings.SaveInstagramLoginStartedAsync("test_account");
            await settings.SaveOnboardingStepAsync(OnboardingStep.CHROME_CHECK);
            var before = await settings.LoadAsync();
            Assert.True(before.InstagramLoginStarted);
            Assert.Equal("test_account", before.InstagramAccountName);
            await settings.ClearInstagramConnectionAsync();
            var after = await settings.LoadAsync();
            Assert.False(after.InstagramLoginStarted);
            Assert.Null(after.InstagramAccountName);
            Assert.Equal("Ольга", after.UserName);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
    }

    [Fact]
    public async Task ReconnectionClearsCompletionButPreservesName()
    {
        var path = Path.Combine(Path.GetTempPath(), "MutualPulseTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var settings = new JsonSettingsService(path);
            await settings.SaveUserNameAndStepAsync("Ольга", OnboardingStep.CHROME_CHECK);
            await settings.SaveOnboardingStepAsync(OnboardingStep.INSTAGRAM);
            await settings.CompleteOnboardingAsync("test_account");
            await settings.SaveOnboardingStepAsync(OnboardingStep.INSTAGRAM);
            var saved = await settings.LoadAsync();
            Assert.False(saved.IsOnboardingCompleted);
            Assert.Equal("Ольга", saved.UserName);
            Assert.Equal("test_account", saved.InstagramAccountName);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
    }

    [Fact]
    public async Task CannotCompleteWithoutCorrectSequence()
    {
        var path = Path.Combine(Path.GetTempPath(), "MutualPulseTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var settings = new JsonSettingsService(path);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                settings.CompleteOnboardingAsync("test_account"));
            await settings.SaveUserNameAndStepAsync("Ольга", OnboardingStep.CHROME_CHECK);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                settings.CompleteOnboardingAsync("test_account"));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
    }
}
