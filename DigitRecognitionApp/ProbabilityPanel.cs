using System.Drawing.Drawing2D;

namespace DigitRecognitionApp;

public sealed class ProbabilityPanel : Panel
{
    private double[] _values = new double[10];

    public ProbabilityPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        MinimumSize = new Size(300, 270);
    }

    public double[] Values
    {
        get => _values;
        set { _values = value is { Length: 10 } ? value.ToArray() : new double[10]; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int[] top = Enumerable.Range(0, 10).OrderByDescending(i => _values[i]).Take(3).ToArray();
        int topY = 8;
        int rowHeight = Math.Max(22, (Height - 16) / 10);
        int barX = 34;
        int valueWidth = 58;
        int barWidth = Math.Max(40, Width - barX - valueWidth - 14);

        for (int i = 0; i < 10; i++)
        {
            int y = topY + i * rowHeight;
            double p = Math.Clamp(_values[i], 0, 1);
            bool winner = top.Length > 0 && i == top[0];
            bool topThree = top.Contains(i);
            Color fillColor = winner ? UiTheme.Pink : topThree ? UiTheme.Accent : Color.FromArgb(87, 73, 116);
            using var labelBrush = new SolidBrush(winner ? UiTheme.Text : UiTheme.Muted);
            using var bg = new SolidBrush(UiTheme.Surface3);
            using var fill = new SolidBrush(fillColor);
            using var labelFont = new Font("Segoe UI", 9f, winner ? FontStyle.Bold : FontStyle.Regular);
            g.DrawString(i.ToString(), labelFont, labelBrush, 9, y + 3);
            g.FillRectangle(bg, barX, y + 6, barWidth, Math.Max(10, rowHeight - 10));
            g.FillRectangle(fill, barX, y + 6, (float)(barWidth * p), Math.Max(10, rowHeight - 10));
            g.DrawString($"{p * 100:0.0}%", labelFont, labelBrush, barX + barWidth + 5, y + 3);
        }
    }
}
