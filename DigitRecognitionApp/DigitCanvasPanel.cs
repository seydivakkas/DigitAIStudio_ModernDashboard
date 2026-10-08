using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DigitRecognitionApp;

public sealed class DigitCanvasPanel : Panel
{
    private readonly Bitmap _canvas;
    private readonly Stack<Bitmap> _undo = new();
    private Point _lastPoint;
    private bool _drawing;

    public DigitCanvasPanel()
    {
        DoubleBuffered = true;
        Size = new Size(280, 280);
        MinimumSize = new Size(280, 280);
        MaximumSize = new Size(280, 280);
        BackColor = Color.Black;
        Cursor = Cursors.Cross;
        _canvas = new Bitmap(280, 280, PixelFormat.Format24bppRgb);
        ClearCanvas(pushUndo: false);
    }

    public event EventHandler? CanvasChanged;
    public int PenWidth { get; set; } = 24;
    public bool EraserEnabled { get; set; }
    public bool CanUndo => _undo.Count > 0;

    public void ClearCanvas(bool pushUndo = true)
    {
        if (pushUndo) PushUndo();
        using Graphics g = Graphics.FromImage(_canvas);
        g.Clear(Color.Black);
        Invalidate();
        CanvasChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (_undo.Count == 0) return;
        using Bitmap previous = _undo.Pop();
        using Graphics g = Graphics.FromImage(_canvas);
        g.DrawImageUnscaled(previous, 0, 0);
        Invalidate();
        CanvasChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool HasInk()
    {
        Rectangle bounds = FindInkBounds();
        return !bounds.IsEmpty;
    }

    public double[] GetMnistVector()
    {
        Rectangle bounds = FindInkBounds();
        if (bounds.IsEmpty)
            return new double[28 * 28];

        using var normalized = new Bitmap(28, 28, PixelFormat.Format24bppRgb);
        using (Graphics g = Graphics.FromImage(normalized))
        {
            g.Clear(Color.Black);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            const int targetBox = 20;
            double scale = Math.Min(targetBox / (double)bounds.Width, targetBox / (double)bounds.Height);
            int scaledWidth = Math.Max(1, (int)Math.Round(bounds.Width * scale));
            int scaledHeight = Math.Max(1, (int)Math.Round(bounds.Height * scale));
            int x = (28 - scaledWidth) / 2;
            int y = (28 - scaledHeight) / 2;
            g.DrawImage(_canvas, new Rectangle(x, y, scaledWidth, scaledHeight), bounds, GraphicsUnit.Pixel);
        }

        var vector = new double[784];
        double mass = 0.0;
        double weightedX = 0.0;
        double weightedY = 0.0;

        for (int y = 0; y < 28; y++)
        {
            for (int x = 0; x < 28; x++)
            {
                Color c = normalized.GetPixel(x, y);
                double v = (c.R + c.G + c.B) / (3.0 * 255.0);
                v = v < 0.06 ? 0.0 : Math.Min(1.0, v * 1.8);
                vector[y * 28 + x] = v;
                mass += v;
                weightedX += x * v;
                weightedY += y * v;
            }
        }

        if (mass > 1e-9)
        {
            double centerX = weightedX / mass;
            double centerY = weightedY / mass;
            int shiftX = (int)Math.Round(13.5 - centerX);
            int shiftY = (int)Math.Round(13.5 - centerY);
            if (shiftX != 0 || shiftY != 0)
                vector = Shift(vector, shiftX, shiftY);
        }

        return vector;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.DrawImage(_canvas, ClientRectangle);
        using var border = new Pen(EraserEnabled ? UiTheme.Pink : UiTheme.Accent, 2f);
        e.Graphics.DrawRectangle(border, 1, 1, Width - 3, Height - 3);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        PushUndo();
        _drawing = true;
        _lastPoint = e.Location;
        DrawStroke(e.Location, e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_drawing || e.Button != MouseButtons.Left) return;
        DrawStroke(_lastPoint, e.Location);
        _lastPoint = e.Location;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        _drawing = false;
        CanvasChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DrawStroke(Point from, Point to)
    {
        using Graphics g = Graphics.FromImage(_canvas);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(EraserEnabled ? Color.Black : Color.White, PenWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        g.DrawLine(pen, from, to);
        Invalidate();
    }

    private void PushUndo()
    {
        _undo.Push((Bitmap)_canvas.Clone());
        while (_undo.Count > 20)
        {
            Bitmap[] items = _undo.ToArray();
            _undo.Clear();
            for (int i = Math.Min(19, items.Length - 1); i >= 0; i--)
                _undo.Push(items[i]);
            foreach (Bitmap item in items.Skip(20)) item.Dispose();
        }
    }

    private Rectangle FindInkBounds()
    {
        int minX = _canvas.Width, minY = _canvas.Height, maxX = -1, maxY = -1;
        for (int y = 0; y < _canvas.Height; y += 2)
        {
            for (int x = 0; x < _canvas.Width; x += 2)
            {
                Color c = _canvas.GetPixel(x, y);
                if (c.R < 12 && c.G < 12 && c.B < 12) continue;
                minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
            }
        }
        if (maxX < minX || maxY < minY) return Rectangle.Empty;

        const int padding = 8;
        minX = Math.Max(0, minX - padding); minY = Math.Max(0, minY - padding);
        maxX = Math.Min(_canvas.Width - 1, maxX + padding); maxY = Math.Min(_canvas.Height - 1, maxY + padding);
        return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    private static double[] Shift(double[] source, int dx, int dy)
    {
        var result = new double[source.Length];
        for (int y = 0; y < 28; y++)
        for (int x = 0; x < 28; x++)
        {
            int nx = x + dx, ny = y + dy;
            if ((uint)nx < 28 && (uint)ny < 28)
                result[ny * 28 + nx] = source[y * 28 + x];
        }
        return result;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _canvas.Dispose();
            foreach (Bitmap bitmap in _undo) bitmap.Dispose();
        }
        base.Dispose(disposing);
    }
}
