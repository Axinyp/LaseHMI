using CommunityToolkit.Mvvm.ComponentModel;

namespace LaserHmi.App.Models;

/// <summary>
/// 设备状态聚合（绑定源：界面所有状态展示均来源于此）
/// </summary>
public partial class MachineStatus : ObservableObject
{
    [ObservableProperty]
    private ConnectionState connection = ConnectionState.Offline;

    [ObservableProperty]
    private MachineState state = MachineState.Offline;

    [ObservableProperty]
    private LaserStatus laser = new();

    [ObservableProperty]
    private MotionStatus motion = new();

    [ObservableProperty]
    private ProcessTaskInfo? task;
}

/// <summary>
/// 激光器子系统状态
/// </summary>
public partial class LaserStatus : ObservableObject
{
    /// <summary>
    /// 出光使能（光闸 + 主控允许）
    /// </summary>
    [ObservableProperty]
    private bool isEnabled;

    /// <summary>
    /// 光路安全联锁（防护门 / 水冷 / 气压均正常）
    /// </summary>
    [ObservableProperty]
    private bool interlockClosed = true;

    /// <summary>
    /// 激光模块温度（摄氏度）
    /// </summary>
    [ObservableProperty]
    private double temperature = 31.5;

    /// <summary>
    /// 当前实际输出功率（%），加工时在设定值附近波动
    /// </summary>
    [ObservableProperty]
    private double actualPower;

    /// <summary>
    /// 功率设定值（%）
    /// </summary>
    [ObservableProperty]
    private double setPowerPercent = 60;

    /// <summary>
    /// 频率设定值（kHz），与参数页镜像
    /// </summary>
    [ObservableProperty]
    private double frequencyKhz = 80;

    /// <summary>
    /// 泵浦源累计出光时长（小时）
    /// </summary>
    [ObservableProperty]
    private double diodeHours = 4823.5;
}

/// <summary>
/// 运动子系统状态（三轴振镜/平移台，单位 mm、mm/s）
/// </summary>
public partial class MotionStatus : ObservableObject
{
    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    [ObservableProperty]
    private double z = 12.0;

    /// <summary>
    /// 当前合成速度（mm/s）
    /// </summary>
    [ObservableProperty]
    private double speed;

    /// <summary>
    /// 是否已完成回零（回零前禁止启动加工）
    /// </summary>
    [ObservableProperty]
    private bool homed;

    /// <summary>
    /// X 轴是否触发硬件限位
    /// </summary>
    [ObservableProperty]
    private bool xAtLimit;

    /// <summary>
    /// Y 轴是否触发硬件限位
    /// </summary>
    [ObservableProperty]
    private bool yAtLimit;
}

/// <summary>
/// 当前加工任务信息
/// </summary>
public partial class ProcessTaskInfo : ObservableObject
{
    /// <summary>
    /// 任务名称（批次号）
    /// </summary>
    public string Name { get; init; } = "-";

    /// <summary>
    /// 加工图案
    /// </summary>
    public string PatternName { get; init; } = "-";

    /// <summary>
    /// 参数预设名称
    /// </summary>
    public string PresetName { get; init; } = "-";

    /// <summary>
    /// 计划件数
    /// </summary>
    public int TotalParts { get; init; }

    /// <summary>
    /// 已完成件数
    /// </summary>
    [ObservableProperty]
    private int completedParts;

    /// <summary>
    /// 当前件加工进度（0~1）
    /// </summary>
    [ObservableProperty]
    private double currentPartProgress;

    /// <summary>
    /// 整体进度（0~1）
    /// </summary>
    [ObservableProperty]
    private double overallProgress;

    /// <summary>
    /// 任务已耗时
    /// </summary>
    [ObservableProperty]
    private TimeSpan elapsed;

    /// <summary>
    /// 预计剩余时间
    /// </summary>
    [ObservableProperty]
    private TimeSpan remaining;

    /// <summary>
    /// 当前工件序号（从 1 开始），任务未运行时为 0
    /// </summary>
    [ObservableProperty]
    private int currentPartIndex;

    /// <summary>
    /// 激光头当前坐标（路径采样点，单位 mm）
    /// </summary>
    [ObservableProperty]
    private System.Windows.Point headPosition;
}
