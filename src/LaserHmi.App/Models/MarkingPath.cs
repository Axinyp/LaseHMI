using System.Windows;

namespace LaserHmi.App.Models;

/// <summary>
/// 演示用打标路径：由若干条折线路径段组成（圆弧已采样为折线）。
/// 提供按弧长取点能力，用于加工进度动画与激光头位置模拟。
/// </summary>
public sealed class MarkingPath
{
    /// <summary>
    /// 振镜加工幅面：100mm × 100mm
    /// </summary>
    public const double WorkAreaSize = 100.0;

    private readonly double[] _segmentStartLengths;
    private readonly double[] _segmentLengths;

    public IReadOnlyList<Point[]> Segments { get; }

    /// <summary>
    /// 各段长度（mm），索引与 <see cref="Segments"/> 对应
    /// </summary>
    public IReadOnlyList<double> SegmentLengths => _segmentLengths;

    public double TotalLength { get; }

    private MarkingPath(IReadOnlyList<Point[]> segments)
    {
        Segments = segments;
        _segmentStartLengths = new double[segments.Count];
        _segmentLengths = new double[segments.Count];
        double acc = 0;
        for (int i = 0; i < segments.Count; i++)
        {
            _segmentStartLengths[i] = acc;
            _segmentLengths[i] = PolylineLength(segments[i]);
            acc += _segmentLengths[i];
        }
        TotalLength = acc;
    }

    /// <summary>
    /// 构建演示图案：圆角外框 → 圆环 → 五角星 → 中心圆。
    /// 按“先外形后细节”的工艺习惯排序，加工动画层次清晰。
    /// </summary>
    public static MarkingPath CreateDemo()
    {
        var segments = new List<Point[]>
        {
            RoundedRect(18, 18, 82, 82, 6),
            Circle(new Point(50, 50), 24),
            Star(new Point(50, 50), outerRadius: 17, innerRadius: 6.5),
            Circle(new Point(50, 50), 5.5, steps: 24)
        };
        return new MarkingPath(segments);
    }

    /// <summary>
    /// 按弧长（mm）取路径上的点；超出范围自动收敛到端点
    /// </summary>
    public Point GetPointAtLength(double length)
    {
        length = Math.Clamp(length, 0, TotalLength);

        int seg = 0;
        for (int i = 1; i < Segments.Count; i++)
        {
            if (length >= _segmentStartLengths[i]) seg = i;
            else break;
        }

        var pts = Segments[seg];
        double local = length - _segmentStartLengths[seg];
        double acc = 0;
        for (int i = 1; i < pts.Length; i++)
        {
            double d = (pts[i] - pts[i - 1]).Length;
            if (acc + d >= local)
            {
                double t = d <= 1e-9 ? 0 : (local - acc) / d;
                return new Point(
                    pts[i - 1].X + (pts[i].X - pts[i - 1].X) * t,
                    pts[i - 1].Y + (pts[i].Y - pts[i - 1].Y) * t);
            }
            acc += d;
        }
        return pts[^1];
    }

    private static double PolylineLength(Point[] pts)
    {
        double acc = 0;
        for (int i = 1; i < pts.Length; i++)
        {
            acc += (pts[i] - pts[i - 1]).Length;
        }
        return acc;
    }

    // ---------- 图形采样 ----------

    private static Point[] Circle(Point center, double radius, int steps = 72)
    {
        var pts = new Point[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            double a = i * 2 * Math.PI / steps;
            pts[i] = new Point(center.X + radius * Math.Cos(a), center.Y + radius * Math.Sin(a));
        }
        return pts;
    }

    private static Point[] RoundedRect(double x1, double y1, double x2, double y2, double r, int arcSteps = 5)
    {
        var pts = new List<Point> { new(x1 + r, y1) };

        void Arc(double cx, double cy, double startDeg, double endDeg)
        {
            for (int i = 1; i <= arcSteps; i++)
            {
                double a = (startDeg + (endDeg - startDeg) * i / arcSteps) * Math.PI / 180.0;
                pts.Add(new Point(cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
            }
        }

        pts.Add(new Point(x2 - r, y1));
        Arc(x2 - r, y1 + r, -90, 0);
        pts.Add(new Point(x2, y2 - r));
        Arc(x2 - r, y2 - r, 0, 90);
        pts.Add(new Point(x1 + r, y2));
        Arc(x1 + r, y2 - r, 90, 180);
        pts.Add(new Point(x1, y1 + r));
        Arc(x1 + r, y1 + r, 180, 270);
        pts.Add(new Point(x1 + r, y1));
        return pts.ToArray();
    }

    private static Point[] Star(Point center, double outerRadius, double innerRadius, int points = 5)
    {
        var pts = new Point[points * 2 + 1];
        for (int i = 0; i < points * 2; i++)
        {
            double r = i % 2 == 0 ? outerRadius : innerRadius;
            double a = (-90 + i * 180.0 / points) * Math.PI / 180.0;
            pts[i] = new Point(center.X + r * Math.Cos(a), center.Y + r * Math.Sin(a));
        }
        pts[^1] = pts[0];
        return pts;
    }
}
