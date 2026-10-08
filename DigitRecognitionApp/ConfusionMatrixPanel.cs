namespace DigitRecognitionApp;

public sealed class ConfusionMatrixPanel : Panel
{
    private int[,] _matrix = new int[10, 10];

    public ConfusionMatrixPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        MinimumSize = new Size(420, 360);
    }

    public int[,] Matrix
    {
        get => _matrix;
        set { _matrix = value ?? new int[10, 10]; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        int left = 42, top = 34;
        int size = Math.Min(Width - left - 14, Height - top - 36);
        int cell = Math.Max(18, size / 10);
        int max = 1;
        foreach (int v in _matrix) max = Math.Max(max, v);
        using var muted = new SolidBrush(UiTheme.Muted);
        using var small = new Font("Segoe UI", 7.5f, FontStyle.Bold);

        for (int i = 0; i < 10; i++)
        {
            g.DrawString(i.ToString(), small, muted, left + i * cell + cell / 2f - 3, 12);
            g.DrawString(i.ToString(), small, muted, 16, top + i * cell + cell / 2f - 6);
        }

        for (int r = 0; r < 10; r++)
        for (int c = 0; c < 10; c++)
        {
            double p = _matrix[r, c] / (double)max;
            Color baseColor = r == c ? UiTheme.Green : UiTheme.Pink;
            Color color = Blend(UiTheme.Surface2, baseColor, Math.Sqrt(p));
            Rectangle rect = new(left + c * cell, top + r * cell, cell - 2, cell - 2);
            using var bg = new SolidBrush(color);
            g.FillRectangle(bg, rect);
            string text = _matrix[r, c].ToString();
            Size textSize = TextRenderer.MeasureText(text, small);
            TextRenderer.DrawText(g, text, small, rect, UiTheme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        g.DrawString("Gerçek sınıf", Font, muted, 8, top + 10 * cell + 7);
        g.DrawString("Tahmin →", Font, muted, left + 10 * cell - 70, top + 10 * cell + 7);
    }

    private static Color Blend(Color a, Color b, double t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t),
        (int)(a.B + (b.B - a.B) * t));
}
