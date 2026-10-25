using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyG.JARVIS.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MessageInput.Focus();
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        SendMessage();
    }

    private void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter &&
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            return;
        }

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

        AddMessage("SEN", message, isUser: true);
        MessageInput.Clear();

        AddMessage(
            "JARVIS",
            "Mesajını aldım. Yapay zekâ API bağlantısı henüz kurulmadığı için şu anda yanıt üretemiyorum. Sonraki aşamada bu mesajı seçilen modele göndereceğiz.",
            isUser: false);

        MessageInput.Focus();
    }

    private void AddMessage(string sender, string message, bool isUser)
    {
        var container = new StackPanel
        {
            HorizontalAlignment = isUser
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Stretch,
            MaxWidth = isUser ? 520 : double.PositiveInfinity,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var card = new Border
        {
            Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    isUser ? "#302719" : "#18191E")),
            BorderBrush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    isUser ? "#6C512B" : "#303139")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14)
        };

        var content = new StackPanel();

        content.Children.Add(new TextBlock
        {
            Text = sender,
            Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    isUser ? "#E7A94B" : "#C5C5CE")),
            FontWeight = FontWeights.SemiBold,
            FontSize = 10,
            Margin = new Thickness(0, 0, 0, 8)
        });

        content.Children.Add(new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#E8E8EC")),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 21
        });

        card.Child = content;
        container.Children.Add(card);
        ChatMessages.Children.Add(container);

        ChatScrollViewer.ScrollToEnd();
    }

    private void NewChatButton_Click(object sender, RoutedEventArgs e)
    {
        ChatMessages.Children.Clear();
        MessageInput.Clear();

        AddMessage(
            "JARVIS",
            "Yeni oturum hazır. API bağlantısı henüz kurulmadı.",
            isUser: false);

        MessageInput.Focus();
    }
}