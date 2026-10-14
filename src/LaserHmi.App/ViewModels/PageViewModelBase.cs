using CommunityToolkit.Mvvm.ComponentModel;

namespace LaserHmi.App.ViewModels;

/// <summary>
/// 页面 ViewModel 基类：导航元数据（标题 / 图标 / 徽标）
/// </summary>
public abstract partial class PageViewModelBase : ObservableObject
{
    /// <summary>
    /// 导航标题
    /// </summary>
    public abstract string Title { get; }

    /// <summary>
    /// 图标资源键（对应 Themes/Icons.xaml 中的 Geometry）
    /// </summary>
    public abstract string IconKey { get; }

    /// <summary>
    /// 导航项徽标数字（0 时不显示，报警页使用）
    /// </summary>
    [ObservableProperty]
    private int badgeCount;
}
