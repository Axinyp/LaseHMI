using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace LaserHmi.App.Controls;

/// <summary>
/// 实时曲线控件：绘制滚动窗口内的数值序列（折线 + 面积渐变），
/// 自动订阅集合变更，供激光功率 / 温度等趋势监视使用。
/// </summary>
public sealed class Sparkline : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IEnumerable), typeof(Sparkline),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender, OnValuesChanged));

    /// <summary>
    /// 窗口容量（与数据源容量保持一致时曲线从左向右铺满）
    /// </summary>
    public static readonly DependencyProperty CapacityProperty = DependencyProperty.Register(
        nameof(Capacity), typeof(int), typeof(Sparkline),
        new FrameworkPropertyMetadata(72, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
        nameof(MinValue), typeof(double), typeof(Sparkline),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        nameof(MaxValue), typeof(double), typeof(Sparkline),
        new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// 自动按窗口数据缩放（上限向下取整到 10 的倍数）
    /// </summary>
    public static readonly DependencyProperty AutoScaleProperty = DependencyProperty.Register(
        nameof(AutoScale), typeof(bool), typeof(Sparkline),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(Sparkline),
        new FrameworkPropertyMetadata(Brushes.DeepSkyBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable? Values
    {
        get => (IEnumerable?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public int Capacity
    {
        get => (int)GetValue(CapacityProperty);
        set => SetValue(CapacityProperty, value);
    }

    public double MinValue
    {
        get => (double)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public bool AutoScale
    {
        get => (bool)GetValue(AutoScaleProperty);
        set => SetValue(AutoScaleProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 10 || h < 10 || Values is null)
        {
            return;
        }

        var data = Values.Cast<double>().ToArray();
        if (data.Length < 2)
        {
            return;
        }

        double min = MinValue;
        double max = MaxValue;
        if (AutoScale)
        {
            double dataMax = data.Max();
            max = Math.Max(Math.Ceiling(dataMax * 1.25 / 10) * 10, 10);
            double dataMin = Math.Min(MinValue, data.Min());
            min = Math.Floor(dataMin / 10) * 10;
            if (max - min < 10)
            {
                max = min + 10;
            }
        }

        const double pad = 3;
        double plotH = h - pad * 2;
        int capacity = Math.Max(Capacity, 2);

        Point Map(int index, double value)
        {
            double x = w * index / (capacity - 1.0);
            double y = pad + (1 - (value - min) / (max - min)) * plotH;
            return new Point(x, y);
        }

        // 中线参考
        var midPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 0x5F, 0x73, 0x8E)), 1);
        midPen.Freeze();
        dc.DrawLine(midPen, new Point(0, h / 2), new Point(w, h / 2));

        // 折线
        var geometry = new StreamGeometry();
        using (StreamGeometryContext ctx = geometry.Open())
        {
            ctx.BeginFigure(Map(0, data[0]), false, false);
            for (int i = 1; i < data.Length; i++)
            {
                ctx.LineTo(Map(i, data[i]), true, false);
            }
        }
        geometry.Freeze();

        var strokePen = new Pen(Stroke, 1.6)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        strokePen.Freeze();

        // 面积：闭合到基线
        if (Fill is not null)
        {
            var areaGeometry = new StreamGeometry();
            using (StreamGeometryContext ctx = areaGeometry.Open())
            {
                ctx.BeginFigure(Map(0, min), true, true);
                var last = Map(0, data[0]);
                ctx.LineTo(last, true, false);
                for (int i = 1; i < data.Length; i++)
                {
                    ctx.LineTo(Map(i, data[i]), true, false);
                }
                ctx.LineTo(Map(data.Length - 1, min), true, false);
            }
            areaGeometry.Freeze();
            dc.DrawGeometry(Fill, null, areaGeometry);
        }

        dc.DrawGeometry(null, strokePen, geometry);

        // 最新点高亮
        var head = Map(data.Length - 1, data[^1]);
        dc.DrawEllipse(Stroke, null, head, 2.4, 2.4);
    }

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (Sparkline)d;

        if (e.OldValue is INotifyCollectionChanged oldNotify)
        {
            oldNotify.CollectionChanged -= control.OnCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newNotify)
        {
            newNotify.CollectionChanged += control.OnCollectionChanged;
        }

        control.InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == StrokeProperty || e.Property == FillProperty)
        {
            InvalidateVisual();
        }
    }
}
