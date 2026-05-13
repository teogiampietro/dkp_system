namespace DkpSystem.Services;

/// <summary>Scoped service for displaying ephemeral toast notifications in the UI.</summary>
public interface IToastService
{
    /// <summary>
    /// Gets the current list of toast messages.
    /// </summary>
    IReadOnlyList<ToastMessage> Toasts { get; }

    /// <summary>
    /// Raised when the toast list changes.
    /// </summary>
    event Action? OnChange;

    /// <summary>
    /// Shows a new toast message.
    /// </summary>
    /// <param name="message">The message to show.</param>
    /// <param name="type">The toast type.</param>
    void Show(string message, ToastType type = ToastType.Info);

    /// <summary>
    /// Dismisses a toast by ID.
    /// </summary>
    /// <param name="id">The toast ID.</param>
    void Dismiss(Guid id);
}
