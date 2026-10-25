using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyG.JARVIS.App;

public partial class MainWindow : Window
{
    private readonly List<ChatSession> _sessions = [];
    private ChatSession _currentSession = null!;

    public MainWindow()
    {
        InitializeComponent();
        CreateSession();
        MessageInput.Focus();
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

    private void SendButton_Click(object sender, RoutedEventArgs e) => SendMessage();

    private void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            return;

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            SendMessage();
        }
    }

    private void SendMessage()
    {
        string message = MessageInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_currentSession.Messages.Count == 1 && _currentSession.Messages[0].Sender == "JARVIS")
            _currentSession.Title = CreateTitle(message);

        _currentSession.Messages.Add(new ChatMessage("SEN", message, true));
        _currentSession.Messages.Add(new ChatMessage(
            "JARVIS",
            "Mesajın bu oturuma kaydedildi. OpenAI bağlantısını bir sonraki aşamada ekleyeceğiz; şu anda gerçek bir yapay zekâ yanıtı üretilmiyor.",
            false));

        MessageInput.Clear();
        RenderCurrentSession();
        RenderSessionList();
        MessageInput.Focus();
    }

    private static string CreateTitle(string message)
    {
        const int maxLength = 28;
        return message.Length <= maxLength ? message : message[..maxLength].TrimEnd() + "…";
    }

    private void CreateSession()
    {
        _currentSession = new ChatSession($"Oturum {_sessions.Count + 1}");
        _currentSession.Messages.Add(new ChatMessage(
            "JARVIS",
            "Merhaba. Sohbet arayüzü hazır. Mesajlarını bu oturumda yazabilir ve sol taraftan önceki oturumlarına dönebilirsin. OpenAI bağlantısını daha sonra ekleyeceğiz.",
            false));

        _sessions.Insert(0, _currentSession);
        RenderCurrentSession();
        RenderSessionList();
    }

    private void NewChatButton_Click(object sender, RoutedEventArgs e)
    {
        CreateSession();
        MessageInput.Clear();
        MessageInput.Focus();
    }

    private void SelectSession(ChatSession session)
    {
        _currentSession = session;
        RenderCurrentSession();
        RenderSessionList();
    }

    private void RenderCurrentSession()
    {
        ChatMessages.Children.Clear();
        foreach (ChatMessage message in _currentSession.Messages)
            AddMessage(message.Sender, message.Text, message.IsUser);

        ChatScrollViewer.ScrollToEnd();
    }

    private void RenderSessionList()
    {
        SessionList.Children.Clear();
        foreach (ChatSession session in _sessions)
        {
            var button = new Button
            {
                Content = session.Title,
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

    private sealed class ChatSession(string title)
    {
        public string Title { get; set; } = title;
        public List<ChatMessage> Messages { get; } = [];
    }

    private sealed record ChatMessage(string Sender, string Text, bool IsUser);
}
