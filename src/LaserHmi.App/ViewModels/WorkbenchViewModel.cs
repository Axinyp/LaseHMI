using CommunityToolkit.Mvvm.ComponentModel;
using LaserHmi.App.Models;
using LaserHmi.App.Services;

namespace LaserHmi.App.ViewModels;

/// <summary>
/// 加工工作区页：图形预览、工件坐标、加工任务的快速操作与参数摘要
/// </summary>
public sealed partial class WorkbenchViewModel : PageViewModelBase
{
    private readonly IMachineService _machine;
    private readonly IParameterService _params;

    public WorkbenchViewModel(
        IMachineService machine, IParameterService parameters, DeviceOperationsViewModel operations)
    {
        _machine = machine;
        _params = parameters;
        Operations = operations;

        // 工艺参数变化时刷新摘要
        foreach (var item in AllParams())
        {
            item.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ParameterSummary));
        }

        // 加工状态 / 任务变化时刷新预览相关属性
        _machine.Status.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsMarking));
            OnPropertyChanged(nameof(ShowPreviewHint));
        };
    }

    public override string Title => "加工工作区";

    public override string IconKey => "IconMarking";

    public MachineStatus Status => _machine.Status;

    public DeviceOperationsViewModel Operations { get; }

    public MarkingPath PreviewPath => _machine.Path;

    /// <summary>
    /// 是否显示幅面网格
    /// </summary>
    [ObservableProperty]
    private bool showGrid = true;

    /// <summary>
    /// 当前是否处于加工中（控制预览中激光头的显示）
    /// </summary>
    public bool IsMarking => Status.State == MachineState.Marking;

    /// <summary>
    /// 是否显示预览空态引导（就绪且无任务时展示操作提示）
    /// </summary>
    public bool ShowPreviewHint => Status.State == MachineState.Ready && Status.Task is null;

    /// <summary>
    /// 当前工艺参数摘要（启动前核对用）
    /// </summary>
    public string ParameterSummary =>
        $"功率 {_params.Get(ParamKeys.Power):F0}%  ·  频率 {_params.Get(ParamKeys.Frequency):F0} kHz  ·  " +
        $"速度 {_params.Get(ParamKeys.ScanSpeed):F0} mm/s  ·  次数 {_params.Get(ParamKeys.PassCount):F0}";

    private IEnumerable<ParamItem> AllParams() =>
        _params.LaserParams.Concat(_params.MotionParams).Concat(_params.SafetyParams);
}
