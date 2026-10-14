using System.Globalization;
using System.Windows;
using System.Windows.Media;
using LaserHmi.App.Models;

namespace LaserHmi.App.Controls;

/// <summary>
/// 加工预览控件：自绘振镜幅面（网格 / 坐标刻度 / 工件板料 / 打标路径），
/// 按加工进度“点亮”已加工轨迹，并绘制当前激光头位置。
/// 坐标系约定：左下角为原点 (0,0)，X 向右、Y 向上，单位 mm，与设备坐标读数一致。
/// </summary>
public sealed class MarkingPreview : FrameworkElement
{
    // 与本项目 Themes/Palette.xaml 保持一致的冻结画刷（自绘控件独立于资源字典，便于复用）
    private static readonly Brush BgBrush = Freeze(new SolidColorBrush(Parse("#0A0F1B")));
    private static readonly Brush GridBrush = Freeze(new SolidColorBrush(Parse("#152032")));
    private static readonly Brush GridStrongBrush = Freeze(new SolidColorBrush(Parse("#22304C")));
    private static readonly Brush PlateBrush = Freeze(new SolidColorBrush(Parse("#101A2C")));
    private static readonly Brush TickTextBrush = Freeze(new SolidColorBrush(Parse("#5F738E")));
    private static readonly Brush RawPathBrush = Freeze(new SolidColorBrush(Parse("#33455F")));
    private static readonly Brush LitPathBrush = Freeze(new SolidColorBrush(Parse("#3EC6FF")));
    private static readonly Brush LitGlowBrush = Freeze(new SolidColorBrush(Color.FromArgb(64, 0x3E, 0xC6, 0xFF)));
    private static readonly Brush LaserBrush = Freeze(new SolidColorBrush(Parse("#FF4D6A")));
    private static readonly Brush LaserCrossBrush = Freeze(new SolidColorBrush(Color.FromArgb(150, 0xFF, 0x4D, 0x6A)));

    private static readonly Pen GridPen = Freeze(new Pen(GridBrush, 1));
    private static readonly Pen GridStrongPen = Freeze(new Pen(GridStrongBrush, 1));
    private static readonly Pen FramePen = Freeze(new Pen(Freeze(new SolidColorBrush(Parse("#35496B"))), 1.2));
    private static readonly Pen RawPathPen = Freeze(new Pen(RawPathBrush, 1.4) { LineJoin = PenLineJoin.Round });
    private static readonly Pen LitPathPen = Freeze(new Pen(LitPathBrush, 1.8)
    {
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round,
        LineJoin = PenLineJoin.Round
    });
    private static readonly Pen LitGlowPen = Freeze(new Pen(LitGlowBrush, 5.5)
    {
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round,
        LineJoin = PenLineJoin.Round
    });
    private static readonly Pen LaserCrossPen = Freeze(new Pen(LaserCrossBrush, 1));

