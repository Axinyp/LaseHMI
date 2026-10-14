using System.Collections.ObjectModel;
using LaserHmi.App.Models;

namespace LaserHmi.App.Services;

/// <summary>
/// 日志与报警服务实现（内存环形日志，容量上限防止长时间运行内存增长）
/// </summary>
public sealed class LogService : ILogService
{
    private const int MaxLogEntries = 2000;

    private readonly ObservableCollection<LogEntry> _logs = [];
    private readonly ObservableCollection<AlarmItem> _alarms = [];
    private long _alarmSequence;

    public ReadOnlyObservableCollection<LogEntry> Logs { get; }

    public ReadOnlyObservableCollection<AlarmItem> Alarms { get; }

    public int ActiveAlarmCount => _alarms.Count(a => !a.IsAcknowledged);

    public event EventHandler? ActiveAlarmCountChanged;

    public LogService()
    {
        Logs = new ReadOnlyObservableCollection<LogEntry>(_logs);
        Alarms = new ReadOnlyObservableCollection<AlarmItem>(_alarms);
    }

    public void Write(LogLevel level, string source, string message)
    {
        _logs.Add(new LogEntry
        {
            Time = DateTime.Now,
            Level = level,
            Source = source,
            Message = message
        });

        while (_logs.Count > MaxLogEntries)
        {
            _logs.RemoveAt(0);
        }
    }

    public void Info(string source, string message) => Write(LogLevel.Info, source, message);

    public void Warning(string source, string message) => Write(LogLevel.Warning, source, message);

    public void Error(string source, string message) => Write(LogLevel.Error, source, message);

    public AlarmItem RaiseAlarm(AlarmSeverity severity, string code, string source, string message)
    {
        var item = new AlarmItem
        {
            Id = ++_alarmSequence,
            Code = code,
            Time = DateTime.Now,
            Severity = severity,
            Source = source,
            Message = message
        };

        _alarms.Insert(0, item);
        Write(severity >= AlarmSeverity.Error ? LogLevel.Error : LogLevel.Warning,
            source, $"报警 [{code}] {message}");

        ActiveAlarmCountChanged?.Invoke(this, EventArgs.Empty);
        return item;
    }

    public void AcknowledgeAlarm(AlarmItem item)
    {
        if (item.IsAcknowledged)
        {
            return;
        }

        item.IsAcknowledged = true;
        item.AcknowledgedTime = DateTime.Now;
        Write(LogLevel.Info, "操作", $"报警已确认 [{item.Code}]");
        ActiveAlarmCountChanged?.Invoke(this, EventArgs.Empty);
    }

    public void MarkRecovered(AlarmItem item)
    {
        if (!item.IsRecovered)
        {
            item.IsRecovered = true;
        }
    }

    public void ClearLogs() => _logs.Clear();
}
