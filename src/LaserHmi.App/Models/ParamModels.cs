using CommunityToolkit.Mvvm.ComponentModel;

namespace LaserHmi.App.Models;

/// <summary>
/// 单个可编辑工艺参数。界面通过 DataTemplate 统一渲染（名称 + 滑杆 + 数值 + 单位）。
/// </summary>
public partial class ParamItem : ObservableObject
{
    public required string Name { get; init; }

    public required string Key { get; init; }

    public required double Min { get; init; }

    public required double Max { get; init; }

    /// <summary>
    /// 滑杆步进
    /// </summary>
    public double Step { get; init; } = 1;

    public required string Unit { get; init; }

    /// <summary>
    /// 小数位（数值显示用）
    /// </summary>
    public int Decimals { get; init; } = 0;

    /// <summary>
    /// 参数含义说明（悬停提示）
    /// </summary>
    public string Description { get; init; } = string.Empty;

    [ObservableProperty]
    private double value;

    public string ValueText => Value.ToString("F" + Decimals);

    partial void OnValueChanged(double value) => OnPropertyChanged(nameof(ValueText));
}

/// <summary>
/// 工艺参数预设（激光 + 运动两组参数的快照）
/// </summary>
public sealed class ParameterPreset
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    /// <summary>
    /// 参数键 → 值
    /// </summary>
    public required Dictionary<string, double> Values { get; init; }
}
