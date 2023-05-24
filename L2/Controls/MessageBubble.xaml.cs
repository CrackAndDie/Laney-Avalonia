using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using ColorTextBlock.Avalonia;
using ELOR.Laney.Controls.Attachments;
using ELOR.Laney.Core;
using ELOR.Laney.Core.Localization;
using ELOR.Laney.Extensions;
using ELOR.Laney.Helpers;
using ELOR.Laney.ViewModels.Controls;
using Serilog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using VKUI.Controls;

namespace ELOR.Laney.Controls {
    public class MessageBubble : TemplatedControl {

        #region Properties

        public static readonly StyledProperty<MessageViewModel> MessageProperty =
            AvaloniaProperty.Register<MessageBubble, MessageViewModel>(nameof(Message));

        public MessageViewModel Message {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        #endregion

        #region Internal

        bool IsOutgoing => Message.IsOutgoing;
        bool IsChat => Message.PeerId > 2000000000;

#if RELEASE
#elif BETA
#else
        public MessageBubble() {
            if (Message == null) {
                Log.Verbose($"> MessageBubble init.");
            } else {
                Log.Verbose($"> MessageBubble init. ({Message.PeerId}_{Message.ConversationMessageId})");
            }
        }
#endif

#endregion

        #region Constants

        const string BACKGROUND_INCOMING = "IncomingMessageBackground";
        const string BACKGROUND_OUTGOING = "OutgoingMessageBackground";
        const string BACKGROUND_GIFT = "GiftMessageBackground";
        const string BACKGROUND_BORDER = "BorderMessageBackground";
        const string BACKGROUND_TRANSPARENT = "TransparentMessageBackground";

        const string INDICATOR_DEFAULT = "DefaultIndicator";
        const string INDICATOR_IMAGE = "ImageIndicator";
        const string INDICATOR_COMPLEX_IMAGE = "ComplexImageIndicator";

        public const double STORY_WIDTH = 124;
        public const double BUBBLE_FIXED_WIDTH = 320;
        public const double STICKER_WIDTH = 168; // 168 в макете figma vk ipad, 176 — в vk ios, 
                                                 // 184 — android, 148 — android with reply

        #endregion

        #region Template elements

        Grid BubbleRoot;
        Border BubbleBackground;
        Button AvatarButton;
        Avatar SenderAvatar;
        Border SenderNameWrap;
        TextBlock SenderName;
        Button ReplyMessageButton;
        GiftUI Gift;
        CTextBlock MessageText;
        AttachmentsContainer MessageAttachments;
        Rectangle Map;
        Border ForwardedMessagesContainer;
        StackPanel ForwardedMessagesStack;
        Border IndicatorContainer;
        TextBlock TimeIndicator;
        VKIcon StateIndicator;
        Ellipse ReadIndicator;

        bool isUILoaded = false;
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
#if RELEASE
#elif BETA
#else
            Log.Verbose($"> MessageBubble OnApplyTemplate exec. ({Message.PeerId}_{Message.ConversationMessageId})");
#endif

            base.OnApplyTemplate(e);
            BubbleRoot = e.NameScope.Find<Grid>(nameof(BubbleRoot));
            BubbleBackground = e.NameScope.Find<Border>(nameof(BubbleBackground));
            AvatarButton = e.NameScope.Find<Button>(nameof(AvatarButton));
            SenderAvatar = e.NameScope.Find<Avatar>(nameof(SenderAvatar));
            SenderNameWrap = e.NameScope.Find<Border>(nameof(SenderNameWrap));
            SenderName = e.NameScope.Find<TextBlock>(nameof(SenderName));
            ReplyMessageButton = e.NameScope.Find<Button>(nameof(ReplyMessageButton));
            Gift = e.NameScope.Find<GiftUI>(nameof(Gift));
            MessageText = e.NameScope.Find<CTextBlock>(nameof(MessageText));
            MessageAttachments = e.NameScope.Find<AttachmentsContainer>(nameof(MessageAttachments));
            Map = e.NameScope.Find<Rectangle>(nameof(Map));
            ForwardedMessagesContainer = e.NameScope.Find<Border>(nameof(ForwardedMessagesContainer));
            ForwardedMessagesStack = e.NameScope.Find<StackPanel>(nameof(ForwardedMessagesStack));
            IndicatorContainer = e.NameScope.Find<Border>(nameof(IndicatorContainer));
            TimeIndicator = e.NameScope.Find<TextBlock>(nameof(TimeIndicator));
            StateIndicator = e.NameScope.Find<VKIcon>(nameof(StateIndicator));
            ReadIndicator = e.NameScope.Find<Ellipse>(nameof(ReadIndicator));

            double mapWidth = BUBBLE_FIXED_WIDTH - 8;
            Map.Width = mapWidth;
            Map.Height = mapWidth / 2;

            AvatarButton.Click += AvatarButton_Click;
            ReplyMessageButton.Click += ReplyMessageButton_Click;

            AvatarButton.PointerPressed += SuppressClickEvent;
            ReplyMessageButton.PointerPressed += SuppressClickEvent;
            MessageAttachments.PointerPressed += SuppressClickEvent;
            Map.PointerPressed += SuppressClickEvent;

            isUILoaded = true;
            RenderElement();

            Unloaded += MessageBubble_Unloaded;
        }

