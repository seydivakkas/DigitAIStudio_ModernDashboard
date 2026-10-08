namespace DigitRecognitionApp;

public sealed class ModernTabControl : TabControl
{
    public ModernTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        ItemSize = new Size(126, 34);
        SizeMode = TabSizeMode.Fixed;
        Padding = new Point(10, 4);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        TabPage page = TabPages[e.Index];
        Rectangle bounds = GetTabRect(e.Index);
        bool selected = e.Index == SelectedIndex;
        using var bg = new SolidBrush(selected ? UiTheme.Surface3 : UiTheme.Surface);
        using var text = new SolidBrush(selected ? UiTheme.Text : UiTheme.Muted);
        e.Graphics.FillRectangle(bg, bounds);
        TextRenderer.DrawText(e.Graphics, page.Text, Font, bounds, text.Color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (selected)
        {
            using var accent = new Pen(UiTheme.Accent, 3f);
            e.Graphics.DrawLine(accent, bounds.Left + 8, bounds.Bottom - 2, bounds.Right - 8, bounds.Bottom - 2);
        }
    }
}
