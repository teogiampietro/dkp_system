using Amazon.S3;
using Amazon.S3.Model;

namespace DkpSystem.Services;

/// <summary>
/// Service for uploading images to Linode Object Storage (S3-compatible).
/// </summary>
public class ImageStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageStorageService"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public ImageStorageService(IConfiguration configuration)
    {
        var section = configuration.GetSection("Linode:S3");
        var endpoint = section["Endpoint"] ?? throw new InvalidOperationException("Linode:S3:Endpoint not configured.");
        _bucketName = section["BucketName"] ?? throw new InvalidOperationException("Linode:S3:BucketName not configured.");
        var accessKey = section["AccessKey"] ?? throw new InvalidOperationException("Linode:S3:AccessKey not configured.");
        var secretKey = section["SecretKey"] ?? throw new InvalidOperationException("Linode:S3:SecretKey not configured.");

        _baseUrl = $"{endpoint}/{_bucketName}";

        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    /// <summary>
    /// Uploads an image to the configured S3 bucket and returns its public URL.
    /// </summary>
    /// <param name="imageData">Raw image bytes.</param>
    /// <param name="contentType">MIME type, e.g. image/png.</param>
    /// <returns>Public URL of the uploaded image.</returns>
    public async Task<string> UploadImageAsync(byte[] imageData, string contentType)
    {
        var extension = contentType switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => "png"
        };

        var key = $"auction-items/{Guid.NewGuid():N}.{extension}";

        using var stream = new MemoryStream(imageData);
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead,
        };

        await _s3Client.PutObjectAsync(request);

        return $"{_baseUrl}/{key}";
    }
}
