using Microsoft.JSInterop;

namespace Ti4Companion.Web.Services;

/// <summary>Tiny dependency-free wrapper over the browser's localStorage.</summary>
public class BrowserStorage(IJSRuntime js)
{
    public async ValueTask<string?> GetAsync(string key)
    {
        try { return await js.InvokeAsync<string?>("localStorage.getItem", key); }
        catch { return null; }
    }

    public async ValueTask SetAsync(string key, string value)
    {
        try { await js.InvokeVoidAsync("localStorage.setItem", key, value); }
        catch { /* storage may be unavailable (private mode) */ }
    }

    public async ValueTask RemoveAsync(string key)
    {
        try { await js.InvokeVoidAsync("localStorage.removeItem", key); }
        catch { /* ignore */ }
    }
}
