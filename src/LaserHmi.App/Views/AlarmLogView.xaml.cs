using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using LaserHmi.App.ViewModels;

namespace LaserHmi.App.Views;

/// <summary>
/// 报警与日志页。新日志到达且「自动滚动」开启时，将日志流滚动到底部；
/// 页面加载 / 卸载时同步订阅与退订，避免页面切换后重复订阅。
/// </summary>
public partial class AlarmLogView : UserControl
{
    private INotifyCollectionChanged? _logsSource;

    public AlarmLogView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachLogs();
        Loaded += (_, _) => AttachLogs();
        Unloaded += (_, _) => DetachLogs();
    }

    private void AttachLogs()
    {
        DetachLogs();

        if (DataContext is AlarmLogViewModel viewModel)
        {
            _logsSource = viewModel.Logs;
            _logsSource.CollectionChanged += OnLogsChanged;
        }
    }

    private void DetachLogs()
    {
        if (_logsSource is not null)
        {
            _logsSource.CollectionChanged -= OnLogsChanged;
            _logsSource = null;
        }
    }

    private void OnLogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is not AlarmLogViewModel { AutoScroll: true })
        {
            return;
        }

        // 等本帧布局完成后再滚动，确保新增行已被容器计算。
        Dispatcher.BeginInvoke(new Action(LogScroll.ScrollToEnd));
    }
}
