using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyG.JARVIS.App.Services;

namespace MyG.JARVIS.App;

public partial class MainWindow : Window
{
    private readonly List<ChatSession> _sessions = [];
    private readonly string _historyPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyGTech", "JARVIS", "chats.json");
    private OpenAiChatService _openAi = new();
    private ChatSession _currentSession = null!;
    private bool _isSending;

    public MainWindow()
    {
        InitializeComponent();
        ApiStatusText.Text = _openAi.IsConfigured ? "OPENAI HAZIR" : "API ANAHTARI GEREKLİ";
        ApiStatusText.Foreground = new SolidColorBrush(
            _openAi.IsConfigured ? Color.FromRgb(116, 196, 132) : Color.FromRgb(201, 165, 108));
        LoadSessions();

        if (_sessions.Count == 0)
            CreateSession();

        _currentSession = _sessions[0];
        RenderCurrentSession();
        RenderSessionList();
        MessageInput.Focus();
    }

    private async void ApiSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = new ApiSettingsWindow { Owner = this };
        settings.ShowDialog();
        if (settings.ValidatedApiKey is not null)
        {
            _openAi = new OpenAiChatService();
            ApiStatusText.Text = "OPENAI HAZIR";
            ApiStatusText.Foreground = new SolidColorBrush(Color.FromRgb(116, 196, 132));
            MessageBox.Show("OpenAI bağlantısı test edildi ve anahtar kaydedildi. Artık sohbet ekranından mesaj gönderebilirsin.",
                "MyG JARVIS", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Önceki boyuta dön" : "Tam ekrana büyüt";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            return;

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        if (_isSending)
            return;

        string message = MessageInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_currentSession.Messages.Count == 0)
            _currentSession.Title = CreateTitle(message);

        _currentSession.Messages.Add(new ChatMessage
        {
            Sender = "SEN",
            Text = message,
            IsUser = true
        });

        MessageInput.Clear();
        SaveSessions();
        _isSending = true;
        SendMessageButton.IsEnabled = false;
        SendMessageButton.Content = "Yanıtlıyor…";
        RenderCurrentSession();
        RenderSessionList();
        MessageInput.Focus();

        try
        {
            var conversation = _currentSession.Messages
                .Where(m => m.Sender is "SEN" or "JARVIS")
                .Select(m => (Role: m.IsUser ? "user" : "assistant", Text: m.Text))
                .ToList();

            string answer = await _openAi.SendAsync(conversation);

            _currentSession.Messages.Add(new ChatMessage
            {
                Sender = "JARVIS",
                Text = answer,
                IsUser = false
            });

            SaveSessions();
            RenderCurrentSession();
        }
        catch (Exception ex)
        {
            AddMessage("BAĞLANTI HATASI", GetFriendlyError(ex), false);
            ChatScrollViewer.ScrollToEnd();
        }
        finally
        {
            _isSending = false;
            SendMessageButton.IsEnabled = true;
            SendMessageButton.Content = "Gönder";
            MessageInput.Focus();
        }
    }

    private static string GetFriendlyError(Exception ex)
    {
        if (ex is InvalidOperationException &&
            ex.Message.Contains("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            return "API anahtarı yapılandırılmamış. Windows kullanıcı ortam değişkenlerine OPENAI_API_KEY ekle, ardından uygulamayı kapatıp yeniden aç. Anahtarını kaynak koduna veya GitHub'a yazma.";
        }

        if (ex is TaskCanceledException)
            return "İstek zaman aşımına uğradı. İnternet bağlantısını kontrol edip tekrar dene.";

        return ex.Message;
    }

    private static string CreateTitle(string message)
    {
        const int maxLength = 28;
        return message.Length <= maxLength ? message : message[..maxLength].TrimEnd() + "…";
    }

    private void CreateSession()
    {
        var session = new ChatSession
        {
            Title = $"Yeni sohbet {_sessions.Count + 1}"
        };
        _sessions.Insert(0, session);
        _currentSession = session;
        SaveSessions();
        RenderCurrentSession();
        RenderSessionList();
    }

    private void NewChatButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSending)
            return;

