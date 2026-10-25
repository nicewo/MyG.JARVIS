using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyG.JARVIS.App.Services;

namespace MyG.JARVIS.App;

public sealed class ApiSettingsWindow : Window
{
    private readonly PasswordBox _keyBox = new();
    private readonly TextBlock _status = new();
    private readonly Button _testButton = new();

    public string? ValidatedApiKey { get; private set; }

    public ApiSettingsWindow()
    {
        Title = "MyG JARVIS · OpenAI API";
        Width = 560;
        Height = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(16, 17, 20));
        Foreground = new SolidColorBrush(Color.FromRgb(232, 232, 236));
        FontFamily = new FontFamily("Segoe UI");

        var root = new StackPanel { Margin = new Thickness(26) };
        root.Children.Add(new TextBlock { Text = "OpenAI API bağlantısı", FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        root.Children.Add(new TextBlock { Text = "API anahtarını yapıştır. JARVIS bağlantıyı test edecek ve sonucu burada gösterecek.", TextWrapping = TextWrapping.Wrap, Foreground = Brush("#92939F"), Margin = new Thickness(0, 0, 0, 18) });
        root.Children.Add(new TextBlock { Text = "API KEY", Foreground = Brush("#E7A94B"), FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 7) });
        _keyBox.Password = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
        _keyBox.Padding = new Thickness(12);
        _keyBox.FontSize = 14;
        _keyBox.Background = Brush("#202127");
        _keyBox.Foreground = Brush("#E8E8EC");
        _keyBox.BorderBrush = Brush("#373841");
        root.Children.Add(_keyBox);
        root.Children.Add(new TextBlock { Text = "Anahtar GitHub'a gönderilmez. Bu bilgisayardaki Windows kullanıcı ortam değişkenine kaydedilir.", TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = Brush("#92939F"), Margin = new Thickness(0, 9, 0, 14) });
        _status.Text = "Bağlantı testi bekleniyor.";
        _status.TextWrapping = TextWrapping.Wrap;
        _status.Margin = new Thickness(0, 0, 0, 14);
        _status.Foreground = Brush("#C9A56C");
        root.Children.Add(_status);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        _testButton.Content = "Bağlantıyı test et ve kaydet";
        _testButton.Padding = new Thickness(16, 10, 16, 10);
        _testButton.Background = Brush("#E7A94B");
        _testButton.Foreground = Brush("#17130D");
        _testButton.BorderThickness = new Thickness(0);
        _testButton.FontWeight = FontWeights.SemiBold;
        _testButton.Click += TestAndSave_Click;
        buttons.Children.Add(_testButton);
        var cancel = new Button { Content = "Kapat", Padding = new Thickness(16, 10, 16, 10), Margin = new Thickness(8, 0, 0, 0), Background = Brush("#303139"), Foreground = Brush("#E8E8EC"), BorderThickness = new Thickness(0) };
        cancel.Click += (_, _) => Close();
        buttons.Children.Add(cancel);
        root.Children.Add(buttons);
        Content = root;
    }

    private async void TestAndSave_Click(object sender, RoutedEventArgs e)
    {
        string key = _keyBox.Password.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            _status.Text = "API anahtarını girmen gerekiyor.";
            return;
        }

        _testButton.IsEnabled = false;
        _testButton.Content = "Bağlantı deneniyor…";
        _status.Foreground = Brush("#C9A56C");
        _status.Text = "OpenAI API'ye test isteği gönderiliyor…";
        try
        {
            await OpenAiChatService.TestConnectionAsync(key);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", key, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", key, EnvironmentVariableTarget.Process);
            ValidatedApiKey = key;
            _status.Foreground = Brush("#74C484");
            _status.Text = "Bağlantı başarılı! API anahtarı kaydedildi. JARVIS sohbeti kullanıma hazır.";
            DialogResult = true;
        }
        catch (Exception ex)
        {
            _status.Foreground = Brush("#F08080");
            _status.Text = "Bağlantı kurulamadı: " + ex.Message + "\nAnahtarın doğru olduğunu, API projesinin aktif olduğunu ve bakiyeni kontrol et.";
        }
        finally
        {
            _testButton.IsEnabled = true;
            _testButton.Content = "Bağlantıyı test et ve kaydet";
        }
    }

    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
}