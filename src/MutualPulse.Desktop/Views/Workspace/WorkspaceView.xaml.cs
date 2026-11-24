using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MutualPulse.Desktop.Views.Workspace;

public partial class WorkspaceView : UserControl, INotifyPropertyChanged
{
    private enum WorkspacePage
    {
        HOME,
        SEARCH,
        SETTINGS
    }

    private bool _isSidebarExpanded = true;
    private WorkspacePage _currentPage = WorkspacePage.HOME;

    public WorkspaceView()
    {
        InitializeComponent();
        DataContext = this;
        UpdateNavigation();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ReconnectRequested;

    public bool IsSidebarExpanded
    {
        get => _isSidebarExpanded;
        private set
        {
            if (_isSidebarExpanded == value) return;
            _isSidebarExpanded = value;
            OnPropertyChanged();
        }
    }

    public void ShowAccount(string? username)
    {
        var label = string.IsNullOrWhiteSpace(username)
            ? "Instagram не подключён"
            : $"Instagram: @{username}";
        AccountLabel.Text = label;
        HeaderAccountLabel.Text = label;
    }

    private void WorkspaceView_OnLoaded(object sender, RoutedEventArgs e)
        => UpdateWindowTitle();

    private void HomeButton_OnClick(object sender, RoutedEventArgs e)
        => NavigateTo(WorkspacePage.HOME);

    private void SearchButton_OnClick(object sender, RoutedEventArgs e)
        => NavigateTo(WorkspacePage.SEARCH);

    private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        => NavigateTo(WorkspacePage.SETTINGS);

    private void ToggleSidebarButton_OnClick(object sender, RoutedEventArgs e)
    {
        IsSidebarExpanded = !IsSidebarExpanded;
        SidebarColumn.Width = new GridLength(IsSidebarExpanded ? 264 : 74);
        CollapseButton.Content = IsSidebarExpanded ? "‹" : "›";
    }

    private void NavigateTo(WorkspacePage page)
    {
        if (_currentPage == page) return;
        _currentPage = page;
        UpdateNavigation();
        UpdateWindowTitle();
    }

    private void UpdateNavigation()
    {
        HomePage.Visibility = _currentPage == WorkspacePage.HOME
            ? Visibility.Visible : Visibility.Collapsed;
        SearchPage.Visibility = _currentPage == WorkspacePage.SEARCH
            ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = _currentPage == WorkspacePage.SETTINGS
            ? Visibility.Visible : Visibility.Collapsed;

        PageTitle.Text = _currentPage switch
        {
            WorkspacePage.HOME => "Главная",
            WorkspacePage.SEARCH => "Поиск",
            _ => "Настройки"
        };

        var activeBackground = new SolidColorBrush(Color.FromRgb(0x4E, 0x35, 0x7B));
        HomeButton.Background = _currentPage == WorkspacePage.HOME
            ? activeBackground : Brushes.Transparent;
        SearchButton.Background = _currentPage == WorkspacePage.SEARCH
            ? activeBackground : Brushes.Transparent;
        SettingsButton.Background = _currentPage == WorkspacePage.SETTINGS
            ? activeBackground : Brushes.Transparent;
    }

    private void UpdateWindowTitle()
    {
        var window = Window.GetWindow(this);
        if (window != null)
            window.Title = $"MutualPulse — {PageTitle.Text}";
    }

    private void ReconnectButton_OnClick(object sender, RoutedEventArgs e)
        => ReconnectRequested?.Invoke(this, EventArgs.Empty);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
