namespace SanechkaHub;

public sealed class MainForm : Form
{
    private const string Version = "3.0.0";

    private readonly UserRecord user;
    private readonly Panel content = new();
    private readonly Label pageTitle = new();
    private readonly Label online = new();
    private readonly Label notificationBadge = new();
    private readonly Timer heartbeat = new();
    private readonly List<Button> nav = new();

    public MainForm(UserRecord currentUser)
    {
        user = currentUser;
        Text = "Санечка Hub 3.0";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1400, 860);
        MinimumSize = new Size(1080, 700);
        BackColor = Theme.Background;
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildShell();
        ShowHome();

        heartbeat.Interval = 30000;
        heartbeat.Tick += (_, _) =>
        {
            AppDatabase.Touch(user.Id, "Синхронизация статуса");
            RefreshHeader();
        };
        heartbeat.Start();

        AppDatabase.Touch(user.Id, "Открыта главная панель");
    }

    private void BuildShell()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 255));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(11, 14, 21),
            Padding = new Padding(18, 22, 18, 18)
        };
        root.Controls.Add(sidebar, 0, 0);

        var brand = Theme.Label("САНЕЧКА HUB", 19, true);
        brand.ForeColor = Theme.Accent;
        brand.Dock = DockStyle.Top;
        brand.Height = 38;
        sidebar.Controls.Add(brand);

        var sub = Theme.Label("PERSONAL CONTROL CENTER", 7.5F, true);
        sub.Dock = DockStyle.Top;
        sub.Height = 28;
        sidebar.Controls.Add(sub);

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 18, 0, 10)
        };
        sidebar.Controls.Add(menu);

        AddNav(menu, "⌂   Главная", ShowHome);
        AddNav(menu, "↓   Загрузки", ShowDownloads);
        AddNav(menu, "▣   История", ShowHistory);
        AddNav(menu, "★   Избранное", ShowFavorites);
        AddNav(menu, "♟   Профиль", ShowProfile);
        AddNav(menu, "⚙   Настройки", ShowSettings);
        AddNav(menu, "◉   Уведомления", ShowNotifications);
        if (user.IsAdmin)
        {
            AddNav(menu, "◆   Админ-панель", ShowAdmin);
            AddNav(menu, "▤   Журнал администратора", ShowAdminLog);
        }

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 88 };
        sidebar.Controls.Add(footer);

        var role = Theme.Label(user.IsAdmin ? "◆  СОЗДАТЕЛЬ" : "●  ПОЛЬЗОВАТЕЛЬ", 8, true);
        role.ForeColor = user.IsAdmin ? Theme.Accent : Theme.Success;
        role.Dock = DockStyle.Bottom;
        role.Height = 26;
        footer.Controls.Add(role);

        var ver = Theme.Label("Sanechka Hub • v" + Version, 8);
        ver.Dock = DockStyle.Bottom;
        ver.Height = 24;
        footer.Controls.Add(ver);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(right, 1, 0);

        var header = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 20, 28, 10) };
        right.Controls.Add(header, 0, 0);

        pageTitle.AutoSize = true;
        pageTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
        pageTitle.ForeColor = Theme.Text;
        pageTitle.Location = new Point(28, 20);
        header.Controls.Add(pageTitle);

        notificationBadge.AutoSize = true;
        notificationBadge.ForeColor = Theme.Warning;
        notificationBadge.Font = new Font("Segoe UI", 8, FontStyle.Bold);
        header.Controls.Add(notificationBadge);

        online.AutoSize = true;
        online.ForeColor = Theme.Success;
        online.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        header.Controls.Add(online);

        header.Resize += (_, _) => PositionHeader(header);
        PositionHeader(header);

        content.Dock = DockStyle.Fill;
        content.AutoScroll = true;
        content.Padding = new Padding(28, 8, 28, 28);
        right.Controls.Add(content, 0, 1);

        RefreshHeader();
    }

    private void PositionHeader(Control header)
    {
        online.Location = new Point(Math.Max(320, header.ClientSize.Width - online.PreferredWidth - 28), 25);
        notificationBadge.Location = new Point(
            Math.Max(180, online.Left - notificationBadge.PreferredWidth - 25), 27);
    }

    private void RefreshHeader()
    {
        var stats = AppDatabase.GetStats();
        online.Text = $"● {stats.Online} онлайн  •  {user.Username}";
        var unread = AppDatabase.UnreadNotifications(user.Id);
        notificationBadge.Text = unread > 0 ? $"● {unread} уведом." : "● уведомлений нет";
        PositionHeader(online.Parent!);
    }

    private void AddNav(FlowLayoutPanel menu, string text, Action action)
    {
        var b = new Button
        {
            Text = text,
            Width = 219,
            Height = 44,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(13, 0, 0, 0),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) =>
        {
            foreach (var x in nav)
            {
                x.BackColor = Color.Transparent;
                x.ForeColor = Theme.Muted;
            }
            b.BackColor = Theme.Surface2;
            b.ForeColor = Theme.Text;
            action();
        };
        b.MouseEnter += (_, _) => b.ForeColor = Theme.Text;
        b.MouseLeave += (_, _) => { if (b.BackColor == Color.Transparent) b.ForeColor = Theme.Muted; };
        nav.Add(b);
        menu.Controls.Add(b);
    }

    private void Begin(string title)
    {
        pageTitle.Text = title;
        content.SuspendLayout();
        foreach (Control c in content.Controls)
            c.Dispose();
        content.Controls.Clear();
        content.ResumeLayout();
    }

    private static Panel Card() => Theme.Card();

    private static Label Header(string text)
    {
        var l = Theme.Label(text, 17, true);
        l.Dock = DockStyle.Top;
        l.Height = 36;
        return l;
    }

    private static string SizeText(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.0} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024d / 1024d:0.0} MB";
        return $"{bytes / 1024d / 1024d / 1024d:0.0} GB";
    }

    private void ShowHome()
    {
        Begin("Главная");
        var s = AppDatabase.GetStats();

        var welcome = Card();
        welcome.Height = 185;
        welcome.Padding = new Padding(24);

        var title = Theme.Label($"Добро пожаловать, {user.Username} 👋", 25, true);
        title.Dock = DockStyle.Top;
        title.Height = 46;
        welcome.Controls.Add(title);

        var desc = Theme.Label(
            user.IsAdmin
                ? "Ты вошёл как создатель. Все инструменты управления доступны слева."
                : "Здесь собраны твои загрузки, история, профиль и настройки.", 10);
        desc.Font = new Font("Segoe UI", 10.5F);
        desc.Dock = DockStyle.Top;
        desc.Height = 34;
        welcome.Controls.Add(desc);

        var status = Theme.Label("●  Система работает нормально", 9, true);
        status.ForeColor = Theme.Success;
        status.Dock = DockStyle.Top;
        status.Height = 28;
        welcome.Controls.Add(status);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, WrapContents = false };
        welcome.Controls.Add(buttons);
        AddQuick(buttons, "Новая загрузка", ShowDownloads);
        AddQuick(buttons, "История", ShowHistory);
        AddQuick(buttons, "Уведомления", ShowNotifications);
        content.Controls.Add(welcome);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 128,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16)
        };
        for (int i = 0; i < 4; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        AddStat(grid, "ПОЛЬЗОВАТЕЛИ", s.Users.ToString(), Theme.Accent, 0);
        AddStat(grid, "ОНЛАЙН", s.Online.ToString(), Theme.Success, 1);
        AddStat(grid, "ЗАГРУЗКИ", s.Downloads.ToString(), Theme.Warning, 2);
        AddStat(grid, "АКТИВНОСТЬ", s.Activities.ToString(), Theme.Cyan, 3);
        content.Controls.Add(grid);

        var lower = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 285,
            ColumnCount = 2
        };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        var activity = Card();
        activity.Dock = DockStyle.Fill;
        activity.Padding = new Padding(18);
        activity.Paint += (x, e) => Theme.PaintRounded(x, e);
        activity.Controls.Add(Header("Твоя активность"));

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface2,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9)
        };
        foreach (var a in AppDatabase.GetActivity(user.Id, 10))
            list.Items.Add($"{a.CreatedUtc.ToLocalTime():dd.MM HH:mm}    {a.Action}");
        activity.Controls.Add(list);
        lower.Controls.Add(activity, 0, 0);

        var info = Card();
        info.Dock = DockStyle.Fill;
        info.Padding = new Padding(18);
        info.Paint += (x, e) => Theme.PaintRounded(x, e);
        info.Controls.Add(Header("Состояние Hub"));
        var state = Theme.Label(
            "\n● Авторизация              OK\n\n" +
            "● SQLite                   OK\n\n" +
            "● История                 OK\n\n" +
            "● Загрузчик               READY\n\n" +
            "● Интерфейс               READY\n\n" +
            $"VERSION                    {Version}",
            9, true);
        state.ForeColor = Theme.Text;
        state.Dock = DockStyle.Fill;
        info.Controls.Add(state);
        lower.Controls.Add(info, 1, 0);

        content.Controls.Add(lower);
    }

    private void AddQuick(FlowLayoutPanel p, string text, Action action)
    {
        var b = Theme.Button(text);
        b.Dock = DockStyle.None;
        b.Width = 145;
        b.Height = 38;
        b.Click += (_, _) => action();
        p.Controls.Add(b);
    }

    private void AddStat(TableLayoutPanel grid, string name, string value, Color accent, int col)
    {
        var card = Card();
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(0, 0, 10, 16);
        card.Padding = new Padding(18);
        var n = Theme.Label(name, 8, true);
        n.Dock = DockStyle.Bottom;
        n.Height = 22;
        var v = Theme.Label(value, 25, true);
        v.ForeColor = accent;
        v.Dock = DockStyle.Top;
        v.Height = 48;
        card.Controls.Add(n);
        card.Controls.Add(v);
        grid.Controls.Add(card, col, 0);
    }

    private void ShowDownloads()
    {
        Begin("Загрузки");

        var card = Card();
        card.Height = 390;
        card.Padding = new Padding(24);
        card.Controls.Add(Header("Менеджер загрузок"));

        var hint = Theme.Label("Скачивай файлы по прямой ссылке. Прогресс отображается ниже.", 9);
        hint.Dock = DockStyle.Top;
        hint.Height = 30;
        card.Controls.Add(hint);

        var url = Theme.Input();
        url.PlaceholderText = "https://example.com/archive.zip";
        url.Dock = DockStyle.Top;
        url.Height = 44;
        url.Margin = new Padding(0, 8, 0, 12);
        card.Controls.Add(url);

        var progress = new ProgressBar { Dock = DockStyle.Top, Height = 10, Visible = false };
        card.Controls.Add(progress);

        var status = Theme.Label("Готово.", 9);
        status.Dock = DockStyle.Top;
        status.Height = 36;
        card.Controls.Add(status);

        var row = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, WrapContents = false };
        var start = Theme.Button("Начать загрузку", true);
        start.Dock = DockStyle.None;
        start.Width = 185;
        start.Height = 42;

        var folder = Theme.Button("Открыть Downloads");
        folder.Dock = DockStyle.None;
        folder.Width = 185;
        folder.Height = 42;
        folder.Click += (_, _) => OpenDownloads();

        start.Click += async (_, _) =>
        {
            start.Enabled = false;
            progress.Visible = true;
            progress.Value = 0;
            try { await Download(url.Text, progress, status); }
            finally { start.Enabled = true; }
        };

        row.Controls.Add(start);
        row.Controls.Add(folder);
        card.Controls.Add(row);

        var safety = Theme.Label(
            "\nБезопасность: используй только ссылки на файлы, которым доверяешь.\n" +
            "Hub не запускает скачанные EXE автоматически.",
            9);
        safety.Dock = DockStyle.Fill;
        card.Controls.Add(safety);

        content.Controls.Add(card);
    }

    private static void OpenDownloads()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(folder);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private async Task Download(string address, ProgressBar progress, Label status)
    {
        if (!Uri.TryCreate(address.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            status.Text = "Введите корректную ссылку http/https.";
            return;
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? -1;
            var name = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(name)) name = "download.bin";

            foreach (var ch in Path.GetInvalidFileNameChars())
                name = name.Replace(ch, '_');

            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(folder);

            var path = Unique(Path.Combine(folder, name));
            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = File.Create(path);

            var buffer = new byte[64 * 1024];
            long done = 0;
            int read;

            while ((read = await input.ReadAsync(buffer)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read));
                done += read;

                if (total > 0)
                {
                    progress.Value = (int)Math.Clamp(done * 100 / total, 0, 100);
                    status.Text = $"{progress.Value}%  •  {SizeText(done)} / {SizeText(total)}";
                }
                else status.Text = SizeText(done);
            }

            AppDatabase.AddDownload(user.Id, Path.GetFileName(path), uri.ToString(), done);
            AppDatabase.Touch(user.Id, "Скачан файл: " + Path.GetFileName(path));
            status.Text = "✓ Загрузка завершена.";
            MessageBox.Show($"Файл сохранён:\n{path}", "Санечка Hub",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            status.Text = "Ошибка загрузки.";
            MessageBox.Show(ex.Message, "Ошибка загрузки",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string Unique(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);

        for (int i = 2; i < 10000; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
        return Path.Combine(dir, $"{name}_{DateTime.Now:yyyyMMddHHmmss}{ext}");
    }

    private void ShowHistory()
    {
        Begin("История");

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var activity = new TabPage("Активность") { BackColor = Theme.Surface };
        var downloads = new TabPage("Загрузки") { BackColor = Theme.Surface };

        var a = List("Время", 160, "Событие", 700);
        foreach (var x in AppDatabase.GetActivity(user.Id))
            a.Items.Add(new ListViewItem(new[]
            {
                x.CreatedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"),
                x.Action
            }));
        activity.Controls.Add(a);

        var d = List("Файл", 270, "Размер", 110, "Ссылка", 520, "Время", 160);
        foreach (var x in AppDatabase.GetDownloads(user.Id))
            d.Items.Add(new ListViewItem(new[]
            {
                x.FileName, SizeText(x.SizeBytes), x.Url,
                x.CreatedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            }));
        downloads.Controls.Add(d);

        tabs.TabPages.Add(activity);
        tabs.TabPages.Add(downloads);
        content.Controls.Add(tabs);
    }

    private static ListView List(params object[] columns)
    {
        var l = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            BackColor = Theme.Surface2,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9)
        };
        for (int i = 0; i < columns.Length; i += 2)
            l.Columns.Add(columns[i].ToString()!, Convert.ToInt32(columns[i + 1]));
        return l;
    }

    private void ShowFavorites()
    {
        Begin("Избранное");
        var card = Card();
        card.Height = 260;
        card.Padding = new Padding(24);
        card.Controls.Add(Header("Избранное"));

        var empty = Theme.Label(
            "\n\n★  Здесь будут твои любимые файлы и быстрые ссылки.\n\n" +
            "Раздел подготовлен для каталога Hub.",
            11, true);
        empty.ForeColor = Theme.Muted;
        empty.Dock = DockStyle.Fill;
        card.Controls.Add(empty);
        content.Controls.Add(card);
    }

    private void ShowNotifications()
    {
        Begin("Уведомления");
        var card = Card();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(18);
        card.Paint += (s, e) => Theme.PaintRounded(s, e);

        var top = new Panel { Dock = DockStyle.Top, Height = 48 };
        top.Controls.Add(Header("Центр уведомлений"));

        var read = Theme.Button("Отметить прочитанными");
        read.Dock = DockStyle.Right;
        read.Width = 190;
        read.Click += (_, _) =>
        {
            AppDatabase.MarkNotificationsRead(user.Id);
            RefreshHeader();
            ShowNotifications();
        };
        top.Controls.Add(read);
        card.Controls.Add(top);

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface2,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5F)
        };

        var notifications = AppDatabase.GetNotifications(user.Id);
        if (notifications.Count == 0)
            list.Items.Add("Нет новых уведомлений.");
        else
            foreach (var n in notifications)
                list.Items.Add($"{(n.Read ? "○" : "●")}  {n.CreatedUtc.ToLocalTime():dd.MM HH:mm}  {n.Title} — {n.Message}");

        card.Controls.Add(list);
        content.Controls.Add(card);
    }

    private void ShowProfile()
    {
        Begin("Профиль");
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 390,
            ColumnCount = 2
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        var profile = Card();
        profile.Dock = DockStyle.Fill;
        profile.Padding = new Padding(24);
        profile.Controls.Add(Header("Профиль пользователя"));

        var p = Theme.Label(
            $"\nЛОГИН\n{user.Username}\n\n" +
            $"EMAIL\n{user.Email}\n\n" +
            $"РЕГИСТРАЦИЯ\n{user.CreatedUtc.ToLocalTime():dd.MM.yyyy HH:mm}\n\n" +
            $"РОЛЬ\n{(user.IsAdmin ? "Создатель программы" : "Пользователь")}",
            10, true);
        p.ForeColor = Theme.Text;
        p.Dock = DockStyle.Fill;
        profile.Controls.Add(p);
        grid.Controls.Add(profile, 0, 0);

        var status = Card();
        status.Dock = DockStyle.Fill;
        status.Padding = new Padding(24);
        status.Controls.Add(Header("Аккаунт"));

        var st = Theme.Label(
            "\n●  Активен\n\n" +
            "●  База подключена\n\n" +
            $"●  Sanechka Hub {Version}\n\n" +
            (user.IsAdmin ? "◆  Права создателя" : "●  Стандартный аккаунт"),
            10, true);
        st.ForeColor = user.IsAdmin ? Theme.Accent : Theme.Success;
        st.Dock = DockStyle.Fill;
        status.Controls.Add(st);
        grid.Controls.Add(status, 1, 0);

        content.Controls.Add(grid);
    }

    private void ShowSettings()
    {
        Begin("Настройки");
        var card = Card();
        card.Height = 420;
        card.Padding = new Padding(24);
        card.Controls.Add(Header("Настройки программы"));

        var theme = Theme.Label("ОФОРМЛЕНИЕ", 8, true);
        theme.Dock = DockStyle.Top;
        theme.Height = 28;
        card.Controls.Add(theme);

        var dark = Theme.Button("Тёмная тема  •  включена");
        dark.Dock = DockStyle.Top;
        dark.Height = 44;
        dark.Click += (_, _) => MessageBox.Show(
            "Тёмная тема является основной темой Sanechka Hub 3.0.",
            "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Information);
        card.Controls.Add(dark);

        var db = Theme.Label("ЛОКАЛЬНАЯ БАЗА", 8, true);
        db.Dock = DockStyle.Top;
        db.Height = 28;
        db.Margin = new Padding(0, 18, 0, 0);
        card.Controls.Add(db);

        var path = Theme.Input();
        path.ReadOnly = true;
        path.Text = AppDatabase.DatabasePath;
        path.Dock = DockStyle.Top;
        path.Height = 42;
        card.Controls.Add(path);

        var copy = Theme.Button("Скопировать путь к базе");
        copy.Dock = DockStyle.Top;
        copy.Height = 42;
        copy.Margin = new Padding(0, 10, 0, 0);
        copy.Click += (_, _) =>
        {
            Clipboard.SetText(AppDatabase.DatabasePath);
            MessageBox.Show("Путь скопирован.", "Настройки");
        };
        card.Controls.Add(copy);

        var note = Theme.Label(
            "\nДанные приложения хранятся локально.\n" +
            "Для общего онлайна между разными компьютерами потребуется серверная версия Hub.",
            9);
        note.Dock = DockStyle.Fill;
        card.Controls.Add(note);

        content.Controls.Add(card);
    }

    private void ShowAdmin()
    {
        if (!user.IsAdmin) return;
        Begin("Админ-панель");

        var s = AppDatabase.GetStats();
        var stats = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 118,
            ColumnCount = 4
        };
        for (int i = 0; i < 4; i++)
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        AddStat(stats, "ПОЛЬЗОВАТЕЛИ", s.Users.ToString(), Theme.Accent, 0);
        AddStat(stats, "ОНЛАЙН", s.Online.ToString(), Theme.Success, 1);
        AddStat(stats, "БАНЫ", s.Banned.ToString(), Theme.Danger, 2);
        AddStat(stats, "АДМИНЫ", s.Admins.ToString(), Theme.Warning, 3);
        content.Controls.Add(stats);

        var card = Card();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(16);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            WrapContents = false
        };

        var refresh = Theme.Button("Обновить");
        refresh.Dock = DockStyle.None;
        refresh.Width = 130;
        refresh.Height = 38;

        var ban = Theme.Button("Заблокировать");
        ban.Dock = DockStyle.None;
        ban.Width = 160;
        ban.Height = 38;

        var unban = Theme.Button("Разблокировать");
        unban.Dock = DockStyle.None;
        unban.Width = 160;
        unban.Height = 38;

        toolbar.Controls.Add(refresh);
        toolbar.Controls.Add(ban);
        toolbar.Controls.Add(unban);
        card.Controls.Add(toolbar);

        var list = List("ID", 55, "Пользователь", 210, "Email", 270, "Роль", 150, "Статус", 150, "Дата", 150);
        card.Controls.Add(list);

        void Fill()
        {
            list.Items.Clear();
            foreach (var u in AppDatabase.GetUsers())
            {
                bool isOnline = u.LastSeenUtc.HasValue &&
                    DateTime.UtcNow - u.LastSeenUtc.Value < TimeSpan.FromSeconds(75);

                list.Items.Add(new ListViewItem(new[]
                {
                    u.Id.ToString(),
                    u.IsAdmin ? "Создатель" : u.Username,
                    u.IsAdmin ? "Скрыто" : u.Email,
                    u.IsAdmin ? "Создатель" : "Пользователь",
                    u.IsAdmin ? "● В сети" : (u.IsBanned ? "Заблокирован" : (isOnline ? "● В сети" : "Не в сети")),
                    u.CreatedUtc.ToLocalTime().ToString("dd.MM.yyyy")
                }) { Tag = u });
            }
        }

        refresh.Click += (_, _) => { Fill(); RefreshHeader(); };

        ban.Click += (_, _) =>
        {
            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выбери пользователя.");
                return;
            }
            if (list.SelectedItems[0].Tag is not UserRecord target) return;

            if (target.IsAdmin)
            {
                MessageBox.Show("Создателя программы нельзя заблокировать.");
                return;
            }

            using var dialog = new Form
            {
                Text = "Блокировка пользователя",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(500, 220),
                BackColor = Theme.Background,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = Theme.Label($"Причина для {target.Username}:", 10, true);
            label.Location = new Point(22, 20);
            dialog.Controls.Add(label);

            var reasonBox = Theme.Input();
            reasonBox.Location = new Point(22, 62);
            reasonBox.Size = new Size(455, 42);
            dialog.Controls.Add(reasonBox);

            var ok = Theme.Button("Заблокировать", true);
            ok.Location = new Point(22, 130);
            ok.Size = new Size(215, 42);
            ok.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(reasonBox.Text))
                {
                    MessageBox.Show("Укажи причину.", "Блокировка");
                    return;
                }
                if (!AppDatabase.SetBan(user.Id, target.Id, true, reasonBox.Text, out var error))
                {
                    MessageBox.Show(error, "Админ-панель");
                    return;
                }
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };
            dialog.Controls.Add(ok);

            var cancel = Theme.Button("Отмена");
            cancel.Location = new Point(262, 130);
            cancel.Size = new Size(215, 42);
            cancel.Click += (_, _) => dialog.Close();
            dialog.Controls.Add(cancel);

            dialog.ShowDialog(this);
            Fill();
        };

        unban.Click += (_, _) =>
        {
            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выбери пользователя.");
                return;
            }
            if (list.SelectedItems[0].Tag is not UserRecord target) return;

            if (!AppDatabase.SetBan(user.Id, target.Id, false, "", out var error))
                MessageBox.Show(error, "Админ-панель");
            Fill();
        };

        Fill();
        content.Controls.Add(card);
    }

    private void ShowAdminLog()
    {
        if (!user.IsAdmin) return;
        Begin("Журнал администратора");

        var card = Card();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(18);

        var list = List("Время", 180, "Событие", 850);
        foreach (var a in AppDatabase.GetActivity(user.Id, 200))
            list.Items.Add(new ListViewItem(new[]
            {
                a.CreatedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"),
                a.Action
            }));

        card.Controls.Add(list);
        content.Controls.Add(card);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        heartbeat.Stop();
        AppDatabase.Touch(user.Id, "Выход из программы");
        base.OnFormClosed(e);
    }
}
