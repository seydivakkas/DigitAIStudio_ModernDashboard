namespace ML.Core.DigitRecognition;

public sealed class NeuralNetwork
{
    public const int DefaultInputSize = 784;
    public const int DefaultHiddenSize = 128;
    public const int DefaultOutputSize = 10;
    private const string ModelMagicV1 = "MLHW-DIGIT-NN-1";
    private const string ModelMagicV2 = "MLHW-DIGIT-NN-2";

    private readonly Random _random;
    private readonly double[] _weightsInputHidden;
    private readonly double[] _biasHidden;
    private readonly double[] _weightsHiddenOutput;
    private readonly double[] _biasOutput;

    private readonly double[] _velocityInputHidden;
    private readonly double[] _velocityBiasHidden;
    private readonly double[] _velocityHiddenOutput;
    private readonly double[] _velocityBiasOutput;

    private double[]? _decoderWeights;
    private double[]? _decoderBias;

    public NeuralNetwork(
        int inputSize = DefaultInputSize,
        int hiddenSize = DefaultHiddenSize,
        int outputSize = DefaultOutputSize,
        int seed = 1234)
    {
        if (inputSize <= 0 || hiddenSize <= 0 || outputSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(inputSize));

        InputSize = inputSize;
        HiddenSize = hiddenSize;
        OutputSize = outputSize;
        _random = new Random(seed);

        _weightsInputHidden = new double[hiddenSize * inputSize];
        _biasHidden = new double[hiddenSize];
        _weightsHiddenOutput = new double[outputSize * hiddenSize];
        _biasOutput = new double[outputSize];

        _velocityInputHidden = new double[_weightsInputHidden.Length];
        _velocityBiasHidden = new double[_biasHidden.Length];
        _velocityHiddenOutput = new double[_weightsHiddenOutput.Length];
        _velocityBiasOutput = new double[_biasOutput.Length];

        InitializeWeights();
    }

    public int InputSize { get; }
    public int HiddenSize { get; }
    public int OutputSize { get; }
    public bool HasAutoencoder => _decoderWeights is not null && _decoderBias is not null;

    public double[] Predict(ReadOnlySpan<double> input)
    {
        ValidateInput(input);
        var hidden = new double[HiddenSize];
        var output = new double[OutputSize];
        Forward(input, hidden, output);
        return output;
    }

    public int PredictClass(ReadOnlySpan<double> input)
    {
        double[] output = Predict(input);
        return ArgMax(output);
    }

    public double[] GetHiddenActivations(ReadOnlySpan<double> input)
    {
        ValidateInput(input);
        var hidden = new double[HiddenSize];
        ComputeHidden(input, hidden);
        return hidden;
    }

    public double[] Reconstruct(ReadOnlySpan<double> input)
    {
        ValidateInput(input);
        if (!HasAutoencoder)
            throw new InvalidOperationException("Bu modelde autoencoder decoder ağırlıkları yok.");

        var hidden = new double[HiddenSize];
        ComputeHidden(input, hidden);
        var reconstruction = new double[InputSize];
        double[] decoderWeights = _decoderWeights!;
        double[] decoderBias = _decoderBias!;

        for (int i = 0; i < InputSize; i++)
        {
            int baseIndex = i * HiddenSize;
            double z = decoderBias[i];
            for (int h = 0; h < HiddenSize; h++)
                z += decoderWeights[baseIndex + h] * hidden[h];
            reconstruction[i] = Sigmoid(z);
        }

        return reconstruction;
    }

    public DigitTrainingResult Train(
        MnistDataset dataset,
        int sampleLimit,
        int classifierEpochs,
        double learningRate,
        double momentum = 0.9,
        bool autoencoderPretraining = true,
        int autoencoderEpochs = 1,
        Action<string, int, double>? epochCompleted = null,
        CancellationToken cancellationToken = default)
    {
        if (dataset.FeatureCount != InputSize)
            throw new ArgumentException($"Dataset feature count {dataset.FeatureCount}; ağ input size {InputSize}.", nameof(dataset));
        if (OutputSize != 10)
            throw new InvalidOperationException("MNIST sınıflandırması 10 output neuron bekler.");
        if (classifierEpochs <= 0)
            throw new ArgumentOutOfRangeException(nameof(classifierEpochs));
        if (autoencoderEpochs < 0)
            throw new ArgumentOutOfRangeException(nameof(autoencoderEpochs));
        if (learningRate <= 0 || !double.IsFinite(learningRate))
            throw new ArgumentOutOfRangeException(nameof(learningRate));
        if (momentum < 0 || momentum >= 1)
            throw new ArgumentOutOfRangeException(nameof(momentum));

        int count = Math.Clamp(sampleLimit, 1, dataset.Count);
        var autoencoderLoss = new List<double>();

        if (autoencoderPretraining && autoencoderEpochs > 0)
        {
            autoencoderLoss.AddRange(PretrainAutoencoder(
                dataset,
                count,
                autoencoderEpochs,
                learningRate,
                epochCompleted,
                cancellationToken));
        }
        else
        {
            _decoderWeights = null;
            _decoderBias = null;
        }

        (IReadOnlyList<double> losses, IReadOnlyList<double> accuracies) = TrainClassifier(
            dataset,
            count,
            classifierEpochs,
            learningRate,
            momentum,
            epochCompleted,
            cancellationToken);

        return new DigitTrainingResult(
            autoencoderLoss,
            losses,
            accuracies,
            count,
            classifierEpochs,
            autoencoderPretraining ? autoencoderEpochs : 0);
    }

