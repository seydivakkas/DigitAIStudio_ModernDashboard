namespace DigitRecognitionApp;

public sealed class MetricCard : Panel
{
    private readonly Label _title;
    private readonly Label _value;

    public MetricCard(string title, string value = "-")
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface2;
        Padding = new Padding(10, 7, 10, 7);
        Height = 62;
        MinimumSize = new Size(118, 62);

        _title = new Label
        {
            Text = title.ToUpperInvariant(), AutoSize = true, ForeColor = UiTheme.Muted,
            Font = new Font("Segoe UI", 7.8f, FontStyle.Bold), Location = new Point(10, 7)
        };
        _value = new Label
        {
            Text = value, AutoSize = true, ForeColor = UiTheme.Text,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold), Location = new Point(9, 27)
        };
        Controls.Add(_title);
        Controls.Add(_value);
    }

    public string Value
    {
        get => _value.Text;
        set => _value.Text = value;
    }
}
