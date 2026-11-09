using SkiaSharp;

namespace FarmerSample.Core.Storage;

/// <summary>
/// Makes the buyer-facing copy of a photo: applies the camera's rotation, scales to at most 1600 px and
/// re-encodes as WebP. Re-encoding writes pixels only, so EXIF (GPS position, camera serial, time) and any
/// other metadata in the original are dropped.
/// </summary>
public static class ImageRenditions
{
    public const int MaxEdge = 1600;

    public sealed record Rendition(byte[] WebP, int Width, int Height);

    public static Rendition Create(byte[] original)
    {
        using var data = SKData.CreateCopy(original);
        using var codec = SKCodec.Create(data) ?? throw new InvalidDataException("This file is not an image we can read (use JPEG, PNG or WebP).");
        using var decoded = SKBitmap.Decode(codec) ?? throw new InvalidDataException("The image could not be decoded.");
        using var oriented = ApplyOrigin(decoded, codec.EncodedOrigin);

        var scale = Math.Min(1.0, (double)MaxEdge / Math.Max(oriented.Width, oriented.Height));
        var w = Math.Max(1, (int)Math.Round(oriented.Width * scale));
        var h = Math.Max(1, (int)Math.Round(oriented.Height * scale));
        using var resized = scale < 1.0 ? oriented.Resize(new SKImageInfo(w, h), new SKSamplingOptions(SKCubicResampler.Mitchell)) : oriented.Copy();
        using var image = SKImage.FromBitmap(resized);
        using var webp = image.Encode(SKEncodedImageFormat.Webp, 82);
        return new Rendition(webp.ToArray(), w, h);
    }

    private static SKBitmap ApplyOrigin(SKBitmap src, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default) return src.Copy();
        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var dst = new SKBitmap(swap ? src.Height : src.Width, swap ? src.Width : src.Height);
        using var canvas = new SKCanvas(dst);
        var (w, h) = (src.Width, src.Height);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Scale(-1, 1, w / 2f, 0); break;
            case SKEncodedOrigin.BottomRight: canvas.RotateDegrees(180, w / 2f, h / 2f); break;
            case SKEncodedOrigin.BottomLeft: canvas.Scale(1, -1, 0, h / 2f); break;
            case SKEncodedOrigin.LeftTop: canvas.Translate(h, 0); canvas.RotateDegrees(90); canvas.Scale(1, -1, 0, h / 2f); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(h, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(h, 0); canvas.RotateDegrees(90); canvas.Scale(-1, -1, w / 2f, h / 2f); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, w); canvas.RotateDegrees(270); break;
        }
        canvas.DrawBitmap(src, 0, 0);
        return dst;
    }
}
