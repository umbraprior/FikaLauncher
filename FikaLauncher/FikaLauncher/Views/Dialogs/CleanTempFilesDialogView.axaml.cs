using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FikaLauncher.Views.Dialogs
{
    public partial class CleanTempFilesDialogView : UserControl
    {
        public CleanTempFilesDialogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

