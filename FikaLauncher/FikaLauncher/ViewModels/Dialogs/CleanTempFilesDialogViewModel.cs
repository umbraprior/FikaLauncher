using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FikaLauncher.Services;

namespace FikaLauncher.ViewModels.Dialogs
{
    public partial class CleanTempFilesDialogViewModel : ViewModelBase
    {
        [RelayCommand]
        private void Cancel()
        {
            DialogService.CloseDialog(false);
        }

        [RelayCommand]
        private void Proceed()
        {
            DialogService.CloseDialog(true);
        }
    }
}

