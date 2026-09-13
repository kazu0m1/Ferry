using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ferry
{
    internal enum ChoiceDialogResult
    {
        Yes,
        No,
        Cancel
    }

    internal static class ChoiceDialog
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

        public static ArchiveConflictResolution ShowArchiveConflict(
            Window owner,
            ArchiveOverwriteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            var result = new ArchiveConflictResolution
            {
                Decision = ArchiveOverwriteDecision.Cancel,
                ApplyToRemainingFileConflictsUnderMergedFolder = false
            };

            bool sameKind = request.ArchiveItemIsDirectory == request.ExistingItemIsDirectory;
            bool primaryAvailable = sameKind && !request.ExistingItemIsReparsePoint;
            string archiveKind = request.ArchiveItemIsDirectory ? "folder" : "file";
            string existingKind = request.ExistingItemIsDirectory ? "folder" : "file";
            string primaryLabel = request.ArchiveItemIsDirectory ? "MERGE" : "REPLACE";

            var window = new Window
            {
                Title = request.ArchiveItemIsDirectory ? "Folder already exists" : "File already exists",
                Owner = owner,
                WindowStartupLocation = owner != null
                    ? WindowStartupLocation.CenterOwner
                    : WindowStartupLocation.CenterScreen,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 560,
                MaxWidth = 760,
                MaxHeight = 820,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Background = SystemColors.WindowBrush
            };

            var root = new Grid { Margin = new Thickness(22) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            bool canRememberMergedFolderFileChoice =
                !request.ArchiveItemIsDirectory &&
                !String.IsNullOrEmpty(request.MergedFolderScopePath);
            if (canRememberMergedFolderFileChoice)
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var builder = new System.Text.StringBuilder();
            if (sameKind)
            {
                builder.Append("A ").Append(archiveKind)
                    .AppendLine(" with the same name already exists at the extraction destination.");
            }
            else
            {
                builder.Append("The ZIP contains a ").Append(archiveKind)
                    .Append(", but a ").Append(existingKind)
                    .AppendLine(" with the same name already exists at the extraction destination.");
            }

            builder.AppendLine();
            builder.AppendLine(request.TargetPath);
            builder.AppendLine();

            if (primaryAvailable)
            {
                if (request.ArchiveItemIsDirectory)
                    builder.AppendLine("MERGE = use the existing folder and extract into it");
                else
                    builder.AppendLine("REPLACE = replace the existing file");
            }
            else if (request.ExistingItemIsReparsePoint)
            {
                builder.AppendLine("Replace/Merge is unavailable because the existing item is a link or reparse point.");
            }
            else
            {
                builder.Append("Replace/Merge is unavailable because the ZIP ").Append(archiveKind)
                    .Append(" conflicts with an existing ").Append(existingKind).AppendLine(".");
            }

            if (!String.IsNullOrEmpty(request.SuggestedKeepBothPath))
                builder.AppendLine("KEEP BOTH = extract separately as " + Path.GetFileName(request.SuggestedKeepBothPath));
            else
                builder.AppendLine("KEEP BOTH = extract separately with an automatically renamed item");

            builder.AppendLine(request.ArchiveItemIsDirectory
                ? "SKIP = do not extract this folder or anything inside it"
                : "SKIP = do not extract this file");
            builder.Append("CANCEL = stop extraction");

            var text = new TextBlock
            {
                Text = builder.ToString(),
                TextWrapping = TextWrapping.Wrap,
                Foreground = SystemColors.WindowTextBrush,
                MaxWidth = 690
            };
            Grid.SetRow(text, 0);
            root.Children.Add(text);

            CheckBox applyUnderMergedFolder = null;
            int buttonRow = 1;

            if (canRememberMergedFolderFileChoice)
            {
                var rememberPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 18, 0, 0)
                };

                applyUnderMergedFolder = new CheckBox
                {
                    Content = "Apply this choice to all remaining file conflicts under this merged folder",
                    IsChecked = false
                };
                rememberPanel.Children.Add(applyUnderMergedFolder);

                rememberPanel.Children.Add(new TextBlock
                {
                    Text = request.MergedFolderScopePath,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = SystemColors.GrayTextBrush,
                    Margin = new Thickness(22, 5, 0, 0),
                    MaxWidth = 660
                });

                Grid.SetRow(rememberPanel, 1);
                root.Children.Add(rememberPanel);
                buttonRow = 2;
            }

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 22, 0, 0)
            };
            Grid.SetRow(buttons, buttonRow);
            root.Children.Add(buttons);

            Button primaryButton = primaryAvailable ? CreateButton(primaryLabel) : null;
            Button keepBothButton = CreateButton("KEEP BOTH");
            Button skipButton = CreateButton("SKIP");
            Button cancelButton = CreateButton("CANCEL");

            keepBothButton.IsDefault = true;
            cancelButton.IsCancel = true;

            if (primaryButton != null)
            {
                primaryButton.Click += delegate
                {
                    result.Decision = ArchiveOverwriteDecision.Overwrite;
                    result.ApplyToRemainingFileConflictsUnderMergedFolder =
                        applyUnderMergedFolder != null && applyUnderMergedFolder.IsChecked == true;
                    window.DialogResult = true;
                };
                buttons.Children.Add(primaryButton);
            }

            keepBothButton.Click += delegate
            {
                result.Decision = ArchiveOverwriteDecision.KeepBoth;
                result.ApplyToRemainingFileConflictsUnderMergedFolder =
                    applyUnderMergedFolder != null && applyUnderMergedFolder.IsChecked == true;
                window.DialogResult = true;
            };
            skipButton.Click += delegate
            {
                result.Decision = ArchiveOverwriteDecision.Skip;
                result.ApplyToRemainingFileConflictsUnderMergedFolder =
                    applyUnderMergedFolder != null && applyUnderMergedFolder.IsChecked == true;
                window.DialogResult = true;
            };
            cancelButton.Click += delegate
            {
                result.Decision = ArchiveOverwriteDecision.Cancel;
                result.ApplyToRemainingFileConflictsUnderMergedFolder = false;
                window.DialogResult = true;
            };

            buttons.Children.Add(keepBothButton);
            buttons.Children.Add(skipButton);
            buttons.Children.Add(cancelButton);

            window.Content = root;
            window.ShowDialog();
            return result;
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
