using Avalonia;
using Avalonia.Controls.Primitives;
using ELOR.Laney.ViewModels.Controls;

namespace ELOR.Laney.Controls {
    public class ChatViewItem : TemplatedControl {
        #region Properties

        public static readonly StyledProperty<MessageViewModel> MessageProperty =
            AvaloniaProperty.Register<ChatViewItem, MessageViewModel>(nameof(Message));

        public MessageViewModel Message {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        #endregion

        #region Template elements

        #endregion

        public ChatViewItem() {
            PointerPressed += ChatViewItem_PointerPressed;
            Unloaded += ChatViewItem_Unloaded;
        }

        // Необходимо для того, чтобы при ПКМ не пробрасывалось
        // событие нажатия к ListBox и не выделялось сообщение.
        private void ChatViewItem_PointerPressed(object sender, Avalonia.Input.PointerPressedEventArgs e) {
            bool isRight = !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
            if (isRight) e.Handled = true;
        }

        private void ChatViewItem_Unloaded(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
            PointerPressed -= ChatViewItem_PointerPressed;
            Unloaded -= ChatViewItem_Unloaded;
        }
    }
}