using System.Windows;
using System.Windows.Controls;

namespace MutualPulse.Desktop.Views.Workspace;

public partial class WorkspaceView : UserControl
{
    public WorkspaceView() => InitializeComponent();

    public event EventHandler? ReconnectRequested;

    public void ShowAccount(string? username)
        => AccountLabel.Text = string.IsNullOrWhiteSpace(username)
            ? "Instagram ещё не подключён" : $"Instagram: @{username}";

    private void ReconnectButton_OnClick(object sender, RoutedEventArgs e)
        => ReconnectRequested?.Invoke(this, EventArgs.Empty);
}
