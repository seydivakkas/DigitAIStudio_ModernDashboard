using System.Drawing.Drawing2D;

namespace DigitRecognitionApp;

public sealed class PixelPreviewPanel : Panel
{
    private double[] _values = new double[784];

    public PixelPreviewPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.Black;
        MinimumSize = new Size(112, 112);
    }

    public double[] Values
    {
        get => _values;
        set
        {
            _values = value is { Length: 784 } ? value.ToArray() : new double[784];
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        float cellW = Width / 28f;
        float cellH = Height / 28f;
        for (int y = 0; y < 28; y++)
        {
            for (int x = 0; x < 28; x++)
            {
                int c = (int)Math.Round(Math.Clamp(_values[y * 28 + x], 0.0, 1.0) * 255);
                using var brush = new SolidBrush(Color.FromArgb(c, c, c));
                e.Graphics.FillRectangle(brush, x * cellW, y * cellH, cellW + 0.7f, cellH + 0.7f);
            }
        }
        using var border = new Pen(UiTheme.Border);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }
}