    private static readonly Typeface TickTypeface = new("Consolas");

    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
        nameof(Path), typeof(MarkingPath), typeof(MarkingPreview),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// 加工进度（0~1）：已加工轨迹按该比例点亮
    /// </summary>
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(MarkingPreview),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HeadPositionProperty = DependencyProperty.Register(
        nameof(HeadPosition), typeof(Point), typeof(MarkingPreview),
        new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsMarkingProperty = DependencyProperty.Register(
        nameof(IsMarking), typeof(bool), typeof(MarkingPreview),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid), typeof(bool), typeof(MarkingPreview),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public MarkingPath? Path
    {
        get => (MarkingPath?)GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Point HeadPosition
    {
        get => (Point)GetValue(HeadPositionProperty);
        set => SetValue(HeadPositionProperty, value);
    }

    public bool IsMarking
    {
        get => (bool)GetValue(IsMarkingProperty);
        set => SetValue(IsMarkingProperty, value);
    }

    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 60 || h < 60)
        {
            return;
        }

        dc.DrawRectangle(BgBrush, null, new Rect(0, 0, w, h));

        const double pad = 34;
        double scale = Math.Min((w - pad * 2) / MarkingPath.WorkAreaSize, (h - pad * 2) / MarkingPath.WorkAreaSize);
        double ox = (w - scale * MarkingPath.WorkAreaSize) / 2;
        double oy = (h - scale * MarkingPath.WorkAreaSize) / 2;

        // mm → 屏幕：Y 轴翻转，使原点位于左下角
        Point ToScreen(Point mm) =>
            new(ox + mm.X * scale, oy + (MarkingPath.WorkAreaSize - mm.Y) * scale);

        // ---- 幅面与网格 ----
        dc.DrawRectangle(PlateBrush, null, new Rect(ox, oy, scale * 100, scale * 100));

        if (ShowGrid)
        {
            for (int mm = 10; mm < 100; mm += 10)
            {
                bool strong = mm % 50 == 0;
                var pen = strong ? GridStrongPen : GridPen;

                dc.DrawLine(pen,
                    ToScreen(new Point(mm, 0)),
                    ToScreen(new Point(mm, 100)));
                dc.DrawLine(pen,
                    ToScreen(new Point(0, mm)),
                    ToScreen(new Point(100, mm)));
            }
        }

        dc.DrawRectangle(null, FramePen, new Rect(ox, oy, scale * 100, scale * 100));

        // ---- 坐标刻度（0 / 50 / 100） ----
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        foreach (int mm in (int[])[0, 50, 100])
        {
            var xText = MakeText(mm.ToString(CultureInfo.InvariantCulture), pixelsPerDip, 10);
            dc.DrawText(xText,
                new Point(ToScreen(new Point(mm, 0)).X - xText.Width / 2,
                          ToScreen(new Point(mm, 0)).Y + 6));

            var yLabel = mm.ToString(CultureInfo.InvariantCulture);
            var yText = MakeText(yLabel, pixelsPerDip, 10);
            dc.DrawText(yText,
                new Point(ToScreen(new Point(0, mm)).X - yText.Width - 7,
                          ToScreen(new Point(0, mm)).Y - yText.Height / 2));
        }

        // ---- 打标路径 ----
        var path = Path;
        if (path is not null && path.Segments.Count > 0)
        {
            double litLength = Math.Clamp(Progress, 0, 1) * path.TotalLength;
            double segmentStart = 0;

            for (int s = 0; s < path.Segments.Count; s++)
            {
                var mmPoints = path.Segments[s];

                dc.DrawGeometry(null, RawPathPen, BuildPolyline(mmPoints, ToScreen));

                double localLit = litLength - segmentStart;
                if (localLit > 0.01)
                {
                    var litPoints = CollectLitPoints(mmPoints, localLit);
                    if (litPoints is not null)
                    {
                        var litGeometry = BuildPolyline(litPoints, ToScreen);
                        dc.DrawGeometry(null, LitGlowPen, litGeometry);
                        dc.DrawGeometry(null, LitPathPen, litGeometry);
                    }
                }

                segmentStart += path.SegmentLengths[s];
            }
        }

        // ---- 激光头 ----
        if (IsMarking)
        {
            var head = ToScreen(HeadPosition);

            var glow = new RadialGradientBrush
            {
                GradientStops =
                [
                    new GradientStop(Color.FromArgb(120, 0xFF, 0x4D, 0x6A), 0),
                    new GradientStop(Color.FromArgb(0, 0xFF, 0x4D, 0x6A), 1)
                ],
                RadiusX = 0.5,
                RadiusY = 0.5
            };

            dc.DrawEllipse(glow, null, head, 16, 16);
            dc.DrawEllipse(LaserBrush, null, head, 3.4, 3.4);
            dc.DrawLine(LaserCrossPen, new Point(head.X - 12, head.Y), new Point(head.X + 12, head.Y));
            dc.DrawLine(LaserCrossPen, new Point(head.X, head.Y - 12), new Point(head.X, head.Y + 12));
        }
    }

    private static Geometry BuildPolyline(IReadOnlyList<Point> mmPoints, Func<Point, Point> toScreen)
    {
        var geometry = new StreamGeometry();
        using (StreamGeometryContext ctx = geometry.Open())
        {
            ctx.BeginFigure(toScreen(mmPoints[0]), false, false);
            for (int i = 1; i < mmPoints.Count; i++)
            {
                ctx.LineTo(toScreen(mmPoints[i]), true, false);
            }
        }
        geometry.Freeze();
        return geometry;
    }

    /// <summary>
    /// 截取折线前 litLength（mm）长度的已加工部分；不足两点时返回 null
    /// </summary>
    private static List<Point>? CollectLitPoints(IReadOnlyList<Point> mmPoints, double litLength)
    {
        var result = new List<Point> { mmPoints[0] };
        double acc = 0;

        for (int i = 1; i < mmPoints.Count; i++)
        {
            double d = (mmPoints[i] - mmPoints[i - 1]).Length;
            if (acc + d <= litLength + 1e-9)
            {
                result.Add(mmPoints[i]);
                acc += d;
            }
            else
            {
                double t = (litLength - acc) / d;
                if (t > 0.001)
                {
                    result.Add(new Point(
                        mmPoints[i - 1].X + (mmPoints[i].X - mmPoints[i - 1].X) * t,
                        mmPoints[i - 1].Y + (mmPoints[i].Y - mmPoints[i - 1].Y) * t));
                }
                break;
            }
        }

        return result.Count >= 2 ? result : null;
    }

    private static FormattedText MakeText(string text, double pixelsPerDip, double size) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            TickTypeface, size, TickTextBrush, pixelsPerDip);

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
