using Avalonia.Controls;
using Avalonia.Input;
using FikaLauncher.ViewModels.Dialogs;

namespace FikaLauncher.Views.Dialogs;

public partial class LoginDialogView : UserControl
{
    public LoginDialogView()
    {
        InitializeComponent();
    }

    private async void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginDialogViewModel viewModel)
        {
            await viewModel.HandleEnterKeyCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }
}