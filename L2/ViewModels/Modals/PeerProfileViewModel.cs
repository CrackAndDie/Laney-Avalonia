﻿using Avalonia.Controls;
using ELOR.Laney.Core;
using ELOR.Laney.Core.Localization;
using ELOR.Laney.DataModels;
using ELOR.Laney.Execute;
using ELOR.Laney.Execute.Objects;
using ELOR.Laney.Helpers;
using ELOR.VKAPILib.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VKUI.Controls;
using VKUI.Popups;

namespace ELOR.Laney.ViewModels.Modals {
    public class PeerProfileViewModel : CommonViewModel {

        private int _id;
        private string _header;
        private string _subhead;
        private Uri _avatar;
        private Command _firstCommand;
        private Command _secondCommand;
        private Command _thirdCommand;
        private Command _moreCommand;

        public int Id { get { return _id; } private set { _id = value; OnPropertyChanged(); } }
        public string Header { get { return _header; } private set { _header = value; OnPropertyChanged(); } }
        public string Subhead { get { return _subhead; } private set { _subhead = value; OnPropertyChanged(); } }
        public Uri Avatar { get { return _avatar; } private set { _avatar = value; OnPropertyChanged(); } }
        public Command FirstCommand { get { return _firstCommand; } private set { _firstCommand = value; OnPropertyChanged(); } }
        public Command SecondCommand { get { return _secondCommand; } private set { _secondCommand = value; OnPropertyChanged(); } }
        public Command ThirdCommand { get { return _thirdCommand; } private set { _thirdCommand = value; OnPropertyChanged(); } }
        public Command MoreCommand { get { return _moreCommand; } private set { _moreCommand = value; OnPropertyChanged(); } }

        private VKSession session;

        public PeerProfileViewModel(VKSession session, int peerId) {
            this.session = session;
            Id = peerId;
            if (peerId > 2000000000) {
                GetChat(peerId);
            } else if (peerId > 0 && peerId < 1900000000) {
                GetUser(peerId);
            } else if (peerId < 0) {
                GetGroup(peerId * -1);
            }
        }

        #region User-specific

        private async void GetUser(int userId) {
            if (IsLoading) return;
            IsLoading = true;
            Placeholder = null;
            try {
                UserEx user = await session.API.GetUserCardAsync(userId);
                Header = String.Join(" ", new string[2] { user.FirstName, user.LastName });
                if (user.Photo != null) Avatar = user.Photo;
                Subhead = VKAPIHelper.GetOnlineInfo(user.OnlineInfo, user.Sex).ToLowerInvariant();

                switch (user.Deactivated) {
                    case DeactivationState.Banned: Subhead = Localizer.Instance["user_blocked"]; break;
                    case DeactivationState.Deleted: Subhead = Localizer.Instance["user_deleted"]; break;
                    default: Subhead = VKAPIHelper.GetOnlineInfo(user.OnlineInfo, user.Sex).ToLowerInvariant(); break;
                }

                SetupCommands(user);
            } catch (Exception ex) {
                Placeholder = PlaceholderViewModel.GetForException(ex, (o) => GetUser(userId));
            }
            IsLoading = false;
        }

