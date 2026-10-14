namespace LaserHmi.App.Services;

/// <summary>
/// 对话框服务：为 ViewModel 提供与视图无关的确认交互
/// （重要/危险操作的二次确认），实现类负责创建具体 WPF 窗口。
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 弹出确认对话框，返回用户是否确认。
    /// </summary>
    /// <param name="title">标题。</param>
    /// <param name="message">正文（可含换行）。</param>
    /// <param name="confirmText">确认按钮文案。</param>
    /// <param name="danger">危险操作（确认按钮显示为红色）。</param>
    bool Confirm(string title, string message, string confirmText = "确认执行", bool danger = false);
}
