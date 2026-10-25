using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MyG.JARVIS.App;

public sealed class PowerShellTerminalWindow : Window
{
    private readonly TextBox _output = new();
    private readonly TextBox _command = new();
    private readonly TextBlock _status = new();
    private Process? _process;
    private readonly StringBuilder _pending = new();

    public PowerShellTerminalWindow()
    {
        Title = "MyG JARVIS · PowerShell";
        Width = 1050;
        Height = 720;
        MinWidth = 760;
        MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush("#101114");
        Foreground = Brush("#E8E8EC");
        FontFamily = new FontFamily("Consolas");

        var root = new DockPanel { Margin = new Thickness(16) };
        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(new TextBlock { Text = "PowerShell", FontSize = 19, FontWeight = FontWeights.SemiBold, Foreground = Brush("#E7A94B"), VerticalAlignment = VerticalAlignment.Center });
        _status.Text = "Başlatılıyor…";
        _status.Foreground = Brush("#92939F");
        _status.Margin = new Thickness(14, 4, 0, 0);
        _status.VerticalAlignment = VerticalAlignment.Center;
        header.Children.Add(_status);
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var bottom = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        DockPanel.SetDock(bottom, Dock.Bottom);
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _command.FontFamily = new FontFamily("Consolas");
        _command.FontSize = 13;
        _command.Padding = new Thickness(10);
        _command.Background = Brush("#202127");
        _command.Foreground = Brush("#E8E8EC");
        _command.BorderBrush = Brush("#373841");
        _command.VerticalContentAlignment = VerticalAlignment.Center;
        _command.ToolTip = "Çalıştırılacak PowerShell komutunu yaz veya yapıştır";
        _command.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter && !System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
            {
                e.Handled = true;
                RunCommand();
            }
        };
        row.Children.Add(_command);

        var run = MakeButton("Çalıştır", true);
        run.Margin = new Thickness(8, 0, 0, 0);
        run.Click += (_, _) => RunCommand();
        Grid.SetColumn(run, 1);
        row.Children.Add(run);

        var paste = MakeButton("Panodan al", false);
        paste.Margin = new Thickness(8, 0, 0, 0);
        paste.Click += (_, _) =>
        {
            try { _command.Text = Clipboard.GetText(); _command.Focus(); _command.CaretIndex = _command.Text.Length; }
            catch (Exception ex) { AppendOutput("Pano okunamadı: " + ex.Message); }
        };
        Grid.SetColumn(paste, 2);
        row.Children.Add(paste);
        bottom.Children.Add(row);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        var copy = MakeButton("Çıktıyı kopyala", false);
        copy.Click += (_, _) => { try { Clipboard.SetText(_output.Text); _status.Text = "Çıktı panoya kopyalandı."; } catch (Exception ex) { AppendOutput("Pano hatası: " + ex.Message); } };
        actions.Children.Add(copy);
        var clear = MakeButton("Temizle", false);
        clear.Margin = new Thickness(8, 0, 0, 0);
        clear.Click += (_, _) => _output.Clear();
        actions.Children.Add(clear);
        bottom.Children.Add(actions);
        root.Children.Add(bottom);

        _output.IsReadOnly = true;
        _output.AcceptsReturn = true;
        _output.TextWrapping = TextWrapping.Wrap;
        _output.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _output.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        _output.FontFamily = new FontFamily("Consolas");
        _output.FontSize = 12;
        _output.Padding = new Thickness(12);
        _output.Background = Brush("#0B0C0F");
        _output.Foreground = Brush("#D9D9E0");
        _output.BorderBrush = Brush("#303139");
        root.Children.Add(_output);

        Content = root;
        Loaded += (_, _) => StartPowerShell();
        Closed += (_, _) => StopPowerShell();
    }

    private void StartPowerShell()
    {
        if (_process is { HasExited: false }) return;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoLogo -NoProfile -ExecutionPolicy RemoteSigned -Command -",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            psi.Environment["POWERSHELL_TELEMETRY_OPTOUT"] = "1";
            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _process.OutputDataReceived += (_, e) => { if (e.Data is not null) AppendOutput(e.Data); };
            _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) AppendOutput(e.Data); };
            _process.Exited += (_, _) => Dispatcher.Invoke(() => _status.Text = "PowerShell kapandı.");
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _process.StandardInput.AutoFlush = true;
            _status.Text = "PowerShell açık · komutlar onayından sonra çalışır";
            AppendOutput("MyG JARVIS PowerShell terminali hazır.");
            AppendOutput("Çalıştırmadan önce komutun ne yaptığını kontrol et. Yönetici yetkisi otomatik verilmez.");
        }
        catch (Exception ex)
        {
            _status.Text = "PowerShell başlatılamadı.";
            AppendOutput("Başlatma hatası: " + ex.Message);
        }
    }

    private void RunCommand()
    {
        string command = _command.Text.Trim();
        if (string.IsNullOrWhiteSpace(command)) return;
        if (_process is null || _process.HasExited)
        {
            AppendOutput("PowerShell çalışmıyor. Pencereyi kapatıp yeniden aç.");
            return;
        }

        var answer = MessageBox.Show(
            "Aşağıdaki komut PowerShell içinde çalıştırılacak. Devam edilsin mi?\n\n" + command,
            "Komut çalıştırma onayı",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            AppendOutput("PS> " + command);
            _process.StandardInput.WriteLine(command);
            _command.Clear();
            _command.Focus();
        }
        catch (Exception ex)
        {
            AppendOutput("Komut gönderilemedi: " + ex.Message);
        }
    }

    private void AppendOutput(string line)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _output.AppendText(line + Environment.NewLine);
            _output.ScrollToEnd();
        });
    }

    private void StopPowerShell()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.StandardInput.Close();
                if (!_process.WaitForExit(1500)) _process.Kill(true);
            }
        }
        catch { /* Closing the terminal should not crash the app. */ }
        finally
        {
            _process?.Dispose();
            _process = null;
        }
    }

    private static Button MakeButton(string text, bool gold) => new()
    {
        Content = text,
        Padding = new Thickness(14, 9, 14, 9),
        Background = Brush(gold ? "#E7A94B" : "#303139"),
        Foreground = Brush(gold ? "#17130D" : "#E8E8EC"),
        BorderThickness = new Thickness(0),
        FontWeight = FontWeights.SemiBold,
        Cursor = System.Windows.Input.Cursors.Hand
    };

    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
}