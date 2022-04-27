﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ELOR.Laney.Core;
using ELOR.Laney.Extensions;
using ELOR.Laney.Views.Modals;
using Serilog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ELOR.Laney.Views {
    public sealed partial class SignInWindow : Window {
        public SignInWindow() {
            InitializeComponent();
            Log.Information($"{nameof(SignInWindow)} initialized.");

            VersionInfo.Text = $"Ver. {App.BuildInfo}";
        }

        private async void SignIn(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            Button button = sender as Button;
            button.IsEnabled = false;

            var result = await AuthManager.AuthWithTokenAsync(this);
            if (result.Item1 != 0) {
                Settings.SetBatch(new Dictionary<string, object> {
                    { Settings.VK_USER_ID, result.Item1 },
                    { Settings.VK_TOKEN, result.Item2 }
                });
                VKSession.StartUserSession(result.Item1, result.Item2);
                App.Current.DesktopLifetime.MainWindow = VKSession.Main.Window;
                Close();
            } else {
                new FieldTestWindow().Show();
            }

            button.IsEnabled = true;
        }
    }
}