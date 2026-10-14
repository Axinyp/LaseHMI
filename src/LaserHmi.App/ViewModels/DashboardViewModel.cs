using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using LaserHmi.App.Models;
using LaserHmi.App.Services;

namespace LaserHmi.App.ViewModels;

/// <summary>
/// 设备总览页：状态卡片、当前任务进度环、实时趋势与最近事件
/// </summary>
public sealed partial class DashboardViewModel : PageViewModelBase
{
    private const int RecentEventCount = 7;

    private readonly IMachineService _machine;
    private readonly ILogService _log;

    public DashboardViewModel(IMachineService machine, ILogService log, DeviceOperationsViewModel operations)
    {
        _machine = machine;
        _log = log;
        Operations = operations;

        RebuildRecentEvents();
        ((INotifyCollectionChanged)_log.Logs).CollectionChanged += (_, _) => RebuildRecentEvents();
    }

    public override string Title => "设备总览";

    public override string IconKey => "IconDashboard";

    public MachineStatus Status => _machine.Status;

    public DeviceOperationsViewModel Operations { get; }

    public ObservableCollection<double> PowerTrend => _machine.PowerTrend;

    public ObservableCollection<double> TemperatureTrend => _machine.TemperatureTrend;

    /// <summary>
    /// 最近事件（最近若干条日志，倒序）
    /// </summary>
    public ObservableCollection<LogEntry> RecentEvents { get; } = [];

    private void RebuildRecentEvents()
    {
        RecentEvents.Clear();
        foreach (var entry in _log.Logs.Reverse().Take(RecentEventCount))
        {
            RecentEvents.Add(entry);
        }
    }
}
