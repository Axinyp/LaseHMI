using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaserHmi.App.Models;
using LaserHmi.App.Services;

namespace LaserHmi.App.ViewModels;

/// <summary>
/// 设备操作命令聚合：连接 / 回零 / 启动 / 暂停 / 继续 / 停止 / 急停 / 复位。
/// 各页面共享同一实例，保证按钮可用性与操作分级规则全局一致：
/// 普通操作直接执行；重要与危险操作需二次确认；急停立即生效（安全优先）。
/// </summary>
public sealed partial class DeviceOperationsViewModel : ObservableObject
{
    private readonly IMachineService _machine;
    private readonly ILogService _log;
    private readonly IDialogService _dialogs;
    private readonly IParameterService _params;

    public DeviceOperationsViewModel(
        IMachineService machine, ILogService log, IDialogService dialogs, IParameterService parameters)
    {
        _machine = machine;
        _log = log;
        _dialogs = dialogs;
        _params = parameters;

        _machine.Status.PropertyChanged += (_, _) => RefreshCommands();
        _log.ActiveAlarmCountChanged += (_, _) => RefreshCommands();
    }

    public MachineStatus Status => _machine.Status;

    /// <summary>
    /// 启动任务件数（工作区页可调整，各页共享）
    /// </summary>
    [ObservableProperty]
    private int taskPartCount = 5;

    /// <summary>
    /// 当前参数匹配的预设名称（用于任务记录与确认提示）
    /// </summary>
    public string CurrentPresetLabel =>
        _params.Presets.FirstOrDefault(_params.MatchesPreset)?.Name ?? "自定义参数";

    // ---------- 通讯 ----------

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private void Connect() => _machine.Connect();

    private bool CanConnect() => Status.Connection == ConnectionState.Offline;

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private void Disconnect() => _machine.Disconnect();

    private bool CanDisconnect() => Status.Connection == ConnectionState.Online
        && Status.State is MachineState.Idle or MachineState.Ready or MachineState.EStopped;

    // ---------- 常规操作 ----------

    [RelayCommand(CanExecute = nameof(CanHome))]
    private void Home() => _machine.Home();

    private bool CanHome() => Status.Connection == ConnectionState.Online
        && Status.State is MachineState.Idle or MachineState.Ready;

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause() => _machine.Pause();

    private bool CanPause() => Status.State == MachineState.Marking;

    [RelayCommand(CanExecute = nameof(CanResume))]
    private void Resume() => _machine.Resume();

    private bool CanResume() => Status.State == MachineState.Paused;

    // ---------- 重要操作（二次确认） ----------

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        bool confirmed = _dialogs.Confirm(
            "启动加工任务",
            $"即将启动加工任务：\n\n" +
            $"• 加工件数：{TaskPartCount} 件\n" +
            $"• 参数预设：{CurrentPresetLabel}\n" +
            $"• 启动后设备将自动执行打标流程，请确认工件已装夹、防护门已关闭。",
            "启动加工");

        if (confirmed)
        {
            _machine.StartTask(TaskPartCount, CurrentPresetLabel);
        }
    }

    private bool CanStart() => Status.State == MachineState.Ready;

    // ---------- 危险操作（红色 + 二次确认） ----------

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        bool confirmed = _dialogs.Confirm(
            "停止加工任务",
            "停止操作将终止当前加工任务，未完成部分作废且无法恢复。\n\n确认停止当前任务？",
            "停止任务",
            danger: true);

        if (confirmed)
        {
            _machine.Stop();
        }
    }

    private bool CanStop() => Status.State is MachineState.Marking or MachineState.Paused or MachineState.Alarm;

    /// <summary>
    /// 紧急停止：立即生效，不弹确认（安全优先，按钮具备防误触设计）
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEmergencyStop))]
    private void EmergencyStop() => _machine.EmergencyStop();

    private bool CanEmergencyStop() => Status.Connection == ConnectionState.Online
        && Status.State != MachineState.EStopped;

    [RelayCommand(CanExecute = nameof(CanResetEmergency))]
    private void ResetEmergency() => _machine.ResetEmergency();

    private bool CanResetEmergency() => Status.State == MachineState.EStopped
        && _log.ActiveAlarmCount == 0;

    // ---------- 报警 ----------

    [RelayCommand(CanExecute = nameof(CanAcknowledgeAlarms))]
    private void AcknowledgeAlarms() => _machine.AcknowledgeAlarms();

    private bool CanAcknowledgeAlarms() => _log.ActiveAlarmCount > 0;

    private void RefreshCommands()
    {
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        HomeCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        ResumeCommand.NotifyCanExecuteChanged();
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        EmergencyStopCommand.NotifyCanExecuteChanged();
        ResetEmergencyCommand.NotifyCanExecuteChanged();
        AcknowledgeAlarmsCommand.NotifyCanExecuteChanged();
    }
}
