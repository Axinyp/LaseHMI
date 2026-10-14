using System.Collections.ObjectModel;
using LaserHmi.App.Models;

namespace LaserHmi.App.Services;

/// <summary>
/// 设备服务：设备连接、状态机控制与过程模拟。
/// 真实项目中该接口由设备通讯层实现（TCP/串口/PLC），本原型由 MockMachineService 模拟。
/// </summary>
public interface IMachineService
{
    /// <summary>
    /// 设备实时状态聚合（界面绑定源）
    /// </summary>
    MachineStatus Status { get; }

    /// <summary>
    /// 当前加载的加工图案路径
    /// </summary>
    MarkingPath Path { get; }

    /// <summary>
    /// 激光功率实时趋势（%），供曲线控件绑定
    /// </summary>
    ObservableCollection<double> PowerTrend { get; }

    /// <summary>
    /// 激光模块温度实时趋势（°C）
    /// </summary>
    ObservableCollection<double> TemperatureTrend { get; }

    void Connect();

    void Disconnect();

    /// <summary>
    /// 三轴回零
    /// </summary>
    void Home();

    /// <summary>
    /// 启动加工任务；前置条件不满足时拒绝并记录日志，返回是否启动成功
    /// </summary>
    bool StartTask(int totalParts, string presetName);

    void Pause();

    void Resume();

    /// <summary>
    /// 停止当前任务（任务作废，设备回到就绪）
    /// </summary>
    void Stop();

    /// <summary>
    /// 紧急停止：立即切断激光输出并停止运动
    /// </summary>
    void EmergencyStop();

    /// <summary>
    /// 急停回路复位（回到待机，需重新回零）
    /// </summary>
    void ResetEmergency();

    /// <summary>
    /// 确认全部未确认报警；若报警状态且异常已恢复则解除挂起
    /// </summary>
    void AcknowledgeAlarms();

    /// <summary>
    /// 检查报警挂起是否可自动解除（异常已恢复且无待确认报警时恢复）
    /// </summary>
    void EvaluateAlarmRecovery();
}
