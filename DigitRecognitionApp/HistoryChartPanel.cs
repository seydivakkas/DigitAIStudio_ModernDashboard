using System.Drawing.Drawing2D;

namespace DigitRecognitionApp;

public sealed class HistoryChartPanel : Panel
{
    private IReadOnlyList<double> _values = Array.Empty<double>();

    public HistoryChartPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        MinimumSize = new Size(280, 170);
    }

    public string Title { get; set; } = "History";
    public bool PercentageAxis { get; set; }
    public Color LineColor { get; set; } = UiTheme.Accent;

    public IReadOnlyList<double> Values
    {
        get => _values;
        set { _values = value ?? Array.Empty<double>(); Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(UiTheme.Text);
        using var mutedBrush = new SolidBrush(UiTheme.Muted);
        g.DrawString(Title, titleFont, titleBrush, 12, 9);

        Rectangle area = new(48, 36, Math.Max(20, Width - 66), Math.Max(20, Height - 66));
        using var gridPen = new Pen(UiTheme.Border);
        for (int i = 0; i <= 4; i++)
        {
            float y = area.Top + i * area.Height / 4f;
            g.DrawLine(gridPen, area.Left, y, area.Right, y);
        }

        if (_values.Count == 0)
        {
            g.DrawString("Henüz veri yok", Font, mutedBrush, area.Left + 12, area.Top + 14);
            return;
        }

        double min = PercentageAxis ? 0 : _values.Where(double.IsFinite).DefaultIfEmpty(0).Min();
        double max = PercentageAxis ? 100 : _values.Where(double.IsFinite).DefaultIfEmpty(1).Max();
        if (Math.Abs(max - min) < 1e-12) max = min + 1;

        if (_values.Count == 1)
        {
            float px = area.Left + area.Width / 2f;
            float py = area.Bottom - (float)((_values[0] - min) / (max - min) * area.Height);
            using var b = new SolidBrush(LineColor);
            g.FillEllipse(b, px - 3, py - 3, 6, 6);
            return;
        }

        var points = new PointF[_values.Count];
        for (int i = 0; i < _values.Count; i++)
        {
            double value = double.IsFinite(_values[i]) ? _values[i] : min;
            points[i] = new PointF(
                area.Left + i * area.Width / (float)(_values.Count - 1),
                area.Bottom - (float)((value - min) / (max - min) * area.Height));
        }
        using var pen = new Pen(LineColor, 2.5f);
        g.DrawLines(pen, points);
        g.DrawString(PercentageAxis ? "100%" : max.ToString("0.####"), Font, mutedBrush, 5, area.Top - 6);
        g.DrawString(PercentageAxis ? "0%" : min.ToString("0.####"), Font, mutedBrush, 5, area.Bottom - 12);
        g.DrawString("Epoch", Font, mutedBrush, area.Right - 38, area.Bottom + 7);
    }
}
