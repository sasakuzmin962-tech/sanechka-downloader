namespace SanechkaHub;

public sealed record UserRecord
{
    public long Id { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public bool IsAdmin { get; init; }
    public bool IsBanned { get; init; }
    public string BanReason { get; init; } = "";
    public DateTime CreatedUtc { get; init; }
    public DateTime? LastSeenUtc { get; init; }
}

public sealed record ActivityRecord(string Action, DateTime CreatedUtc);

public sealed record DownloadRecord(
    string FileName,
    long SizeBytes,
    string Url,
    DateTime CreatedUtc);

public sealed class UserStats
{
    public int Users { get; init; }
    public int Admins { get; init; }
    public int Online { get; init; }
    public int Banned { get; init; }
    public int Activities { get; init; }
    public int Downloads { get; init; }
}
