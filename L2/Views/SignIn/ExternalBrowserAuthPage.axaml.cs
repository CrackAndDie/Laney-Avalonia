using Avalonia.Controls;
using Avalonia.Interactivity;
using ELOR.Laney.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using VKUI.Controls;

namespace ELOR.Laney.Views.SignIn {
    public partial class ExternalBrowserAuthPage : Page {
        public ExternalBrowserAuthPage() {
            InitializeComponent();
            Loaded += ExternalBrowserAuthPage_Loaded;
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        private void ExternalBrowserAuthPage_Loaded(object sender, RoutedEventArgs e) {
            Loaded -= ExternalBrowserAuthPage_Loaded;
            WaitAuthAsync();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) {
            cts.Cancel();
        }

        private async void WaitAuthAsync() {
            string hash = await AuthManager.GetOauthHashAsync();
            if (String.IsNullOrEmpty(hash)) {
                // TODO: display error
                await NavigationRouter.BackAsync();
                return;
            }
            Tuple<long, string> result = new Tuple<long, string>(0, String.Empty);
            result = await AuthManager.AuthViaExternalBrowserAsync(cts, hash);

            if (result.Item1 != 0) {
                Window window = TopLevel.GetTopLevel(this) as Window;
                window.Show();
                window.Activate();
                Settings.SetBatch(new Dictionary<string, object> {
                    { Settings.VK_USER_ID, result.Item1 },
                    { Settings.VK_TOKEN, result.Item2 }
                });
                VKSession.StartUserSession(result.Item1, result.Item2);
                App.Current.DesktopLifetime.MainWindow = VKSession.Main.Window;
                window.Close();
            } else {
                await NavigationRouter.BackAsync();
            }
        }
    }
}