    public DigitEvaluationResult Evaluate(
        MnistDataset dataset,
        int sampleCount,
        int seed = 42,
        CancellationToken cancellationToken = default)
    {
        if (dataset.FeatureCount != InputSize)
            throw new ArgumentException("Dataset boyutu ağ girişi ile uyuşmuyor.", nameof(dataset));

        int count = Math.Clamp(sampleCount, 1, dataset.Count);
        int[] indices = Enumerable.Range(0, dataset.Count).ToArray();
        var random = new Random(seed);
        for (int i = 0; i < count; i++)
        {
            int j = random.Next(i, indices.Length);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        var input = new double[InputSize];
        var hidden = new double[HiddenSize];
        var output = new double[OutputSize];
        var confusion = new int[OutputSize, OutputSize];
        var wrong = new List<DigitPredictionSample>(24);
        int correct = 0;

        for (int n = 0; n < count; n++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int index = indices[n];
            dataset.CopyNormalizedImage(index, input);
            Forward(input, hidden, output);
            int predicted = ArgMax(output);
            int actual = dataset.GetLabel(index);
            confusion[actual, predicted]++;
            if (predicted == actual)
            {
                correct++;
            }
            else if (wrong.Count < 24)
            {
                wrong.Add(new DigitPredictionSample(index, actual, predicted, output[predicted]));
            }
        }

        return new DigitEvaluationResult(correct, count, 100.0 * correct / count, confusion, wrong);
    }

    public void Save(string path)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write(ModelMagicV2);
        writer.Write(InputSize);
        writer.Write(HiddenSize);
        writer.Write(OutputSize);
        WriteArray(writer, _weightsInputHidden);
        WriteArray(writer, _biasHidden);
        WriteArray(writer, _weightsHiddenOutput);
        WriteArray(writer, _biasOutput);
        writer.Write(HasAutoencoder);
        if (HasAutoencoder)
        {
            WriteArray(writer, _decoderWeights!);
            WriteArray(writer, _decoderBias!);
        }
    }

    public static NeuralNetwork Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        string magic = reader.ReadString();
        if (magic != ModelMagicV1 && magic != ModelMagicV2)
            throw new InvalidDataException("Dosya bu uygulamanın digit model formatında değil.");

        int input = reader.ReadInt32();
        int hidden = reader.ReadInt32();
        int output = reader.ReadInt32();
        var model = new NeuralNetwork(input, hidden, output);
        ReadArray(reader, model._weightsInputHidden);
        ReadArray(reader, model._biasHidden);
        ReadArray(reader, model._weightsHiddenOutput);
        ReadArray(reader, model._biasOutput);

        if (magic == ModelMagicV2 && reader.ReadBoolean())
        {
            model._decoderWeights = new double[input * hidden];
            model._decoderBias = new double[input];
            ReadArray(reader, model._decoderWeights);
            ReadArray(reader, model._decoderBias);
        }

