using System.Windows;
using LaserHmi.App.Dialogs;

namespace LaserHmi.App.Services;

/// <summary>
/// 对话框服务实现：弹出深色风格确认对话框（Owner 跟随主窗口）
/// </summary>
public sealed class DialogService : IDialogService
{
    public bool Confirm(string title, string message, string confirmText = "确认执行", bool danger = false)
    {
        var dialog = new ConfirmDialog(title, message, confirmText, danger)
        {
            Owner = Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true;
    }
}
