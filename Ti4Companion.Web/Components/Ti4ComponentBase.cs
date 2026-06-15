using Microsoft.AspNetCore.Components;
using Ti4Companion.Web.Localization;
using Ti4Companion.Web.Services;

namespace Ti4Companion.Web.Components;

/// <summary>
/// Base for pages/components that react to language and shared-state changes. Re-renders
/// automatically whenever <see cref="Loc"/> or <see cref="Store"/> raise their change events.
/// Derived components that override <see cref="OnInitialized"/> must call <c>base.OnInitialized()</c>.
/// </summary>
public abstract class Ti4ComponentBase : ComponentBase, IDisposable
{
    [Inject] protected Loc Loc { get; set; } = default!;
    [Inject] protected SessionStore Store { get; set; } = default!;

    protected override void OnInitialized()
    {
        Loc.OnChange += Changed;
        Store.OnChange += Changed;
    }

    private void Changed() => InvokeAsync(StateHasChanged);

    public virtual void Dispose()
    {
        Loc.OnChange -= Changed;
        Store.OnChange -= Changed;
        GC.SuppressFinalize(this);
    }
}