        return model;
    }

    private (IReadOnlyList<double> Losses, IReadOnlyList<double> Accuracies) TrainClassifier(
        MnistDataset dataset,
        int count,
        int epochs,
        double learningRate,
        double momentum,
        Action<string, int, double>? epochCompleted,
        CancellationToken cancellationToken)
    {
        var history = new List<double>(epochs);
        var accuracyHistory = new List<double>(epochs);
        var input = new double[InputSize];
        var hidden = new double[HiddenSize];
        var output = new double[OutputSize];
        var deltaOutput = new double[OutputSize];
        var deltaHidden = new double[HiddenSize];

        int[] order = Enumerable.Range(0, count).ToArray();

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Shuffle(order);
            double totalLoss = 0.0;
            int correct = 0;

            for (int sample = 0; sample < count; sample++)
            {
                if ((sample & 31) == 0)
                    cancellationToken.ThrowIfCancellationRequested();

                int index = order[sample];
                dataset.CopyNormalizedImage(index, input);
                int label = dataset.GetLabel(index);
                Forward(input, hidden, output);

                if (ArgMax(output) == label)
                    correct++;
                totalLoss -= Math.Log(output[label] + 1e-12);

                for (int o = 0; o < OutputSize; o++)
                    deltaOutput[o] = output[o] - (o == label ? 1.0 : 0.0);

                for (int h = 0; h < HiddenSize; h++)
                {
                    double error = 0.0;
                    for (int o = 0; o < OutputSize; o++)
                        error += deltaOutput[o] * _weightsHiddenOutput[o * HiddenSize + h];
                    deltaHidden[h] = error * hidden[h] * (1.0 - hidden[h]);
                }

                for (int o = 0; o < OutputSize; o++)
                {
                    int baseIndex = o * HiddenSize;
                    double d = deltaOutput[o];
                    for (int h = 0; h < HiddenSize; h++)
                    {
                        int wi = baseIndex + h;
                        double grad = d * hidden[h];
                        _velocityHiddenOutput[wi] = momentum * _velocityHiddenOutput[wi] - learningRate * grad;
                        _weightsHiddenOutput[wi] += _velocityHiddenOutput[wi];
                    }

                    _velocityBiasOutput[o] = momentum * _velocityBiasOutput[o] - learningRate * d;
                    _biasOutput[o] += _velocityBiasOutput[o];
                }

                for (int h = 0; h < HiddenSize; h++)
                {
                    int baseIndex = h * InputSize;
                    double d = deltaHidden[h];
                    for (int i = 0; i < InputSize; i++)
                    {
                        int wi = baseIndex + i;
                        double grad = d * input[i];
                        _velocityInputHidden[wi] = momentum * _velocityInputHidden[wi] - learningRate * grad;
                        _weightsInputHidden[wi] += _velocityInputHidden[wi];
                    }

                    _velocityBiasHidden[h] = momentum * _velocityBiasHidden[h] - learningRate * d;
                    _biasHidden[h] += _velocityBiasHidden[h];
                }
            }

            double loss = totalLoss / count;
            double accuracy = 100.0 * correct / count;
            history.Add(loss);
            accuracyHistory.Add(accuracy);
            epochCompleted?.Invoke("Classifier", epoch + 1, loss);
        }

        return (history, accuracyHistory);
    }

    private IReadOnlyList<double> PretrainAutoencoder(
        MnistDataset dataset,
        int count,
        int epochs,
        double learningRate,
        Action<string, int, double>? epochCompleted,
        CancellationToken cancellationToken)
    {
        var history = new List<double>(epochs);
        _decoderWeights = new double[InputSize * HiddenSize];
        _decoderBias = new double[InputSize];
        double[] decoderWeights = _decoderWeights;
        double[] decoderBias = _decoderBias;

        double stdDev = Math.Sqrt(2.0 / HiddenSize);
        for (int i = 0; i < decoderWeights.Length; i++)
            decoderWeights[i] = NextGaussian() * stdDev;

        var input = new double[InputSize];
        var hidden = new double[HiddenSize];
        var reconstruction = new double[InputSize];
        var deltaOutput = new double[InputSize];
        var deltaHidden = new double[HiddenSize];
        int[] order = Enumerable.Range(0, count).ToArray();

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Shuffle(order);
            double totalSquaredError = 0.0;

            for (int sample = 0; sample < count; sample++)
            {
                if ((sample & 15) == 0)
                    cancellationToken.ThrowIfCancellationRequested();

                dataset.CopyNormalizedImage(order[sample], input);
                ComputeHidden(input, hidden);

                for (int i = 0; i < InputSize; i++)
                {
                    int baseIndex = i * HiddenSize;
                    double z = decoderBias[i];
                    for (int h = 0; h < HiddenSize; h++)
                        z += decoderWeights[baseIndex + h] * hidden[h];
                    reconstruction[i] = Sigmoid(z);

                    double error = reconstruction[i] - input[i];
                    totalSquaredError += error * error;
                    deltaOutput[i] = error * reconstruction[i] * (1.0 - reconstruction[i]);
                }

                for (int h = 0; h < HiddenSize; h++)
                {
                    double error = 0.0;
                    for (int i = 0; i < InputSize; i++)
                        error += deltaOutput[i] * decoderWeights[i * HiddenSize + h];
                    deltaHidden[h] = error * hidden[h] * (1.0 - hidden[h]);
                }

                for (int i = 0; i < InputSize; i++)
                {
                    int baseIndex = i * HiddenSize;
                    double d = deltaOutput[i];
                    for (int h = 0; h < HiddenSize; h++)
                        decoderWeights[baseIndex + h] -= learningRate * d * hidden[h];
                    decoderBias[i] -= learningRate * d;
                }

                for (int h = 0; h < HiddenSize; h++)
                {
                    int baseIndex = h * InputSize;
                    double d = deltaHidden[h];
                    for (int i = 0; i < InputSize; i++)
                        _weightsInputHidden[baseIndex + i] -= learningRate * d * input[i];
                    _biasHidden[h] -= learningRate * d;
                }
            }

            double mse = totalSquaredError / (count * InputSize);
            history.Add(mse);
            epochCompleted?.Invoke("Autoencoder", epoch + 1, mse);
        }

        return history;
    }

    private void Forward(ReadOnlySpan<double> input, Span<double> hidden, Span<double> output)
    {
        ValidateInput(input);
        if (hidden.Length < HiddenSize || output.Length < OutputSize)
            throw new ArgumentException("Forward buffer boyutu yetersiz.");

        ComputeHidden(input, hidden);

        double max = double.NegativeInfinity;
        for (int o = 0; o < OutputSize; o++)
        {
            int baseIndex = o * HiddenSize;
            double z = _biasOutput[o];
            for (int h = 0; h < HiddenSize; h++)
                z += _weightsHiddenOutput[baseIndex + h] * hidden[h];
            output[o] = z;
            if (z > max)
                max = z;
        }

        double sum = 0.0;
        for (int o = 0; o < OutputSize; o++)
        {
            output[o] = Math.Exp(output[o] - max);
            sum += output[o];
        }

        if (sum <= 0 || !double.IsFinite(sum))
        {
            double uniform = 1.0 / OutputSize;
            for (int o = 0; o < OutputSize; o++)
                output[o] = uniform;
            return;
        }

        for (int o = 0; o < OutputSize; o++)
            output[o] /= sum;
    }

    private void ComputeHidden(ReadOnlySpan<double> input, Span<double> hidden)
    {
        ValidateInput(input);
        if (hidden.Length < HiddenSize)
            throw new ArgumentException("Hidden buffer boyutu yetersiz.", nameof(hidden));

        for (int h = 0; h < HiddenSize; h++)
        {
            int baseIndex = h * InputSize;
            double z = _biasHidden[h];
            for (int i = 0; i < InputSize; i++)
                z += _weightsInputHidden[baseIndex + i] * input[i];
            hidden[h] = Sigmoid(z);
        }
    }

    private void InitializeWeights()
    {
        double stdInput = Math.Sqrt(2.0 / InputSize);
        for (int i = 0; i < _weightsInputHidden.Length; i++)
            _weightsInputHidden[i] = NextGaussian() * stdInput;

        double stdHidden = Math.Sqrt(2.0 / HiddenSize);
        for (int i = 0; i < _weightsHiddenOutput.Length; i++)
            _weightsHiddenOutput[i] = NextGaussian() * stdHidden;
    }

    private void Shuffle(int[] values)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private double NextGaussian()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    private static double Sigmoid(double x)
    {
        if (x >= 0)
        {
            double z = Math.Exp(-x);
            return 1.0 / (1.0 + z);
        }

        double negativeZ = Math.Exp(x);
        return negativeZ / (1.0 + negativeZ);
    }

    private static int ArgMax(ReadOnlySpan<double> values)
    {
        int bestIndex = 0;
        double bestValue = values[0];
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > bestValue)
            {
                bestValue = values[i];
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private void ValidateInput(ReadOnlySpan<double> input)
    {
        if (input.Length != InputSize)
            throw new ArgumentException($"Input uzunluğu {InputSize} olmalı, gelen {input.Length}.", nameof(input));
    }

    private static void WriteArray(BinaryWriter writer, double[] values)
    {
        writer.Write(values.Length);
        foreach (double value in values)
            writer.Write(value);
    }

    private static void ReadArray(BinaryReader reader, double[] destination)
    {
        int length = reader.ReadInt32();
        if (length != destination.Length)
            throw new InvalidDataException("Model ağırlık boyutları uyuşmuyor.");
        for (int i = 0; i < length; i++)
            destination[i] = reader.ReadDouble();
    }
}
