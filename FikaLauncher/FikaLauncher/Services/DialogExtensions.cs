using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace FikaLauncher.Services
{
    public static class DialogExtensions
    {
        public static async Task<bool> ShowMessageDialog(string message, string title = "Message")
        {
            var content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = title, FontWeight = Avalonia.Media.FontWeight.Bold },
                    new TextBlock { Text = message },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 10,
                        Children =
                        {
                            new Button { Content = "OK", Tag = true },
                            new Button { Content = "Cancel", Tag = false }
                        }
                    }
                }
            };

            var result = await DialogService.ShowDialog(content);
            return result is bool boolResult && boolResult;
        }

        public static async Task<string?> ShowInputDialog(string message, string title = "Input")
        {
            var textBox = new TextBox();
            var content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = title, FontWeight = Avalonia.Media.FontWeight.Bold },
                    new TextBlock { Text = message },
                    textBox,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 10,
                        Children =
                        {
                            new Button { Content = "OK", Tag = true },
                            new Button { Content = "Cancel", Tag = false }
                        }
                    }
                }
            };

            var result = await DialogService.ShowDialog(content);
            return result is bool boolResult && boolResult ? textBox.Text : null;
        }
    }
}

