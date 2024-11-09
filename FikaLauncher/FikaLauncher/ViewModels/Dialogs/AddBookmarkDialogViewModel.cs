using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FikaLauncher.Database;
using FikaLauncher.Services;
using Jeek.Avalonia.Localization;
using Microsoft.EntityFrameworkCore;

namespace FikaLauncher.ViewModels.Dialogs;

public partial class AddBookmarkDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _serverAddress;
    
    [ObservableProperty]
    private string _serverPort;
    
    [ObservableProperty]
    private string _bookmarkName = string.Empty;

    public AddBookmarkDialogViewModel(string serverAddress, string serverPort, string? bookmarkName = null)
    {
        ServerAddress = serverAddress;
        ServerPort = serverPort;
        BookmarkName = bookmarkName;
    }

    [RelayCommand]
    private async Task Save()
    {
        using var context = new AppDbContext();
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == AuthService.CurrentUsername);
            
        if (user == null)
        {
            ShowNotification(Localizer.Get("Error"), 
                Localizer.Get("UserNotFound"), 
                NotificationType.Error);
            return;
        }

        var existingBookmark = await context.ServerBookmarks
            .FirstOrDefaultAsync(b => 
                b.UserId == user.Id && 
                b.ServerAddress == ServerAddress && 
                b.ServerPort == int.Parse(ServerPort));

        if (existingBookmark != null)
        {
            existingBookmark.BookmarkName = !string.IsNullOrWhiteSpace(BookmarkName) ? BookmarkName.Trim() : null;
            
            if (!existingBookmark.IsValid())
            {
                ShowNotification(Localizer.Get("Error"), 
                    Localizer.Get("InvalidBookmarkData"), 
                    NotificationType.Error);
                return;
            }

            try
            {
                await context.SaveChangesAsync();
                ShowNotification(Localizer.Get("Success"), 
                    Localizer.Get("BookmarkUpdated"), 
                    NotificationType.Success);
                DialogService.CloseDialog(existingBookmark);
            }
            catch (DbUpdateException)
            {
                ShowNotification(Localizer.Get("Error"), 
                    Localizer.Get("BookmarkUpdateFailed"), 
                    NotificationType.Error);
            }
            return;
        }

        var newBookmark = new ServerBookmarkEntity
        {
            UserId = user.Id,
            ServerAddress = ServerAddress,
            ServerPort = int.Parse(ServerPort),
            BookmarkName = !string.IsNullOrWhiteSpace(BookmarkName) ? BookmarkName.Trim() : null
        };

        if (!newBookmark.IsValid())
        {
            ShowNotification(Localizer.Get("Error"), 
                Localizer.Get("InvalidBookmarkData"), 
                NotificationType.Error);
            return;
        }

        try
        {
            context.ServerBookmarks.Add(newBookmark);
            await context.SaveChangesAsync();
            ShowNotification(Localizer.Get("Success"), 
                Localizer.Get("BookmarkAdded"), 
                NotificationType.Success);
            DialogService.CloseDialog(newBookmark);
        }
        catch (DbUpdateException)
        {
            ShowNotification(Localizer.Get("Error"), 
                Localizer.Get("BookmarkAlreadyExists"), 
                NotificationType.Error);
        }
    }

    private void ShowNotification(string title, string message, NotificationType type)
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.ShowNotification(title, message, type);
            }
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogService.CloseDialog(null);
    }
}
