using Microsoft.JSInterop;

namespace PlanningTable.Web.Services;

/// <summary>
/// Thông báo (toast), hộp xác nhận và giao diện sáng/tối dùng chung trong một phiên làm việc.
/// Chỉ gọi các hàm có sẵn của trình duyệt (confirm, setAttribute, localStorage).
/// </summary>
public class UiService(IJSRuntime js)
{
    public string? ToastMessage { get; private set; }
    public event Action? Changed;
    private CancellationTokenSource? _cts;

    public void Toast(string message)
    {
        ToastMessage = message;
        Changed?.Invoke();
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Delay(2400, token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            ToastMessage = null;
            Changed?.Invoke();
        }, TaskScheduler.Default);
    }

    public ValueTask<bool> ConfirmAsync(string message) => js.InvokeAsync<bool>("confirm", message);

    public async ValueTask ApplyThemeAsync(string theme)
    {
        await js.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", theme);
        // Ghi nhớ để lần sau áp dụng ngay khi trang vừa mở (xem index.html).
        try { await js.InvokeVoidAsync("localStorage.setItem", "planner-theme", theme); } catch (JSException) { }
    }
}
