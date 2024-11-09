using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FikaLauncher.Views.Dialogs;

public partial class AddBookmarkDialog : UserControl
{
    public AddBookmarkDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
} 