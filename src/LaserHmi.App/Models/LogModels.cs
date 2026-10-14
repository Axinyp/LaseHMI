namespace LaserHmi.App.Models;

/// <summary>
/// 运行日志条目（只读值对象，写入后不可变）
/// </summary>
public sealed class LogEntry
{
    public required DateTime Time { get; init; }

    public required LogLevel Level { get; init; }

    /// <summary>
    /// 来源模块，如“激光器 / 运动系统 / 操作 / 任务”
    /// </summary>
    public required string Source { get; init; }

    public required string Message { get; init; }

    public string TimeText => Time.ToString("HH:mm:ss.fff");
}

/// <summary>
/// 报警条目（活动报警 = 未确认的报警）
/// </summary>
public sealed partial class AlarmItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public required long Id { get; init; }

    /// <summary>
    /// 报警代码，如 L-2001
    /// </summary>
    public required string Code { get; init; }

    public required DateTime Time { get; init; }

    public required AlarmSeverity Severity { get; init; }

    /// <summary>
    /// 来源模块
    /// </summary>
    public required string Source { get; init; }

    public required string Message { get; init; }

    /// <summary>
    /// 是否已被操作员确认
    /// </summary>
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool isAcknowledged;

    /// <summary>
    /// 确认时间（确认后写入）
    /// </summary>
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private DateTime? acknowledgedTime;

    /// <summary>
    /// 异常是否已自行恢复（如温度回落），供操作员确认后恢复生产
    /// </summary>
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool isRecovered;

    public string TimeText => Time.ToString("MM-dd HH:mm:ss");
}
