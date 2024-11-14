using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using FikaLauncher.ViewModels;

namespace FikaLauncher.Views;

public partial class MainWindow : Window
{
    private readonly WindowNotificationManager _notificationManager;
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _notificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3,
            Margin = new Thickness(0, 40, 20, 0)
        };

        _viewModel.InitializeNotifications(_notificationManager);
    }
}