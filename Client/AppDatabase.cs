using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace SanechkaHub;

public static class AppDatabase
{
    private static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SanechkaHub");

    private static readonly string DbPath = Path.Combine(Folder, "sanechka-hub.db");
    private static string ConnectionString => $"Data Source={DbPath};Mode=ReadWriteCreate";
    public static string DatabasePath => DbPath;

    public static void Initialize()
    {
        Directory.CreateDirectory(Folder);

        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;

            CREATE TABLE IF NOT EXISTS Users(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Email TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                IsAdmin INTEGER NOT NULL DEFAULT 0,
                IsBanned INTEGER NOT NULL DEFAULT 0,
                BanReason TEXT NOT NULL DEFAULT '',
                CreatedUtc TEXT NOT NULL,
                LastSeenUtc TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS Activity(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Action TEXT NOT NULL,
                CreatedUtc TEXT NOT NULL,
                FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Downloads(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                FileName TEXT NOT NULL,
                Url TEXT NOT NULL,
                SizeBytes INTEGER NOT NULL DEFAULT 0,
                CreatedUtc TEXT NOT NULL,
                FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Notifications(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Message TEXT NOT NULL,
                IsRead INTEGER NOT NULL DEFAULT 0,
                CreatedUtc TEXT NOT NULL,
                FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );
            """;
        cmd.ExecuteNonQuery();

        AddColumnIfMissing(c, "Users", "IsBanned", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(c, "Users", "BanReason", "TEXT NOT NULL DEFAULT ''");
        EnsureAdmin(c);
    }

    private static void EnsureAdmin(SqliteConnection c)
    {
        using var check = c.CreateCommand();
        check.CommandText = "SELECT Id FROM Users WHERE IsAdmin=1 LIMIT 1;";
        if (check.ExecuteScalar() is not null)
            return;

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO Users
            (Username,Email,PasswordHash,IsAdmin,IsBanned,BanReason,CreatedUtc)
            VALUES($u,$e,$p,1,0,'',$t);
            """;
        cmd.Parameters.AddWithValue("$u", "sanechka_admin");
        cmd.Parameters.AddWithValue("$e", "admin@local.invalid");
        cmd.Parameters.AddWithValue("$p", HashPassword("S4nechka!Hub#2026"));
        cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private static void AddColumnIfMissing(SqliteConnection c, string table, string column, string definition)
    {
        using var check = c.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table});";
        using var reader = check.ExecuteReader();
        while (reader.Read())
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return;

        using var alter = c.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        alter.ExecuteNonQuery();
    }

    public static UserRecord? Login(string login, string password, out string error)
    {
        error = "";
        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT Id,Username,Email,PasswordHash,IsAdmin,IsBanned,BanReason,CreatedUtc,LastSeenUtc
            FROM Users
            WHERE lower(Username)=lower($login) OR lower(Email)=lower($login)
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$login", login.Trim());

        using var r = cmd.ExecuteReader();
        if (!r.Read())
        {
            error = "Пользователь не найден.";
            return null;
        }

        var user = ReadUser(r);
        if (!VerifyPassword(password, user.PasswordHash))
        {
            error = "Неверный пароль.";
            return null;
        }

        if (user.IsBanned)
        {
            error = string.IsNullOrWhiteSpace(user.BanReason)
                ? "Этот аккаунт заблокирован."
                : "Этот аккаунт заблокирован.\nПричина: " + user.BanReason;
            return null;
        }

        Touch(user.Id, "Вход в программу");
        return user with { LastSeenUtc = DateTime.UtcNow };
    }

    public static bool Register(string username, string email, string password, out string error)
    {
        error = "";
        username = username.Trim();
        email = email.Trim();

        if (username.Length is < 3 or > 24)
        {
            error = "Логин должен содержать от 3 до 24 символов.";
            return false;
        }

        if (!username.All(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-'))
        {
            error = "Логин может содержать только буквы, цифры, _ и -.";
            return false;
        }

        if (!email.Contains('@') || email.Length > 120)
        {
            error = "Укажи корректный email.";
            return false;
        }

        if (password.Length < 6)
        {
            error = "Пароль должен содержать минимум 6 символов.";
            return false;
        }

        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var exists = c.CreateCommand();
        exists.CommandText = """
            SELECT COUNT(*) FROM Users
            WHERE lower(Username)=lower($u) OR lower(Email)=lower($e);
            """;
        exists.Parameters.AddWithValue("$u", username);
        exists.Parameters.AddWithValue("$e", email);

        if (Convert.ToInt32(exists.ExecuteScalar()) > 0)
        {
            error = "Такой логин или email уже используется.";
            return false;
        }

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Users
            (Username,Email,PasswordHash,IsAdmin,IsBanned,BanReason,CreatedUtc)
            VALUES($u,$e,$p,0,0,'',$t);
            """;
        cmd.Parameters.AddWithValue("$u", username);
        cmd.Parameters.AddWithValue("$e", email);
        cmd.Parameters.AddWithValue("$p", HashPassword(password));
        cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        return true;
    }

    public static void Touch(long userId, string action)
    {
        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var u = c.CreateCommand();
        u.CommandText = "UPDATE Users SET LastSeenUtc=$t WHERE Id=$id;";
        u.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("O"));
        u.Parameters.AddWithValue("$id", userId);
        u.ExecuteNonQuery();

        using var a = c.CreateCommand();
        a.CommandText = "INSERT INTO Activity(UserId,Action,CreatedUtc) VALUES($id,$a,$t);";
        a.Parameters.AddWithValue("$id", userId);
        a.Parameters.AddWithValue("$a", action);
        a.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("O"));
        a.ExecuteNonQuery();
    }

    public static UserStats GetStats()
    {
        using var c = new SqliteConnection(ConnectionString);
        c.Open();
        return new UserStats
        {
            Users = Scalar(c, "SELECT COUNT(*) FROM Users;"),
            Admins = Scalar(c, "SELECT COUNT(*) FROM Users WHERE IsAdmin=1;"),
            Banned = Scalar(c, "SELECT COUNT(*) FROM Users WHERE IsBanned=1;"),
            Online = Scalar(c,
                "SELECT COUNT(*) FROM Users WHERE LastSeenUtc >= $cut AND IsBanned=0;",
                ("$cut", DateTime.UtcNow.AddSeconds(-75).ToString("O"))),
            Activities = Scalar(c, "SELECT COUNT(*) FROM Activity;"),
            Downloads = Scalar(c, "SELECT COUNT(*) FROM Downloads;")
        };
    }

    public static List<UserRecord> GetUsers()
    {
        var list = new List<UserRecord>();
        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT Id,Username,Email,PasswordHash,IsAdmin,IsBanned,BanReason,CreatedUtc,LastSeenUtc
            FROM Users ORDER BY IsAdmin DESC, Username;
            """;

        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(ReadUser(r));
        return list;
    }

    public static List<ActivityRecord> GetActivity(long userId, int limit = 100)
    {
        var list = new List<ActivityRecord>();
        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT Action,CreatedUtc FROM Activity
            WHERE UserId=$u ORDER BY Id DESC LIMIT $l;
            """;
        cmd.Parameters.AddWithValue("$u", userId);
        cmd.Parameters.AddWithValue("$l", limit);

        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ActivityRecord(r.GetString(0), DateTime.Parse(r.GetString(1))));
        return list;
    }

    public static List<DownloadRecord> GetDownloads(long userId)
    {
        var list = new List<DownloadRecord>();
        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT FileName,SizeBytes,Url,CreatedUtc FROM Downloads
            WHERE UserId=$u ORDER BY Id DESC;
            """;
        cmd.Parameters.AddWithValue("$u", userId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new DownloadRecord(
                r.GetString(0), r.GetInt64(1), r.GetString(2), DateTime.Parse(r.GetString(3))));
        return list;
    }

    public static void AddDownload(long userId, string fileName, string url, long sizeBytes)
    {
        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Downloads(UserId,FileName,Url,SizeBytes,CreatedUtc)
            VALUES($u,$f,$url,$s,$t);
            """;
        cmd.Parameters.AddWithValue("$u", userId);
        cmd.Parameters.AddWithValue("$f", fileName);
        cmd.Parameters.AddWithValue("$url", url);
        cmd.Parameters.AddWithValue("$s", sizeBytes);
        cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public static int UnreadNotifications(long userId)
    {
        using var c = new SqliteConnection(ConnectionString);
        c.Open();
        return Scalar(c, "SELECT COUNT(*) FROM Notifications WHERE UserId=$u AND IsRead=0;", ("$u", userId));
    }

    public static List<(string Title, string Message, DateTime CreatedUtc, bool Read)> GetNotifications(long userId)
    {
        var list = new List<(string,string,DateTime,bool)>();
        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT Title,Message,CreatedUtc,IsRead FROM Notifications
            WHERE UserId=$u ORDER BY Id DESC LIMIT 50;
            """;
        cmd.Parameters.AddWithValue("$u", userId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetString(0), r.GetString(1), DateTime.Parse(r.GetString(2)), r.GetInt64(3) == 1));
        return list;
    }

    public static void MarkNotificationsRead(long userId)
    {
        using var c = new SqliteConnection(ConnectionString);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE Notifications SET IsRead=1 WHERE UserId=$u;";
        cmd.Parameters.AddWithValue("$u", userId);
        cmd.ExecuteNonQuery();
    }

    public static bool SetBan(long actorId, long targetId, bool banned, string reason, out string error)
    {
        error = "";

        if (actorId == targetId)
        {
            error = "Нельзя заблокировать самого себя.";
            return false;
        }

        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var target = c.CreateCommand();
        target.CommandText = "SELECT IsAdmin FROM Users WHERE Id=$id;";
        target.Parameters.AddWithValue("$id", targetId);
        var value = target.ExecuteScalar();

        if (value is null)
        {
            error = "Пользователь не найден.";
            return false;
        }

        if (Convert.ToInt32(value) == 1)
        {
            error = "Создателя программы нельзя заблокировать.";
            return false;
        }

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            UPDATE Users SET IsBanned=$b,BanReason=$r WHERE Id=$id;
            """;
        cmd.Parameters.AddWithValue("$b", banned ? 1 : 0);
        cmd.Parameters.AddWithValue("$r", banned ? reason.Trim() : "");
        cmd.Parameters.AddWithValue("$id", targetId);
        cmd.ExecuteNonQuery();

        Touch(actorId, banned
            ? $"Заблокирован пользователь #{targetId}"
            : $"Разблокирован пользователь #{targetId}");

        return true;
    }

    private static UserRecord ReadUser(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        Username = r.GetString(1),
        Email = r.GetString(2),
        PasswordHash = r.GetString(3),
        IsAdmin = r.GetInt64(4) == 1,
        IsBanned = r.GetInt64(5) == 1,
        BanReason = r.GetString(6),
        CreatedUtc = DateTime.Parse(r.GetString(7)),
        LastSeenUtc = r.IsDBNull(8) ? null : DateTime.Parse(r.GetString(8))
    };

    private static int Scalar(SqliteConnection c, string sql, params (string Key, object Value)[] p)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var item in p)
            cmd.Parameters.AddWithValue(item.Key, item.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(
            sha.ComputeHash(Encoding.UTF8.GetBytes("SANECHKA-HUB-V3|" + password)));
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(HashPassword(password)),
                Convert.FromHexString(hash));
        }
        catch
        {
            return false;
        }
    }
}
