using System.Text;
using FarmerSample.Core.Storage;
using SkiaSharp;

namespace FarmerSample.UnitTests;

public class ImageRenditionTests
{
    /// <summary>A JPEG with an EXIF block carrying a GPS-like canary string, as a phone camera would write.</summary>
    internal static byte[] JpegWithExif(int width, int height, string canary)
    {
        using var bmp = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bmp)) canvas.Clear(new SKColor(15, 61, 46));
        using var img = SKImage.FromBitmap(bmp);
        var jpeg = img.Encode(SKEncodedImageFormat.Jpeg, 90).ToArray();

        var payload = Encoding.ASCII.GetBytes("Exif\0\0MM\0*\0\0\0\b\0\0" + canary);
        var len = payload.Length + 2;
        var app1 = new byte[] { 0xFF, 0xE1, (byte)(len >> 8), (byte)len }.Concat(payload).ToArray();
        return jpeg.Take(2).Concat(app1).Concat(jpeg.Skip(2)).ToArray();   // insert right after SOI
    }

    [Fact]
    public void Rendition_drops_all_metadata_and_is_webp()
    {
        var original = JpegWithExif(800, 600, "GPS 7.2906N 80.6337E CANARY-SERIAL-123");
        Assert.Contains("CANARY", Encoding.ASCII.GetString(original));

        var r = ImageRenditions.Create(original);
        var text = Encoding.ASCII.GetString(r.WebP);
        Assert.StartsWith("RIFF", text);
        Assert.Contains("WEBP", text[..16]);
        Assert.DoesNotContain("CANARY", text);
        Assert.DoesNotContain("Exif", text);
        Assert.DoesNotContain("EXIF", text);
    }

    [Fact]
    public void Large_photos_are_scaled_to_1600_px_keeping_aspect_ratio()
    {
        var r = ImageRenditions.Create(JpegWithExif(4000, 3000, "x"));
        Assert.Equal((1600, 1200), (r.Width, r.Height));
    }

    [Fact]
    public void Non_images_are_rejected() =>
        Assert.Throws<InvalidDataException>(() => ImageRenditions.Create(Encoding.UTF8.GetBytes("not an image")));
}
