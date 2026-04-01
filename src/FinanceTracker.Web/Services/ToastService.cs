namespace FinanceTracker.Web.Services;

public class ToastService
{
    private readonly List<ToastMessage> _toasts = new();

    public IReadOnlyList<ToastMessage> Toasts => _toasts;

    public event Action? OnChange;

    public void Show(string message, ToastType type = ToastType.Success)
    {
        var toast = new ToastMessage(Guid.NewGuid(), message, type);
        _toasts.Add(toast);
        OnChange?.Invoke();
        _ = DismissAfterDelayAsync(toast.Id);
    }

    public void Dismiss(Guid id)
    {
        _toasts.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }

    private async Task DismissAfterDelayAsync(Guid id)
    {
        await Task.Delay(3500);
        Dismiss(id);
    }
}

public record ToastMessage(Guid Id, string Message, ToastType Type);

public enum ToastType { Success, Error, Info }
