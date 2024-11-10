using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace FikaLauncher.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}