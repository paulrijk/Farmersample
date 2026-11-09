using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace FarmerSample.Core.Storage;

/// <summary>Private blob storage: Azurite locally, Azure Blob Storage in the cloud. Nothing is publicly readable.</summary>
public interface IBlobStore
{
    Task PutAsync(string container, string name, Stream content, string contentType, CancellationToken ct);
    Task<(Stream Content, string ContentType)?> GetAsync(string container, string name, CancellationToken ct);
}

public static class Containers
{
    public const string PhotoOriginals = "photo-originals";
    public const string PhotoRenditions = "photo-renditions";
}

internal sealed class AzureBlobStore(BlobServiceClient client) : IBlobStore
{
    public async Task PutAsync(string container, string name, Stream content, string contentType, CancellationToken ct)
    {
        var c = client.GetBlobContainerClient(container);
        await c.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        await c.GetBlobClient(name).UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, ct);
    }

    public async Task<(Stream Content, string ContentType)?> GetAsync(string container, string name, CancellationToken ct)
    {
        var blob = client.GetBlobContainerClient(container).GetBlobClient(name);
        if (!await blob.ExistsAsync(ct)) return null;
        var download = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return (download.Value.Content, download.Value.Details.ContentType ?? "application/octet-stream");
    }
}
