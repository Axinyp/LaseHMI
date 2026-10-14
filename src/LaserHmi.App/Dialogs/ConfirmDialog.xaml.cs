using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LaserHmi.App.Dialogs;

/// <summary>
/// 深色风格确认对话框：用于重要 / 危险操作的二次确认。
/// 危险操作（danger=true）时确认按钮与图标切换为红色语义。
/// </summary>
public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message, string confirmText, bool danger)
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;

        if (danger)
        {
            ConfirmButton.Style = (Style)FindResource("DangerSolidButton");
            IconBadge.Background = (Brush)FindResource("BrushDangerSoft");
            IconPath.Stroke = (Brush)FindResource("BrushDanger");
            IconPath.Data = (Geometry)FindResource("IconWarning");
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    /// <summary>
    /// 支持拖动标题区域移动无边框窗口
    /// </summary>
    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
