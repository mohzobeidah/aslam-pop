using SkiaSharp;

namespace CampRegistrationApp.Services;

public interface IFileCompressionService
{
    Task<byte[]> CompressAsync(byte[] data, string fileName);
}

public class FileCompressionService : IFileCompressionService
{
    private const int JpegQuality = 70;

    public Task<byte[]> CompressAsync(byte[] data, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext is ".jpg" or ".jpeg")
            return Task.FromResult(CompressJpeg(data));

        if (ext == ".png")
            return Task.FromResult(CompressPng(data));

        if (ext is ".bmp" or ".gif" or ".tiff" or ".webp")
            return Task.FromResult(ConvertToJpeg(data));

        return Task.FromResult(data);
    }

    private static byte[] EncodeImage(byte[] data, SKEncodedImageFormat format, int quality)
    {
        using var input = new MemoryStream(data);
        using var bitmap = SKBitmap.Decode(input);
        if (bitmap == null) return data;

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(format, quality);
        return encoded.ToArray();
    }

    private static byte[] CompressJpeg(byte[] data) => EncodeImage(data, SKEncodedImageFormat.Jpeg, JpegQuality);
    private static byte[] CompressPng(byte[] data) => EncodeImage(data, SKEncodedImageFormat.Png, 100);
    private static byte[] ConvertToJpeg(byte[] data) => EncodeImage(data, SKEncodedImageFormat.Jpeg, JpegQuality);
}
