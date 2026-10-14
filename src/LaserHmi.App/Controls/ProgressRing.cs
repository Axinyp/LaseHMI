using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace LaserHmi.App.Controls;

/// <summary>
/// 环形进度控件：绘制轨道圆与按进度行进的弧线，中心显示百分比数值。
/// </summary>
public sealed class ProgressRing : FrameworkElement
{
    private static readonly Brush DefaultTrackBrush =
        Freeze(new SolidColorBrush(Color.FromRgb(0x1B, 0x29, 0x42)));

    private static readonly Brush DefaultValueBrush =
        Freeze(new SolidColorBrush(Color.FromRgb(0xE8, 0xF0, 0xFA)));

    private static readonly Brush DefaultCaptionBrush =
        Freeze(new SolidColorBrush(Color.FromRgb(0x5F, 0x73, 0x8E)));

    private static readonly Typeface ValueTypeface = new("Consolas");

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(11.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(ProgressRing),
        new FrameworkPropertyMetadata(Brushes.DeepSkyBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackProperty = DependencyProperty.Register(
        nameof(Track), typeof(Brush), typeof(ProgressRing),
        new FrameworkPropertyMetadata(DefaultTrackBrush, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// 中心说明文字（如“整体进度”），为空则不绘制
    /// </summary>
    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption), typeof(string), typeof(ProgressRing),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public Brush Track
    {
        get => (Brush)GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 40 || h < 40)
        {
            return;
        }

        double size = Math.Min(w, h);
        double thick = Math.Min(Thickness, size / 4);
        double radius = (size - thick) / 2 - 2;
        var center = new Point(w / 2, h / 2);

        // 轨道
        var trackPen = new Pen(Track, thick);
        trackPen.Freeze();
        dc.DrawEllipse(null, trackPen, center, radius, radius);

        // 进度弧：从 12 点方向顺时针
        double progress = Math.Clamp(Progress, 0, 1);
        if (progress > 0.0005)
        {
            var arcPen = new Pen(Stroke, thick)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            arcPen.Freeze();

            if (progress >= 0.9995)
            {
                dc.DrawEllipse(null, arcPen, center, radius, radius);
            }
            else
            {
                double startAngle = -90;
                double endAngle = startAngle + 360 * progress;

                Point At(double deg)
                {
                    double rad = deg * Math.PI / 180;
                    return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
                }

                var geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    ctx.BeginFigure(At(startAngle), false, false);
                    ctx.ArcTo(At(endAngle), new Size(radius, radius), 0,
                        progress > 0.5, SweepDirection.Clockwise, true, false);
                }
                geometry.Freeze();
                dc.DrawGeometry(null, arcPen, geometry);
            }
        }

        // 中心数值
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var valueText = new FormattedText(
            (progress * 100).ToString("F0", CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            ValueTypeface, size * 0.21, DefaultValueBrush, pixelsPerDip);

        double yOffset = string.IsNullOrEmpty(Caption) ? valueText.Height / 2 : valueText.Height * 0.1;
        dc.DrawText(valueText, new Point(center.X - valueText.Width / 2, center.Y - yOffset));

        var percentText = new FormattedText(
            "%", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            ValueTypeface, size * 0.1, DefaultCaptionBrush, pixelsPerDip);
        dc.DrawText(percentText,
            new Point(center.X - percentText.Width / 2, center.Y - yOffset + valueText.Height - percentText.Height + 2));

        if (!string.IsNullOrEmpty(Caption))
        {
            var captionText = new FormattedText(
                Caption, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Microsoft YaHei UI"), size * 0.085, DefaultCaptionBrush, pixelsPerDip);
            dc.DrawText(captionText,
                new Point(center.X - captionText.Width / 2, center.Y + valueText.Height * 0.62));
        }
    }

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
