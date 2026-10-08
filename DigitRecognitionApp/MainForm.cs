using System.Diagnostics;
using ML.Core.DigitRecognition;

namespace DigitRecognitionApp;

public sealed class MainForm : Form
{
    private readonly DigitCanvasPanel _canvas = new();
    private readonly PixelPreviewPanel _preprocessPreview = new() { Dock = DockStyle.Fill };
    private readonly PixelPreviewPanel _aeOriginalPreview = new() { Dock = DockStyle.Fill };
    private readonly PixelPreviewPanel _aeReconstructionPreview = new() { Dock = DockStyle.Fill };
    private readonly PixelPreviewPanel _testPreview = new() { Dock = DockStyle.Fill };
    private readonly ProbabilityPanel _probabilities = new() { Dock = DockStyle.Fill };
    private readonly HiddenActivationPanel _hiddenActivations = new() { Dock = DockStyle.Fill };
    private readonly ConfusionMatrixPanel _confusionMatrix = new() { Dock = DockStyle.Fill };
    private readonly MisclassifiedGalleryPanel _wrongGallery = new() { Dock = DockStyle.Fill };
    private readonly HistoryChartPanel _lossChart = new() { Dock = DockStyle.Fill, Title = "Classifier Cross-Entropy Loss", LineColor = UiTheme.Pink };
    private readonly HistoryChartPanel _accuracyChart = new() { Dock = DockStyle.Fill, Title = "Training Accuracy", PercentageAxis = true, LineColor = UiTheme.Green };
    private readonly RichTextBox _log = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None };

    private readonly NumericUpDown _trainSamples = new() { Minimum = 100, Maximum = 60000, Value = 2000, Increment = 100, Width = 118 };
    private readonly NumericUpDown _epochs = new() { Minimum = 1, Maximum = 200, Value = 5, Width = 118 };
    private readonly NumericUpDown _learningRate = new() { DecimalPlaces = 4, Minimum = 0.0001m, Maximum = 1m, Value = 0.05m, Increment = 0.005m, Width = 118 };
    private readonly NumericUpDown _momentum = new() { DecimalPlaces = 2, Minimum = 0m, Maximum = 0.99m, Value = 0.90m, Increment = 0.05m, Width = 118 };
    private readonly CheckBox _useAutoencoder = new() { Text = "Autoencoder pretraining", Checked = true, AutoSize = true };
    private readonly NumericUpDown _autoencoderEpochs = new() { Minimum = 1, Maximum = 50, Value = 1, Width = 118 };
    private readonly NumericUpDown _evaluationSamples = new() { Minimum = 10, Maximum = 10000, Value = 500, Increment = 50, Width = 118 };
    private readonly TrackBar _penWidth = new() { Minimum = 8, Maximum = 44, Value = 24, TickFrequency = 6, Width = 210 };

    private readonly Button _datasetButton = UiTheme.Button("MNIST SELECT", true);
    private readonly Button _trainButton = UiTheme.Button("TRAIN MODEL", true);
    private readonly Button _cancelButton = UiTheme.Button("CANCEL");
    private readonly Button _evaluateButton = UiTheme.Button("EVALUATE", true);
    private readonly Button _saveButton = UiTheme.Button("SAVE MODEL");
    private readonly Button _loadButton = UiTheme.Button("LOAD MODEL");
    private readonly Button _predictButton = UiTheme.Button("PREDICT", true);
    private readonly Button _undoButton = UiTheme.Button("UNDO");
    private readonly Button _eraserButton = UiTheme.Button("ERASER");
    private readonly Button _clearButton = UiTheme.Button("CLEAR");
    private readonly Button _nextTestButton = UiTheme.Button("NEXT SAMPLE");
    private readonly Button _nextWrongButton = UiTheme.Button("NEXT WRONG");

    private readonly Label _datasetLabel = UiTheme.MutedLabel("MNIST dataset not loaded");
    private readonly Label _statusLabel = UiTheme.MutedLabel("Ready");
    private readonly Label _predictionDigit = new()
    {
        Text = "?", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = UiTheme.Text, Font = new Font("Segoe UI", 58f, FontStyle.Bold)
    };
    private readonly Label _predictionConfidence = new()
    {
        Text = "confidence -", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopCenter,
        ForeColor = UiTheme.Pink, Font = new Font("Segoe UI", 14f, FontStyle.Bold)
    };
    private readonly Label[] _top3Labels = Enumerable.Range(0, 3).Select(_ => new Label
    {
        AutoSize = true, ForeColor = UiTheme.Text, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Margin = new Padding(5)
    }).ToArray();
    private readonly Label _testSampleLabel = UiTheme.MutedLabel("Test sample: -");
    private readonly Label _aeStatusLabel = UiTheme.MutedLabel("Autoencoder reconstruction requires a pretrained/loaded decoder.");
    private readonly Label _modelBadge = new()
    {
        Text = "NO MODEL", AutoSize = true, ForeColor = UiTheme.Warning,
        Font = new Font("Segoe UI", 9f, FontStyle.Bold), Padding = new Padding(8, 4, 8, 4), BackColor = UiTheme.Surface3
    };

    private readonly MetricCard _testAccuracyMetric = new("Test accuracy");
    private readonly MetricCard _trainingTimeMetric = new("Training time");
    private readonly MetricCard _latencyMetric = new("Prediction latency");
    private readonly MetricCard _aeMetric = new("Autoencoder");
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Height = 10 };

    private MnistDataset? _trainData;
    private MnistDataset? _testData;
    private NeuralNetwork? _model;
    private DigitEvaluationResult? _lastEvaluation;
    private CancellationTokenSource? _trainingCts;
    private int _currentTestIndex = -1;
    private int _wrongCursor = -1;

    public MainForm()
    {
        Text = "NEURAL DIGIT // Handwritten Digit Recognition";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1420, 860);
        Size = new Size(1660, 980);
        BackColor = UiTheme.Background;
        ForeColor = UiTheme.Text;
        Font = new Font("Segoe UI", 9.5f);

        foreach (Control input in new Control[] { _trainSamples, _epochs, _learningRate, _momentum, _autoencoderEpochs, _evaluationSamples, _penWidth })
            UiTheme.StyleInput(input);
        _useAutoencoder.ForeColor = UiTheme.Text;
        _useAutoencoder.BackColor = UiTheme.Surface;
        _log.BackColor = UiTheme.Surface2;
        _log.ForeColor = UiTheme.Text;
        _cancelButton.Enabled = false;
        _evaluateButton.Enabled = false;
        _saveButton.Enabled = false;
        _predictButton.Enabled = false;
        _nextTestButton.Enabled = false;
        _nextWrongButton.Enabled = false;

        WireEvents();
        Controls.Add(BuildRoot());
        UpdateCanvasPreview();
        AppendLog("AI dashboard ready. Select an MNIST folder or load a saved .dnn model.");
    }

    private void WireEvents()
    {
        _datasetButton.Click += async (_, _) => await SelectDatasetAsync();
        _trainButton.Click += async (_, _) => await TrainAsync();
        _cancelButton.Click += (_, _) => _trainingCts?.Cancel();
        _evaluateButton.Click += async (_, _) => await EvaluateAsync();
        _saveButton.Click += (_, _) => SaveModel();
        _loadButton.Click += (_, _) => LoadModel();
        _predictButton.Click += (_, _) => PredictCanvas();
        _undoButton.Click += (_, _) => _canvas.Undo();
        _clearButton.Click += (_, _) =>
        {
            _canvas.ClearCanvas();
            ResetPredictionView();
        };
        _eraserButton.Click += (_, _) => ToggleEraser();
        _penWidth.ValueChanged += (_, _) => _canvas.PenWidth = _penWidth.Value;
        _canvas.CanvasChanged += (_, _) => UpdateCanvasPreview();
        _useAutoencoder.CheckedChanged += (_, _) => _autoencoderEpochs.Enabled = _useAutoencoder.Checked;
        _nextTestButton.Click += (_, _) => ShowNextTestSample();
        _nextWrongButton.Click += (_, _) => ShowNextWrongSample();
        _wrongGallery.SampleSelected += index => ShowTestSample(index);
        FormClosing += (_, _) => _trainingCts?.Cancel();
    }

    private Control BuildRoot()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3,
            Padding = new Padding(14), BackColor = UiTheme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 238));

        Control header = BuildHeader();
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 3);

        Control training = BuildTrainingCard();
        root.Controls.Add(training, 0, 1);
        root.SetRowSpan(training, 2);
        root.Controls.Add(BuildCanvasCard(), 1, 1);
        root.Controls.Add(BuildInsights(), 2, 1);

        Control history = BuildHistoryStrip();
        root.Controls.Add(history, 1, 2);
        root.SetColumnSpan(history, 2);
        return root;
    }

    private Control BuildHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
        var title = UiTheme.Heading("NEURAL DIGIT", 22f);
        title.Location = new Point(4, 5);
        var subtitle = UiTheme.MutedLabel("from-scratch neural network • MNIST • autoencoder • visual inference lab");
        subtitle.Location = new Point(7, 42);
        _modelBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _modelBadge.Location = new Point(Width - 150, 14);
        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        panel.Controls.Add(_modelBadge);
        panel.Resize += (_, _) => _modelBadge.Left = panel.ClientSize.Width - _modelBadge.Width - 10;
        return panel;
    }

    private Control BuildTrainingCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 10, 0), AutoScroll = true };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = UiTheme.Surface, Padding = new Padding(4), Margin = new Padding(0)
        };

        flow.Controls.Add(UiTheme.Heading("Model Training", 16f));
        flow.Controls.Add(Description("784 → 128 Sigmoid → 10 Softmax\nManual forward/backprop + momentum SGD"));
        flow.Controls.Add(Section("MNIST DATASET"));
        flow.Controls.Add(WideButton(_datasetButton, 270));
        _datasetLabel.MaximumSize = new Size(270, 0);
        flow.Controls.Add(_datasetLabel);

        flow.Controls.Add(Section("TRAINING PARAMETERS"));
        flow.Controls.Add(Field("Train samples", _trainSamples));
        flow.Controls.Add(Field("Epoch", _epochs));
        flow.Controls.Add(Field("Learning rate", _learningRate));
        flow.Controls.Add(Field("Momentum", _momentum));
        flow.Controls.Add(_useAutoencoder);
        flow.Controls.Add(Field("AE epoch", _autoencoderEpochs));

        var trainButtons = new FlowLayoutPanel { Width = 280, Height = 46, WrapContents = false, BackColor = UiTheme.Surface };
        trainButtons.Controls.Add(_trainButton);
        trainButtons.Controls.Add(_cancelButton);
        flow.Controls.Add(trainButtons);

        flow.Controls.Add(Section("EVALUATION"));
        flow.Controls.Add(Field("Test samples", _evaluationSamples));
        flow.Controls.Add(WideButton(_evaluateButton, 270));

        flow.Controls.Add(Section("MODEL FILE"));
        var fileButtons = new FlowLayoutPanel { Width = 280, Height = 46, WrapContents = false, BackColor = UiTheme.Surface };
        fileButtons.Controls.Add(_loadButton);
        fileButtons.Controls.Add(_saveButton);
        flow.Controls.Add(fileButtons);

        flow.Controls.Add(Section("STATUS"));
        _statusLabel.MaximumSize = new Size(270, 0);
        flow.Controls.Add(_statusLabel);
        var progressHost = new Panel { Width = 270, Height = 18, BackColor = UiTheme.Surface, Padding = new Padding(0, 5, 0, 3) };
        progressHost.Controls.Add(_progress);
        flow.Controls.Add(progressHost);

        card.Controls.Add(flow);
        return card;
    }

    private Control BuildCanvasCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 10, 0) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, BackColor = UiTheme.Surface };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 292));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heading = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        var h1 = UiTheme.Heading("Drawing Canvas", 15f); h1.Location = new Point(4, 2);
        var h2 = UiTheme.MutedLabel("Draw a centered, thick digit. Input is normalized to MNIST geometry."); h2.Location = new Point(5, 29);
        heading.Controls.Add(h1); heading.Controls.Add(h2);

        var canvasHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        _canvas.Location = new Point(61, 4);
        canvasHost.Controls.Add(_canvas);

        var toolRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = UiTheme.Surface, Padding = new Padding(2, 5, 0, 0) };
        foreach (Button b in new[] { _undoButton, _eraserButton, _clearButton }) { b.Width = 104; toolRow.Controls.Add(b); }

        var penRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = UiTheme.Surface, Padding = new Padding(3, 2, 0, 0) };
        penRow.Controls.Add(new Label { Text = "Brush", AutoSize = true, ForeColor = UiTheme.Muted, Margin = new Padding(3, 11, 8, 0) });
        penRow.Controls.Add(_penWidth);
        _predictButton.Width = 126;
        penRow.Controls.Add(_predictButton);

        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(canvasHost, 0, 1);
        layout.Controls.Add(toolRow, 0, 2);
        layout.Controls.Add(penRow, 0, 3);
        layout.Controls.Add(new Label { Text = "28 × 28 NETWORK INPUT", AutoSize = true, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI", 8f, FontStyle.Bold) }, 0, 4);

        var previewHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        _preprocessPreview.Size = new Size(126, 126);
        _preprocessPreview.Dock = DockStyle.None;
        _preprocessPreview.Location = new Point(4, 3);
        var explanation = Description("Pipeline\ncanvas → ink bounds → 20×20 scale → 28×28 center → center-of-mass shift → [0,1]");
        explanation.Location = new Point(145, 8);
        explanation.MaximumSize = new Size(235, 0);
        previewHost.Controls.Add(_preprocessPreview);
        previewHost.Controls.Add(explanation);
        layout.Controls.Add(previewHost, 0, 5);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildInsights()
    {
        var tabs = new ModernTabControl { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0), BackColor = UiTheme.Surface, ForeColor = UiTheme.Text };
        tabs.TabPages.Add(BuildPredictionTab());
        tabs.TabPages.Add(BuildEvaluationTab());
        tabs.TabPages.Add(BuildAutoencoderTab());
        tabs.TabPages.Add(BuildHiddenTab());
        foreach (TabPage page in tabs.TabPages) page.BackColor = UiTheme.Surface;
        return tabs;
    }

    private TabPage BuildPredictionTab()
    {
        var page = new TabPage("PREDICTION") { BackColor = UiTheme.Surface, Padding = new Padding(12) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = UiTheme.Surface };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));

        var hero = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = UiTheme.Surface2 };
        hero.RowStyles.Add(new RowStyle(SizeType.Percent, 70)); hero.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        hero.Controls.Add(_predictionDigit, 0, 0); hero.Controls.Add(_predictionConfidence, 0, 1);

        var top3 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = UiTheme.Surface };
        top3.Controls.Add(Section("TOP-3 PREDICTIONS"));
        foreach (Label label in _top3Labels) { label.Text = "-"; top3.Controls.Add(label); }

        var latency = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = UiTheme.Surface, Padding = new Padding(0, 3, 0, 0) };
        _latencyMetric.Width = 160; _aeMetric.Width = 160;
        latency.Controls.Add(_latencyMetric); latency.Controls.Add(_aeMetric);

        layout.Controls.Add(hero, 0, 0);
        layout.Controls.Add(top3, 0, 1);
        layout.Controls.Add(_probabilities, 0, 2);
        layout.Controls.Add(latency, 0, 3);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildEvaluationTab()
    {
        var page = new TabPage("EVALUATION") { BackColor = UiTheme.Surface, Padding = new Padding(8) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = UiTheme.Surface };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var metrics = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = UiTheme.Surface };
        _testAccuracyMetric.Width = 180; _trainingTimeMetric.Width = 180;
        metrics.Controls.Add(_testAccuracyMetric); metrics.Controls.Add(_trainingTimeMetric);

        var innerTabs = new ModernTabControl { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        var samplePage = new TabPage("Sample") { BackColor = UiTheme.Surface, Padding = new Padding(10) };
        samplePage.Controls.Add(BuildTestSampleViewer());
        var confusionPage = new TabPage("Confusion") { BackColor = UiTheme.Surface, Padding = new Padding(6) };
        confusionPage.Controls.Add(_confusionMatrix);
        var wrongPage = new TabPage("Wrong") { BackColor = UiTheme.Surface, Padding = new Padding(6) };
        wrongPage.Controls.Add(_wrongGallery);
        innerTabs.TabPages.Add(samplePage); innerTabs.TabPages.Add(confusionPage); innerTabs.TabPages.Add(wrongPage);

        layout.Controls.Add(metrics, 0, 0); layout.Controls.Add(innerTabs, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private Control BuildTestSampleViewer()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 2, BackColor = UiTheme.Surface };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _testPreview.Dock = DockStyle.Fill;
        layout.Controls.Add(_testPreview, 0, 0);
        layout.SetColumnSpan(_testPreview, 1);
        var info = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, BackColor = UiTheme.Surface };
        info.Controls.Add(UiTheme.Heading("MNIST Test Explorer", 13f));
        _testSampleLabel.MaximumSize = new Size(280, 0);
        info.Controls.Add(_testSampleLabel);
        info.Controls.Add(Description("Browse test examples, inspect true/predicted labels, or jump directly to misclassified samples."));
        layout.Controls.Add(info, 1, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = UiTheme.Surface };
        buttons.Controls.Add(_nextTestButton); buttons.Controls.Add(_nextWrongButton);
        layout.Controls.Add(buttons, 0, 1); layout.SetColumnSpan(buttons, 2);
        return layout;
    }

    private TabPage BuildAutoencoderTab()
    {
        var page = new TabPage("AUTOENCODER") { BackColor = UiTheme.Surface, Padding = new Padding(12) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 2, BackColor = UiTheme.Surface };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        layout.Controls.Add(new Label { Text = "ORIGINAL / INPUT", ForeColor = UiTheme.Muted, AutoSize = true, Font = new Font("Segoe UI", 8f, FontStyle.Bold) }, 0, 0);
        layout.Controls.Add(new Label { Text = "RECONSTRUCTION", ForeColor = UiTheme.Muted, AutoSize = true, Font = new Font("Segoe UI", 8f, FontStyle.Bold) }, 1, 0);
        layout.Controls.Add(_aeOriginalPreview, 0, 1); layout.Controls.Add(_aeReconstructionPreview, 1, 1);
        _aeStatusLabel.MaximumSize = new Size(520, 0);
        layout.Controls.Add(_aeStatusLabel, 0, 2); layout.SetColumnSpan(_aeStatusLabel, 2);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildHiddenTab()
    {
        var page = new TabPage("HIDDEN LAYER") { BackColor = UiTheme.Surface, Padding = new Padding(12) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = UiTheme.Surface };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(Description("Each cell represents one of the 128 sigmoid neurons. Brighter cells indicate stronger activation for the current input."), 0, 0);
        layout.Controls.Add(_hiddenActivations, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private Control BuildHistoryStrip()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0), Padding = new Padding(10) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = UiTheme.Surface };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        layout.Controls.Add(_lossChart, 0, 0); layout.Controls.Add(_accuracyChart, 1, 0);
        var logHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface2, Padding = new Padding(10) };
        var logTitle = new Label { Text = "ACTIVITY LOG", Dock = DockStyle.Top, Height = 26, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI", 8f, FontStyle.Bold) };
        logHost.Controls.Add(_log); logHost.Controls.Add(logTitle); logTitle.BringToFront();
        layout.Controls.Add(logHost, 2, 0);
        card.Controls.Add(layout);
        return card;
    }

    private async Task SelectDatasetAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select folder containing MNIST train/t10k IDX files",
            UseDescriptionForTitle = true, ShowNewFolderButton = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        SetBusy(true, "Loading MNIST...", false);
        try
        {
            string folder = dialog.SelectedPath;
            string trainImages = FindMnistFile(folder, "train-images-idx3-ubyte", "train-images.idx3-ubyte");
            string trainLabels = FindMnistFile(folder, "train-labels-idx1-ubyte", "train-labels.idx1-ubyte");
            string testImages = FindMnistFile(folder, "t10k-images-idx3-ubyte", "t10k-images.idx3-ubyte");
            string testLabels = FindMnistFile(folder, "t10k-labels-idx1-ubyte", "t10k-labels.idx1-ubyte");

            var loaded = await Task.Run(() => (MnistReader.Load(trainImages, trainLabels), MnistReader.Load(testImages, testLabels)));
            _trainData = loaded.Item1; _testData = loaded.Item2;
            _trainSamples.Maximum = _trainData.Count; _trainSamples.Value = Math.Min(2000, _trainData.Count);
            _evaluationSamples.Maximum = _testData.Count; _evaluationSamples.Value = Math.Min(500, _testData.Count);
            _datasetLabel.Text = $"{_trainData.Count:N0} train / {_testData.Count:N0} test\n{folder}";
            _nextTestButton.Enabled = true;
            _evaluateButton.Enabled = _model is not null;
            _currentTestIndex = -1;
            ShowNextTestSample();
            AppendLog($"MNIST loaded: {_trainData.Count:N0} train, {_testData.Count:N0} test.");
            _statusLabel.Text = "MNIST ready. Configure training parameters.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "MNIST load error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppendLog("MNIST load failed: " + ex.Message);
        }
        finally { SetBusy(false, null); }
    }

    private async Task TrainAsync()
    {
        if (_trainData is null)
        {
            MessageBox.Show("Select the MNIST folder first.", "Dataset required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var model = new NeuralNetwork(784, 128, 10, 1234);
        int sampleLimit = (int)_trainSamples.Value;
        int classifierEpochs = (int)_epochs.Value;
        int aeEpochs = _useAutoencoder.Checked ? (int)_autoencoderEpochs.Value : 0;
        double lr = (double)_learningRate.Value;
        double momentum = (double)_momentum.Value;
        bool useAe = _useAutoencoder.Checked;
        int totalEpochs = classifierEpochs + (useAe ? aeEpochs : 0);

        _trainingCts?.Dispose();
        _trainingCts = new CancellationTokenSource();
        CancellationToken token = _trainingCts.Token;
        _lossChart.Values = Array.Empty<double>(); _accuracyChart.Values = Array.Empty<double>();
        _progress.Value = 0;
        _testAccuracyMetric.Value = "-";
        _lastEvaluation = null;
        _wrongGallery.SetItems(_testData, null);
        _confusionMatrix.Matrix = new int[10, 10];
        AppendLog($"New network 784→128→10 | samples={sampleLimit} | epochs={classifierEpochs} | lr={lr:0.####} | momentum={momentum:0.##}");
        if (useAe) AppendLog($"Autoencoder pretraining enabled: {aeEpochs} epoch(s).");

        SetBusy(true, "Training model...", true);
        var watch = Stopwatch.StartNew();
        try
        {
            int completed = 0;
            var liveLoss = new List<double>();
            void Progress(string phase, int epoch, double loss)
            {
                completed++;
                if (phase == "Classifier") liveLoss.Add(loss);
                int percentage = Math.Clamp((int)Math.Round(100.0 * completed / Math.Max(1, totalEpochs)), 0, 100);
                if (!IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _progress.Value = percentage;
                        _statusLabel.Text = $"{phase} epoch {epoch} • loss {loss:0.######}";
                        AppendLog($"{phase} epoch {epoch}: loss={loss:0.######}");
                        if (phase == "Classifier") _lossChart.Values = liveLoss.ToArray();
                    }));
                }
            }

            DigitTrainingResult result = await Task.Run(() => model.Train(
                _trainData, sampleLimit, classifierEpochs, lr, momentum, useAe, aeEpochs, Progress, token), token);
            watch.Stop();

            _model = model;
            _lossChart.Values = result.ClassifierLoss;
            _accuracyChart.Values = result.ClassifierAccuracy;
            _trainingTimeMetric.Value = FormatDuration(watch.Elapsed);
            _aeMetric.Value = model.HasAutoencoder ? "READY" : "OFF";
            _modelBadge.Text = model.HasAutoencoder ? "MODEL + AE" : "MODEL READY";
            _modelBadge.ForeColor = UiTheme.Green;
            _predictButton.Enabled = true; _saveButton.Enabled = true; _evaluateButton.Enabled = _testData is not null;
            _statusLabel.Text = $"Training complete • {result.SamplesUsed:N0} samples • final loss {result.ClassifierLoss.LastOrDefault():0.######}";
            _progress.Value = 100;
            AppendLog($"Training complete in {watch.Elapsed.TotalSeconds:0.00}s. Final training accuracy={result.ClassifierAccuracy.LastOrDefault():0.0}%.");
            RefreshCurrentVisualizations();
        }
        catch (OperationCanceledException)
        {
            watch.Stop(); AppendLog("Training cancelled by user."); _statusLabel.Text = "Training cancelled.";
        }
        catch (Exception ex)
        {
            watch.Stop(); MessageBox.Show(ex.Message, "Training error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppendLog("Training error: " + ex.Message); _statusLabel.Text = "Training failed.";
        }
        finally
        {
            _trainingCts?.Dispose(); _trainingCts = null; SetBusy(false, null);
        }
    }

    private async Task EvaluateAsync()
    {
        if (_model is null || _testData is null)
        {
            MessageBox.Show("A model and MNIST test set are required.", "Missing data", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int count = (int)_evaluationSamples.Value;
        SetBusy(true, $"Evaluating {count} test samples...", false);
        try
        {
            DigitEvaluationResult result = await Task.Run(() => _model.Evaluate(_testData, count));
            _lastEvaluation = result;
            _testAccuracyMetric.Value = $"{result.Accuracy:0.00}%";
            _confusionMatrix.Matrix = result.ConfusionMatrix;
            _wrongGallery.SetItems(_testData, result.Misclassified);
            _nextWrongButton.Enabled = result.Misclassified.Count > 0;
            _wrongCursor = -1;
            _statusLabel.Text = $"Evaluation complete • {result.Correct}/{result.Total} correct";
            AppendLog($"Evaluation: {result.Correct}/{result.Total} = {result.Accuracy:0.00}% | wrong preview count={result.Misclassified.Count}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Evaluation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetBusy(false, null); }
    }

    private void PredictCanvas()
    {
        if (_model is null) return;
        double[] input = _canvas.GetMnistVector();
        if (input.All(v => v <= 1e-9))
        {
            MessageBox.Show("Draw a digit on the canvas first.", "Empty canvas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        RunInference(input, "Canvas", null);
    }

    private void RunInference(double[] input, string source, int? actual)
    {
        if (_model is null) return;
        var watch = Stopwatch.StartNew();
        double[] output = _model.Predict(input);
        double[] hidden = _model.GetHiddenActivations(input);
        watch.Stop();
        int predicted = ArgMax(output);
        double latencyMs = watch.Elapsed.TotalMilliseconds;

        _predictionDigit.Text = predicted.ToString();
        _predictionConfidence.Text = $"{output[predicted] * 100:0.0}% confidence";
        _probabilities.Values = output;
        _hiddenActivations.Values = hidden;
        _latencyMetric.Value = $"{latencyMs:0.###} ms";
        UpdateTop3(output);
        _aeOriginalPreview.Values = input;
        UpdateReconstruction(input);
        _statusLabel.Text = actual.HasValue
            ? $"{source}: actual {actual.Value}, predicted {predicted}, confidence {output[predicted] * 100:0.0}%"
            : $"{source}: predicted {predicted}, confidence {output[predicted] * 100:0.0}%";
        AppendLog($"{source} prediction={predicted}, confidence={output[predicted] * 100:0.0}%, latency={latencyMs:0.###}ms" + (actual.HasValue ? $", actual={actual.Value}" : ""));
    }

    private void UpdateTop3(double[] output)
    {
        int[] top = Enumerable.Range(0, output.Length).OrderByDescending(i => output[i]).Take(3).ToArray();
        for (int rank = 0; rank < 3; rank++)
            _top3Labels[rank].Text = $"#{rank + 1}    digit {top[rank]}    {output[top[rank]] * 100:0.00}%";
    }

    private void UpdateReconstruction(double[] input)
    {
        if (_model is null || !_model.HasAutoencoder)
        {
            _aeReconstructionPreview.Values = new double[784];
            _aeStatusLabel.Text = "No decoder stored. Train with Autoencoder enabled or load a v2 model containing decoder weights.";
            _aeMetric.Value = "OFF";
            return;
        }
        _aeReconstructionPreview.Values = _model.Reconstruct(input);
        _aeStatusLabel.Text = "Decoder output shown from the same encoder used by the classifier. This is a real 784→128→784 reconstruction.";
        _aeMetric.Value = "READY";
    }

    private void UpdateCanvasPreview()
    {
        _preprocessPreview.Values = _canvas.GetMnistVector();
    }

    private void RefreshCurrentVisualizations()
    {
        if (_model is null) return;
        if (_canvas.HasInk())
        {
            double[] input = _canvas.GetMnistVector();
            _hiddenActivations.Values = _model.GetHiddenActivations(input);
            _aeOriginalPreview.Values = input;
            UpdateReconstruction(input);
        }
        else if (_testData is not null && _currentTestIndex >= 0)
        {
            ShowTestSample(_currentTestIndex);
        }
    }

    private void ShowNextTestSample()
    {
        if (_testData is null) return;
        _currentTestIndex = (_currentTestIndex + 1) % _testData.Count;
        ShowTestSample(_currentTestIndex);
    }

    private void ShowNextWrongSample()
    {
        if (_lastEvaluation is null || _lastEvaluation.Misclassified.Count == 0) return;
        _wrongCursor = (_wrongCursor + 1) % _lastEvaluation.Misclassified.Count;
        ShowTestSample(_lastEvaluation.Misclassified[_wrongCursor].Index);
    }

    private void ShowTestSample(int index)
    {
        if (_testData is null || index < 0 || index >= _testData.Count) return;
        _currentTestIndex = index;
        double[] input = _testData.GetNormalizedImage(index);
        int actual = _testData.GetLabel(index);
        _testPreview.Values = input;
        if (_model is null)
        {
            _testSampleLabel.Text = $"Index {index:N0}\nActual label: {actual}\nPrediction: model not loaded";
            return;
        }

        double[] output = _model.Predict(input);
        int predicted = ArgMax(output);
        _testSampleLabel.Text = $"Index {index:N0}\nActual label: {actual}\nPredicted: {predicted}\nConfidence: {output[predicted] * 100:0.00}%";
        RunInference(input, $"MNIST[{index}]", actual);
    }

    private void SaveModel()
    {
        if (_model is null) return;
        using var dialog = new SaveFileDialog
        {
            Filter = "Neural Digit model (*.dnn)|*.dnn|All files (*.*)|*.*", DefaultExt = "dnn",
            FileName = "neural-digit-784-128-10.dnn", AddExtension = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _model.Save(dialog.FileName);
            AppendLog("Model saved: " + dialog.FileName);
            _statusLabel.Text = "Model saved.";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void LoadModel()
    {
        using var dialog = new OpenFileDialog { Filter = "Neural Digit model (*.dnn)|*.dnn|All files (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            NeuralNetwork loaded = NeuralNetwork.Load(dialog.FileName);
            if (loaded.InputSize != 784 || loaded.HiddenSize != 128 || loaded.OutputSize != 10)
                throw new InvalidDataException("Expected architecture is 784 → 128 → 10.");
            _model = loaded;
            _modelBadge.Text = loaded.HasAutoencoder ? "MODEL + AE" : "MODEL READY";
            _modelBadge.ForeColor = UiTheme.Green;
            _aeMetric.Value = loaded.HasAutoencoder ? "READY" : "OFF";
            _predictButton.Enabled = true; _saveButton.Enabled = true; _evaluateButton.Enabled = _testData is not null;
            _statusLabel.Text = "Model loaded. Draw a digit or browse MNIST samples.";
            AppendLog($"Model loaded: {dialog.FileName} | autoencoder decoder={(loaded.HasAutoencoder ? "yes" : "no")}");
            RefreshCurrentVisualizations();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Load error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ToggleEraser()
    {
        _canvas.EraserEnabled = !_canvas.EraserEnabled;
        _eraserButton.Text = _canvas.EraserEnabled ? "PEN" : "ERASER";
        _eraserButton.BackColor = _canvas.EraserEnabled ? UiTheme.Pink : UiTheme.Surface3;
        _canvas.Invalidate();
    }

    private void ResetPredictionView()
    {
        _predictionDigit.Text = "?"; _predictionConfidence.Text = "confidence -";
        _probabilities.Values = new double[10]; _hiddenActivations.Values = Array.Empty<double>();
        foreach (Label label in _top3Labels) label.Text = "-";
        _latencyMetric.Value = "-"; _aeOriginalPreview.Values = new double[784]; _aeReconstructionPreview.Values = new double[784];
    }

    private void SetBusy(bool busy, string? status, bool cancellable = false)
    {
        _datasetButton.Enabled = !busy; _trainButton.Enabled = !busy; _cancelButton.Enabled = busy && cancellable;
        _loadButton.Enabled = !busy; _saveButton.Enabled = !busy && _model is not null;
        _evaluateButton.Enabled = !busy && _model is not null && _testData is not null;
        _predictButton.Enabled = !busy && _model is not null;
        _nextTestButton.Enabled = !busy && _testData is not null;
        _nextWrongButton.Enabled = !busy && _lastEvaluation is not null && _lastEvaluation.Misclassified.Count > 0;
        if (status is not null) _statusLabel.Text = status;
    }

    private void AppendLog(string text)
    {
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        _log.SelectionStart = _log.TextLength; _log.ScrollToCaret();
    }

    private static Control Field(string label, Control control)
    {
        var row = new TableLayoutPanel { Width = 276, Height = 36, ColumnCount = 2, BackColor = UiTheme.Surface, Margin = new Padding(0, 2, 0, 2) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        row.Controls.Add(new Label { Text = label, ForeColor = UiTheme.Muted, AutoSize = true, Margin = new Padding(0, 8, 3, 0) }, 0, 0);
        row.Controls.Add(control, 1, 0); return row;
    }

    private static Label Section(string text) => new()
    {
        Text = text, AutoSize = true, ForeColor = UiTheme.Accent,
        Font = new Font("Segoe UI", 8f, FontStyle.Bold), Margin = new Padding(0, 14, 0, 5)
    };

    private static Label Description(string text) => new()
    {
        Text = text, AutoSize = true, ForeColor = UiTheme.Muted,
        Font = new Font("Segoe UI", 8.5f), MaximumSize = new Size(285, 0), Margin = new Padding(0, 3, 0, 5)
    };

    private static Button WideButton(Button button, int width) { button.Width = width; return button; }

    private static int ArgMax(double[] values)
    {
        int best = 0;
        for (int i = 1; i < values.Length; i++) if (values[i] > values[best]) best = i;
        return best;
    }

    private static string FormatDuration(TimeSpan elapsed) => elapsed.TotalMinutes >= 1
        ? $"{elapsed.TotalMinutes:0.0} min" : $"{elapsed.TotalSeconds:0.00} s";

    private static string FindMnistFile(string folder, params string[] baseNames)
    {
        foreach (string baseName in baseNames)
        {
            string raw = Path.Combine(folder, baseName);
            if (File.Exists(raw)) return raw;
            if (File.Exists(raw + ".gz")) return raw + ".gz";
        }
        throw new FileNotFoundException("MNIST file not found. Expected: " + string.Join(", ", baseNames.Select(x => x + "[.gz]")));
    }
}
