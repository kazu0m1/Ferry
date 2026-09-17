using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Ferry
{
    internal sealed partial class MainWindow : Window
    {
        private async void CompressSelected(TabState state, IList<string> paths)
        {
            if (state == null || state.IsRecycleBin || paths == null || paths.Count == 0) return;
            if (archiveOperationActive)
            {
                ShowTimedStatusMessage("An archive operation is already in progress.", 2500);
                return;
            }

            InvalidateRenameUndo();
            List<string> copy = new List<string>();
            for (int i = 0; i < paths.Count; i++) copy.Add(paths[i]);

            string initialDestination;
            try { initialDestination = ArchiveHelper.SuggestZipPath(copy, state.CurrentPath); }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CreateZipSetupWindow setup = new CreateZipSetupWindow(this, copy, initialDestination);
            if (setup.ShowDialog() != true) return;

            string destination = setup.DestinationZipPath;
            bool overwrite = false;
            if (File.Exists(destination))
            {
                ChoiceDialogResult replace = ChoiceDialog.ShowYesNo(
                    this,
                    "Replace ZIP?",
                    "The destination ZIP already exists. Replace it?\n\n" + destination,
                    ChoiceDialogResult.No);
                if (replace != ChoiceDialogResult.Yes) return;
                overwrite = true;
            }

            BeginArchiveOperation("Compressing ZIP");
            try
            {
                Progress<ArchiveProgressInfo> progress = new Progress<ArchiveProgressInfo>(UpdateArchiveProgress);
                ArchiveOperationResult result = await archiveService.CreateZipAsync(
                    copy,
                    destination,
                    overwrite,
                    progress,
                    archiveCancellation.Token);

                string completionMessage = result.Status == ArchiveOperationStatus.Completed
                    ? "ZIP compression complete."
                    : "ZIP compression cancelled.";
                EndArchiveOperation(completionMessage);

                if (contexts.ContainsKey(state.Id) && !state.IsSearching) ScheduleFolderRefresh(state);
                else UpdateStatus();
            }
            catch (Exception ex)
            {
                EndArchiveOperation(null);
                MessageBox.Show(this, ex.Message, "Compression failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExtractZip(TabState state, string zipPath, bool namedFolder)
        {
            if (state == null || state.IsRecycleBin || string.IsNullOrEmpty(zipPath)) return;
            if (archiveOperationActive)
            {
                ShowTimedStatusMessage("An archive operation is already in progress.", 2500);
                return;
            }

            InvalidateRenameUndo();
            string initialDestination;
            try { initialDestination = ArchiveHelper.SuggestExtractionDirectory(zipPath, namedFolder); }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ferry", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExtractZipSetupWindow setup = new ExtractZipSetupWindow(this, zipPath, initialDestination);
            if (setup.ShowDialog() != true) return;

            BeginArchiveOperation("Extracting ZIP");
            try
            {
                Progress<ArchiveProgressInfo> progress = new Progress<ArchiveProgressInfo>(UpdateArchiveProgress);
                ArchiveOperationResult result = await archiveService.ExtractZipAsync(
                    setup.SourceZipPath,
                    setup.DestinationDirectory,
                    archiveThresholds,
                    ConfirmArchiveSafetyRiskAsync,
                    ConfirmArchiveConflictAsync,
                    progress,
                    archiveCancellation.Token);

                string completionMessage = "ZIP extraction complete.";
                if (result.Status == ArchiveOperationStatus.Cancelled)
                {
                    completionMessage = result.CompletedFiles > 0
                        ? "ZIP extraction cancelled (partial result kept)."
                        : "ZIP extraction cancelled.";
                    if (result.CompletedFiles > 0)
                    {
                        MessageBox.Show(
                            this,
                            "Extraction was cancelled. " + result.CompletedFiles.ToString("N0") + " completed file(s) remain in the destination.",
                            "Extraction cancelled",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else if (result.Status == ArchiveOperationStatus.Partial || result.Status == ArchiveOperationStatus.Failed)
                {
                    completionMessage = result.Status == ArchiveOperationStatus.Partial
                        ? "ZIP extraction stopped (partial result kept)."
                        : "ZIP extraction failed.";
                    string detail = string.IsNullOrEmpty(result.ErrorMessage) ? "Extraction did not complete." : result.ErrorMessage;
                    if (result.Status == ArchiveOperationStatus.Partial)
                        detail = "Extraction stopped after " + result.CompletedFiles.ToString("N0") + " file(s) were completed. Those files remain in the destination.\n\n" + detail;
                    MessageBox.Show(this, detail, "Extraction failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (result.SkippedFiles > 0 || result.KeptBothConflicts > 0)
                {
                    var summaryParts = new List<string>();
                    if (result.KeptBothConflicts > 0)
                        summaryParts.Add(result.KeptBothConflicts.ToString("N0") + " conflict(s) kept separately");
                    if (result.SkippedFiles > 0)
                        summaryParts.Add(result.SkippedFiles.ToString("N0") + " file(s) skipped");
                    completionMessage = "ZIP extraction complete. " + String.Join(", ", summaryParts.ToArray()) + ".";
                }

                EndArchiveOperation(completionMessage);
                if (contexts.ContainsKey(state.Id) && !state.IsSearching) ScheduleFolderRefresh(state);
                else UpdateStatus();
            }
            catch (InvalidDataException ex)
            {
                EndArchiveOperation("ZIP extraction blocked.");
                MessageBox.Show(
                    this,
                    "Ferry blocked this ZIP because its path information is unsafe or invalid.\n\n" + ex.Message,
                    "ZIP blocked",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                EndArchiveOperation(null);
                MessageBox.Show(this, ex.Message, "Extraction failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Task<bool> ConfirmArchiveSafetyRiskAsync(ArchiveSafetyReport report)
        {
            bool continueExtraction = false;
            Action show = delegate
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("This ZIP may require unusually large resources.");
                builder.AppendLine();
                builder.AppendLine("Compressed data: " + FormatArchiveBytes(report.CompressedBytes));
                builder.AppendLine("Expanded data:   " + FormatArchiveBytes(report.ExpandedBytes));
                builder.AppendLine("Files:           " + report.FileCount.ToString("N0"));
                builder.AppendLine("Compression ratio: " + FormatArchiveRatio(report.CompressionRatio));
                builder.AppendLine();
                builder.AppendLine("Triggered warning(s):");
                for (int i = 0; i < report.Issues.Count; i++) builder.AppendLine("  • " + report.Issues[i].Message);
                builder.AppendLine();
                builder.Append("Continue extracting?");

                ChoiceDialogResult answer = ChoiceDialog.ShowYesNo(
                    this,
                    "ZIP resource warning",
                    builder.ToString(),
                    ChoiceDialogResult.No);
                continueExtraction = answer == ChoiceDialogResult.Yes;
            };

            if (Dispatcher.CheckAccess()) show(); else Dispatcher.Invoke(show);
            return Task.FromResult(continueExtraction);
        }

        private Task<ArchiveConflictResolution> ConfirmArchiveConflictAsync(ArchiveOverwriteRequest request)
        {
            ArchiveConflictResolution resolution = null;
            Action show = delegate
            {
                resolution = ChoiceDialog.ShowArchiveConflict(this, request);
            };

            if (Dispatcher.CheckAccess()) show(); else Dispatcher.Invoke(show);
            return Task.FromResult(resolution ?? new ArchiveConflictResolution
            {
                Decision = ArchiveOverwriteDecision.Cancel
            });
        }

        private void BeginArchiveOperation(string label)
        {
            if (archiveCancellation != null)
            {
                try { archiveCancellation.Dispose(); } catch { }
            }
            archiveCancellation = new CancellationTokenSource();
            archiveOperationActive = true;
            archiveOperationLabel = string.IsNullOrEmpty(label) ? "Archive operation" : label;
            archiveProgressInfo = new ArchiveProgressInfo { Phase = ArchivePhase.Scanning, CurrentItem = "Preparing..." };
            UpdateStatus();
        }

        private void CancelArchiveOperation()
        {
            if (!archiveOperationActive || archiveCancellation == null) return;
            if (!archiveCancellation.IsCancellationRequested) archiveCancellation.Cancel();
            if (archiveCancelButton != null) archiveCancelButton.IsEnabled = false;
            if (statusText != null) statusText.Text = "Cancelling archive operation...";
        }

        private void UpdateArchiveProgress(ArchiveProgressInfo info)
        {
            if (!archiveOperationActive || info == null) return;
            archiveProgressInfo = info;
            UpdateStatus();
        }

        private void EndArchiveOperation(string completionMessage)
        {
            archiveOperationActive = false;
            archiveProgressInfo = null;
            archiveOperationLabel = null;
            if (archiveCancellation != null)
            {
                try { archiveCancellation.Dispose(); } catch { }
                archiveCancellation = null;
            }
            if (archiveCancelButton != null)
            {
                archiveCancelButton.Visibility = Visibility.Collapsed;
                archiveCancelButton.IsEnabled = true;
            }
            if (statusProgress != null)
            {
                statusProgress.Visibility = Visibility.Collapsed;
                statusProgress.IsIndeterminate = false;
                statusProgress.Value = 0;
            }
            if (closeAfterArchiveCancellation)
            {
                closeAfterArchiveCancellation = false;
                Dispatcher.BeginInvoke(new Action(Close));
                return;
            }
            if (!string.IsNullOrEmpty(completionMessage))
            {
                ShowTimedStatusMessage(completionMessage, 3500);
                return;
            }
            UpdateStatus();
        }

        private void ShowTimedStatusMessage(string message, int milliseconds)
        {
            transientStatusMessage = message ?? string.Empty;
            transientStatusUntilUtc = DateTime.UtcNow.AddMilliseconds(Math.Max(1, milliseconds));
            if (transientStatusTimer == null)
            {
                transientStatusTimer = new DispatcherTimer(DispatcherPriority.Background);
                transientStatusTimer.Tick += delegate
                {
                    if (DateTime.UtcNow < transientStatusUntilUtc) return;
                    transientStatusTimer.Stop();
                    transientStatusMessage = null;
                    UpdateStatus();
                };
            }
            transientStatusTimer.Stop();
            transientStatusTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, milliseconds));
            transientStatusTimer.Start();
            UpdateStatus();
        }

        private bool ShowTransientStatusMessage()
        {
            if (string.IsNullOrEmpty(transientStatusMessage)) return false;
            if (DateTime.UtcNow >= transientStatusUntilUtc)
            {
                transientStatusMessage = null;
                if (transientStatusTimer != null) transientStatusTimer.Stop();
                return false;
            }
            HideArchiveStatusControls();
            if (statusText != null)
            {
                statusText.Text = transientStatusMessage;
                statusText.ToolTip = transientStatusMessage;
            }
            return true;
        }

        private bool ShowArchiveActivityStatus()
        {
            if (!archiveOperationActive) return false;

            ArchiveProgressInfo info = archiveProgressInfo;
            string label = FormatArchiveProgressStatus(info);
            if (statusText != null)
            {
                statusText.Text = label;
                statusText.ToolTip = label;
            }

            if (statusProgress != null)
            {
                statusProgress.Visibility = Visibility.Visible;
                bool indeterminate = info == null || info.Phase == ArchivePhase.Scanning || info.Phase == ArchivePhase.WaitingForConfirmation;
                statusProgress.IsIndeterminate = indeterminate;
                if (!indeterminate) statusProgress.Value = info == null ? 0 : info.OverallPercent;
            }

            if (archiveCancelButton != null)
            {
                archiveCancelButton.Visibility = Visibility.Visible;
                archiveCancelButton.IsEnabled = archiveCancellation != null && !archiveCancellation.IsCancellationRequested;
            }
            return true;
        }

        private void HideArchiveStatusControls()
        {
            if (statusProgress != null)
            {
                statusProgress.Visibility = Visibility.Collapsed;
                statusProgress.IsIndeterminate = false;
            }
            if (archiveCancelButton != null)
            {
                archiveCancelButton.Visibility = Visibility.Collapsed;
                archiveCancelButton.IsEnabled = true;
            }
        }

        private string FormatArchiveProgressStatus(ArchiveProgressInfo info)
        {
            if (info == null) return (archiveOperationLabel ?? "Archive operation") + "...";

            string phase;
            switch (info.Phase)
            {
                case ArchivePhase.Scanning: phase = "Scanning"; break;
                case ArchivePhase.WaitingForConfirmation: phase = "Waiting for confirmation"; break;
                case ArchivePhase.Compressing: phase = "Compressing"; break;
                case ArchivePhase.Extracting: phase = "Extracting"; break;
                case ArchivePhase.Finalizing: phase = "Finalizing"; break;
                case ArchivePhase.Cancelled: phase = "Cancelling"; break;
                default: phase = archiveOperationLabel ?? "Archive operation"; break;
            }

            StringBuilder builder = new StringBuilder(phase);
            if (!string.IsNullOrEmpty(info.CurrentItem) &&
                info.Phase != ArchivePhase.Scanning &&
                info.Phase != ArchivePhase.WaitingForConfirmation &&
                info.Phase != ArchivePhase.Finalizing)
            {
                builder.Append(" — ");
                builder.Append(info.CurrentItem);
            }
            if (info.TotalBytes > 0)
            {
                builder.Append("  •  ");
                builder.Append(FormatArchiveBytes(info.ProcessedBytes));
                builder.Append(" / ");
                builder.Append(FormatArchiveBytes(info.TotalBytes));
                builder.Append(" (");
                builder.Append(info.OverallPercent.ToString("F0"));
                builder.Append("%)");
            }
            else if (info.TotalFiles > 0)
            {
                builder.Append("  •  Files ");
                builder.Append(info.ProcessedFiles.ToString("N0"));
                builder.Append(" / ");
                builder.Append(info.TotalFiles.ToString("N0"));
            }

            if (info.TotalFiles > 0 && info.TotalBytes > 0)
            {
                builder.Append("  •  Files ");
                builder.Append(info.ProcessedFiles.ToString("N0"));
                builder.Append(" / ");
                builder.Append(info.TotalFiles.ToString("N0"));
            }

            if (info.BytesPerSecond > 0)
            {
                builder.Append("  •  ");
                builder.Append(FormatArchiveBytes((long)info.BytesPerSecond));
                builder.Append("/s");
                if (info.EstimatedRemaining.HasValue)
                {
                    builder.Append("  •  Remaining ");
                    builder.Append(FormatArchiveEta(info.EstimatedRemaining.Value));
                }
            }
            else if (info.Phase == ArchivePhase.Compressing || info.Phase == ArchivePhase.Extracting)
            {
                builder.Append("  •  Remaining calculating...");
            }

            return builder.ToString();
        }

        private static string FormatArchiveBytes(long bytes)
        {
            if (bytes < 0) bytes = 0;
            double value = bytes;
            string[] units = new string[] { "B", "KiB", "MiB", "GiB", "TiB" };
            int unit = 0;
            while (value >= 1024.0 && unit < units.Length - 1)
            {
                value /= 1024.0;
                unit++;
            }
            if (unit == 0) return ((long)value).ToString("N0") + " " + units[unit];
            return value.ToString(value >= 100 ? "F0" : value >= 10 ? "F1" : "F2") + " " + units[unit];
        }

        private static string FormatArchiveEta(TimeSpan value)
        {
            if (value < TimeSpan.Zero) value = TimeSpan.Zero;
            if (value.TotalSeconds < 60) return Math.Ceiling(value.TotalSeconds).ToString("F0") + " sec";
            if (value.TotalMinutes < 60) return Math.Ceiling(value.TotalMinutes).ToString("F0") + " min";
            return ((int)value.TotalHours).ToString() + "h " + value.Minutes.ToString() + "m";
        }

        private static string FormatArchiveRatio(double ratio)
        {
            if (double.IsInfinity(ratio)) return "∞";
            if (double.IsNaN(ratio)) return "n/a";
            return ratio.ToString("N1") + "×";
        }
    }
}
