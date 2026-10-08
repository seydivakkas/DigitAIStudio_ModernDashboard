using System.IO.Compression;

namespace ML.Core.DigitRecognition;

public static class MnistReader
{
    private const int ImageMagic = 2051;
    private const int LabelMagic = 2049;

    public static MnistDataset Load(string imageFile, string labelFile)
    {
        if (!File.Exists(imageFile))
            throw new FileNotFoundException("MNIST image dosyası bulunamadı.", imageFile);
        if (!File.Exists(labelFile))
            throw new FileNotFoundException("MNIST label dosyası bulunamadı.", labelFile);

        using Stream imageStream = OpenMaybeGzip(imageFile);
        using Stream labelStream = OpenMaybeGzip(labelFile);
        using var imageReader = new BinaryReader(imageStream);
        using var labelReader = new BinaryReader(labelStream);

        int imageMagic = ReadBigEndianInt32(imageReader);
        int imageCount = ReadBigEndianInt32(imageReader);
        int rows = ReadBigEndianInt32(imageReader);
        int columns = ReadBigEndianInt32(imageReader);

        int labelMagic = ReadBigEndianInt32(labelReader);
        int labelCount = ReadBigEndianInt32(labelReader);

        if (imageMagic != ImageMagic)
            throw new InvalidDataException($"Geçersiz MNIST image magic number: {imageMagic} (beklenen {ImageMagic}).");
        if (labelMagic != LabelMagic)
            throw new InvalidDataException($"Geçersiz MNIST label magic number: {labelMagic} (beklenen {LabelMagic}).");
        if (imageCount != labelCount)
            throw new InvalidDataException($"Image count ({imageCount}) ve label count ({labelCount}) eşleşmiyor.");
        if (rows != 28 || columns != 28)
            throw new InvalidDataException($"Bu uygulama 28x28 MNIST bekliyor; dosya {rows}x{columns}.");

        int pixelCount = checked(imageCount * rows * columns);
        byte[] pixels = ReadExactly(imageReader, pixelCount);
        byte[] labels = ReadExactly(labelReader, labelCount);

        return new MnistDataset(pixels, labels, rows, columns);
    }

    private static Stream OpenMaybeGzip(string path)
    {
        Stream file = File.OpenRead(path);
        if (!path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            return file;

        return new GZipStream(file, CompressionMode.Decompress);
    }

    private static int ReadBigEndianInt32(BinaryReader reader)
    {
        Span<byte> bytes = stackalloc byte[4];
        int read = reader.BaseStream.Read(bytes);
        if (read != 4)
            throw new EndOfStreamException("MNIST başlığı beklenenden kısa.");

        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    private static byte[] ReadExactly(BinaryReader reader, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = reader.BaseStream.Read(buffer, offset, count - offset);
            if (read <= 0)
                throw new EndOfStreamException($"MNIST dosyasında {count} byte bekleniyordu, {offset} byte okunabildi.");
            offset += read;
        }
        return buffer;
    }
}
