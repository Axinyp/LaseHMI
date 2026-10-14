using System.Collections.ObjectModel;
using System.Windows.Threading;
using LaserHmi.App.Models;

namespace LaserHmi.App.Services;

/// <summary>
/// 设备服务模拟实现：以 100ms 定时器驱动一套设备状态机，
/// 模拟连接握手、三轴回零、打标加工、过温保护与急停等过程，
/// 使 HMI 在没有真实硬件时具备完整的业务闭环（不含真实加工算法）。
/// </summary>
public sealed class MockMachineService : IMachineService
{
    // ---------- 模拟参数（工程真值经演示放大，便于观察流程） ----------

    private const double TickSeconds = 0.1;

    /// <summary>
    /// 演示倍率：真实打标节奏（亚秒级）放大 20 倍，保留流程观赏性
    /// </summary>
    private const double DemoSpeedFactor = 20.0;

    private const double MinPartSeconds = 8.0;
    private const double MaxPartSeconds = 40.0;

    /// <summary>
    /// 件间定位 / 上下料时间（秒）
    /// </summary>
    private const double InterPartDelaySeconds = 1.6;

    private const double HomeDurationSeconds = 2.5;
    private const double ConnectDurationSeconds = 1.2;

    private const double CoolAmbientTemp = 30.0;

    /// <summary>
    /// 温度回落至该值以下视为“异常已恢复”，允许继续加工
    /// </summary>
    private const double ResumeSafeTemp = 55.0;

    /// <summary>
    /// 基准升温速率（°C/s，功率 60% 时）
    /// </summary>
    private const double HeatRatePerSecond = 0.55;

    /// <summary>
    /// 每完成一件的温升台阶（°C）
    /// </summary>
    private const double PartFinishHeat = 3.0;

    /// <summary>
    /// 温度预警线（一次性提示）
    /// </summary>
    private const double TempWarnLine = 57.0;

    private const int TrendCapacity = 72;

    // ---------- 依赖 ----------

    private readonly ILogService _log;
    private readonly IParameterService _params;
    private readonly Random _rng = new();

    private readonly DispatcherTimer _timer = new()
    {
        Interval = TimeSpan.FromMilliseconds(TickSeconds * 1000)
    };

    // ---------- 过程状态 ----------

    private double _connectRemaining;
    private int _connectStep;

    private double _homeRemaining;
    private double _homeFromX, _homeFromY, _homeFromZ;

    private double _partElapsed;
    private double _partDuration = 12;
    private double _interDelayRemaining;

    private int _trendDivider;
    private bool _tempWarned;
    private AlarmItem? _tempAlarm;

    public MachineStatus Status { get; } = new();

    public MarkingPath Path { get; } = MarkingPath.CreateDemo();

    public ObservableCollection<double> PowerTrend { get; } = [];

    public ObservableCollection<double> TemperatureTrend { get; } = [];

    public MockMachineService(ILogService log, IParameterService parameters)
    {
        _log = log;
        _params = parameters;

        _timer.Tick += (_, _) => OnTick();
        _timer.Start();

        _log.Info("系统", "激光打标上位机已启动（Mock 模拟模式，未连接硬件）");
    }

    // ---------- 对外命令 ----------

    public void Connect()
    {
        if (Status.Connection != ConnectionState.Offline)
        {
            return;
        }

        Status.Connection = ConnectionState.Connecting;
        _connectRemaining = ConnectDurationSeconds;
        _connectStep = 0;
    }

    public void Disconnect()
    {
        if (Status.Connection != ConnectionState.Online)
        {
            return;
        }

        if (Status.State is MachineState.Marking or MachineState.Paused
            or MachineState.Alarm or MachineState.Homing)
        {
            _log.Warning("操作", "断开连接被拒绝：请先停止加工或等待流程结束");
            return;
        }

        Status.Connection = ConnectionState.Offline;
        Status.State = MachineState.Offline;
        Status.Motion.Homed = false;
        Status.Laser.IsEnabled = false;
        Status.Laser.ActualPower = 0;
        Status.Motion.Speed = 0;
        Status.Task = null;
        _log.Info("通讯", "已断开与设备的连接");
    }

