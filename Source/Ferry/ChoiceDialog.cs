using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ferry
{
    internal static partial class ChoiceDialog
    {
        public static ChoiceDialogResult ShowYesNo(
            Window owner,
            string title,
            string message,
            ChoiceDialogResult defaultChoice)
        {
            return Show(owner, title, message, false, defaultChoice);
        }

        public static ChoiceDialogResult ShowYesNoCancel(
            Window owner,
            string title,
            string message,
            ChoiceDialogResult defaultChoice)
        {
            return Show(owner, title, message, true, defaultChoice);
        }

        private static ChoiceDialogResult Show(
            Window owner,
            string title,
            string message,
            bool includeCancel,
            ChoiceDialogResult defaultChoice)
        {
            ChoiceDialogResult result = includeCancel
                ? ChoiceDialogResult.Cancel
                : ChoiceDialogResult.No;

            var window = new Window
            {
                Title = title,
                Owner = owner,
                WindowStartupLocation = owner != null
                    ? WindowStartupLocation.CenterOwner
                    : WindowStartupLocation.CenterScreen,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 460,
                MaxWidth = 620,
                MaxHeight = 720,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Background = SystemColors.WindowBrush
            };

            var root = new Grid { Margin = new Thickness(22) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var text = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = SystemColors.WindowTextBrush,
                MaxWidth = 560
            };
            Grid.SetRow(text, 0);
            root.Children.Add(text);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 22, 0, 0)
            };
            Grid.SetRow(buttons, 1);
            root.Children.Add(buttons);

            Button yesButton = CreateButton("YES");
            Button noButton = CreateButton("NO");
            Button cancelButton = includeCancel ? CreateButton("CANCEL") : null;

            yesButton.IsDefault = defaultChoice == ChoiceDialogResult.Yes;
            noButton.IsDefault = defaultChoice == ChoiceDialogResult.No;
            if (cancelButton != null)
                cancelButton.IsDefault = defaultChoice == ChoiceDialogResult.Cancel;

            if (includeCancel)
                cancelButton.IsCancel = true;
            else
                noButton.IsCancel = true;

            yesButton.Click += delegate
            {
                result = ChoiceDialogResult.Yes;
                window.DialogResult = true;
            };
            noButton.Click += delegate
            {
                result = ChoiceDialogResult.No;
                window.DialogResult = true;
            };
            if (cancelButton != null)
            {
                cancelButton.Click += delegate
                {
                    result = ChoiceDialogResult.Cancel;
                    window.DialogResult = true;
                };
            }

            buttons.Children.Add(yesButton);
            buttons.Children.Add(noButton);
            if (cancelButton != null)
                buttons.Children.Add(cancelButton);

            window.Content = root;
            window.ShowDialog();
            return result;
        }

        private static Button CreateButton(string label)
        {
            return new Button
            {
                Content = label,
                MinWidth = 92,
                Height = 30,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(12, 2, 12, 2)
            };
        }
    }
}
