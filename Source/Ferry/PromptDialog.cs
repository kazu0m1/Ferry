using System.Windows;
using System.Windows.Controls;

namespace Ferry
{
    internal sealed class PromptDialog : Window
    {
        private TextBox input;
        public string Value { get { return input.Text; } }
        public PromptDialog(Window owner, string title, string label, string initial) : this(owner, title, label, initial, 0, initial == null ? 0 : initial.Length)
        {
        }

        public PromptDialog(Window owner, string title, string label, string initial, int selectionStart, int selectionLength)
        {
            Owner = owner; Title = title; Width = 460; Height = 160; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Grid root = new Grid { Margin = new Thickness(16) }; root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            TextBlock text = new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 6) }; root.Children.Add(text); Grid.SetRow(text, 0);
            input = new TextBox { Text = initial, Padding = new Thickness(7, 5, 7, 5) }; root.Children.Add(input); Grid.SetRow(input, 1);
            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            Button cancel = new Button { Content = "Cancel", MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) }; cancel.Click += delegate { DialogResult = false; };
            Button ok = new Button { Content = "OK", MinWidth = 80, IsDefault = true }; ok.Click += delegate { DialogResult = true; };
            buttons.Children.Add(cancel); buttons.Children.Add(ok); root.Children.Add(buttons); Grid.SetRow(buttons, 2); Content = root;
            Loaded += delegate { input.Focus(); int start = selectionStart; int length = selectionLength; if (start < 0) start = 0; if (start > input.Text.Length) start = input.Text.Length; if (length < 0) length = 0; if (start + length > input.Text.Length) length = input.Text.Length - start; input.Select(start, length); };
        }
    }
}