    public void Home()
    {
        if (Status.Connection != ConnectionState.Online)
        {
            return;
        }

        if (Status.State is not (MachineState.Idle or MachineState.Ready))
        {
            _log.Warning("操作", "回零被拒绝：当前状态不允许执行回零");
            return;
        }

        Status.State = MachineState.Homing;
        Status.Laser.IsEnabled = false;
        _homeRemaining = HomeDurationSeconds;
        _homeFromX = Status.Motion.X;
        _homeFromY = Status.Motion.Y;
        _homeFromZ = Status.Motion.Z;
        _log.Info("运动系统", "开始三轴回零 …");
    }

    public bool StartTask(int totalParts, string presetName)
    {
        if (Status.Connection != ConnectionState.Online || Status.State != MachineState.Ready)
        {
            _log.Warning("操作", "启动被拒绝：设备未处于就绪状态");
            return false;
        }

        var blocking = _log.Alarms.FirstOrDefault(a => !a.IsAcknowledged && a.Severity >= AlarmSeverity.Error);
        if (blocking is not null)
        {
            _log.Warning("操作", $"启动被拒绝：存在未确认报警 [{blocking.Code}]");
            return false;
        }

        if (!Status.Motion.Homed)
        {
            _log.Warning("操作", "启动被拒绝：请先执行回零");
            return false;
        }

        // 单件时长 = 路径总长 / 扫描速度 × 加工次数 × 演示倍率
        double scanSpeed = Math.Max(1, _params.Get(ParamKeys.ScanSpeed));
        double passes = Math.Max(1, _params.Get(ParamKeys.PassCount));
        _partDuration = Math.Clamp(
            Path.TotalLength / scanSpeed * passes * DemoSpeedFactor,
            MinPartSeconds, MaxPartSeconds);

        var task = new ProcessTaskInfo
        {
            Name = $"批次 L-{DateTime.Now:MMdd}-{_rng.Next(100, 999)}",
            PatternName = "演示图案（外框 / 圆环 / 五角星 / 中心圆）",
            PresetName = presetName,
            TotalParts = Math.Max(1, totalParts),
            CurrentPartIndex = 1,
            HeadPosition = Path.GetPointAtLength(0)
        };

        Status.Task = task;
        Status.State = MachineState.Marking;
        Status.Laser.SetPowerPercent = _params.Get(ParamKeys.Power);
        Status.Laser.FrequencyKhz = _params.Get(ParamKeys.Frequency);

        _partElapsed = 0;
        _interDelayRemaining = InterPartDelaySeconds;
        _tempWarned = false;

        _log.Info("任务", $"任务 {task.Name} 已启动：{task.TotalParts} 件，" +
                          $"单件预计 {_partDuration + InterPartDelaySeconds:F0} s（演示倍率 1:{DemoSpeedFactor:F0}）");
        return true;
    }

    public void Pause()
    {
        if (Status.State != MachineState.Marking)
        {
            return;
        }

        Status.State = MachineState.Paused;
        Status.Laser.IsEnabled = false;
        Status.Laser.ActualPower = 0;
        Status.Motion.Speed = 0;
        _log.Info("操作", "操作员暂停加工，激光输出已关闭");
    }

    public void Resume()
    {
        if (Status.State != MachineState.Paused)
        {
            return;
        }

        if (Status.Laser.Temperature > ResumeSafeTemp)
        {
            _log.Warning("操作", $"无法继续：激光模块温度 {Status.Laser.Temperature:F1}°C 偏高，" +
                                 $"请等待冷却至 {ResumeSafeTemp:F0}°C 以下");
            return;
        }

        Status.State = MachineState.Marking;
        _log.Info("操作", "加工继续");
    }

