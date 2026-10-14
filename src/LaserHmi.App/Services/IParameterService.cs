using LaserHmi.App.Models;

namespace LaserHmi.App.Services;

/// <summary>
/// 工艺参数服务：参数组持有、读取/写入、预设应用
/// </summary>
public interface IParameterService
{
    IReadOnlyList<ParamItem> LaserParams { get; }

    IReadOnlyList<ParamItem> MotionParams { get; }

    IReadOnlyList<ParamItem> SafetyParams { get; }

    IReadOnlyList<ParameterPreset> Presets { get; }

    /// <summary>
    /// 按参数键读取当前值
    /// </summary>
    double Get(string key);

    /// <summary>
    /// 按参数键写入（自动收敛到合法范围）
    /// </summary>
    void Set(string key, double value);

    /// <summary>
    /// 应用预设（按键批量写入）
    /// </summary>
    void ApplyPreset(ParameterPreset preset);

    /// <summary>
    /// 当前值是否与指定预设完全一致
    /// </summary>
    bool MatchesPreset(ParameterPreset preset);
}
