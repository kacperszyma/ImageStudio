using Google.Cloud.Storage.V1;

namespace Generation;

internal sealed class GcsCloudBucket(
    StorageClient client,
    string bucket,
    UrlSigner? signer,
    string? emulatorHost) : ICloudBucket
{
    private static readonly TimeSpan UrlExpiry = TimeSpan.FromMinutes(15);

    public async Task Upload(string key, Stream imageData) =>
        await client.UploadObjectAsync(bucket, key, contentType: null, imageData);

    public async Task<string> GetUrl(string key) =>
        signer is not null
            ? await signer.SignAsync(bucket, key, UrlExpiry)
            // fake-gcs-server has no real IAM to sign against — its -public-host
            // flag serves objects at a plain URL instead.
            : $"{emulatorHost}/{bucket}/{key}";
}
