using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace FikaLauncher.Views;

public partial class MainWindow : Window
{
    public WindowNotificationManager? NotificationManager { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        SetupNotificationManager();
    }

    private void SetupNotificationManager()
    {
        NotificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3,
            Margin = new Avalonia.Thickness(0, 40, 0, 0)
        };
    }
}
