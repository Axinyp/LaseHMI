using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaserHmi.App.Models;
using LaserHmi.App.Services;

namespace LaserHmi.App.ViewModels;

/// <summary>
/// 参数设置页：激光 / 运动 / 安全三组参数编辑、预设应用与参数合理性提示。
/// 参数修改后需点击“应用参数”下发（模拟），下发的值供启动任务时使用。
/// </summary>
public sealed partial class ParametersViewModel : PageViewModelBase
{
    private readonly IParameterService _params;
    private readonly ILogService _log;

    public ParametersViewModel(IParameterService parameters, ILogService log)
    {
        _params = parameters;
        _log = log;

        SelectedPreset = Presets[0];

        foreach (var item in AllParams())
        {
            item.PropertyChanged += (_, _) =>
            {
                IsDirty = true;
                UpdateValidationHint();
            };
        }

        UpdateValidationHint();
    }

    public override string Title => "参数设置";

    public override string IconKey => "IconParams";

    public IReadOnlyList<ParamItem> LaserParams => _params.LaserParams;

    public IReadOnlyList<ParamItem> MotionParams => _params.MotionParams;

    public IReadOnlyList<ParamItem> SafetyParams => _params.SafetyParams;

    public IReadOnlyList<ParameterPreset> Presets => _params.Presets;

    [ObservableProperty]
    private ParameterPreset? selectedPreset;

    /// <summary>
    /// 是否存在未下发的修改
    /// </summary>
    [ObservableProperty]
    private bool isDirty;

    /// <summary>
    /// 参数合理性提示（空字符串表示在推荐范围内）
    /// </summary>
    [ObservableProperty]
    private string validationHint = string.Empty;

    /// <summary>
    /// 当前匹配的预设名称
    /// </summary>
    public string MatchedPresetName =>
        _params.Presets.FirstOrDefault(_params.MatchesPreset)?.Name ?? "自定义参数";

    /// <summary>
    /// 应用所选预设（覆盖全部参数）
    /// </summary>
    [RelayCommand]
    private void ApplyPreset()
    {
        if (SelectedPreset is null)
        {
            return;
        }

        _params.ApplyPreset(SelectedPreset);
        _log.Info("参数", $"已应用预设「{SelectedPreset.Name}」：{SelectedPreset.Description}");
        MarkApplied();
    }

    /// <summary>
    /// 下发当前参数至控制器（模拟）
    /// </summary>
    [RelayCommand]
    private void Apply()
    {
        _log.Info("参数",
            $"参数已下发至控制器（模拟）：功率 {_params.Get(ParamKeys.Power):F0}% / " +
            $"频率 {_params.Get(ParamKeys.Frequency):F0}kHz / 速度 {_params.Get(ParamKeys.ScanSpeed):F0}mm/s / " +
            $"间距 {_params.Get(ParamKeys.HatchSpacing):F3}mm / 次数 {_params.Get(ParamKeys.PassCount):F0}");
        MarkApplied();
    }

    /// <summary>
    /// 恢复出厂默认参数（标准打标）
    /// </summary>
    [RelayCommand]
    private void RestoreDefaults()
    {
        SelectedPreset = Presets[0];
        _params.ApplyPreset(SelectedPreset);
        _log.Info("参数", "已恢复出厂默认参数");
        MarkApplied();
    }

    private void MarkApplied()
    {
        IsDirty = false;
        OnPropertyChanged(nameof(MatchedPresetName));
        UpdateValidationHint();
    }

    private void UpdateValidationHint()
    {
        double power = _params.Get(ParamKeys.Power);
        double speed = _params.Get(ParamKeys.ScanSpeed);
        double spacing = _params.Get(ParamKeys.HatchSpacing);
        double tempLimit = _params.Get(ParamKeys.TempLimit);

        ValidationHint = (power, speed, spacing, tempLimit) switch
        {
            ( >= 80, _, <= 0.01, _) =>
                "高功率密堆积：热影响区可能偏大，建议减小功率或增大加工间距",
            ( >= 70, >= 2500, _, _) =>
                "高速高功率组合：打点重叠率下降，标记质量可能不稳定",
            (_, _, _, <= 55) =>
                "过温阈值偏低：连续加工时可能频繁触发过温保护",
            _ => string.Empty
        };
    }

    private IEnumerable<ParamItem> AllParams() =>
        _params.LaserParams.Concat(_params.MotionParams).Concat(_params.SafetyParams);
}