    public void Stop()
    {
        if (Status.State is not (MachineState.Marking or MachineState.Paused or MachineState.Alarm))
        {
            return;
        }

        var task = Status.Task;
        if (task is not null)
        {
            _log.Warning("操作", $"任务 {task.Name} 已被停止：完成 {task.CompletedParts}/{task.TotalParts} 件，剩余部分作废");
        }

        Status.State = MachineState.Ready;
        Status.Laser.IsEnabled = false;
        Status.Laser.ActualPower = 0;
        Status.Motion.Speed = 0;
        Status.Task = null;
    }

    public void EmergencyStop()
    {
        if (Status.State is MachineState.EStopped or MachineState.Offline)
        {
            return;
        }

        bool hadRunningTask = Status.Task is not null
            && Status.State is MachineState.Marking or MachineState.Paused or MachineState.Alarm;

        Status.State = MachineState.EStopped;
        Status.Laser.IsEnabled = false;
        Status.Laser.ActualPower = 0;
        Status.Motion.Speed = 0;
        Status.Task = null;

        _log.Error("安全系统", "紧急停止已触发：激光输出已切断，运动轴已停止");
        _log.RaiseAlarm(AlarmSeverity.Critical, "S-9001", "安全系统",
            hadRunningTask ? "操作员触发紧急停止，当前加工任务已终止" : "操作员触发紧急停止");
    }

    public void ResetEmergency()
    {
        if (Status.State != MachineState.EStopped)
        {
            return;
        }

        Status.State = MachineState.Idle;
        Status.Motion.Homed = false;
        _log.Info("操作", "急停回路已复位，设备回到待机，请重新执行回零");
    }

    public void AcknowledgeAlarms()
    {
        foreach (var alarm in _log.Alarms.Where(a => !a.IsAcknowledged).ToList())
        {
            _log.AcknowledgeAlarm(alarm);
        }

        EvaluateAlarmRecovery();
    }

    public void EvaluateAlarmRecovery()
    {
        if (Status.State != MachineState.Alarm)
        {
            return;
        }

        if (Status.Laser.Temperature <= ResumeSafeTemp)
        {
            ResumeFromAlarm();
        }
        else
        {
            _log.Warning("操作", $"报警已确认，但激光模块温度（{Status.Laser.Temperature:F1}°C）仍偏高，" +
                                 "温度回落至安全范围后将自动解除挂起");
        }
    }

    // ---------- 模拟主循环 ----------

    private void OnTick()
    {
        UpdateConnection();

        switch (Status.State)
        {
            case MachineState.Homing:
                TickHoming();
                break;
            case MachineState.Marking:
                TickMarking();
                break;
            case MachineState.Alarm:
                TickAlarmWatcher();
                break;
            default:
                TickIdleSettling();
                break;
        }

        UpdateLaserAndThermal();
        UpdateTrends();
    }

    private void UpdateConnection()
    {
        if (Status.Connection != ConnectionState.Connecting)
        {
            return;
        }

        _connectRemaining -= TickSeconds;
        double elapsed = ConnectDurationSeconds - _connectRemaining;

        while (_connectStep < 3 && elapsed >= _connectStep * (ConnectDurationSeconds / 3.2))
        {
            _connectStep++;
            switch (_connectStep)
            {
                case 1:
                    _log.Info("通讯", "正在连接设备 192.168.1.50:502 …");
                    break;
                case 2:
                    _log.Info("通讯", "链路建立成功，正在读取控制器信息 …");
                    break;
                case 3:
                    _log.Info("通讯", "连接完成：LK-60 光纤激光打标机 / 固件 v3.2.1 / 红光指示正常");
                    break;
            }
        }

        if (_connectRemaining <= 0)
        {
            Status.Connection = ConnectionState.Online;
            Status.State = MachineState.Idle;
            _log.Info("通讯", "设备已上线，当前处于待机状态，请执行回零");
        }
    }

