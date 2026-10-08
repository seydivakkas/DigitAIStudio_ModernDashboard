namespace ML.Core.DigitRecognition;

public sealed record DigitTrainingResult(
    IReadOnlyList<double> AutoencoderLoss,
    IReadOnlyList<double> ClassifierLoss,
    IReadOnlyList<double> ClassifierAccuracy,
    int SamplesUsed,
    int ClassifierEpochs,
    int AutoencoderEpochs);

public sealed record DigitPredictionSample(
    int Index,
    int Actual,
    int Predicted,
    double Confidence);

public sealed record DigitEvaluationResult(
    int Correct,
    int Total,
    double Accuracy,
    int[,] ConfusionMatrix,
    IReadOnlyList<DigitPredictionSample> Misclassified);
