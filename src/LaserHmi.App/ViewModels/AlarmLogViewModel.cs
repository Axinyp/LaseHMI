using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaserHmi.App.Models;
using LaserHmi.App.Services;

namespace LaserHmi.App.ViewModels;

/// <summary>
/// 报警与日志页：活动报警列表（确认 / 恢复状态跟踪）与运行日志流
/// （按级别与关键字过滤，支持清空）。
/// </summary>
public sealed partial class AlarmLogViewModel : PageViewModelBase
{
    private readonly ILogService _log;
    private readonly IMachineService _machine;

    public AlarmLogViewModel(ILogService log, IMachineService machine)
    {
        _log = log;
        _machine = machine;

        AlarmListView = CollectionViewSource.GetDefaultView(_log.Alarms);
        AlarmListView.Filter = o => o is AlarmItem alarm && (!OnlyPending || !alarm.IsAcknowledged);

        LogListView = CollectionViewSource.GetDefaultView(_log.Logs);
        LogListView.Filter = FilterLog;

        BadgeCount = _log.ActiveAlarmCount;
        _log.ActiveAlarmCountChanged += (_, _) =>
        {
            BadgeCount = _log.ActiveAlarmCount;
            OnPropertyChanged(nameof(ActiveAlarmCount));
            AcknowledgeAlarmCommand.NotifyCanExecuteChanged();
            AcknowledgeAllCommand.NotifyCanExecuteChanged();
        };
    }

    public override string Title => "报警与日志";

    public override string IconKey => "IconAlarm";

    public ReadOnlyObservableCollection<AlarmItem> Alarms => _log.Alarms;

    /// <summary>
    /// 日志原始集合（View 用于自动滚动订阅）
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> Logs => _log.Logs;

    /// <summary>
    /// 新日志到达时自动滚动到底部
    /// </summary>
    [ObservableProperty]
    private bool autoScroll = true;

    public ICollectionView AlarmListView { get; }

    public ICollectionView LogListView { get; }

    public int ActiveAlarmCount => _log.ActiveAlarmCount;

    /// <summary>
    /// 仅显示未确认报警
    /// </summary>
    [ObservableProperty]
    private bool onlyPending;

    // ---------- 日志过滤 ----------

    public IReadOnlyList<string> LevelOptions { get; } = ["全部级别", "信息及以上", "警告及以上", "仅错误"];

    [ObservableProperty]
    private string selectedLevelOption = "全部级别";

    [ObservableProperty]
    private string searchText = string.Empty;

    partial void OnOnlyPendingChanged(bool value) => AlarmListView.Refresh();

    partial void OnSelectedLevelOptionChanged(string value) => LogListView.Refresh();

    partial void OnSearchTextChanged(string value) => LogListView.Refresh();

    private bool FilterLog(object obj)
    {
        if (obj is not LogEntry entry)
        {
            return false;
        }

        bool levelPassed = SelectedLevelOption switch
        {
            "信息及以上" => true,
            "警告及以上" => entry.Level >= LogLevel.Warning,
            "仅错误" => entry.Level == LogLevel.Error,
            _ => true
        };

        if (!levelPassed)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        string query = SearchText.Trim();
        return entry.Message.Contains(query, StringComparison.OrdinalIgnoreCase)
               || entry.Source.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- 报警操作 ----------

    [RelayCommand(CanExecute = nameof(CanAcknowledgeAlarm))]
    private void AcknowledgeAlarm(AlarmItem? item)
    {
        if (item is null || item.IsAcknowledged)
        {
            return;
        }

        _log.AcknowledgeAlarm(item);
        _machine.EvaluateAlarmRecovery();
    }

    private bool CanAcknowledgeAlarm() => ActiveAlarmCount > 0;

    [RelayCommand(CanExecute = nameof(CanAcknowledgeAll))]
    private void AcknowledgeAll() => _machine.AcknowledgeAlarms();

    private bool CanAcknowledgeAll() => ActiveAlarmCount > 0;

    // ---------- 日志操作 ----------

    [RelayCommand]
    private void ClearLogs() => _log.ClearLogs();
}
