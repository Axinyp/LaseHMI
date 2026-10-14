using System.Collections.ObjectModel;
using System.Collections.Specialized;
using LaserHmi.App.Models;
using LaserHmi.App.Services;

namespace LaserHmi.App.ViewModels;

/// <summary>
/// 设备控制页：按操作风险分层展示操作面板（常规 / 重要 / 危险 / 紧急停止），
/// 右侧显示联锁状态与最近操作记录，帮助操作员理解每一步的后果。
/// </summary>
public sealed class DeviceControlViewModel : PageViewModelBase
{
    private const int RecentActionCount = 6;

    private readonly IMachineService _machine;
    private readonly ILogService _log;

    public DeviceControlViewModel(
        IMachineService machine, ILogService log, DeviceOperationsViewModel operations)
    {
        _machine = machine;
        _log = log;
        Operations = operations;

        RebuildRecentActions();
        ((INotifyCollectionChanged)_log.Logs).CollectionChanged += (_, _) => RebuildRecentActions();
    }

    public override string Title => "设备控制";

    public override string IconKey => "IconControl";

    public MachineStatus Status => _machine.Status;

    public DeviceOperationsViewModel Operations { get; }

    /// <summary>
    /// 最近操作 / 任务事件
    /// </summary>
    public ObservableCollection<LogEntry> RecentActions { get; } = [];

    private void RebuildRecentActions()
    {
        RecentActions.Clear();
        foreach (var entry in _log.Logs
                     .Where(e => e.Source is "操作" or "任务" or "安全系统")
                     .Reverse()
                     .Take(RecentActionCount))
        {
            RecentActions.Add(entry);
        }
    }
}
