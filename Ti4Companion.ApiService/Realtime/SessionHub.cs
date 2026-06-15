using Microsoft.AspNetCore.SignalR;

namespace Ti4Companion.ApiService.Realtime;

/// <summary>
/// Real-time fan-out for a session. Every device (beamer, iPad, phones) joins the group named
/// after the session's join code; after any mutation the API notifies the group and clients
/// reload the shared state.
/// </summary>
public class SessionHub : Hub
{
    public Task JoinSession(string code)
        => Groups.AddToGroupAsync(Context.ConnectionId, Normalize(code));

    public Task LeaveSession(string code)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, Normalize(code));

    internal static string Normalize(string code) => code.Trim().ToUpperInvariant();
}

public static class SessionHubExtensions
{
    public const string SessionChanged = "SessionChanged";

    /// <summary>Tell every device watching this session to reload the shared state.</summary>
    public static Task NotifySessionChanged(this IHubContext<SessionHub> hub, string joinCode)
        => hub.Clients.Group(SessionHub.Normalize(joinCode)).SendAsync(SessionChanged);
}
