using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LaserHmi.App;

/// <summary>
/// 主窗口：仅承载导航框架与全局状态栏，业务逻辑全部位于 ViewModel
/// </summary>
public partial class MainWindow : Window
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyDarkTitleBar();
    }

    /// <summary>
    /// 将系统标题栏切换为深色，与工业风主题保持视觉统一
    /// </summary>
    private void ApplyDarkTitleBar()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        try
        {
            var handle = new WindowInteropHelper(this).Handle;
            int enabled = 1;
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // 无桌面合成器（如某些远程会话）时忽略，保持默认标题栏。
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