    private void TickHoming()
    {
        _homeRemaining -= TickSeconds;
        double p = Math.Clamp(1 - _homeRemaining / HomeDurationSeconds, 0, 1);
        double e = EaseInOut(p);

        Status.Motion.X = _homeFromX * (1 - e);
        Status.Motion.Y = _homeFromY * (1 - e);
        Status.Motion.Z = _homeFromZ + (12.0 - _homeFromZ) * e;
        Status.Motion.Speed = 400 * (1 - Math.Abs(2 * e - 1));

        if (_homeRemaining <= 0)
        {
            Status.Motion.X = 0;
            Status.Motion.Y = 0;
            Status.Motion.Speed = 0;
            Status.Motion.Homed = true;
            Status.State = MachineState.Ready;
            _log.Info("运动系统", "三轴回零完成，设备就绪");
        }
    }

    private void TickMarking()
    {
        var task = Status.Task;
        if (task is null)
        {
            Status.State = MachineState.Ready;
            return;
        }

        if (_interDelayRemaining > 0)
        {
            _interDelayRemaining -= TickSeconds;
            Status.Motion.Speed = _params.Get(ParamKeys.PosSpeed);
            task.HeadPosition = Path.GetPointAtLength(0);
            Status.Motion.X = task.HeadPosition.X;
            Status.Motion.Y = task.HeadPosition.Y;

            if (_interDelayRemaining <= 0)
            {
                Status.Laser.IsEnabled = true;
                _log.Info("任务", $"开始加工第 {task.CurrentPartIndex} / {task.TotalParts} 件");
            }

            AccumulateTaskTime(task);
            return;
        }

        _partElapsed += TickSeconds;
        double p = Math.Clamp(_partElapsed / _partDuration, 0, 1);

        var point = Path.GetPointAtLength(p * Path.TotalLength);
        task.CurrentPartProgress = p;
        task.HeadPosition = point;
        Status.Motion.X = point.X;
        Status.Motion.Y = point.Y;
        Status.Motion.Speed = _params.Get(ParamKeys.ScanSpeed);

        AccumulateTaskTime(task);

        if (p >= 1)
        {
            CompleteCurrentPart(task);
        }
    }

    private void AccumulateTaskTime(ProcessTaskInfo task)
    {
        task.Elapsed += TimeSpan.FromSeconds(TickSeconds);

        double remainingParts = task.TotalParts - task.CompletedParts - task.CurrentPartProgress;
        task.Remaining = TimeSpan.FromSeconds(Math.Max(0, remainingParts) * (_partDuration + InterPartDelaySeconds));
    }

    private void CompleteCurrentPart(ProcessTaskInfo task)
    {
        task.CompletedParts++;
        task.CurrentPartProgress = 0;
        task.OverallProgress = (double)task.CompletedParts / task.TotalParts;
        Status.Laser.Temperature += PartFinishHeat;

        _log.Info("任务", $"第 {task.CompletedParts} / {task.TotalParts} 件加工完成（本件用时 {_partDuration:F1} s）");

        if (task.CompletedParts >= task.TotalParts)
        {
            FinishTask(task);
            return;
        }

        task.CurrentPartIndex++;
        Status.Laser.IsEnabled = false;
        Status.Motion.Speed = 0;
        _interDelayRemaining = InterPartDelaySeconds;

        if (_rng.NextDouble() < 0.35)
        {
            _log.Warning("激光器", $"输出功率波动 ±{_rng.NextDouble() * 1.5:F1}%，能量闭环已自动补偿");
        }
    }

    private void FinishTask(ProcessTaskInfo task)
    {
        Status.State = MachineState.Ready;
        Status.Laser.IsEnabled = false;
        Status.Laser.ActualPower = 0;
        Status.Motion.Speed = 0;
        task.OverallProgress = 1;
        task.CurrentPartProgress = 0;
        _tempWarned = false;

        _log.Info("任务", $"任务 {task.Name} 全部完成：共 {task.TotalParts} 件，总耗时 {task.Elapsed:mm\\:ss}");
    }

