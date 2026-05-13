namespace DkpSystem.Services;

/// <summary>
/// Service for uploading images to Linode Object Storage (S3-compatible).
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Uploads an image to the configured S3 bucket and returns its public URL.
    /// </summary>
    /// <param name="imageData">Raw image bytes.</param>
    /// <param name="contentType">MIME type, e.g. image/png.</param>
    /// <returns>Public URL of the uploaded image.</returns>
    Task<string> UploadImageAsync(byte[] imageData, string contentType);
}
