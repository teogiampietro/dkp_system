namespace DkpSystem.Services;

public enum ToastType { Success, Error, Info, Warning }

public record ToastMessage(Guid Id, string Message, ToastType Type);

/// <summary>Scoped service for displaying ephemeral toast notifications in the UI.</summary>
public class ToastService
{
    private readonly List<ToastMessage> _toasts = new();

    public IReadOnlyList<ToastMessage> Toasts => _toasts;

    public event Action? OnChange;

    public void Show(string message, ToastType type = ToastType.Info)
    {
        _toasts.Add(new ToastMessage(Guid.NewGuid(), message, type));
        OnChange?.Invoke();
    }

    public void Dismiss(Guid id)
    {
        _toasts.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }
}
