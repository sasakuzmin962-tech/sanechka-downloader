namespace SanechkaHub;

public sealed class AuthForm : Form
{
    private const string Version = "3.0.0";

    private readonly TextBox login = Theme.Input();
    private readonly TextBox email = Theme.Input();
    private readonly TextBox password = Theme.Input();
    private readonly TextBox confirm = Theme.Input();

    private readonly Panel emailField = new();
    private readonly Panel confirmField = new();

    private readonly Label title = Theme.Label("С возвращением", 25, true);
    private readonly Label subtitle = Theme.Label("Войди в Санечка Hub.", 10);
    private readonly Button action = Theme.Button("Войти", true);
    private readonly Button toggle = Theme.Button("Создать аккаунт");
    private bool register;

    public UserRecord? AuthenticatedUser { get; private set; }

    public AuthForm()
    {
        Text = "Санечка Hub";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1180, 760);
        MinimumSize = new Size(900, 620);
        BackColor = Theme.Background;
        AutoScaleMode = AutoScaleMode.Dpi;

        Build();
        UpdateMode();
    }

    private void Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(46)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        Controls.Add(root);

        var hero = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(28, 42, 72, 30)
        };
        hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        hero.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        hero.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var a = Theme.Label("САНЕЧКА", 38, true);
        a.ForeColor = Theme.Accent;
        a.Dock = DockStyle.Fill;
        hero.Controls.Add(a, 0, 0);

        var h = Theme.Label("HUB", 38, true);
        h.Dock = DockStyle.Fill;
        hero.Controls.Add(h, 0, 1);

        var d = Theme.Label("Твой цифровой центр\nдля файлов, игр и проектов.", 15);
        d.Dock = DockStyle.Fill;
        hero.Controls.Add(d, 0, 2);

        var v = Theme.Label("●  VERSION " + Version, 9, true);
        v.ForeColor = Theme.Success;
        v.Dock = DockStyle.Fill;
        hero.Controls.Add(v, 0, 3);

        var card = Theme.Card();
        card.Height = 132;
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(18);
        card.Paint += (s, e) => Theme.PaintRounded(s, e);
        var txt = Theme.Label(
            "SANECHKA HUB 3.0\n\n" +
            "◆ Новый интерфейс\n" +
            "◆ Быстрее и удобнее\n" +
            "◆ Расширенная админка\n" +
            "◆ История и уведомления",
            9, true);
        txt.ForeColor = Theme.Text;
        txt.Dock = DockStyle.Fill;
        card.Controls.Add(txt);
        hero.Controls.Add(card, 0, 5);

        var f = Theme.Label("© Санечка Hub", 8);
        f.Dock = DockStyle.Fill;
        hero.Controls.Add(f, 0, 6);

        root.Controls.Add(hero, 0, 0);

        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 8, 16) };
        root.Controls.Add(host, 1, 0);

        var auth = Theme.Card();
        auth.Dock = DockStyle.Fill;
        auth.Padding = new Padding(42);
        auth.Paint += (s, e) => Theme.PaintRounded(s, e);
        host.Controls.Add(auth);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        auth.Controls.Add(layout);

        title.Dock = DockStyle.Fill;
        subtitle.Dock = DockStyle.Fill;
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(subtitle, 0, 1);
        layout.Controls.Add(BuildField("Логин или email", login), 0, 3);
        layout.Controls.Add(BuildField("Пароль", password), 0, 4);

        emailField.Dock = DockStyle.Fill;
        emailField.Controls.Add(BuildField("Email", email));
        layout.Controls.Add(emailField, 0, 5);

        confirmField.Dock = DockStyle.Fill;
        confirmField.Controls.Add(BuildField("Повтори пароль", confirm));
        layout.Controls.Add(confirmField, 0, 6);

        password.UseSystemPasswordChar = true;
        confirm.UseSystemPasswordChar = true;

        action.Dock = DockStyle.Top;
        action.Height = 46;
        action.Margin = new Padding(0, 12, 0, 0);
        layout.Controls.Add(action, 0, 7);

        toggle.Dock = DockStyle.Bottom;
        toggle.Height = 42;
        toggle.Margin = new Padding(0, 10, 0, 0);
        layout.Controls.Add(toggle, 0, 7);

        action.Click += (_, _) => Submit();
        toggle.Click += (_, _) => { register = !register; UpdateMode(); };
        AcceptButton = action;
    }

    private static Panel BuildField(string caption, TextBox box)
    {
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 8) };
        var l = Theme.Label(caption, 9, true);
        l.Dock = DockStyle.Top;
        l.Height = 22;
        box.Dock = DockStyle.Fill;
        p.Controls.Add(box);
        p.Controls.Add(l);
        return p;
    }

    private void UpdateMode()
    {
        title.Text = register ? "Создай аккаунт" : "С возвращением";
        subtitle.Text = register ? "Аккаунт сохранится в локальной базе." : "Войди в Санечка Hub.";
        action.Text = register ? "Зарегистрироваться" : "Войти";
        toggle.Text = register ? "Уже есть аккаунт? Войти" : "Нет аккаунта? Создать";
        emailField.Visible = register;
        confirmField.Visible = register;
        login.PlaceholderText = register ? "Придумай логин" : "Логин или email";
        password.PlaceholderText = "Пароль";
        login.Focus();
    }

    private void Submit()
    {
        if (register)
        {
            if (string.IsNullOrWhiteSpace(email.Text))
            {
                MessageBox.Show("Введи email.", "Регистрация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                email.Focus();
                return;
            }

            if (password.Text != confirm.Text)
            {
                MessageBox.Show("Пароли не совпадают.", "Регистрация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                confirm.Focus();
                return;
            }

            if (!AppDatabase.Register(login.Text, email.Text, password.Text, out var error))
            {
                MessageBox.Show(error, "Регистрация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Аккаунт создан. Теперь войди.", "Санечка Hub",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            register = false;
            email.Clear();
            confirm.Clear();
            password.Clear();
            UpdateMode();
            return;
        }

        if (string.IsNullOrWhiteSpace(login.Text) || string.IsNullOrWhiteSpace(password.Text))
        {
            MessageBox.Show("Заполни логин и пароль.", "Санечка Hub",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var user = AppDatabase.Login(login.Text, password.Text, out var errorText);
        if (user is null)
        {
            MessageBox.Show(errorText, "Вход", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            password.SelectAll();
            password.Focus();
            return;
        }

        AuthenticatedUser = user;
        DialogResult = DialogResult.OK;
        Close();
    }
}
