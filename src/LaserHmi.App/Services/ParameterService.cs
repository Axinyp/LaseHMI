using LaserHmi.App.Models;

namespace LaserHmi.App.Services;

/// <summary>
/// 工艺参数服务实现（内存态，模拟参数下发）
/// </summary>
public sealed class ParameterService : IParameterService
{
    private readonly Dictionary<string, ParamItem> _byKey;

    public IReadOnlyList<ParamItem> LaserParams { get; }

    public IReadOnlyList<ParamItem> MotionParams { get; }

    public IReadOnlyList<ParamItem> SafetyParams { get; }

    public IReadOnlyList<ParameterPreset> Presets { get; }

    public ParameterService()
    {
        LaserParams = ParameterCatalog.CreateLaserParams();
        MotionParams = ParameterCatalog.CreateMotionParams();
        SafetyParams = ParameterCatalog.CreateSafetyParams();
        Presets = ParameterCatalog.CreatePresets();

        _byKey = LaserParams.Concat(MotionParams).Concat(SafetyParams)
            .ToDictionary(p => p.Key);
    }

    public double Get(string key) => _byKey[key].Value;

    public void Set(string key, double value)
    {
        var item = _byKey[key];
        item.Value = Math.Clamp(value, item.Min, item.Max);
    }

    public void ApplyPreset(ParameterPreset preset)
    {
        foreach (var (key, value) in preset.Values)
        {
            if (_byKey.ContainsKey(key))
            {
                Set(key, value);
            }
        }
    }

    public bool MatchesPreset(ParameterPreset preset) =>
        preset.Values.All(kv => _byKey.TryGetValue(kv.Key, out var item) && Math.Abs(item.Value - kv.Value) < 1e-9);
}
