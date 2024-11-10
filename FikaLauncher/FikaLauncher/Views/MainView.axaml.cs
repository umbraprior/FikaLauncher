using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using FikaLauncher.ViewModels;

namespace FikaLauncher.Views;

public partial class MainView : UserControl
{
    private WindowNotificationManager? _notificationManager;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null) _notificationManager = new WindowNotificationManager(topLevel) { MaxItems = 3 };
    }

    public void ShowNotification(string title, string message, NotificationType type)
    {
        _notificationManager?.Show(new Notification(title, message, type));
    }

    private void SplitView_PaneClosing(object sender, CancelRoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel) viewModel.UpdatePaneState(false);
    }
}