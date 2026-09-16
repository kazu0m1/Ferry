using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
    }
}
