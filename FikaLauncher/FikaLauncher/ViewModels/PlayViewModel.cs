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
using FikaLauncher.Localization;
using Avalonia.Controls.ApplicationLifetimes;
using FikaLauncher.Database;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace FikaLauncher.ViewModels;

public partial class PlayViewModel : ViewModelBase
{
    private PlayView? _view;

    private const string DEFAULT_ADDRESS = "127.0.0.1";


    [ObservableProperty] private bool _isConnecting;

    [ObservableProperty] private string _serverAddress = DEFAULT_ADDRESS;

    [ObservableProperty] private string _serverPort = "6969";

    [ObservableProperty] private string _fullServerAddress = string.Empty;

    partial void OnServerPortChanged(string value)
    {
        if (int.TryParse(value, out var port) && port >= 1 && port <= 9999)
        {
            ServerPort = port.ToString();
            UpdateCurrentBookmark();
            FullServerAddress = $"{ServerAddress}:{value}";

            var state = ApplicationStateService.GetCurrentState();
            state.LastServerPort = value;
            ApplicationStateService.SaveState();
        }
        else
        {
            ServerPort = "6969";
        }
    }

    [ObservableProperty] private bool _isLoggedIn;

    [ObservableProperty] private string _currentUsername = string.Empty;

    [ObservableProperty] private string _greetingPrefix = string.Empty;

    [ObservableProperty] private string _greetingSuffix = string.Empty;

    [ObservableProperty] private bool _isLocalhost;

    partial void OnServerAddressChanged(string value)
    {
        IsLocalhost = value == DEFAULT_ADDRESS;
        UpdateCurrentBookmark();
        FullServerAddress = $"{value}:{ServerPort}";

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
            NotificationController.ShowError(Localizer.Get("ServerAddressEmpty"));
            return;
        }

        IsConnecting = true;

        try
        {
            await Task.Delay(3000);
            IsConnected = true;
            NotificationController.ShowConnectionSuccess(IsLocalhost);
        }
        catch (Exception)
        {
            IsConnected = false;
            NotificationController.ShowConnectionError(IsLocalhost);
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
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                if (IsLocalhost)
                    NotificationController.ShowServerShutdown();
                else
                    NotificationController.ShowDisconnected();
            }
    }

    [ObservableProperty] private bool _isConnected;

    [ObservableProperty] private bool _isConnectedToLocalhost;

    partial void OnIsConnectedChanged(bool value)
    {
        UpdateCanConnect();
        IsConnectedToLocalhost = value && IsLocalhost;
        if (value) CheckIfServerIsBookmarked();
        OnPropertyChanged(nameof(IsEnabled));
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
        OnPropertyChanged(nameof(IsEnabled));
    }

    [ObservableProperty] private bool _canConnect = true;

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
                if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                    mainViewModel.ShowNotification(
                        Localizer.Get("Success"),
                        Localizer.Get("LoggedIn"),
                        NotificationType.Success);
        }
    }

    [RelayCommand]
    private async Task LogOut()
    {
        AuthService.Logout();

        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                mainViewModel.ShowNotification(
                    Localizer.Get("Success"),
                    Localizer.Get("LoggedOut"),
                    NotificationType.Information);
    }


    [ObservableProperty] private ObservableCollection<ServerBookmarkEntity> _bookmarks = new();

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

    [ObservableProperty] private bool _isCurrentServerBookmarked;

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

    [ObservableProperty] private string _newBookmarkName = string.Empty;

    private Button? _bookmarkButton;

    public void SetBookmarkButton(Button button)
    {
        _bookmarkButton = button;
    }

    [ObservableProperty] private ServerBookmarkEntity? _editingBookmark;

    private System.Timers.Timer? _flyoutTimer;

    public void OnBookmarkFlyoutOpening()
    {
        NewBookmarkName = string.Empty;
        AddNewBookmark();
        _flyoutTimer?.Dispose();
        _flyoutTimer = new System.Timers.Timer(1200);
        _flyoutTimer.Elapsed += (s, e) =>
        {
            if (_bookmarkButton?.Flyout is Flyout flyout) Dispatcher.UIThread.Post(() => flyout.Hide());
            _flyoutTimer?.Dispose();
            _flyoutTimer = null;
        };
        _flyoutTimer.AutoReset = false;
        _flyoutTimer.Start();
    }

    public void ResetFlyoutTimer()
    {
        if (_flyoutTimer != null)
        {
            _flyoutTimer.Stop();
            _flyoutTimer.Start();
        }
    }

    private async void AddNewBookmark()
    {
        if (string.IsNullOrEmpty(AuthService.CurrentUsername))
            return;

        using var context = new AppDbContext();
        var currentUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == AuthService.CurrentUsername);

        if (currentUser == null)
            return;

        var bookmark = new ServerBookmarkEntity
        {
            UserId = currentUser.Id,
            ServerAddress = ServerAddress,
            ServerPort = int.Parse(ServerPort),
            BookmarkName = FullServerAddress
        };

        if (!bookmark.IsValid())
            return;

        try
        {
            context.ServerBookmarks.Add(bookmark);
            await context.SaveChangesAsync();

            Bookmarks.Add(bookmark);
            IsCurrentServerBookmarked = true;
            EditingBookmark = bookmark;

            NotificationController.ShowBookmarkAdded();
        }
        catch (DbUpdateException)
        {
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
                    mainViewModel.ShowNotification(
                        Localizer.Get("Error"),
                        Localizer.Get("BookmarkAlreadyExists"),
                        NotificationType.Error);

            if (_bookmarkButton?.Flyout is Flyout flyout) flyout.Hide();
        }
    }

    [RelayCommand]
    private async Task BookmarkCurrentServer()
    {
        var flyout = _bookmarkButton?.Flyout as Flyout;

        if (EditingBookmark == null || string.IsNullOrWhiteSpace(NewBookmarkName))
        {
            flyout?.Hide();
            return;
        }

        using var context = new AppDbContext();
        var entity = await context.ServerBookmarks.FindAsync(EditingBookmark.Id);
        if (entity != null)
        {
            entity.UpdateBookmarkName(NewBookmarkName.Trim());
            await context.SaveChangesAsync();

            var index = Bookmarks.IndexOf(EditingBookmark);
            if (index != -1)
            {
                EditingBookmark.BookmarkName = NewBookmarkName.Trim();
                Bookmarks[index] = EditingBookmark;
            }

            NotificationController.ShowBookmarkEditSuccess();
        }

        flyout?.Hide();
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

            NotificationController.ShowBookmarkRemoved();
        }
    }

    [ObservableProperty] private ServerBookmarkEntity? _currentBookmark;

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
        foreach (var other in Bookmarks)
            if (other != bookmark)
                other.IsEditing = false;

        bookmark.IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveBookmarkName(ServerBookmarkEntity bookmark)
    {
        bookmark.IsEditing = false;

        if (string.IsNullOrWhiteSpace(bookmark.BookmarkName)) return;

        using var context = new AppDbContext();
        var entity = await context.ServerBookmarks.FindAsync(bookmark.Id);
        if (entity != null)
        {
            entity.UpdateBookmarkName(bookmark.BookmarkName);
            await context.SaveChangesAsync();

            var index = Bookmarks.IndexOf(bookmark);
            if (index != -1)
            {
                Bookmarks[index] = bookmark;
                UpdateCurrentBookmark();
            }

            NotificationController.ShowBookmarkEditSuccess();
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

        NotificationController.ShowBookmarkRemoved();
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

    public bool IsEnabled => !IsConnecting && !IsConnected;
}