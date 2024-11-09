using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls.Notifications;
using Avalonia.Controls;
using FikaLauncher.Views;
using Avalonia.Media.Imaging;
using System;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia;
using Avalonia.Interactivity;
using System.ComponentModel;
using System.Threading;
using FikaLauncher.Services;
using FikaLauncher.Views.Dialogs;
using FikaLauncher.ViewModels.Dialogs;
using Jeek.Avalonia.Localization;
using Avalonia.Controls.ApplicationLifetimes;
using FikaLauncher.Database;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace FikaLauncher.ViewModels;

public partial class PlayViewModel : ViewModelBase
{
    private PlayView? _view;

    private const string DEFAULT_ADDRESS = "127.0.0.1";


    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private string _serverAddress = DEFAULT_ADDRESS;

    [ObservableProperty]
    private string _serverPort = "6969";

    partial void OnServerPortChanged(string value)
    {
        if (int.TryParse(value, out int port) && port >= 1 && port <= 9999)
        {
            ServerPort = port.ToString();
            UpdateCurrentBookmark();
            
            var state = ApplicationStateService.GetCurrentState();
            state.LastServerPort = value;
            ApplicationStateService.SaveState();
        }
        else
        {
            ServerPort = "6969";
        }
    }

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _currentUsername = string.Empty;

    [ObservableProperty]
    private string _greetingPrefix = string.Empty;

    [ObservableProperty]
    private string _greetingSuffix = string.Empty;

    [ObservableProperty]
    private bool _isLocalhost;

    partial void OnServerAddressChanged(string value)
    {
        IsLocalhost = value == DEFAULT_ADDRESS;
        UpdateCurrentBookmark();
        
        var state = ApplicationStateService.GetCurrentState();
        state.LastServerAddress = value;
        ApplicationStateService.SaveState();
    }

    public void UpdateLoginState()
    {
        IsLoggedIn = AuthService.IsLoggedIn;
        CurrentUsername = AuthService.CurrentUsername;
        
        if (IsLoggedIn && !string.IsNullOrEmpty(CurrentUsername))
        {
            var (prefix, suffix) = ApplicationStateService.GetOrUpdateGreeting(CurrentUsername);
            GreetingPrefix = prefix;
            GreetingSuffix = suffix;
            OnPropertyChanged(nameof(GreetingPrefix));
            OnPropertyChanged(nameof(GreetingSuffix));
            OnPropertyChanged(nameof(CurrentUsername));
            OnPropertyChanged(nameof(IsLoggedIn));
        }
        else
        {
            GreetingPrefix = string.Empty;
            GreetingSuffix = string.Empty;
        }
    }

    private void OnAuthStateChanged(object? sender, bool isLoggedIn)
    {
        UpdateLoginState();
    }

    public PlayViewModel()
    {
        AuthService.AuthStateChanged += OnAuthStateChanged;
        AuthService.CurrentUsernameChanged += OnCurrentUsernameChanged;
        
        // Load last used server address
        var state = ApplicationStateService.GetCurrentState();
        ServerAddress = state.LastServerAddress;
        ServerPort = state.LastServerPort;
        
        UpdateLoginState();
        IsLocalhost = ServerAddress == DEFAULT_ADDRESS;
        LoadBookmarks();
        _ = RefreshBookmarks();
    }

    private async void OnCurrentUsernameChanged(object? sender, string? newUsername)
    {
        await RefreshBookmarks();
    }

    public override void Dispose()
    {
        AuthService.AuthStateChanged -= OnAuthStateChanged;
        AuthService.CurrentUsernameChanged -= OnCurrentUsernameChanged;
        base.Dispose();
    }

    public void SetView(PlayView view)
    {
        _view = view;
        UpdateLoginState();
    }
    

