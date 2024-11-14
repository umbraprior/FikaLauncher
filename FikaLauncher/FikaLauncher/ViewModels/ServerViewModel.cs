using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace FikaLauncher.ViewModels;

public partial class ServerViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isServerRunning;

    [ObservableProperty]
    private string _serverStatus = "Stopped";

    public ServerViewModel()
    {
        // Initialize any required services or state
    }

    [RelayCommand]
    private async Task StartServer()
    {
        // Implement server start logic
    }

    [RelayCommand]
    private async Task StopServer()
    {
        // Implement server stop logic
    }
}