        private void MessageBubble_Unloaded(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
            if (Message != null) {
                Message.PropertyChanged -= Message_PropertyChanged;
                Message.MessageEdited -= Message_MessageEdited;

                Debug.WriteLine($"Message bubble UI for {Message.PeerId}_{Message.ConversationMessageId} is unloaded");
            } else {
                Debug.WriteLine($"Message bubble UI is unloaded");
            }

            AvatarButton.Click -= AvatarButton_Click;
            ReplyMessageButton.Click -= ReplyMessageButton_Click;

            AvatarButton.PointerPressed -= SuppressClickEvent;
            ReplyMessageButton.PointerPressed -= SuppressClickEvent;
            MessageAttachments.PointerPressed -= SuppressClickEvent;
            Map.PointerPressed -= SuppressClickEvent;
            Unloaded -= MessageBubble_Unloaded;

            Message = null;
            BubbleRoot.Children.Clear();
        }

        // Это чтобы событие нажатия не доходили до родителей (особенно к ListBox)
        private void SuppressClickEvent(object sender, Avalonia.Input.PointerPressedEventArgs e) {
            e.Handled = true;
        }

        #endregion

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);

            if (change.Property == MessageProperty) {
                if (change.OldValue is MessageViewModel oldm) {
                    oldm.PropertyChanged -= Message_PropertyChanged;
                    oldm.MessageEdited -= Message_MessageEdited;
                }
                if (Message == null) {
                    IsVisible = false;
                    return;
                }
                IsVisible = true;
                Message.PropertyChanged += Message_PropertyChanged;
                Message.MessageEdited += Message_MessageEdited;
                RenderElement();
            }
        }

        private void Message_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(MessageViewModel.Text):
                    if (isUILoaded && Message.CanShowInUI) {
                        Log.Verbose($">> MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} Message.Text prop changed.");
                        SetText(Message.Text);
                        Log.Verbose($"<< MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} Message.Text prop changed.");
                    }
                    break;
                case nameof(MessageViewModel.State):
                case nameof(MessageViewModel.IsImportant):
                case nameof(MessageViewModel.EditTime):
                case nameof(MessageViewModel.IsSenderNameVisible):
                case nameof(MessageViewModel.IsSenderAvatarVisible):
                    Log.Verbose($">> MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} Message.IsSenderAvatarVisible prop changed.");
                    ChangeUI();
                    Log.Verbose($"<< MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} Message.IsSenderAvatarVisible prop changed.");
                    break;
            }
        }

        private void Message_MessageEdited(object sender, EventArgs e) {
            RenderElement();
        }

        private void RenderElement() {
            if (!isUILoaded || !Message.CanShowInUI) return;

            Log.Verbose($">> MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} rendering...");
            var sw = Stopwatch.StartNew();

            // Outgoing
            BubbleRoot.HorizontalAlignment = IsOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            MessageAttachments.IsOutgoing = IsOutgoing;

            MessageUIType uiType = Message.UIType;
            bool hasReply = Message.ReplyMessage != null;
            bool singleImage = uiType == MessageUIType.SingleImage
                || uiType == MessageUIType.Story
                || uiType == MessageUIType.StoryWithSticker
                || (uiType == MessageUIType.Sticker && !hasReply)
                || (uiType == MessageUIType.Graffiti && !hasReply);

            // Bubble background
            var bbc = BubbleBackground.Classes;
            bbc.Clear();
            if (singleImage) {
                bbc.Add(BACKGROUND_TRANSPARENT);
            //} else if ((uiType == MessageUIType.Sticker || uiType == MessageUIType.Graffiti) && hasReply) {
            //    bbc.Add(BACKGROUND_BORDER);
            } else if (uiType == MessageUIType.Gift) {
                bbc.Add(BACKGROUND_GIFT);
            } else {
                bbc.Add(IsOutgoing ? BACKGROUND_OUTGOING : BACKGROUND_INCOMING);
            }

            // Avatar
            AvatarButton.IsVisible = IsChat && !IsOutgoing;

            // Sender name
            SenderNameWrap.IsVisible = !singleImage;

            // Message bubble width
            if (uiType == MessageUIType.Sticker || uiType == MessageUIType.StoryWithSticker) {
                // при BACKGROUND_BORDER у стикера будет отступ в 8px по сторонам.
                BubbleRoot.Width = hasReply ? STICKER_WIDTH + 16 : STICKER_WIDTH;
            } else if (uiType == MessageUIType.Story) {
                BubbleRoot.Width = STORY_WIDTH;
            } else if (uiType == MessageUIType.Graffiti) {
                // при BACKGROUND_BORDER у граффити будет отступ в 8px по сторонам.
                BubbleRoot.Width = hasReply ? BUBBLE_FIXED_WIDTH : BUBBLE_FIXED_WIDTH - 8;
            } else if (uiType == MessageUIType.Complex) {
                BubbleRoot.Width = BUBBLE_FIXED_WIDTH;
            } else {
                BubbleRoot.Width = Double.NaN;
            }

            // Attachments margin
            double amargin = 0;
            if (!hasReply) {
                if (uiType == MessageUIType.Story || uiType == MessageUIType.Sticker || uiType == MessageUIType.StoryWithSticker) {
                    amargin = -8;
                } else if (uiType == MessageUIType.SingleImage || uiType == MessageUIType.Graffiti) {
                    amargin = -4;
                }
            }
            MessageAttachments.Margin = new Thickness(amargin, 0, amargin, amargin);

            // Attachments
            MessageAttachments.Attachments = Message.Attachments;

            // Forwarded messages
            ForwardedMessagesStack.Children.Clear();
            ForwardedMessagesContainer.IsVisible = Message.ForwardedMessages?.Count > 0;
            var fmcmargin = ForwardedMessagesContainer.Margin;
            var fmcborder = ForwardedMessagesContainer.BorderThickness;
            var fmsmargin = ForwardedMessagesStack.Margin;
            double fmwidth = fmcmargin.Left + fmcmargin.Right + fmcborder.Left + fmsmargin.Left;
            foreach (var message in Message.ForwardedMessages) {
                ForwardedMessagesStack.Children.Add(new PostUI {
                    Width = BubbleRoot.Width - fmwidth,
                    Post = message
                });
            }

            // Gift
            var mtm = MessageText.Margin;
            if (Message.Gift != null) {
                Gift.Gift = Message.Gift;
                Gift.HorizontalAlignment = String.IsNullOrEmpty(Message.Text) ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
                Gift.Margin = new Thickness(4, 4, 4, String.IsNullOrEmpty(Message.Text) ? 12 : 0); 
                Gift.IsVisible = true;
                MessageText.TextAlignment = TextAlignment.Center;
                MessageText.Margin = new Thickness(mtm.Left, mtm.Top, mtm.Right, 12);
            } else {
                Gift.IsVisible = false;
                MessageText.TextAlignment = TextAlignment.Left;
                MessageText.Margin = new Thickness(mtm.Left, mtm.Top, mtm.Right, 8);
            }

            // Text
            SetText(Message.Text);

            // Map
            if (Message.Location != null) {
                var glong = Message.Location.Coordinates.Longitude.ToString().Replace(",", ".");
                var glat = Message.Location.Coordinates.Latitude.ToString().Replace(",", ".");
                var w = Map.Width * App.Current.DPI;
                var h = Map.Height * App.Current.DPI;
                Map.SetImageFillAsync(new Uri($"https://static-maps.yandex.ru/1.x/?ll={glong},{glat}&size={w},{h}&z=12&lang=ru_RU&l=pmap&pt={glong},{glat},vkbkm"), Convert.ToInt32(Map.Width));
            }

            // Time & indicator class
            IndicatorContainer.Classes.RemoveAll(new string[3] { INDICATOR_DEFAULT, INDICATOR_IMAGE, INDICATOR_COMPLEX_IMAGE });
            if (uiType == MessageUIType.StoryWithSticker || uiType == MessageUIType.SingleImage || uiType == MessageUIType.Story) {
                IndicatorContainer.Classes.Add(INDICATOR_IMAGE);
            } else if (uiType == MessageUIType.Sticker || uiType == MessageUIType.Graffiti) {
                IndicatorContainer.Classes.Add(hasReply ? INDICATOR_COMPLEX_IMAGE : INDICATOR_IMAGE);
            } else if (uiType == MessageUIType.Complex &&
                (Message.ImagesCount == Message.Attachments.Count || Message.Location != null) &&
                Message.ForwardedMessages.Count == 0) {
                IndicatorContainer.Classes.Add(INDICATOR_COMPLEX_IMAGE);
            } else {
                IndicatorContainer.Classes.Add(INDICATOR_DEFAULT);
            }

            // UI
            ChangeUI();

            sw.Stop();
            Log.Verbose($"<< MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} rendered. ({sw.ElapsedMilliseconds} ms.)");
            if (sw.ElapsedMilliseconds > ((double)1000 / (double)30)) {
                Log.Warning($"MessageBubble: rendering {Message.PeerId}_{Message.ConversationMessageId} took too long! ({sw.ElapsedMilliseconds} ms.)");
            }
        }

        private void SetText(string text) {
            Log.Verbose($">>> MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} setting text...");
            TextParser.SetText(text, MessageText, OnLinkClicked);

            // Empty space for sent time/status
            if (Message.Attachments.Count == 0 && Message.ForwardedMessages.Count == 0) {
                string editedPlaceholder = Message.EditTime != null ? Localizer.Instance["edited_indicator"] : "";
                string favoritePlaceholder = Message.IsImportant ? "W" : "";
                string outgoingPlaceholder = Message.IsOutgoing ? "WW" : "";
                MessageText.Content.Add(new CRun { 
                    Text = $"{favoritePlaceholder}{editedPlaceholder} 22:22{outgoingPlaceholder}",
                    Foreground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    FontSize = 12
                });
            }
            Log.Verbose($"<<< MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} text rendered.");
        }

        private void OnLinkClicked(string link) {
            Router.LaunchLink(VKSession.GetByDataContext(this), link);
        }

        // Смена некоторых частей UI сообщения, которые не влияют
        // в целом на само облачко.
        // Конечно, можно и через TemplateBinding такие вещи делать,
        // но code-behind лучше.
        private void ChangeUI() {
            if (!isUILoaded || !Message.CanShowInUI) return;

            Log.Verbose($">>> MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} exec ChangeUI...");

            // Avatar visibility
            SenderAvatar.Opacity = Message.IsSenderAvatarVisible ? 1 : 0;

            // Message state
            var state = Message.State;
            ReadIndicator.IsVisible = !IsOutgoing && state == MessageVMState.Unread;
            switch (state) {
                case MessageVMState.Unread:
                    StateIndicator.IsVisible = IsOutgoing;
                    StateIndicator.Width = StateIndicator.Height = 16; // ¯\_(ツ)_/¯
                    StateIndicator.Id = VKIconNames.Icon16CheckOutline;
                    break;
                case MessageVMState.Read:
                    StateIndicator.IsVisible = IsOutgoing;
                    StateIndicator.Width = StateIndicator.Height = 16;
                    StateIndicator.Id = VKIconNames.Icon16CheckDoubleOutline;
                    break;
                case MessageVMState.Loading:
                    StateIndicator.IsVisible = true;
                    StateIndicator.Width = StateIndicator.Height = 12; // ¯\_(ツ)_/¯
                    StateIndicator.Id = VKIconNames.Icon16ClockOutline;
                    break;
                case MessageVMState.Deleted:
                    StateIndicator.IsVisible = true;
                    StateIndicator.Width = StateIndicator.Height = 12;
                    StateIndicator.Id = VKIconNames.Icon16DeleteOutline;
                    break;
            }

            // Time & is edited
            TimeIndicator.Text = Message.SentTime.ToString("H:mm");

            // Reply msg button margin-top
            double replyTopMargin = Message.IsSenderNameVisible ? 6 : 10;
            var rmm = ReplyMessageButton.Margin;
            ReplyMessageButton.Margin = new Thickness(rmm.Left, replyTopMargin, rmm.Right, rmm.Bottom);

            // Text margin-top
            double textTopMargin = Message.IsSenderNameVisible || Message.ReplyMessage != null || Message.Gift != null ? 0 : 8;
            var mtm = MessageText.Margin;
            MessageText.Margin = new Thickness(mtm.Left, textTopMargin, mtm.Right, mtm.Bottom);

            // Attachments margin-top
            double atchTopMargin = 0;

            if (Message.UIType == MessageUIType.Complex && Message.ReplyMessage == null && String.IsNullOrEmpty(Message.Text)) {
                atchTopMargin = Message.ImagesCount > 0 ? 4 : 8;
            }
            var mam = MessageAttachments.Margin;
            MessageAttachments.Margin = new Thickness(mam.Left, atchTopMargin, mam.Right, mam.Bottom);

            // Map margin-top
            double mapTopMargin = Message.IsSenderNameVisible || Message.ReplyMessage != null || 
                !String.IsNullOrEmpty(Message.Text) || Message.Attachments.Count > 0 ? 0 : 4;
            var mapm = Map.Margin;
            Map.Margin = new Thickness(mapm.Left, mapTopMargin, mapm.Right, mapm.Bottom);

            // Forwarded messages margin-top
            double fwdTopMargin = !String.IsNullOrEmpty(Message.Text) || Message.Attachments.Count > 0 ? 0 : 8;
            var fwm = ForwardedMessagesContainer.Margin;
            ForwardedMessagesContainer.Margin = new Thickness(fwm.Left, fwdTopMargin, fwm.Right, fwm.Bottom);

            Log.Verbose($"<<< MessageBubble: {Message.PeerId}_{Message.ConversationMessageId} ChangeUI completed.");
        }

        #region Template events

        private void AvatarButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
            Router.OpenPeerProfile(Message.OwnerSession, Message.SenderId);
        }

        private void ReplyMessageButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
            Message.OwnerSession.CurrentOpenedChat.GoToMessage(Message.ReplyMessage);
        }

        #endregion
    }
}