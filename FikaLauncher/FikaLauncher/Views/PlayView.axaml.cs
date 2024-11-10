using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FikaLauncher.Database;
using FikaLauncher.ViewModels;

namespace FikaLauncher.Views;

public partial class PlayView : UserControl
{
    public PlayView()
    {
        InitializeComponent();
        if (DataContext is PlayViewModel viewModel) viewModel.SetView(this);
    }

    private void OnBookmarkSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems?.Count > 0 && e.AddedItems[0] is ServerBookmarkEntity bookmark)
            if (DataContext is PlayViewModel viewModel)
                viewModel.SelectBookmarkCommand.Execute(bookmark);

        if (sender is ListBox listBox) listBox.SelectedItem = null;
    }

    private void OnListBoxItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(null);
        if (point.Properties.IsRightButtonPressed)
            if (e.Source is Control control)
            {
                var listBoxItem = control.FindAncestorOfType<ListBoxItem>();
                if (listBoxItem != null)
                {
                    e.Handled = true;
                    listBoxItem.IsSelected = false;
                }
            }
    }

    private void OnBookmarkEditLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox &&
            textBox.DataContext is ServerBookmarkEntity bookmark &&
            DataContext is PlayViewModel viewModel)
            viewModel.CancelBookmarkEditCommand.Execute(bookmark);
    }

    private void OnBookmarkEditStarted(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Grid grid &&
            grid.DataContext is ServerBookmarkEntity bookmark &&
            bookmark.ShouldFocus)
        {
            bookmark.ShouldFocus = false;
            Dispatcher.UIThread.Post(() =>
            {
                var textBox = grid.FindDescendantOfType<TextBox>();
                if (textBox != null)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            }, DispatcherPriority.Render);
        }
    }

    private void OnBookmarkEditTextBoxVisibilityChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is TextBox textBox &&
            textBox.IsVisible &&
            textBox.DataContext is ServerBookmarkEntity bookmark &&
            bookmark.IsEditing)
            Dispatcher.UIThread.Post(() =>
            {
                textBox.Focus();
                textBox.CaretIndex = textBox.Text?.Length ?? 0;
            }, DispatcherPriority.Render);
    }

    private void OnBookmarkFlyoutOpening(object? sender, EventArgs e)
    {
        if (DataContext is PlayViewModel viewModel) viewModel.OnBookmarkFlyoutOpening();
    }

    private void OnBookmarkButtonLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && DataContext is PlayViewModel viewModel) viewModel.SetBookmarkButton(button);
    }

    private void OnFlyoutInteraction(object? sender, PointerEventArgs e)
    {
        if (DataContext is PlayViewModel viewModel) viewModel.ResetFlyoutTimer();
    }

    private void OnFlyoutInteraction(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is PlayViewModel viewModel) viewModel.ResetFlyoutTimer();
    }
}