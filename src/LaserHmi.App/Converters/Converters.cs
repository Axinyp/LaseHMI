using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LaserHmi.App.Models;

namespace LaserHmi.App.Converters;

/// <summary>
/// 连接状态 → 显示文本
/// </summary>
public sealed class ConnectionTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ConnectionState.Offline => "未连接",
        ConnectionState.Connecting => "连接中…",
        ConnectionState.Online => "已连接",
        _ => "-"
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>
/// 连接状态 → 状态点颜色
/// </summary>
public sealed class ConnectionBrushConverter : IValueConverter
{
    private static readonly Brush Online = Frozen("#46D08A");
    private static readonly Brush Connecting = Frozen("#FFB454");
    private static readonly Brush Offline = Frozen("#FF5D5D");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ConnectionState.Online => Online,
        ConnectionState.Connecting => Connecting,
        _ => Offline
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// 设备主状态 → 显示文本
/// </summary>
public sealed class MachineStateTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        MachineState.Offline => "离线",
        MachineState.Idle => "待机",
        MachineState.Homing => "回零中",
        MachineState.Ready => "就绪",
        MachineState.Marking => "加工中",
        MachineState.Paused => "已暂停",
        MachineState.Alarm => "报警",
        MachineState.EStopped => "急停",
        _ => "-"
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>
/// 设备主状态 → 语义色（正常绿 / 运行蓝 / 暂停黄 / 异常红 / 离线灰）
/// </summary>
public sealed class MachineStateBrushConverter : IValueConverter
{
    private static readonly Brush Ready = Frozen("#46D08A");
    private static readonly Brush Running = Frozen("#38C3FF");
    private static readonly Brush Paused = Frozen("#FFB454");
    private static readonly Brush Fault = Frozen("#FF5D5D");
    private static readonly Brush Idle = Frozen("#61748F");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        MachineState.Ready => Ready,
        MachineState.Marking or MachineState.Homing => Running,
        MachineState.Paused => Paused,
        MachineState.Alarm or MachineState.EStopped => Fault,
        _ => Idle
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// 日志级别 → 文本颜色
/// </summary>
public sealed class LogLevelBrushConverter : IValueConverter
{
    private static readonly Brush Info = Frozen("#9AAEC8");
    private static readonly Brush Warning = Frozen("#FFB454");
    private static readonly Brush Error = Frozen("#FF5D5D");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        LogLevel.Warning => Warning,
        LogLevel.Error => Error,
        _ => Info
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// 日志级别 → 徽章底色
/// </summary>
public sealed class LogLevelBadgeBrushConverter : IValueConverter
{
    private static readonly Brush Info = Frozen("#1A2740");
    private static readonly Brush Warning = Frozen("#3A2A12");
    private static readonly Brush Error = Frozen("#3A161C");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        LogLevel.Warning => Warning,
        LogLevel.Error => Error,
        _ => Info
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// 报警严重度 → 显示文本
/// </summary>
public sealed class AlarmSeverityTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        AlarmSeverity.Info => "提示",
        AlarmSeverity.Warning => "警告",
        AlarmSeverity.Error => "错误",
        AlarmSeverity.Critical => "严重",
        _ => "-"
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>
/// 报警严重度 → 语义色
/// </summary>
public sealed class AlarmSeverityBrushConverter : IValueConverter
{
    private static readonly Brush Info = Frozen("#38C3FF");
    private static readonly Brush Warning = Frozen("#FFB454");
    private static readonly Brush Error = Frozen("#FF5D5D");
    private static readonly Brush Critical = Frozen("#FF4D6A");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        AlarmSeverity.Warning => Warning,
        AlarmSeverity.Error => Error,
        AlarmSeverity.Critical => Critical,
        _ => Info
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// 布尔取反（用于反向启用/可见性判断）
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
}

/// <summary>
/// 布尔取反 → 可见性（true 时隐藏）
/// </summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>
/// 图标资源键 → Geometry（导航项图标绑定用，从应用资源字典解析）
/// </summary>
public sealed class IconKeyToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key && Application.Current?.TryFindResource(key) is Geometry geometry
            ? geometry
            : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>
/// 日志级别 → 中文文本
/// </summary>
public sealed class LogLevelTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        LogLevel.Warning => "警告",
        LogLevel.Error => "错误",
        _ => "信息"
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>
/// 设备主状态 → 操作引导提示语
/// </summary>
public sealed class MachineStateHintConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        MachineState.Offline => "上位机与设备未建立通讯，请先连接设备",
        MachineState.Idle => "设备已连接，请执行回零以建立坐标系",
        MachineState.Homing => "三轴回零执行中，请稍候…",
        MachineState.Ready => "设备就绪，可启动加工任务",
        MachineState.Marking => "加工任务执行中，请勿打开防护门",
        MachineState.Paused => "任务已暂停，可继续加工或停止任务",
        MachineState.Alarm => "存在报警，请处理并在报警页确认后恢复",
        MachineState.EStopped => "急停已触发，请确认报警后复位并重新回零",
        _ => string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>
/// 布尔 → 中文文本（ConverterParameter 形如 "真文本|假文本"，默认 "是|否"）
/// </summary>
public sealed class BoolToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = value is true;
        string[] parts = (parameter as string ?? "是|否").Split('|');
        return flag ? parts[0] : parts.Length > 1 ? parts[1] : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