        private void SetupCommands(UserEx user) {
            FirstCommand = null;
            SecondCommand = null;
            ThirdCommand = null;
            MoreCommand = null;
            List<Command> commands = new List<Command>();
            List<Command> moreCommands = new List<Command>();

            // Если нет истории сообщений с этим юзером,
            // и ему нельзя писать сообщение,
            // или если открыт чат с этим юзером,
            // то не будем добавлять эту кнопку
            if ((user.CanWritePrivateMessage || user.MessagesCount > 0) && session.CurrentOpenedChat?.PeerId != user.Id) {
                Command messageCmd = new Command(VKIconNames.Icon28MessageOutline, Localizer.Instance["message"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                commands.Add(messageCmd);
            }

            // Friend
            if (session.UserId != user.Id && !user.Blacklisted && !user.BlacklistedByMe 
                && user.Deactivated == DeactivationState.No && user.CanSendFriendRequest) {
                string ficon = VKIconNames.Icon28SettingsOutline;
                string flabel = "";

                switch (user.FriendStatus) {
                    case FriendStatus.None:
                        flabel = Localizer.Instance["pp_friend_add"];
                        ficon = VKIconNames.Icon28UserAddOutline;
                        break;
                    case FriendStatus.IsFriend:
                        flabel = Localizer.Instance["pp_friend_your"];
                        ficon = VKIconNames.Icon28UserAddedOutline;
                        break;
                    case FriendStatus.InboundRequest:
                        flabel = Localizer.Instance["pp_friend_accept"];
                        ficon = VKIconNames.Icon28UserIncomingOutline;
                        break;
                    case FriendStatus.RequestSent:
                        flabel = Localizer.Instance["pp_friend_request"];
                        ficon = VKIconNames.Icon28UserOutgoingOutline;
                        break;
                }

                Command friendCmd = new Command(ficon, flabel, false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                commands.Add(friendCmd);
            }

            // Notifications
            string notifIcon = user.NotificationsDisabled ? VKIconNames.Icon28NotificationDisableOutline : VKIconNames.Icon28Notifications;
            Command notifsCmd = new Command(notifIcon, Localizer.Instance["settings_notifications"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
            commands.Add(notifsCmd);

            // Open in browser
            string linkIcon = commands.Count >= 3 ? VKIconNames.Icon20LinkCircleOutline : VKIconNames.Icon28LinkCircleOutline;
            Command openExternalCmd = new Command(linkIcon, Localizer.Instance["pp_profile"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
            commands.Add(openExternalCmd);

            // Ban/unban
            if (session.UserId != user.Id && !user.Blacklisted) {
                string banIcon = user.BlacklistedByMe ? VKIconNames.Icon20UnlockOutline : VKIconNames.Icon20BlockOutline;
                string banLabel = Localizer.Instance[user.BlacklistedByMe ? "unblock" : "block"];
                Command banCmd = new Command(banIcon, banLabel, true, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                moreCommands.Add(banCmd);
            }

            // Clear history
            if (user.MessagesCount > 0) {
                Command clearCmd = new Command(VKIconNames.Icon20DeleteOutline, Localizer.Instance["chat_clear_history"], true, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                moreCommands.Add(clearCmd);
            }

            Command moreCommand = new Command(VKIconNames.Icon28MoreHorizontal, Localizer.Instance["more"], false, (a) => OpenContextMenu(a, commands, moreCommands));

            FirstCommand = commands[0];
            SecondCommand = commands[1];

            if (commands.Count < 3) {
                ThirdCommand = moreCommand;
            } else {
                ThirdCommand = commands[2];
                MoreCommand = moreCommand;
            }
        }

        #endregion

        #region Group-specific

        private async void GetGroup(int groupId) {
            if (IsLoading) return;
            IsLoading = true;
            Placeholder = null;
            try {
                GroupEx group = await session.API.GetGroupCardAsync(groupId);
                Header = group.Name;
                if (group.Photo != null) Avatar = group.Photo;
                Subhead = group.Activity;
                SetupCommands(group);
            } catch (Exception ex) {
                Placeholder = PlaceholderViewModel.GetForException(ex, (o) => GetGroup(groupId));
            }
            IsLoading = false;
        }

        private void SetupCommands(GroupEx group) {
            FirstCommand = null;
            SecondCommand = null;
            ThirdCommand = null;
            MoreCommand = null;
            List<Command> commands = new List<Command>();
            List<Command> moreCommands = new List<Command>();

            if ((group.CanMessage || group.MessagesCount > 0) && session.CurrentOpenedChat.PeerId != -group.Id) {
                Command messageCmd = new Command(VKIconNames.Icon28MessageOutline, Localizer.Instance["message"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                commands.Add(messageCmd);
            }

            // Notifications
            string notifIcon = group.NotificationsDisabled ? VKIconNames.Icon28NotificationDisableOutline : VKIconNames.Icon28Notifications;
            Command notifsCmd = new Command(notifIcon, Localizer.Instance["settings_notifications"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
            commands.Add(notifsCmd);

            // Open in browser
            string linkIcon = commands.Count >= 3 ? VKIconNames.Icon20LinkCircleOutline : VKIconNames.Icon28LinkCircleOutline;
            Command openExternalCmd = new Command(linkIcon, Localizer.Instance["pp_group"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
            commands.Add(openExternalCmd);

            // Allow/deny messages from group

            string banIcon = group.MessagesAllowed ? VKIconNames.Icon20BlockOutline : VKIconNames.Icon20Check;
            string banLabel = Localizer.Instance[group.MessagesAllowed ? "pp_deny" : "pp_allow"];
            Command banCmd = new Command(banIcon, banLabel, group.MessagesAllowed, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
            moreCommands.Add(banCmd);

            // Clear history
            if (group.MessagesCount > 0) {
                Command clearCmd = new Command(VKIconNames.Icon20DeleteOutline, Localizer.Instance["chat_clear_history"], true, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                moreCommands.Add(clearCmd);
            }

            Command moreCommand = new Command(VKIconNames.Icon28MoreHorizontal, Localizer.Instance["more"], false, (a) => OpenContextMenu(a, commands, moreCommands));

            FirstCommand = commands[0];
            SecondCommand = commands[1];

            if (commands.Count < 3) {
                ThirdCommand = moreCommand;
            } else {
                ThirdCommand = commands[2];
                MoreCommand = moreCommand;
            }
        }

        #endregion

        #region Chat-specific

        private async void GetChat(int peerId) {
            if (IsLoading) return;
            IsLoading = true;
            Placeholder = null;
            try {
                ChatInfoEx chat = await session.API.GetChatAsync(peerId - 2000000000, VKAPIHelper.Fields);
                Header = chat.Name;
                if (chat.PhotoUri != null) Avatar = chat.PhotoUri;

                if (chat.State == UserStateInChat.In) {
                    Subhead = String.Empty;
                    if (chat.IsCasperChat) Subhead = $"{Localizer.Instance["casper_chat"].ToLowerInvariant()}, ";
                    Subhead += Localizer.Instance.GetDeclensionFormatted(chat.MembersCount, "members_sub");
                } else {
                    Subhead = Localizer.Instance[chat.State == UserStateInChat.Left ? "chat_left" : "chat_kicked"].ToLowerInvariant();
                }

                SetupCommands(chat);
            } catch (Exception ex) {
                Placeholder = PlaceholderViewModel.GetForException(ex, (o) => GetChat(peerId));
            }
            IsLoading = false;
        }

        private void SetupCommands(ChatInfoEx chat) {
            FirstCommand = null;
            SecondCommand = null;
            ThirdCommand = null;
            MoreCommand = null;
            List<Command> commands = new List<Command>();
            List<Command> moreCommands = new List<Command>();

            // Edit
            if (chat.ACL.CanChangeInfo) {
                Command editCmd = new Command(VKIconNames.Icon28EditOutline, Localizer.Instance["edit"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                commands.Add(editCmd);
            }

            // Add member
            if (chat.ACL.CanInvite) {
                Command addCmd = new Command(VKIconNames.Icon28UserAddOutline, Localizer.Instance["add"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                commands.Add(addCmd);
            }

            // Notifications
            string notifIcon = chat.PushSettings.DisabledForever ? VKIconNames.Icon28NotificationDisableOutline : VKIconNames.Icon28Notifications;
            Command notifsCmd = new Command(VKIconNames.Icon28Notifications, Localizer.Instance["settings_notifications"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
            commands.Add(notifsCmd);

            // Link
            if (chat.ACL.CanSeeInviteLink) {
                string linkIcon = commands.Count >= 3 ? VKIconNames.Icon20LinkCircleOutline : VKIconNames.Icon28LinkCircleOutline;
                Command chatLinkCmd = new Command(linkIcon, Localizer.Instance["link"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                commands.Add(chatLinkCmd);
            }

            // Unpin message
            if (chat.ACL.CanChangePin && chat.PinnedMessage != null) {
                string pinIcon = commands.Count >= 3 ? VKIconNames.Icon20PinSlashOutline : VKIconNames.Icon28DoorArrowRightOutline;
                Command unpinCmd = new Command(pinIcon, Localizer.Instance["pp_unpin_message"], false, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                commands.Add(unpinCmd);
            }

            // Clear history
            Command clearCmd = new Command(VKIconNames.Icon20DeleteOutline, Localizer.Instance["chat_clear_history"], true, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
            moreCommands.Add(clearCmd);

            // Exit or return to chat/channel
            if (chat.State != UserStateInChat.Kicked) {
                string exitLabel = Localizer.Instance[chat.IsChannel ? "pp_exit_channel" : "pp_exit_chat"];
                string returnLabel = Localizer.Instance[chat.IsChannel ? "pp_return_channel" : "pp_return_chat"];
                string icon = chat.State == UserStateInChat.In ? VKIconNames.Icon20DoorArrowRightOutline : VKIconNames.Icon20DoorEnterArrowRightOutline;
                Command exitRetCmd = new Command(icon, chat.State == UserStateInChat.In ? exitLabel : returnLabel, true, (a) => ExceptionHelper.ShowNotImplementedDialogAsync(session.Window));
                moreCommands.Add(exitRetCmd);
            }

            Command moreCommand = new Command(VKIconNames.Icon28MoreHorizontal, Localizer.Instance["more"], false, (a) => OpenContextMenu(a, commands, moreCommands));

            FirstCommand = commands[0];

            if (commands.Count < 2) {
                SecondCommand = moreCommand;
            } else if (commands.Count == 2) {
                SecondCommand = commands[1];
                ThirdCommand = moreCommand;
            } else {
                SecondCommand = commands[1];
                ThirdCommand = commands[2];
                MoreCommand = moreCommand;
            }
        }

        #endregion

        #region General commands

        private void OpenContextMenu(object target, List<Command> commands, List<Command> moreCommands) {
            ActionSheet ash = new ActionSheet();

            if (commands.Count > 3) {
                commands = commands.GetRange(3, commands.Count - 3);
                foreach (var item in CollectionsMarshal.AsSpan(commands)) {
                    ActionSheetItem asi = new ActionSheetItem {
                        Before = new VKIcon {
                            Id = item.IconId
                        },
                        Header = item.Label
                    };
                    asi.Click += (a, b) => item.Action.Execute(asi);
                    if (item.IsDestructive) asi.Classes.Add("Destructive");
                    ash.Items.Add(asi);
                }
            }

            if (ash.Items.Count > 0) ash.Items.Add(new ActionSheetItem());

            foreach (var item in CollectionsMarshal.AsSpan(moreCommands)) {
                ActionSheetItem asi = new ActionSheetItem {
                    Before = new VKIcon {
                        Id = item.IconId
                    },
                    Header = item.Label
                };
                asi.Click += (a, b) => item.Action.Execute(asi);
                if (item.IsDestructive) asi.Classes.Add("Destructive");
                ash.Items.Add(asi);
            }

            ash.ShowAt(target as Control, true);

        }

        #endregion
    }
}