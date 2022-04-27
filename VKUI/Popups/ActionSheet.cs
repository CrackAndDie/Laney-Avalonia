﻿using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Metadata;
using VKUI.Controls;
using System.Collections;
using System.Collections.Generic;

namespace VKUI.Popups {
    public sealed class ActionSheet : FlyoutBase {
        public ActionSheet() {
            _items = new List<ActionSheetItem>();
        }

        private List<ActionSheetItem> _items;

        public List<ActionSheetItem> Items {
            get => _items;
        }

        public bool CloseAfterClick { get; set; } = true;

        protected override Control CreatePresenter() {
            StackPanel items = new StackPanel { 
                Margin = new Thickness(0, 8, 0, 8),
            };

            foreach (ActionSheetItem item in _items) {
                if (item.Before == null && item.Header == null) { // Экстравагатным образом добавляем сепаратор
                    Rectangle separator = new Rectangle();
                    separator.Classes.Add("Separator");
                    items.Children.Add(separator);
                    continue;
                }
                item.Click += Item_Click;
                items.Children.Add(item);
            }

            return new VKUIFlyoutPresenter {
                Content = items
            };
        }

        private void Item_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            if (CloseAfterClick) Hide();
        }

        protected override void OnOpened() {
            base.OnOpened();
        }
    }
}