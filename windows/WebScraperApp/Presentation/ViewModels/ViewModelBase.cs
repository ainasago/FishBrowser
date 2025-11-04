using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FishBrowser.WPF.Presentation.ViewModels;

/// <summary>
/// ViewModel 基类
/// 实现 INotifyPropertyChanged 接口，支持数据绑定
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发属性变更事件
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 设置属性值并触发变更事件
    /// </summary>
    protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(backingField, value))
            return false;

        backingField = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
