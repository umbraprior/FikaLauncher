using Avalonia.Controls;
using System.Threading.Tasks;
using DialogHostAvalonia;
using System;
using FikaLauncher.ViewModels;

namespace FikaLauncher.Services
{
    public static class DialogService
    {
        public static async Task<object?> ShowDialog(object content, string dialogIdentifier = "RootDialog")
        {
            return await DialogHost.Show(content, dialogIdentifier);
        }

        public static async Task<object?> ShowDialog<TView>(object viewModel, string dialogIdentifier = "RootDialog") where TView : Control, new()
        {
            var viewType = typeof(TView);
            var viewNamespace = viewType.Namespace;
            var viewAssembly = viewType.Assembly;

            // Check if the view is in the Dialogs subfolder
            var dialogViewType = viewAssembly.GetType($"{viewNamespace}.Dialogs.{viewType.Name}");
            
            Control view;
            if (dialogViewType != null)
            {
                view = (Control)Activator.CreateInstance(dialogViewType)!;
            }
            else
            {
                // Fallback to the original behavior if not found in Dialogs subfolder
                view = new TView();
            }

            view.DataContext = viewModel;
            return await DialogHost.Show(view, dialogIdentifier);
        }

        public static void CloseDialog(object? parameter = null, string dialogIdentifier = "RootDialog")
        {
            DialogHost.Close(dialogIdentifier, parameter);
        }

        public static async Task<T?> ShowNestedDialog<T>(ViewModelBase viewModel, string identifier = "RootDialog")
        {
            if (DialogHost.GetDialogSession(identifier)?.Content is ViewModelBase currentViewModel)
            {
                // Store current dialog content
                var currentContent = DialogHost.GetDialogSession(identifier)?.Content;
                
                // Show new dialog
                var result = await DialogHost.Show(viewModel, identifier);
                
                // Restore previous dialog content
                await DialogHost.Show(currentContent, identifier);
                
                return (T?)result;
            }
            
            return default;
        }
    }
}
