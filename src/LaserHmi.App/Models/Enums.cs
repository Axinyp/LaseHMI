namespace LaserHmi.App.Models;

/// <summary>
/// 上位机与设备的通讯连接状态
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// 未连接
    /// </summary>
    Offline,
    /// <summary>
    /// 连接中
    /// </summary>
    Connecting,
    /// <summary>
    /// 已连接
    /// </summary>
    Online
}

/// <summary>
/// 设备主状态机。
/// 流转关系：Offline → Idle → Homing → Ready ⇄ Marking ⇄ Paused；
/// 报警或急停可从任一状态进入 Alarm / EStopped。
/// </summary>
public enum MachineState
{
    /// <summary>
    /// 离线（通讯断开）
    /// </summary>
    Offline,
    /// <summary>
    /// 待机（已连接、未回零）
    /// </summary>
    Idle,
    /// <summary>
    /// 回零中
    /// </summary>
    Homing,
    /// <summary>
    /// 就绪（可启动加工）
    /// </summary>
    Ready,
    /// <summary>
    /// 加工中
    /// </summary>
    Marking,
    /// <summary>
    /// 操作员暂停
    /// </summary>
    Paused,
    /// <summary>
    /// 报警暂停（异常导致任务挂起，处理后需确认）
    /// </summary>
    Alarm,
    /// <summary>
    /// 紧急停止（光闸关闭、各轴已断电保持）
    /// </summary>
    EStopped
}

/// <summary>
/// 运行日志级别
/// </summary>
public enum LogLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// 报警严重度
/// </summary>
public enum AlarmSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
