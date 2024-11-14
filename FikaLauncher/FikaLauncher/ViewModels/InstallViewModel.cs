using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FikaLauncher.Localization;
using FikaLauncher.Services.GitHub;
using System.Windows.Input;

namespace FikaLauncher.ViewModels;

public partial class InstallViewModel : ViewModelBase
{
    public ICommand TestRateLimitCommand { get; }

    public InstallViewModel()
    {
        TestRateLimitCommand = new RelayCommand(TestRateLimit);
    }

    private void TestRateLimit()
    {
        GitHubRateLimitService.Instance.HandleRateLimit();
    }
}