using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

internal static class Program
{
    const string Product = "Snap 截图工具";
    const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SnapScreenshot";
    static readonly string InstallDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "SnapScreenshot");
    static readonly string DesktopLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Snap截图工具.lnk");
    static readonly string MenuLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Snap截图工具.lnk");
    static string LogPath => Path.Combine(Path.GetTempPath(), "SnapSetup.log");

    [STAThread]
    static int Main(string[] args)
    {
        Forms.Application.SetHighDpiMode(Forms.HighDpiMode.PerMonitorV2); Forms.Application.EnableVisualStyles(); Forms.Application.SetCompatibleTextRenderingDefault(false);
#if UNINSTALLER
        try { return Uninstall(args.Contains("--quiet")); }
        catch (Exception ex) { Forms.MessageBox.Show(ex.Message, "卸载失败"); return 1; }
#else
        if (args.Contains("--silent-install"))
        {
            try { Install(); return 0; }
            catch (Exception ex) { File.WriteAllText(LogPath, ex.ToString()); return 1; }
        }
        ShowInstaller();
        return 0;
#endif
    }

    static void EnsureClosed()
    {
        foreach (var process in Process.GetProcessesByName("Snap.App"))
        {
            using (process)
            {
                string? path;
                try { path = process.MainModule?.FileName; } catch { continue; }
                if (string.Equals(path, Path.Combine(InstallDir, "Snap.App.exe"), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("请先在右下角系统托盘中退出 Snap，再重试。");
            }
        }
    }

    static void Install()
    {
        EnsureClosed();
        using var existing = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (Directory.Exists(InstallDir) && existing?.GetValue("DisplayName") as string != Product)
            throw new InvalidOperationException("安装目录已存在且不属于此安装程序，请先检查：" + InstallDir);
        foreach (var link in new[] { DesktopLink, MenuLink })
            if (File.Exists(link) && existing is null)
                throw new InvalidOperationException("已有同名快捷方式，请先重命名：" + link);

        string stage = InstallDir + ".staging-" + Guid.NewGuid().ToString("N");
        string backup = InstallDir + ".backup-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.GetDirectoryName(InstallDir)!);
        bool oldMoved = false;
        bool newMoved = false;
        try
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")
                ?? throw new InvalidOperationException("安装包缺少程序文件。"))
            using (var zip = new ZipArchive(stream))
                zip.ExtractToDirectory(stage);
            if (!File.Exists(Path.Combine(stage, "Snap.App.exe")) || !File.Exists(Path.Combine(stage, "SnapUninstall.exe")))
                throw new InvalidOperationException("安装包不完整。");
            if (Directory.Exists(InstallDir)) { Directory.Move(InstallDir, backup); oldMoved = true; }
            Directory.Move(stage, InstallDir);
            newMoved = true;
            CreateShortcut(DesktopLink);
            CreateShortcut(MenuLink);
            using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
            key.SetValue("DisplayName", Product);
            key.SetValue("DisplayVersion", Assembly.GetExecutingAssembly().GetName().Version!.ToString(3));
            key.SetValue("Publisher", "ccy6");
            key.SetValue("InstallLocation", InstallDir);
            key.SetValue("DisplayIcon", Path.Combine(InstallDir, "Snap.App.exe"));
            key.SetValue("UninstallString", "\"" + Path.Combine(InstallDir, "SnapUninstall.exe") + "\"");
            key.SetValue("QuietUninstallString", "\"" + Path.Combine(InstallDir, "SnapUninstall.exe") + "\" --quiet");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            key.SetValue("EstimatedSize", (int)(Directory.GetFiles(InstallDir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length) / 1024), RegistryValueKind.DWord);
        }
        catch
        {
            if (newMoved && Directory.Exists(InstallDir)) Directory.Delete(InstallDir, true);
            if (oldMoved) Directory.Move(backup, InstallDir);
            else { File.Delete(DesktopLink); File.Delete(MenuLink); Registry.CurrentUser.DeleteSubKeyTree(RegistryPath, false); }
            throw;
        }
        finally
        {
            if (Directory.Exists(stage)) Directory.Delete(stage, true);
        }
        if (oldMoved) { try { Directory.Delete(backup, true); } catch { /* A retained backup does not invalidate installation. */ } }
    }

    static void CreateShortcut(string path)
    {
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
        dynamic link = shell.CreateShortcut(path);
        link.TargetPath = Path.Combine(InstallDir, "Snap.App.exe");
        link.WorkingDirectory = InstallDir;
        link.IconLocation = Path.Combine(InstallDir, "Snap.App.exe") + ",0";
        link.Description = "Snap 截图工具，截图快捷键 Alt + Q";
        link.Save();
    }

    static void ShowInstaller()
    {
        using var form = new Forms.Form { Text = Product + " · 安装", ClientSize = new System.Drawing.Size(570, 330), FormBorderStyle = Forms.FormBorderStyle.FixedDialog, MaximizeBox = false, StartPosition = Forms.FormStartPosition.CenterScreen, Font = new System.Drawing.Font("Microsoft YaHei UI", 10) };
        form.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        var heading = new Forms.Label { Text = "安装 Snap 截图工具", Left = 28, Top = 25, Width = 520, Height = 38, Font = new System.Drawing.Font("Microsoft YaHei UI", 18, System.Drawing.FontStyle.Bold) };
        var body = new Forms.Label { Text = "截图、标注、贴图和截图历史\n\n• 自带运行环境，无需额外下载\n• 自动创建桌面和开始菜单快捷方式\n• 默认截图快捷键：Alt + Q", Left = 30, Top = 80, Width = 510, Height = 125 };
        var location = new Forms.Label { Text = "安装到当前用户：\n" + InstallDir, Left = 30, Top = 210, Width = 510, Height = 50, AutoEllipsis = true };
        var button = new Forms.Button { Text = "安装", Left = 390, Top = 278, Width = 150, Height = 36 };
        var status = new Forms.Label { Left = 30, Top = 284, Width = 350, Height = 30, Text = "Windows 10 / 11 · 64 位" };
        bool busy = false, complete = false;
        form.FormClosing += (_, e) => { if (busy) e.Cancel = true; };
        button.Click += async (_, _) =>
        {
            if (complete)
            {
                try { Process.Start(new ProcessStartInfo(Path.Combine(InstallDir, "Snap.App.exe")) { UseShellExecute = true }); form.Close(); }
                catch (Exception ex) { Forms.MessageBox.Show(form, ex.Message, "启动失败"); }
                return;
            }
            busy = true; button.Enabled = false; status.Text = "正在安装，请稍候…";
            try
            {
                await Task.Run(Install);
                complete = true; heading.Text = "安装完成"; body.Text = "现在可以通过桌面快捷方式启动 Snap。\n\n启动后按 Alt + Q 开始截图。\n在系统托盘右键 Snap 图标可打开截图台或退出。\n\n可以在 Windows 设置的“应用”中卸载。";
                status.Text = "安装成功"; button.Text = "启动 Snap";
            }
            catch (Exception ex) { File.WriteAllText(LogPath, ex.ToString()); status.Text = "安装失败，可重试"; Forms.MessageBox.Show(form, ex.Message, "安装失败"); }
            finally { busy = false; button.Enabled = true; }
        };
        form.Controls.AddRange([heading, body, location, button, status]);
        Forms.Application.Run(form);
    }

    static int Uninstall(bool quiet)
    {
        if (!string.Equals(Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar), InstallDir, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("请从已安装的程序目录运行卸载程序。");
        if (!quiet && Forms.MessageBox.Show("卸载 Snap 截图工具？\n截图历史和个人设置将保留。", Product, Forms.MessageBoxButtons.YesNo, Forms.MessageBoxIcon.Question) != Forms.DialogResult.Yes) return 0;
        EnsureClosed();
        // The cleanup process independently derives and validates the one permitted target.
        string script = " $ErrorActionPreference='Stop'; " +
            "$target=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\\SnapScreenshot'; " +
            "$expected=[IO.Path]::GetFullPath($target); " +
            "if (!(Test-Path -LiteralPath (Join-Path $expected 'SnapUninstall.exe'))) {exit 2}; " +
            "$reg='HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\SnapScreenshot'; " +
            "if ((Get-ItemProperty -LiteralPath $reg).InstallLocation -ne $expected) {exit 3}; " +
            "Wait-Process -Id " + Environment.ProcessId + " -ErrorAction SilentlyContinue; " +
            "Remove-Item -LiteralPath $expected -Recurse -Force; " +
            "foreach ($folder in @('DesktopDirectory','Programs')) { $link=Join-Path ([Environment]::GetFolderPath($folder)) 'Snap截图工具.lnk'; if (Test-Path -LiteralPath $link) { $ws=New-Object -ComObject WScript.Shell; if ($ws.CreateShortcut($link).TargetPath -eq (Join-Path $expected 'Snap.App.exe')) {Remove-Item -LiteralPath $link -Force} } }; " +
            "Remove-Item -LiteralPath $reg -Force; ";
        string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
        Process.Start(new ProcessStartInfo(powershell, "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script))) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
        return 0;
    }
}
