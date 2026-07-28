using System.Windows;

namespace SteamManager.Services;

public interface IMessageBoxService
{
    MessageBoxResult Show(string message, string caption, MessageBoxButton button, MessageBoxImage icon);
}

public class MessageBoxService : IMessageBoxService
{
    public MessageBoxResult Show(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        return System.Windows.MessageBox.Show(message, caption, button, icon);
    }
}
