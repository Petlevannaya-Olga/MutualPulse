using System.Windows;

namespace MutualPulse.Desktop.Views.Dialogs;

public partial class ExitOnboardingDialog : Window
{
    public ExitOnboardingDialog()
    {
        InitializeComponent();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void ContinueSetup_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

}