        CreateSession();
        MessageInput.Clear();
        MessageInput.Focus();
    }

    private void SelectSession(ChatSession session)
    {
        if (_isSending)
            return;

        _currentSession = session;
        RenderCurrentSession();
        RenderSessionList();
    }

    private void RenderCurrentSession()
    {
        ChatMessages.Children.Clear();

        if (_currentSession.Messages.Count == 0)
        {
            ChatMessages.Children.Add(new TextBlock
            {
                Text = "Yeni sohbet hazır. Mesajını yazıp gönderebilirsin.",
                Foreground = new SolidColorBrush(Color.FromRgb(146, 147, 159)),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22,
                Margin = new Thickness(4, 8, 4, 18)
            });
        }
        else
        {
            foreach (ChatMessage message in _currentSession.Messages)
                AddMessage(message.Sender, message.Text, message.IsUser);
        }

        ChatScrollViewer.ScrollToEnd();
    }

    private void RenderSessionList()
    {
        SessionList.Children.Clear();
        foreach (ChatSession session in _sessions)
        {
            var button = new Button
            {
                Content = string.IsNullOrWhiteSpace(session.Title) ? "Yeni sohbet" : session.Title,
                Tag = session,
                Style = (Style)FindResource("NavButton"),
                FontSize = 12,
                Padding = new Thickness(10, 9, 8, 9),
                Margin = new Thickness(0, 2, 0, 2),
                Background = ReferenceEquals(session, _currentSession)
                    ? new SolidColorBrush(Color.FromRgb(41, 42, 48))
                    : Brushes.Transparent,
                Foreground = ReferenceEquals(session, _currentSession)
                    ? new SolidColorBrush(Color.FromRgb(231, 169, 75))
                    : new SolidColorBrush(Color.FromRgb(196, 196, 205)),
                ToolTip = session.Title
            };
            button.Click += (_, _) => SelectSession(session);
            SessionList.Children.Add(button);
        }
    }

    private void AddMessage(string sender, string message, bool isUser)
    {
        var container = new StackPanel
        {
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Stretch,
            MaxWidth = isUser ? 520 : double.PositiveInfinity,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var card = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isUser ? "#302719" : "#18191E")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isUser ? "#6C512B" : "#303139")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14)
        };

        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = sender,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isUser ? "#E7A94B" : "#C5C5CE")),
            FontWeight = FontWeights.SemiBold,
            FontSize = 10,
            Margin = new Thickness(0, 0, 0, 8)
        });
        content.Children.Add(new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E8EC")),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 21
        });

        card.Child = content;
        container.Children.Add(card);
        ChatMessages.Children.Add(container);
    }

    private void LoadSessions()
    {
        try
        {
            if (!File.Exists(_historyPath))
                return;

            string json = File.ReadAllText(_historyPath);
            var loaded = JsonSerializer.Deserialize<List<ChatSession>>(json);
            if (loaded is not null)
                _sessions.AddRange(loaded.Where(s => s is not null));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            MessageBox.Show(
                $"Sohbet geçmişi okunamadı. Yeni bir oturumla devam edilecek.\n\n{ex.Message}",
                "MyG JARVIS",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void SaveSessions()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_historyPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(_sessions, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_historyPath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"Sohbet geçmişi diske kaydedilemedi.\n\n{ex.Message}",
                "MyG JARVIS",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private sealed class ChatSession
    {
        public ChatSession() { }
        public string Title { get; set; } = "Yeni sohbet";
        public List<ChatMessage> Messages { get; set; } = [];
    }

    private sealed class ChatMessage
    {
        public ChatMessage() { }
        public string Sender { get; set; } = "";
        public string Text { get; set; } = "";
        public bool IsUser { get; set; }
    }
}
