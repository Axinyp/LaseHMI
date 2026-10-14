using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaserHmi.App.Models;
using LaserHmi.App.Services;

namespace LaserHmi.App.ViewModels;

/// <summary>
/// 主窗口 ViewModel：页面导航、顶栏状态与系统时钟
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IMachineService _machine;
    private readonly ILogService _log;
    private readonly AlarmLogViewModel _alarmLog;

    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public MainViewModel(
        IMachineService machine,
        ILogService log,
        DeviceOperationsViewModel operations,
        DashboardViewModel dashboard,
        WorkbenchViewModel workbench,
        ParametersViewModel parameters,
        DeviceControlViewModel control,
        AlarmLogViewModel alarmLog)
    {
        _machine = machine;
        _log = log;
        _alarmLog = alarmLog;

        Operations = operations;
        Pages = [dashboard, workbench, parameters, control, alarmLog];
        SelectedPage = dashboard;

        _log.ActiveAlarmCountChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ActiveAlarmCount));
            OnPropertyChanged(nameof(HasActiveAlarms));
        };

        _clockTimer.Tick += (_, _) => ClockText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _clockTimer.Start();
        ClockText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public IReadOnlyList<PageViewModelBase> Pages { get; }

    public DeviceOperationsViewModel Operations { get; }

    public MachineStatus Status => _machine.Status;

    [ObservableProperty]
    private PageViewModelBase? selectedPage;

    [ObservableProperty]
    private string clockText = string.Empty;

    public int ActiveAlarmCount => _log.ActiveAlarmCount;

    public bool HasActiveAlarms => ActiveAlarmCount > 0;

    /// <summary>
    /// 顶栏报警铃：跳转到报警与日志页
    /// </summary>
    [RelayCommand]
    private void GoToAlarms() => SelectedPage = _alarmLog;
}
