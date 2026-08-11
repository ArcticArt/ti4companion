namespace Ti4Companion.Web.Services;

/// <summary>
/// One session this device has taken part in, remembered so it can be picked up again from the start page.
/// <para>
/// Kept per device in localStorage (see <see cref="SessionStore"/>), never on the server: it is a
/// convenience for one browser, and the server has no notion of "my sessions" — a session belongs to
/// whoever holds its join code.
/// </para>
/// <para>
/// <see cref="PlayerId"/> is the point of the whole record. The device token identifies the DEVICE and is
/// shared by every session it plays, so a single stored player id (which is what this replaced) could only
/// ever describe one session — coming back to an older one lost you your seat and you rejoined as a
/// stranger. The name fields are only there to make the entry readable in the list.
/// </para>
/// </summary>
public sealed record RecentSession(
    string Code,
    string Name,
    string PlayerName,
    Guid PlayerId,
    DateTimeOffset LastSeen);
