using System.Windows;
using LaserHmi.App.Services;
using LaserHmi.App.ViewModels;

namespace LaserHmi.App;

/// <summary>
/// 应用入口：组装依赖与主窗口（Composition Root）。
/// 原型规模下使用手工组装替代 DI 容器，依赖关系集中、显式、易于审查。
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ---- 服务层 ----
        ILogService log = new LogService();
        IParameterService parameters = new ParameterService();
        IMachineService machine = new MockMachineService(log, parameters);
        IDialogService dialogs = new DialogService();

        // ---- 页面 ViewModel ----
        var operations = new DeviceOperationsViewModel(machine, log, dialogs, parameters);
        var dashboard = new DashboardViewModel(machine, log, operations);
        var workbench = new WorkbenchViewModel(machine, parameters, operations);
        var parametersVm = new ParametersViewModel(parameters, log);
        var control = new DeviceControlViewModel(machine, log, operations);
        var alarmLog = new AlarmLogViewModel(log, machine);

        var main = new MainViewModel(
            machine, log, operations, dashboard, workbench, parametersVm, control, alarmLog);

        // ---- 主窗口 ----
        var window = new MainWindow { DataContext = main };
        MainWindow = window;
        window.Show();
    }
}
