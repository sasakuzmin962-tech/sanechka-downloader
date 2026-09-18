namespace SanechkaHub;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (_, e) =>
        {
            try
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SanechkaHub");
                Directory.CreateDirectory(folder);
                File.AppendAllText(
                    Path.Combine(folder, "errors.log"),
                    $"[{DateTime.Now:O}] {e.Exception}\n\n");
            }
            catch { }

            MessageBox.Show(
                "Ошибка интерфейса:\n\n" + e.Exception.Message,
                "Sanechka Hub",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        try
        {
            AppDatabase.Initialize();

            using var auth = new AuthForm();
            if (auth.ShowDialog() == DialogResult.OK && auth.AuthenticatedUser is not null)
                Application.Run(new MainForm(auth.AuthenticatedUser));
        }
        catch (Exception ex)
        {
            try
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SanechkaHub");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "startup-error.txt"), ex.ToString());
            }
            catch { }

            MessageBox.Show(
                "Не удалось запустить Sanechka Hub.\n\n" + ex.Message,
                "Ошибка запуска",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
