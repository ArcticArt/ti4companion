using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Services;

/// <summary>
/// The instance-wide announcement: one short line the operator can put in front of every client ("the server
/// restarts in ten minutes", "there is a bug in the agenda phase, please reload"), which every reader can
/// click away.
/// <para>
/// It is a FILE, not a table and not a setting. A table would need a migration and a write endpoint, and a
/// public API that can make text appear on every screen is exactly the endpoint you do not want to have to
/// protect. A setting would need a deploy to change. The file is written over SSH by the Ops tool, which
/// already has that access and nothing else does — and it lives beside the session database, i.e. in the
/// data directory that SURVIVES a deploy (the application directory is swapped out wholesale).
/// </para>
/// <para>
/// Shape: <c>{ "text": "…", "updatedAt": "2026-08-13T18:00:00Z" }</c>. An empty (or missing, or unreadable)
/// file means there is nothing to announce, which is the normal state — so every failure here ends in
/// silence rather than in an error on somebody's screen.
/// </para>
/// </summary>
public sealed class NoticeService
{
    /// <summary>Long enough for a sentence or two, short enough that it cannot take over the screen.</summary>
    public const int MaxLength = 400;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly string _path;
    private readonly ILogger<NoticeService> _log;
    private readonly Lock _gate = new();

    private DateTime _stamp = DateTime.MinValue;   // last write time of the file we parsed
    private NoticeDto _cached = new("", "");

    public NoticeService(IConfiguration config, ILogger<NoticeService> log)
    {
        _log = log;
        _path = Resolve(config);
        _log.LogInformation("Notice file: {Path}", _path);
    }

    /// <summary>Explicit <c>Ti4:NoticePath</c>, else <c>notice.json</c> beside the session database — that is
    /// the one directory known to be writable, persistent and outside the deployed tree.</summary>
    private static string Resolve(IConfiguration config)
    {
        var configured = config["Ti4:NoticePath"];
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);

        var cs = config.GetConnectionString("ti4db") ?? "Data Source=ti4.db";
        var dataSource = new SqliteConnectionStringBuilder(cs).DataSource;
        var dir = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        return Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, "notice.json");
    }

    /// <summary>The current announcement, re-read only when the file has actually changed. Clients poll this,
    /// so the common case has to cost nothing but a stat().</summary>
    public NoticeDto Current()
    {
        try
        {
            var info = new FileInfo(_path);
            if (!info.Exists) return Clear();

            lock (_gate)
            {
                if (info.LastWriteTimeUtc == _stamp) return _cached;
                _stamp = info.LastWriteTimeUtc;
                _cached = Parse(File.ReadAllText(_path));
                return _cached;
            }
        }
        catch (Exception ex)
        {
            // A missing or half-written file is not an incident: say nothing and try again next time.
            _log.LogDebug(ex, "Notice file could not be read");
            return new NoticeDto("", "");
        }
    }

    private NoticeDto Clear()
    {
        lock (_gate)
        {
            _stamp = DateTime.MinValue;
            _cached = new NoticeDto("", "");
            return _cached;
        }
    }

    private NoticeDto Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new NoticeDto("", "");
        var file = JsonSerializer.Deserialize<NoticeFile>(raw, Json);
        var text = (file?.Text ?? "").Trim();
        if (text.Length == 0) return new NoticeDto("", "");
        if (text.Length > MaxLength) text = text[..MaxLength];

        // The id has to change with the message and stay the same while it does not, because a device stores
        // it to remember what it has already clicked away. Hashing the text itself means a message re-sent
        // unchanged does not come back for people who dismissed it, which is the friendlier of the two.
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..12].ToLowerInvariant();
        return new NoticeDto(text, id);
    }

    private sealed record NoticeFile(string? Text, DateTimeOffset? UpdatedAt);
}
