namespace ML.Core.DigitRecognition;

public sealed class MnistDataset
{
    private readonly byte[] _pixels;
    private readonly byte[] _labels;

    public MnistDataset(byte[] pixels, byte[] labels, int rows, int columns)
    {
        if (rows <= 0 || columns <= 0)
            throw new ArgumentOutOfRangeException(nameof(rows));
        if (pixels.Length != labels.Length * rows * columns)
            throw new ArgumentException("Piksel ve etiket sayıları uyuşmuyor.");

        _pixels = pixels;
        _labels = labels;
        Rows = rows;
        Columns = columns;
    }

    public int Count => _labels.Length;
    public int Rows { get; }
    public int Columns { get; }
    public int FeatureCount => Rows * Columns;

    public byte GetLabel(int index)
    {
        ValidateIndex(index);
        return _labels[index];
    }

    public void CopyNormalizedImage(int index, Span<double> destination)
    {
        ValidateIndex(index);
        if (destination.Length < FeatureCount)
            throw new ArgumentException($"Destination en az {FeatureCount} eleman olmalı.", nameof(destination));

        int offset = index * FeatureCount;
        for (int i = 0; i < FeatureCount; i++)
            destination[i] = _pixels[offset + i] / 255.0;
    }

    public double[] GetNormalizedImage(int index)
    {
        var result = new double[FeatureCount];
        CopyNormalizedImage(index, result);
        return result;
    }

    public byte[] GetRawImage(int index)
    {
        ValidateIndex(index);
        var result = new byte[FeatureCount];
        Buffer.BlockCopy(_pixels, index * FeatureCount, result, 0, FeatureCount);
        return result;
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(index));
    }
}