    [RelayCommand]
    private async Task ConnectToServer()
    {
        if (string.IsNullOrWhiteSpace(ServerAddress))
        {
            if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.ShowNotification(
                        Localizer.Get("Error"), 
                        Localizer.Get("ServerAddressEmpty"), 
                        NotificationType.Error);
                }
            }
            return;
        }
        
        IsConnecting = true;
        
        try
        {
            await Task.Delay(3000); // Replace this with actual connection logic
            
            IsConnected = true;
            
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    if (IsLocalhost)
                    {
                        mainViewModel.ShowNotification(
                            Localizer.Get("Success"), 
                            Localizer.Get("ServerStarted"), 
                            NotificationType.Success);
                    }
                    else
                    {
                        mainViewModel.ShowNotification(
                            Localizer.Get("Success"), 
                            Localizer.Get("Connected"), 
                            NotificationType.Success);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            IsConnected = false;
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    if (IsLocalhost)
                    {
                        mainViewModel.ShowNotification(
                            Localizer.Get("Error"), 
                            Localizer.Get("ServerStartFailed"), 
                            NotificationType.Error);
                    }
                    else
                    {
                        mainViewModel.ShowNotification(
                            Localizer.Get("Error"), 
                            Localizer.Get("ConnectionFailed"), 
                            NotificationType.Error);
                    }
                }
            }
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private void DisconnectFromServer()
    {
        IsConnected = false;
        
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                if (IsLocalhost)
                {
                    mainViewModel.ShowNotification(
                        Localizer.Get("Success"), 
                        Localizer.Get("ServerShutdown"), 
                        NotificationType.Success);
                }
                else
                {
                    mainViewModel.ShowNotification(
                        Localizer.Get("Success"), 
                        Localizer.Get("Disconnected"), 
                        NotificationType.Success);
                }
            }
        }
    }

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnectedToLocalhost;

    partial void OnIsConnectedChanged(bool value)
    {
        UpdateCanConnect();
        IsConnectedToLocalhost = value && IsLocalhost;
        if (value)
        {
            CheckIfServerIsBookmarked();
        }
    }

    partial void OnIsLocalhostChanged(bool value)
    {
        // Update the connected to localhost state when localhost status changes
        IsConnectedToLocalhost = IsConnected && value;
    }

    private void UpdateCanConnect()
    {
        CanConnect = !IsConnecting && !IsConnected;
    }

    partial void OnIsConnectingChanged(bool value)
    {
        UpdateCanConnect();
    }

    [ObservableProperty]
    private bool _canConnect = true;

    private IAsyncRelayCommand? _openLoginDialogCommand;
    public IAsyncRelayCommand OpenLoginDialogCommand => 
        _openLoginDialogCommand ??= new AsyncRelayCommand(OpenLoginDialogAsync);

    private async Task OpenLoginDialogAsync()
    {
        var dialogResult = await DialogService.ShowDialog<LoginDialogView>(new LoginDialogViewModel());
        
        if (dialogResult is bool result && result)
        {
            UpdateLoginState();
            
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.ShowNotification(
                        Localizer.Get("Success"),
                        Localizer.Get("LoggedIn"),
                        NotificationType.Success);
                }
            }
        }
    }

    [RelayCommand]
    private async Task LogOut()
    {
        AuthService.Logout();

        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.ShowNotification(
                    Localizer.Get("Success"),
                    Localizer.Get("LoggedOut"),
                    NotificationType.Information);
            }
        }
    }


    [ObservableProperty]
    private ObservableCollection<ServerBookmarkEntity> _bookmarks = new();

    private async void LoadBookmarks()
    {
        if (string.IsNullOrEmpty(AuthService.CurrentUsername))
        {
            Bookmarks.Clear();
            return;
        }

        using var context = new AppDbContext();
        var currentUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == AuthService.CurrentUsername);
        
        if (currentUser != null)
        {
            var bookmarks = await context.ServerBookmarks
                .Where(b => b.UserId == currentUser.Id)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();
                
            Bookmarks = new ObservableCollection<ServerBookmarkEntity>(bookmarks);
        }
        else
        {
            Bookmarks.Clear();
        }
    }

    [ObservableProperty]
    private bool _isCurrentServerBookmarked;

    private void CheckIfServerIsBookmarked()
    {
        IsCurrentServerBookmarked = Bookmarks.Any(b => 
            b.ServerAddress == ServerAddress && 
            b.ServerPort.ToString() == ServerPort);
    }

    [RelayCommand]
    private void SelectBookmark(ServerBookmarkEntity? bookmark)
    {
        if (bookmark != null)
        {
            ServerAddress = bookmark.ServerAddress;
            ServerPort = bookmark.ServerPort.ToString();
        }
    }

    [RelayCommand]
    private async Task BookmarkCurrentServer()
    {
        var viewModel = new AddBookmarkDialogViewModel(ServerAddress, ServerPort);
        var result = await DialogService.ShowDialog<AddBookmarkDialog>(viewModel);
        
        if (result is ServerBookmarkEntity bookmark)
        {
            Bookmarks.Add(bookmark);
            IsCurrentServerBookmarked = true;
        }
    }

    [RelayCommand]
    private async Task UnbookmarkCurrentServer()
    {
        var bookmark = Bookmarks.FirstOrDefault(b => 
            b.ServerAddress == ServerAddress && 
            b.ServerPort.ToString() == ServerPort);
        
        if (bookmark != null)
        {
            using var context = new AppDbContext();
            context.ServerBookmarks.Remove(bookmark);
            await context.SaveChangesAsync();
            
            Bookmarks.Remove(bookmark);
            IsCurrentServerBookmarked = false;

            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.ShowNotification(
                        Localizer.Get("Success"),
                        Localizer.Get("BookmarkRemoved"),
                        NotificationType.Success);
                }
            }
        }
    }

    [ObservableProperty]
    private ServerBookmarkEntity? _currentBookmark;

    partial void OnCurrentBookmarkChanged(ServerBookmarkEntity? value)
    {
        if (value != null)
        {
            ServerAddress = value.ServerAddress;
            ServerPort = value.ServerPort.ToString();
        }
    }

    private void UpdateCurrentBookmark()
    {
        CurrentBookmark = Bookmarks.FirstOrDefault(b => 
            b.ServerAddress == ServerAddress && 
            b.ServerPort.ToString() == ServerPort);
    }

    [RelayCommand]
    private void EditBookmark(ServerBookmarkEntity bookmark)
    {
        // Set all other bookmarks' IsEditing to false
        foreach (var other in Bookmarks)
        {
            if (other != bookmark)
            {
                other.IsEditing = false;
            }
        }
        
        // Enable editing for the selected bookmark
        bookmark.IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveBookmarkName(ServerBookmarkEntity bookmark)
    {
        // Disable editing mode
        bookmark.IsEditing = false;
        
        // Don't update if the name is empty or only whitespace
        if (string.IsNullOrWhiteSpace(bookmark.BookmarkName))
        {
            return;
        }

        using var context = new AppDbContext();
        var entity = await context.ServerBookmarks.FindAsync(bookmark.Id);
        if (entity != null)
        {
            entity.UpdateBookmarkName(bookmark.BookmarkName);
            await context.SaveChangesAsync();
            
            // Update the local collection
            var index = Bookmarks.IndexOf(bookmark);
            if (index != -1)
            {
                Bookmarks[index] = bookmark;
                UpdateCurrentBookmark();
            }
            
            // Show success notification
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.ShowNotification(
                        Localizer.Get("Success"),
                        Localizer.Get("BookmarkUpdate"),
                        NotificationType.Success);
                }
            }
        }
    }

    [RelayCommand]
    private async Task DeleteBookmark(ServerBookmarkEntity bookmark)
    {
        using var context = new AppDbContext();
        context.ServerBookmarks.Remove(bookmark);
        await context.SaveChangesAsync();
        
        Bookmarks.Remove(bookmark);
        UpdateCurrentBookmark();
        
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.ShowNotification(
                    Localizer.Get("Success"),
                    Localizer.Get("BookmarkRemoved"),
                    NotificationType.Success);
            }
        }
    }

    [RelayCommand]
    private void CancelBookmarkEdit(ServerBookmarkEntity bookmark)
    {
        bookmark.CancelEditing();
    }

    public bool IsEditingAnyBookmark => Bookmarks.Any(b => b.IsEditing);

    private async Task RefreshBookmarks()
    {
        if (string.IsNullOrEmpty(AuthService.CurrentUsername))
        {
            Bookmarks.Clear();
            return;
        }

        using var context = new AppDbContext();
        var currentUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == AuthService.CurrentUsername);
        
        if (currentUser != null)
        {
            var bookmarks = await context.ServerBookmarks
                .Where(b => b.UserId == currentUser.Id)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();
                
            Bookmarks = new ObservableCollection<ServerBookmarkEntity>(bookmarks);
        }
        else
        {
            Bookmarks.Clear();
        }
    }
}
