using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MutualPulse.Desktop.ViewModels;

namespace MutualPulse.Desktop.Views.Onboarding;

public partial class InstagramConnectView : UserControl
{
    private readonly InstagramAccountSetupViewModel _viewModel = new();
    private bool _isBusy;

    public InstagramConnectView()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        RefreshButtons();
    }

    public event EventHandler? BackRequested;
    public event EventHandler? OpenRequested;
    public event EventHandler? VerifyRequested;
    public event EventHandler? ContinueRequested;
    public event EventHandler? ResetRequested;

    public string AccountName => _viewModel.Name;
    public bool CanContinue => !_isBusy && _viewModel.CanContinue;

    public void RestoreAccountName(string? accountName)
        => _viewModel.Name = accountName ?? string.Empty;

    public void RestoreConnection(string? accountName, bool loginStarted)
    {
        _viewModel.RestoreConnection(accountName, loginStarted);
        ClearStatus();
    }

    public void MarkBrowserOpened() => _viewModel.MarkBrowserOpened();
    public void MarkSessionVerified() => _viewModel.MarkSessionVerified();

    public void ResetFlow()
    {
        _viewModel.ResetFlow();
        ClearStatus();
    }

    public void SetBusy(string message)
    {
        _isBusy = true;
        ShowStatus(message);
        CheckProgress.Visibility = Visibility.Visible;
        AccountNameInput.IsEnabled = false;
        RefreshButtons();
    }

    public void ShowStatus(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.Foreground = (Brush)FindResource("TextSecondary");
        StatusLabel.Visibility = string.IsNullOrWhiteSpace(message)
            ? Visibility.Collapsed : Visibility.Visible;
    }

    public void ShowVerifiedStatus(string account)
    {
        StatusLabel.Text = "✓ Сессия @" + account + " успешно подтверждена.";
        StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x8A, 0x5B));
        StatusLabel.Visibility = Visibility.Visible;
    }

    public void ClearStatus() => ShowStatus(string.Empty);

    public void SetReady()
    {
        _isBusy = false;
        CheckProgress.Visibility = Visibility.Collapsed;
        AccountNameInput.IsEnabled = true;
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        BackButton.IsEnabled = !_isBusy;
        OpenButton.IsEnabled = !_isBusy && _viewModel.CanOpen;
        VerifyButton.IsEnabled = !_isBusy && _viewModel.CanVerify;
        ContinueButton.IsEnabled = !_isBusy && _viewModel.CanContinue;
        ResetButton.IsEnabled = !_isBusy && _viewModel.ShowReset;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(InstagramAccountSetupViewModel.Name) && !_isBusy)
            ClearStatus();

        RefreshButtons();
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_isBusy) BackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OpenButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_isBusy && _viewModel.CanOpen)
            OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    private void VerifyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_isBusy && _viewModel.CanVerify)
            VerifyRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ContinueButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CanContinue)
            ContinueRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_isBusy && _viewModel.ShowReset)
            ResetRequested?.Invoke(this, EventArgs.Empty);
    }
}
