using System.Windows;
using System.Windows.Controls;
using MutualPulse.Desktop.Services;

namespace MutualPulse.Desktop.Views.Onboarding;

public partial class ChromeCheckView : UserControl
{
    public ChromeCheckView() => InitializeComponent();

    public event EventHandler? BackRequested;
    public event EventHandler? RetryRequested;
    public event EventHandler? DownloadRequested;
    public event EventHandler? ContinueRequested;

    public bool CanContinue { get; private set; }

    public void SetBusy()
    {
        CanContinue = false;
        ContinueButton.IsEnabled = false;
        ContinueButton.Visibility = Visibility.Collapsed;
        StatusLabel.Text = "Проверяем Google Chrome…";
        StatusLabel.Visibility = Visibility.Visible;
        SuccessDetails.Visibility = Visibility.Collapsed;
        CheckProgress.Visibility = Visibility.Visible;
        BackButton.IsEnabled = false;
        RetryButton.Visibility = Visibility.Collapsed;
        DownloadButton.Visibility = Visibility.Collapsed;
    }

    public void ShowResult(ChromeCheckResult result)
    {
        CanContinue = result.Kind == ChromeCheckResultKind.AVAILABLE;
        ContinueButton.IsEnabled = CanContinue;
        ContinueButton.Visibility = CanContinue ? Visibility.Visible : Visibility.Collapsed;
        CheckProgress.Visibility = Visibility.Collapsed;
        BackButton.IsEnabled = true;
        StatusLabel.Text = result.Message;
        StatusLabel.Visibility = CanContinue ? Visibility.Collapsed : Visibility.Visible;
        SuccessDetails.Visibility = CanContinue ? Visibility.Visible : Visibility.Collapsed;
        RetryButton.Visibility = CanContinue ? Visibility.Collapsed : Visibility.Visible;
        DownloadButton.Visibility = result.Kind == ChromeCheckResultKind.NOT_INSTALLED
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
        => BackRequested?.Invoke(this, EventArgs.Empty);

    private void RetryButton_OnClick(object sender, RoutedEventArgs e)
        => RetryRequested?.Invoke(this, EventArgs.Empty);

    private void DownloadButton_OnClick(object sender, RoutedEventArgs e)
        => DownloadRequested?.Invoke(this, EventArgs.Empty);

    private void ContinueButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CanContinue)
            ContinueRequested?.Invoke(this, EventArgs.Empty);
    }
}
