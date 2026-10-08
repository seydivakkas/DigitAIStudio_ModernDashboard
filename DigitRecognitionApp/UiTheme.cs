namespace DigitRecognitionApp;

internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(15, 12, 24);
    public static readonly Color Surface = Color.FromArgb(27, 22, 42);
    public static readonly Color Surface2 = Color.FromArgb(37, 30, 56);
    public static readonly Color Surface3 = Color.FromArgb(48, 39, 70);
    public static readonly Color Border = Color.FromArgb(72, 61, 96);
    public static readonly Color Text = Color.FromArgb(246, 242, 255);
    public static readonly Color Muted = Color.FromArgb(167, 156, 188);
    public static readonly Color Accent = Color.FromArgb(157, 82, 255);
    public static readonly Color Pink = Color.FromArgb(255, 86, 166);
    public static readonly Color Cyan = Color.FromArgb(91, 205, 255);
    public static readonly Color Green = Color.FromArgb(76, 218, 160);
    public static readonly Color Warning = Color.FromArgb(255, 190, 91);
    public static readonly Color Error = Color.FromArgb(255, 99, 120);

    public static Button Button(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Height = 38,
            Width = 126,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Surface3,
            ForeColor = Text,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(4)
        };
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(177, 110, 255) : Color.FromArgb(60, 50, 84);
        return button;
    }

    public static Label Heading(string text, float size = 18f) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Text,
        Font = new Font("Segoe UI", size, FontStyle.Bold)
    };

    public static Label MutedLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Muted,
        Font = new Font("Segoe UI", 9f)
    };

    public static void StyleInput(Control control)
    {
        control.BackColor = Surface2;
        control.ForeColor = Text;
        control.Font = new Font("Segoe UI", 9.5f);
    }
}
