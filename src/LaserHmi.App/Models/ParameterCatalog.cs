namespace LaserHmi.App.Models;

/// <summary>
/// 工艺参数键（跨模块引用的唯一标识）
/// </summary>
public static class ParamKeys
{
    public const string Power = "laser.power";
    public const string Frequency = "laser.frequency";
    public const string PulseWidth = "laser.pulseWidth";
    public const string ScanSpeed = "laser.scanSpeed";
    public const string HatchSpacing = "laser.hatchSpacing";
    public const string PassCount = "laser.passCount";

    public const string PosSpeed = "motion.posSpeed";
    public const string JumpSpeed = "motion.jumpSpeed";
    public const string Acceleration = "motion.acceleration";
    public const string ZOffset = "motion.zOffset";
    public const string GalvoDelay = "motion.galvoDelay";

    public const string TempLimit = "safety.tempLimit";
}

/// <summary>
/// 参数目录：默认参数定义与出厂预设（模拟数据）
/// </summary>
public static class ParameterCatalog
{
    public static List<ParamItem> CreateLaserParams() =>
    [
        new ParamItem
        {
            Key = ParamKeys.Power, Name = "激光功率", Min = 10, Max = 100, Step = 5,
            Unit = "%", Value = 60, Description = "平均输出功率百分比，影响刻蚀深度与热输入"
        },
        new ParamItem
        {
            Key = ParamKeys.Frequency, Name = "脉冲频率", Min = 20, Max = 200, Step = 5,
            Unit = "kHz", Value = 80, Description = "脉冲重复频率，高频更细腻、单脉冲能量更低"
        },
        new ParamItem
        {
            Key = ParamKeys.PulseWidth, Name = "脉宽", Min = 20, Max = 500, Step = 10,
            Unit = "ns", Value = 120, Description = "单脉冲宽度，影响热影响区与峰值功率"
        },
        new ParamItem
        {
            Key = ParamKeys.ScanSpeed, Name = "扫描速度", Min = 100, Max = 3000, Step = 50,
            Unit = "mm/s", Value = 800, Description = "振镜扫描线速度，与频率共同决定打点重叠率"
        },
        new ParamItem
        {
            Key = ParamKeys.HatchSpacing, Name = "加工间距", Min = 0.005, Max = 0.05, Step = 0.005,
            Unit = "mm", Value = 0.02, Decimals = 3, Description = "填充线间距，越小填充越密、效率越低"
        },
        new ParamItem
        {
            Key = ParamKeys.PassCount, Name = "加工次数", Min = 1, Max = 5, Step = 1,
            Unit = "次", Value = 1, Description = "同一路径重复加工遍数，多次可加深标记"
        }
    ];

    public static List<ParamItem> CreateMotionParams() =>
    [
        new ParamItem
        {
            Key = ParamKeys.PosSpeed, Name = "定位速度", Min = 100, Max = 5000, Step = 100,
            Unit = "mm/s", Value = 2000, Description = "工件定位运动速度"
        },
        new ParamItem
        {
            Key = ParamKeys.JumpSpeed, Name = "跳转速度", Min = 500, Max = 8000, Step = 100,
            Unit = "mm/s", Value = 4000, Description = "空程跳转速度（两段路径之间）"
        },
        new ParamItem
        {
            Key = ParamKeys.Acceleration, Name = "加速度", Min = 500, Max = 20000, Step = 500,
            Unit = "mm/s²", Value = 8000, Description = "运动轴加速度，过高可能引起振动超差"
        },
        new ParamItem
        {
            Key = ParamKeys.ZOffset, Name = "Z 轴偏移", Min = -5, Max = 5, Step = 0.1,
            Unit = "mm", Value = 0, Decimals = 1, Description = "焦点相对工件表面的偏移量"
        },
        new ParamItem
        {
            Key = ParamKeys.GalvoDelay, Name = "振镜延时", Min = 0, Max = 500, Step = 10,
            Unit = "µs", Value = 150, Description = "拐角处出光延时，过小会造成圆角缺失"
        }
    ];

    public static List<ParamItem> CreateSafetyParams() =>
    [
        new ParamItem
        {
            Key = ParamKeys.TempLimit, Name = "过温保护阈值", Min = 50, Max = 75, Step = 1,
            Unit = "°C", Value = 63, Description = "激光模块温度达到该值时自动挂起加工"
        }
    ];

    /// <summary>
    /// 出厂预设（标准打标为默认值快照）
    /// </summary>
    public static List<ParameterPreset> CreatePresets()
    {
        var standard = new Dictionary<string, double>
        {
            [ParamKeys.Power] = 60, [ParamKeys.Frequency] = 80, [ParamKeys.PulseWidth] = 120,
            [ParamKeys.ScanSpeed] = 800, [ParamKeys.HatchSpacing] = 0.02, [ParamKeys.PassCount] = 1,
            [ParamKeys.PosSpeed] = 2000, [ParamKeys.JumpSpeed] = 4000, [ParamKeys.Acceleration] = 8000,
            [ParamKeys.ZOffset] = 0, [ParamKeys.GalvoDelay] = 150,
            [ParamKeys.TempLimit] = 63
        };

        var fine = new Dictionary<string, double>(standard)
        {
            [ParamKeys.Power] = 35, [ParamKeys.Frequency] = 120, [ParamKeys.PulseWidth] = 80,
            [ParamKeys.ScanSpeed] = 400, [ParamKeys.HatchSpacing] = 0.01, [ParamKeys.PassCount] = 3
        };

        var fast = new Dictionary<string, double>(standard)
        {
            [ParamKeys.Power] = 75, [ParamKeys.Frequency] = 60, [ParamKeys.PulseWidth] = 150,
            [ParamKeys.ScanSpeed] = 2200, [ParamKeys.HatchSpacing] = 0.03, [ParamKeys.PassCount] = 1
        };

        return
        [
            new ParameterPreset { Name = "标准打标", Description = "不锈钢标牌通用参数，平衡效率与效果", Values = standard },
            new ParameterPreset { Name = "精细刻蚀", Description = "低功率高频率多次加工，适合精密刻蚀", Values = fine },
            new ParameterPreset { Name = "高速浅雕", Description = "高速度大间距，适合快速浅层标记", Values = fast }
        ];
    }
}
