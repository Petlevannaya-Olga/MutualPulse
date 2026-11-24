using MutualPulse.Desktop.ViewModels;
using Xunit;

namespace MutualPulse.Tests;

public sealed class InstagramAccountSetupViewModelTests
{
    [Fact]
    public void EmptyNameIsNotAnErrorUntilUserStartsTyping()
    {
        var model = new InstagramAccountSetupViewModel();

        Assert.True(model.IsNameEmpty);
        Assert.False(model.IsNameValid);
        Assert.False(model.IsNameInvalid);
        Assert.True(model.ShowOpen);
        Assert.False(model.CanOpen);
        Assert.False(model.ShowVerify);
        Assert.False(model.ShowReset);
    }

    [Theory]
    [InlineData("my_account")]
    [InlineData("abc.123")]
    [InlineData("A")]
    public void ValidAccountNameAllowsOpeningInstagramOnly(string name)
    {
        var model = new InstagramAccountSetupViewModel { Name = name };

        Assert.True(model.IsNameValid);
        Assert.False(model.IsNameInvalid);
        Assert.True(model.CanOpen);
        Assert.False(model.CanVerify);
        Assert.False(model.CanContinue);
    }

    [Theory]
    [InlineData("@name")]
    [InlineData("name ")]
    [InlineData("name name")]
    [InlineData("foo..bar")]
    [InlineData("https://www.instagram.com/name")]
    public void InvalidAccountNameShowsValidationError(string name)
    {
        var model = new InstagramAccountSetupViewModel { Name = name };

        Assert.False(model.IsNameValid);
        Assert.True(model.IsNameInvalid);
        Assert.NotEmpty(model.ValidationMessage);
        Assert.False(model.CanOpen);
    }

    [Fact]
    public void SuccessfulBrowserOpenEnablesVerificationButNotReset()
    {
        var model = new InstagramAccountSetupViewModel { Name = "my_account" };

        model.MarkBrowserOpened();

        Assert.Equal(InstagramConnectionStage.READY_TO_VERIFY, model.Stage);
        Assert.False(model.ShowOpen);
        Assert.True(model.ShowVerify);
        Assert.True(model.CanVerify);
        Assert.False(model.ShowContinue);
        Assert.False(model.ShowReset);
    }

    [Fact]
    public void SuccessfulVerificationAllowsExplicitCompletionAndSessionReset()
    {
        var model = new InstagramAccountSetupViewModel { Name = "my_account" };

        model.MarkBrowserOpened();
        model.MarkSessionVerified();

        Assert.Equal(InstagramConnectionStage.VERIFIED, model.Stage);
        Assert.False(model.ShowOpen);
        Assert.False(model.ShowVerify);
        Assert.True(model.ShowContinue);
        Assert.True(model.CanContinue);
        Assert.True(model.ShowReset);
    }

    [Fact]
    public void RestorePendingLoginRequiresNewVerificationButNotNewBrowserLogin()
    {
        var model = new InstagramAccountSetupViewModel();
        model.RestoreConnection("my_account", loginStarted: true);

        Assert.Equal("my_account", model.Name);
        Assert.Equal(InstagramConnectionStage.READY_TO_VERIFY, model.Stage);
        Assert.False(model.CanOpen);
        Assert.True(model.CanVerify);
        Assert.False(model.CanContinue);
        model.MarkSessionVerified();
        Assert.True(model.CanContinue);
    }

    [Fact]
    public void RestoreWithoutPreviousLoginStillRequiresOpeningInstagram()
    {
        var model = new InstagramAccountSetupViewModel();
        model.RestoreConnection("my_account", loginStarted: false);
        Assert.True(model.CanOpen);
        Assert.False(model.CanVerify);
    }

    [Fact]
    public void ChangingAccountAfterVerificationDiscardsOldApproval()
    {
        var model = new InstagramAccountSetupViewModel { Name = "first_account" };
        model.MarkBrowserOpened();
        model.MarkSessionVerified();

        model.Name = "second_account";

        Assert.Equal(InstagramConnectionStage.NOT_OPENED, model.Stage);
        Assert.True(model.CanOpen);
        Assert.False(model.CanContinue);
        Assert.False(model.ShowReset);
    }

    [Fact]
    public void ResetReturnsToOpeningStepAndDoesNotClearEnteredName()
    {
        var model = new InstagramAccountSetupViewModel { Name = "my_account" };
        model.MarkBrowserOpened();
        model.MarkSessionVerified();

        model.ResetFlow();

        Assert.Equal("my_account", model.Name);
        Assert.True(model.CanOpen);
        Assert.False(model.ShowVerify);
        Assert.False(model.ShowReset);
    }

    [Fact]
    public void CannotMarkSessionVerifiedWithoutOpeningBrowser()
    {
        var model = new InstagramAccountSetupViewModel { Name = "my_account" };

        Assert.Throws<InvalidOperationException>(() => model.MarkSessionVerified());
    }
}
