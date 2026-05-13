namespace DkpSystem.Services;

/// <summary>
/// Represents the result of a service operation.
/// </summary>
public class ServiceResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private ServiceResult(bool isSuccess, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful service result.</returns>
    public static ServiceResult Success() => new ServiceResult(true);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed service result.</returns>
    public static ServiceResult Failure(string errorMessage) => new ServiceResult(false, errorMessage);
}
