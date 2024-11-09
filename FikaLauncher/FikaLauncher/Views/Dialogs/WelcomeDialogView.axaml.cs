using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FikaLauncher.ViewModels.Dialogs;
using Avalonia;
using System;

namespace FikaLauncher.Views.Dialogs;

public partial class TermsDialogView : UserControl
{
    private ScrollViewer? _termsScroller;
    
    public TermsDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _termsScroller = this.Get<ScrollViewer>("TermsScroller");
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer && DataContext is TermsDialogViewModel viewModel)
        {
            var isAtEnd = scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height - 1;
            viewModel.HasScrolledToEnd = isAtEnd;
        }
    }

    public void ResetScroll()
    {
        if (_termsScroller != null)
        {
            _termsScroller.Offset = new Vector(0, 0);
            Console.WriteLine("Scroll position reset");
        }
    }
}
