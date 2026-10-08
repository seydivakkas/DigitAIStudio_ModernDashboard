using ML.Core.DigitRecognition;

namespace DigitRecognitionApp;

public sealed class MisclassifiedGalleryPanel : Panel
{
    private MnistDataset? _dataset;
    private IReadOnlyList<DigitPredictionSample> _items = Array.Empty<DigitPredictionSample>();
    private readonly List<Rectangle> _itemBounds = new();

    public MisclassifiedGalleryPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        AutoScroll = true;
        Cursor = Cursors.Hand;
    }

    public event Action<int>? SampleSelected;

    public void SetItems(MnistDataset? dataset, IReadOnlyList<DigitPredictionSample>? items)
    {
        _dataset = dataset;
        _items = items ?? Array.Empty<DigitPredictionSample>();
        AutoScrollPosition = Point.Empty;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
        _itemBounds.Clear();

        if (_dataset is null || _items.Count == 0)
        {
            using var b = new SolidBrush(UiTheme.Muted);
            g.DrawString("Değerlendirmeden sonra yanlış tahmin edilen örnekler burada gösterilir.", Font, b, 12, 12);
            return;
        }

        int cardW = 118, cardH = 142, gap = 10;
        int cols = Math.Max(1, (ClientSize.Width - 8) / (cardW + gap));
        for (int n = 0; n < _items.Count; n++)
        {
            int row = n / cols, col = n % cols;
            Rectangle card = new(8 + col * (cardW + gap), 8 + row * (cardH + gap), cardW, cardH);
            _itemBounds.Add(card);
            using var bg = new SolidBrush(UiTheme.Surface2);
            using var border = new Pen(UiTheme.Border);
            g.FillRectangle(bg, card);
            g.DrawRectangle(border, card);

            DrawDigit(g, _dataset.GetRawImage(_items[n].Index), new Rectangle(card.X + 17, card.Y + 9, 84, 84));
            using var text = new SolidBrush(UiTheme.Text);
            using var muted = new SolidBrush(UiTheme.Muted);
            using var bold = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            g.DrawString($"Gerçek {_items[n].Actual}  →  {_items[n].Predicted}", bold, text, card.X + 9, card.Y + 99);
            g.DrawString($"Confidence {_items[n].Confidence * 100:0.0}%", Font, muted, card.X + 9, card.Y + 119);
        }
        int rows = (int)Math.Ceiling(_items.Count / (double)cols);
        AutoScrollMinSize = new Size(0, 16 + rows * (cardH + gap));
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        Point p = new(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);
        for (int i = 0; i < _itemBounds.Count; i++)
        {
            if (_itemBounds[i].Contains(p))
            {
                SampleSelected?.Invoke(_items[i].Index);
                return;
            }
        }
    }

    private static void DrawDigit(Graphics g, byte[] pixels, Rectangle bounds)
    {
        float cw = bounds.Width / 28f, ch = bounds.Height / 28f;
        using var black = new SolidBrush(Color.Black);
        g.FillRectangle(black, bounds);
        for (int y = 0; y < 28; y++)
        for (int x = 0; x < 28; x++)
        {
            int v = pixels[y * 28 + x];
            if (v < 8) continue;
            using var b = new SolidBrush(Color.FromArgb(v, v, v));
            g.FillRectangle(b, bounds.X + x * cw, bounds.Y + y * ch, cw + 0.5f, ch + 0.5f);
        }
    }
}
