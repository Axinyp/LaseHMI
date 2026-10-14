using System.Collections.ObjectModel;
using LaserHmi.App.Models;

namespace LaserHmi.App.Services;

/// <summary>
/// 日志与报警服务：运行日志记录、报警产生/确认/恢复跟踪
/// </summary>
public interface ILogService
{
    ReadOnlyObservableCollection<LogEntry> Logs { get; }

    /// <summary>
    /// 全部报警（新报警插入顶部）
    /// </summary>
    ReadOnlyObservableCollection<AlarmItem> Alarms { get; }

    /// <summary>
    /// 未确认报警数量（导航徽标与顶栏铃铛使用）
    /// </summary>
    int ActiveAlarmCount { get; }

    /// <summary>
    /// 未确认报警数量变化通知
    /// </summary>
    event EventHandler? ActiveAlarmCountChanged;

    void Write(LogLevel level, string source, string message);

    void Info(string source, string message);

    void Warning(string source, string message);

    void Error(string source, string message);

    /// <summary>
    /// 产生报警并同步写入日志，返回报警条目
    /// </summary>
    AlarmItem RaiseAlarm(AlarmSeverity severity, string code, string source, string message);

    /// <summary>
    /// 操作员确认报警
    /// </summary>
    void AcknowledgeAlarm(AlarmItem item);

    /// <summary>
    /// 标记报警对应异常已自行恢复（如温度回落）
    /// </summary>
    void MarkRecovered(AlarmItem item);

    /// <summary>
    /// 清空运行日志（不影响报警记录）
    /// </summary>
    void ClearLogs();
}
