namespace Generation;

internal interface ICloudBucket
{
    Task Upload(string key, Stream imageData);
    Task<string> GetUrl(string key);
}
