namespace DigitRecognitionApp;

public sealed class HiddenActivationPanel : Panel
{
    private double[] _values = Array.Empty<double>();

    public HiddenActivationPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        MinimumSize = new Size(360, 230);
    }

    public double[] Values
    {
        get => _values;
        set { _values = value?.ToArray() ?? Array.Empty<double>(); Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        using var text = new SolidBrush(UiTheme.Muted);
        if (_values.Length == 0)
        {
            g.DrawString("Tahmin yaptıktan sonra 128 hidden neuron aktivasyonu burada görünür.", Font, text, 12, 12);
            return;
        }

        const int cols = 16;
        int rows = (int)Math.Ceiling(_values.Length / (double)cols);
        float gap = 3f;
        float cellW = (Width - 24 - (cols - 1) * gap) / cols;
        float cellH = (Height - 48 - (rows - 1) * gap) / rows;
        for (int i = 0; i < _values.Length; i++)
        {
            int row = i / cols, col = i % cols;
            double v = Math.Clamp(_values[i], 0, 1);
            Color c = Blend(UiTheme.Surface3, UiTheme.Pink, v);
            using var brush = new SolidBrush(c);
            g.FillRectangle(brush, 12 + col * (cellW + gap), 26 + row * (cellH + gap), cellW, cellH);
        }
        g.DrawString("Hidden layer activations (128 sigmoid neurons)", Font, text, 12, 6);
    }

    private static Color Blend(Color a, Color b, double t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t),
        (int)(a.B + (b.B - a.B) * t));
}
