using Microsoft.JSInterop;

namespace Ti4Companion.Web.Services;

/// <summary>Tiny dependency-free wrapper over the browser's localStorage.</summary>
public class BrowserStorage(IJSRuntime js)
{
    /// <summary>
    /// Read a key, saying whether the READ ITSELF worked. The difference matters for anything that would
    /// otherwise treat "could not read" as "not stored yet" — above all the device token, where that
    /// mistake mints a second identity and writes it over the first (see <c>SessionStore.InitAsync</c>).
    /// </summary>
    /// <returns><c>Ok</c> false means localStorage could not be reached at all; the value is then meaningless.</returns>
    public async ValueTask<(bool Ok, string? Value)> TryGetAsync(string key)
    {
        try { return (true, await js.InvokeAsync<string?>("localStorage.getItem", key)); }
        catch { return (false, null); }
    }

    /// <summary>Read a key, treating an unreachable storage as an absent value. Fine for preferences —
    /// the worst case is a default. NOT fine for anything that identifies this device.</summary>
    public async ValueTask<string?> GetAsync(string key) => (await TryGetAsync(key)).Value;

    /// <summary>Write a key. Reports whether it actually landed, so a caller that stores an identity can
    /// tell "saved" from "this page load only".</summary>
    public async ValueTask<bool> SetAsync(string key, string value)
    {
        try { await js.InvokeVoidAsync("localStorage.setItem", key, value); return true; }
        catch { return false; /* storage may be unavailable (private mode) */ }
    }

    public async ValueTask RemoveAsync(string key)
    {
        try { await js.InvokeVoidAsync("localStorage.removeItem", key); }
        catch { /* ignore */ }
    }
}