    private void TickAlarmWatcher()
    {
        if (Status.Laser.Temperature > ResumeSafeTemp)
        {
            return;
        }

        if (_tempAlarm is { IsRecovered: false })
        {
            _log.MarkRecovered(_tempAlarm);
            _log.Info("激光器", "激光模块温度已回落至安全范围");
        }

        if (!_log.Alarms.Any(a => !a.IsAcknowledged))
        {
            ResumeFromAlarm();
        }
    }

    private void ResumeFromAlarm()
    {
        Status.State = Status.Task is not null ? MachineState.Paused : MachineState.Ready;
        _log.Info("系统", Status.Task is not null
            ? "报警解除，任务保持暂停状态，可点击“继续加工”恢复"
            : "报警解除，设备恢复就绪");
    }

    private void TickIdleSettling()
    {
        if (Status.Motion.Speed > 0)
        {
            Status.Motion.Speed = Math.Max(0, Status.Motion.Speed - 2000 * TickSeconds);
        }
    }

    /// <summary>
    /// 激光输出与热模型模拟
    /// </summary>
    private void UpdateLaserAndThermal()
    {
        bool isLasing = Status.State == MachineState.Marking
                        && _interDelayRemaining <= 0
                        && Status.Laser.IsEnabled;

        if (isLasing)
        {
            double power = _params.Get(ParamKeys.Power);
            Status.Laser.SetPowerPercent = power;
            Status.Laser.ActualPower = Math.Round(power * (0.97 + _rng.NextDouble() * 0.06), 1);
            Status.Laser.Temperature += HeatRatePerSecond * (power / 60.0) * TickSeconds;

            if (!_tempWarned && Status.Laser.Temperature >= TempWarnLine)
            {
                _tempWarned = true;
                _log.Warning("激光器", $"激光模块温度偏高（{Status.Laser.Temperature:F1}°C），请留意散热");
            }

            if (Status.Laser.Temperature >= _params.Get(ParamKeys.TempLimit))
            {
                EnterThermalAlarm();
            }
        }
        else
        {
            Status.Laser.ActualPower = 0;

            // 停机时按指数曲线向环境温度回落
            double delta = Status.Laser.Temperature - CoolAmbientTemp;
            if (delta > 0.01)
            {
                Status.Laser.Temperature -= delta * 0.03;
            }
        }
    }

    private void EnterThermalAlarm()
    {
        double temp = Status.Laser.Temperature;
        double limit = _params.Get(ParamKeys.TempLimit);

        Status.State = MachineState.Alarm;
        Status.Laser.IsEnabled = false;
        Status.Laser.ActualPower = 0;
        Status.Motion.Speed = 0;

        _tempAlarm = _log.RaiseAlarm(AlarmSeverity.Error, "L-2001", "激光器",
            $"激光模块温度 {temp:F1}°C 达到阈值 {limit:F0}°C，加工已自动挂起，等待冷却");
        _log.Error("激光器", "过温保护触发，激光输出已自动关闭");
    }

    private void UpdateTrends()
    {
        _trendDivider++;
        if (_trendDivider < 6)
        {
            return;
        }

        _trendDivider = 0;
        AppendTrend(PowerTrend, Status.Laser.ActualPower);
        AppendTrend(TemperatureTrend, Math.Round(Status.Laser.Temperature, 1));
    }

    private static void AppendTrend(ObservableCollection<double> trend, double value)
    {
        trend.Add(value);
        while (trend.Count > TrendCapacity)
        {
            trend.RemoveAt(0);
        }
    }

    private static double EaseInOut(double p) =>
        p < 0.5 ? 2 * p * p : 1 - 2 * (1 - p) * (1 - p);
}
