using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ferry
{
    public sealed class ArchiveService
    {
        private const int BufferSize = 128 * 1024;
        private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

        public async Task<ArchiveOperationResult> CreateZipAsync(
            IEnumerable<string> sourcePaths,
            string destinationZipPath,
            bool overwriteDestination,
            IProgress<ArchiveProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            if (sourcePaths == null)
                throw new ArgumentNullException("sourcePaths");
            if (String.IsNullOrWhiteSpace(destinationZipPath))
                throw new ArgumentException("Destination ZIP path is required.", "destinationZipPath");

            string[] sources = sourcePaths.Where(p => !String.IsNullOrWhiteSpace(p)).ToArray();
            if (sources.Length == 0)
                throw new ArgumentException("At least one source path is required.", "sourcePaths");

            string destinationFullPath = Path.GetFullPath(destinationZipPath);
            Report(progress, new ArchiveProgressInfo { Phase = ArchivePhase.Scanning, CurrentItem = "Scanning source items..." });

            List<CompressionPlanItem> plan;
            try
            {
                plan = await Task.Run(
                    delegate { return BuildCompressionPlan(sources, destinationFullPath, cancellationToken); },
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Report(progress, new ArchiveProgressInfo { Phase = ArchivePhase.Cancelled, CurrentItem = "Compression cancelled during scan." });
                return new ArchiveOperationResult { Status = ArchiveOperationStatus.Cancelled };
            }

            long totalBytes = CheckedSum(plan.Where(p => !p.IsDirectory).Select(p => p.Length));
            int totalFiles = plan.Count(p => !p.IsDirectory);

            if (File.Exists(destinationFullPath) && !overwriteDestination)
                throw new IOException("The destination ZIP already exists: " + destinationFullPath);

            string destinationDirectory = Path.GetDirectoryName(destinationFullPath);
            if (String.IsNullOrEmpty(destinationDirectory))
                destinationDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(destinationDirectory);

            string tempZipPath = CreateSiblingTempPath(destinationFullPath, "compressing");

            try
            {
                ArchiveOperationResult result = await Task.Run(
                    delegate
                    {
                        return CreateZipWorkerAsync(
                            plan,
                            destinationFullPath,
                            tempZipPath,
                            overwriteDestination,
                            totalBytes,
                            totalFiles,
                            progress,
                            cancellationToken);
                    },
                    cancellationToken);

                return result;
            }
            catch (OperationCanceledException)
            {
                TryDeleteFile(tempZipPath);
                Report(progress, new ArchiveProgressInfo { Phase = ArchivePhase.Cancelled, CurrentItem = "Compression cancelled." });
                return new ArchiveOperationResult
                {
                    Status = ArchiveOperationStatus.Cancelled,
                    TotalFiles = totalFiles
                };
            }
            catch
            {
                TryDeleteFile(tempZipPath);
                Report(progress, new ArchiveProgressInfo { Phase = ArchivePhase.Failed, CurrentItem = "Compression failed." });
                throw;
            }
        }

        public async Task<ArchiveOperationResult> ExtractZipAsync(
            string zipPath,
            string destinationDirectory,
            ArchiveThresholds thresholds,
            Func<ArchiveSafetyReport, Task<bool>> confirmRiskAsync,
            Func<ArchiveOverwriteRequest, Task<ArchiveConflictResolution>> confirmOverwriteAsync,
            IProgress<ArchiveProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(zipPath))
                throw new ArgumentException("ZIP path is required.", "zipPath");
            if (String.IsNullOrWhiteSpace(destinationDirectory))
                throw new ArgumentException("Destination directory is required.", "destinationDirectory");
            if (thresholds == null)
                thresholds = new ArchiveThresholds();

            string zipFullPath = Path.GetFullPath(zipPath);
            string destinationRoot = Path.GetFullPath(destinationDirectory);

            Report(progress, new ArchiveProgressInfo { Phase = ArchivePhase.Scanning, CurrentItem = "Scanning ZIP..." });

            ArchiveSafetyReport safety;
            try
            {
                safety = await Task.Run(
                    delegate { return AnalyzeArchive(zipFullPath, destinationRoot, thresholds, cancellationToken); },
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Report(progress, new ArchiveProgressInfo { Phase = ArchivePhase.Cancelled, CurrentItem = "Extraction cancelled during scan." });
                return new ArchiveOperationResult { Status = ArchiveOperationStatus.Cancelled };
            }

            if (safety.RequiresConfirmation)
            {
                Report(progress, new ArchiveProgressInfo
                {
                    Phase = ArchivePhase.WaitingForConfirmation,
                    CurrentItem = "Waiting for safety confirmation...",
                    TotalBytes = safety.ExpandedBytes,
                    TotalFiles = safety.FileCount
                });

                bool continueExtraction = confirmRiskAsync == null || await confirmRiskAsync(safety);
                if (!continueExtraction)
                {
                    Report(progress, new ArchiveProgressInfo { Phase = ArchivePhase.Cancelled, CurrentItem = "Extraction cancelled." });
                    return new ArchiveOperationResult
                    {
                        Status = ArchiveOperationStatus.Cancelled,
                        TotalFiles = safety.FileCount
                    };
                }
            }

            Directory.CreateDirectory(destinationRoot);

            try
            {
                return await Task.Run(
                    delegate
                    {
                        return ExtractZipWorkerAsync(
                            zipFullPath,
                            destinationRoot,
                            safety,
                            confirmOverwriteAsync,
                            progress,
                            cancellationToken);
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Report(progress, new ArchiveProgressInfo { Phase = ArchivePhase.Cancelled, CurrentItem = "Extraction cancelled." });
                return new ArchiveOperationResult
                {
                    Status = ArchiveOperationStatus.Cancelled,
                    TotalFiles = safety.FileCount
                };
            }
        }

        private static async Task<ArchiveOperationResult> CreateZipWorkerAsync(
            IList<CompressionPlanItem> plan,
            string destinationFullPath,
            string tempZipPath,
            bool overwriteDestination,
            long totalBytes,
            int totalFiles,
            IProgress<ArchiveProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            var result = new ArchiveOperationResult { TotalFiles = totalFiles };
            var estimator = new ThroughputEstimator();
            var uiProgress = new ThrottledProgressReporter(progress, 100);
            long processedBytes = 0;
            int processedFiles = 0;
            byte[] buffer = new byte[BufferSize];

            Report(progress, new ArchiveProgressInfo
            {
                Phase = ArchivePhase.Compressing,
                TotalBytes = totalBytes,
                TotalFiles = totalFiles,
                CurrentItem = "Starting compression..."
            });

            try
            {
                using (var output = new FileStream(tempZipPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                using (var archive = new ZipArchive(output, ZipArchiveMode.Create, false))
                {
                    foreach (CompressionPlanItem item in plan)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (item.IsDirectory)
                        {
                            ZipArchiveEntry directoryEntry = archive.CreateEntry(EnsureDirectoryEntryName(item.EntryName));
                            TrySetEntryTime(directoryEntry, item.LastWriteTime);
                            continue;
                        }

                        ZipArchiveEntry entry = archive.CreateEntry(item.EntryName, CompressionLevel.Optimal);
                        TrySetEntryTime(entry, item.LastWriteTime);

                        long currentBytes = 0;
                        using (Stream entryStream = entry.Open())
                        using (var input = new FileStream(item.SourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                        {
                            long openLength = input.Length;
                            if (openLength != item.Length)
                            {
                                totalBytes = CheckedAdd(totalBytes, openLength - item.Length);
                                item.Length = openLength;
                                estimator.Reset(processedBytes);
                            }

                            while (true)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                int read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                                if (read == 0)
                                    break;

                                await entryStream.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                                currentBytes += read;
                                processedBytes += read;
                                if (currentBytes > item.Length)
                                {
                                    long growth = currentBytes - item.Length;
                                    totalBytes = CheckedAdd(totalBytes, growth);
                                    item.Length = currentBytes;
                                    estimator.Reset(processedBytes);
                                }
                                estimator.Add(processedBytes);

                                uiProgress.Report(BuildProgress(
                                    ArchivePhase.Compressing,
                                    item.EntryName,
                                    processedBytes,
                                    totalBytes,
                                    currentBytes,
                                    item.Length,
                                    processedFiles,
                                    totalFiles,
                                    estimator), false);
                            }
                        }

                        if (currentBytes < item.Length)
                        {
                            totalBytes = CheckedAdd(totalBytes, currentBytes - item.Length);
                            item.Length = currentBytes;
                            estimator.Reset(processedBytes);
                        }

                        processedFiles++;
                        result.CompletedFiles = processedFiles;
                        uiProgress.Report(BuildProgress(
                            ArchivePhase.Compressing,
                            item.EntryName,
                            processedBytes,
                            totalBytes,
                            item.Length,
                            item.Length,
                            processedFiles,
                            totalFiles,
                            estimator), true);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                Report(progress, new ArchiveProgressInfo
                {
                    Phase = ArchivePhase.Finalizing,
                    CurrentItem = "Finalizing ZIP...",
                    ProcessedBytes = totalBytes,
                    TotalBytes = totalBytes,
                    ProcessedFiles = totalFiles,
                    TotalFiles = totalFiles
                });

                CommitTempFile(tempZipPath, destinationFullPath, overwriteDestination);

                result.Status = ArchiveOperationStatus.Completed;
                result.CompletedFiles = totalFiles;
                result.CompletedPaths.Add(destinationFullPath);

                Report(progress, new ArchiveProgressInfo
                {
                    Phase = ArchivePhase.Completed,
                    CurrentItem = "Compression completed.",
                    ProcessedBytes = totalBytes,
                    TotalBytes = totalBytes,
                    ProcessedFiles = totalFiles,
                    TotalFiles = totalFiles
                });
                return result;
            }
            catch (OperationCanceledException)
            {
                TryDeleteFile(tempZipPath);
                throw;
            }
            catch (Exception ex)
            {
                TryDeleteFile(tempZipPath);
                result.Status = ArchiveOperationStatus.Failed;
                result.ErrorMessage = ex.Message;
                throw;
            }
        }

        private static async Task<ArchiveOperationResult> ExtractZipWorkerAsync(
            string zipFullPath,
            string destinationRoot,
            ArchiveSafetyReport safety,
            Func<ArchiveOverwriteRequest, Task<ArchiveConflictResolution>> confirmOverwriteAsync,
            IProgress<ArchiveProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            var result = new ArchiveOperationResult { TotalFiles = safety.FileCount };
            var estimator = new ThroughputEstimator();
            var uiProgress = new ThrottledProgressReporter(progress, 100);
            var conflictState = new ExtractionConflictState();
            var directoryMap = new Dictionary<string, string>(PathComparer);
            var skippedDirectoryPrefixes = new HashSet<string>(PathComparer);
            var createdDirectories = new HashSet<string>(PathComparer);
            directoryMap[String.Empty] = destinationRoot;

            long processedBytes = 0;
            int processedFiles = 0;
            int skippedFiles = 0;
            int keptBothConflicts = 0;
            byte[] buffer = new byte[BufferSize];

            Report(progress, new ArchiveProgressInfo
            {
                Phase = ArchivePhase.Extracting,
                CurrentItem = "Starting extraction...",
                TotalBytes = safety.ExpandedBytes,
                TotalFiles = safety.FileCount
            });

            try
            {
                using (var inputFile = new FileStream(zipFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                using (var archive = new ZipArchive(inputFile, ZipArchiveMode.Read, false))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        bool isDirectory = IsDirectoryEntry(entry);

                        // Validate the original ZIP path before any Ferry-side remapping is considered.
                        // This keeps path traversal/absolute-path rules non-overridable.
                        GetSafeTargetPath(destinationRoot, entry.FullName);

                        ExtractionTarget resolved = await ResolveExtractionTargetAsync(
                            destinationRoot,
                            entry.FullName,
                            isDirectory,
                            directoryMap,
                            skippedDirectoryPrefixes,
                            createdDirectories,
                            conflictState,
                            confirmOverwriteAsync,
                            cancellationToken).ConfigureAwait(false);

                        if (resolved.KeptBothConflict)
                            keptBothConflicts++;

                        if (isDirectory)
                        {
                            if (!resolved.Skip)
                            {
                                EnsureNoExistingNestedReparsePoint(destinationRoot, resolved.TargetPath);
                                Directory.CreateDirectory(resolved.TargetPath);
                                createdDirectories.Add(resolved.TargetPath);
                            }
                            continue;
                        }

                        if (resolved.Skip)
                        {
                            processedBytes = CheckedAdd(processedBytes, entry.Length);
                            processedFiles++;
                            skippedFiles++;
                            result.SkippedFiles = skippedFiles;
                            result.CompletedFiles = processedFiles - skippedFiles;
                            result.KeptBothConflicts = keptBothConflicts;
                            estimator.Reset(processedBytes);
                            uiProgress.Report(BuildProgress(
                                ArchivePhase.Extracting,
                                entry.FullName + " (skipped)",
                                processedBytes,
                                safety.ExpandedBytes,
                                entry.Length,
                                entry.Length,
                                processedFiles,
                                safety.FileCount,
                                estimator), true);
                            continue;
                        }

                        string targetPath = resolved.TargetPath;
                        string targetDirectory = Path.GetDirectoryName(targetPath);
                        if (String.IsNullOrEmpty(targetDirectory))
                            targetDirectory = destinationRoot;

                        EnsureNoExistingNestedReparsePoint(destinationRoot, targetDirectory);
                        Directory.CreateDirectory(targetDirectory);

                        bool overwrite = resolved.Overwrite;
                        string tempPath = CreateSiblingTempPath(targetPath, "extracting");
                        long currentBytes = 0;

                        try
                        {
                            using (Stream entryStream = entry.Open())
                            using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                            {
                                while (true)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    int read = await entryStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                                    if (read == 0)
                                        break;

                                    await output.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                                    currentBytes += read;
                                    if (currentBytes > entry.Length)
                                        throw new InvalidDataException("ZIP entry produced more data than its declared size: " + entry.FullName);

                                    processedBytes = CheckedAdd(processedBytes, read);
                                    estimator.Add(processedBytes);

                                    uiProgress.Report(BuildProgress(
                                        ArchivePhase.Extracting,
                                        entry.FullName,
                                        processedBytes,
                                        safety.ExpandedBytes,
                                        currentBytes,
                                        entry.Length,
                                        processedFiles,
                                        safety.FileCount,
                                        estimator), false);
                                }
                            }

                            if (currentBytes != entry.Length)
                                throw new InvalidDataException("Extracted byte count does not match the ZIP entry metadata: " + entry.FullName);

                            CommitTempFile(tempPath, targetPath, overwrite);
                            TrySetFileTime(targetPath, entry.LastWriteTime);
                            result.CompletedPaths.Add(targetPath);
                        }
                        catch
                        {
                            TryDeleteFile(tempPath);
                            throw;
                        }

                        processedFiles++;
                        result.CompletedFiles = processedFiles - skippedFiles;
                        result.KeptBothConflicts = keptBothConflicts;
                        uiProgress.Report(BuildProgress(
                            ArchivePhase.Extracting,
                            entry.FullName,
                            processedBytes,
                            safety.ExpandedBytes,
                            entry.Length,
                            entry.Length,
                            processedFiles,
                            safety.FileCount,
                            estimator), true);
                    }
                }

                result.Status = ArchiveOperationStatus.Completed;
                result.SkippedFiles = skippedFiles;
                result.KeptBothConflicts = keptBothConflicts;

                Report(progress, new ArchiveProgressInfo
                {
                    Phase = ArchivePhase.Completed,
                    CurrentItem = "Extraction completed.",
                    ProcessedBytes = safety.ExpandedBytes,
                    TotalBytes = safety.ExpandedBytes,
                    ProcessedFiles = safety.FileCount,
                    TotalFiles = safety.FileCount
                });
                return result;
            }
            catch (OperationCanceledException)
            {
                result.Status = ArchiveOperationStatus.Cancelled;
                result.CompletedFiles = result.CompletedPaths.Count;
                result.SkippedFiles = skippedFiles;
                result.KeptBothConflicts = keptBothConflicts;
                Report(progress, new ArchiveProgressInfo
                {
                    Phase = ArchivePhase.Cancelled,
                    CurrentItem = result.CompletedPaths.Count > 0
                        ? "Extraction cancelled. Completed files were left in place."
                        : "Extraction cancelled.",
                    ProcessedBytes = processedBytes,
                    TotalBytes = safety.ExpandedBytes,
                    ProcessedFiles = processedFiles,
                    TotalFiles = safety.FileCount
                });
                return result;
            }
            catch (Exception ex)
            {
                result.Status = result.CompletedPaths.Count > 0 ? ArchiveOperationStatus.Partial : ArchiveOperationStatus.Failed;
                result.CompletedFiles = result.CompletedPaths.Count;
                result.SkippedFiles = skippedFiles;
                result.KeptBothConflicts = keptBothConflicts;
                result.ErrorMessage = ex.Message;
                Report(progress, new ArchiveProgressInfo
                {
                    Phase = ArchivePhase.Failed,
                    CurrentItem = result.CompletedPaths.Count > 0
                        ? "Extraction stopped after a partial result."
                        : "Extraction failed.",
                    ProcessedBytes = processedBytes,
                    TotalBytes = safety.ExpandedBytes,
                    ProcessedFiles = processedFiles,
                    TotalFiles = safety.FileCount
                });
                return result;
            }
        }

        private static async Task<ExtractionTarget> ResolveExtractionTargetAsync(
            string destinationRoot,
            string entryFullName,
            bool isDirectoryEntry,
            IDictionary<string, string> directoryMap,
            ISet<string> skippedDirectoryPrefixes,
            ISet<string> createdDirectories,
            ExtractionConflictState conflictState,
            Func<ArchiveOverwriteRequest, Task<ArchiveConflictResolution>> confirmConflictAsync,
            CancellationToken cancellationToken)
        {
            string normalized = entryFullName
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            string[] components = normalized.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            if (components.Length == 0)
                return new ExtractionTarget { TargetPath = destinationRoot };

            int directoryComponentCount = isDirectoryEntry ? components.Length : components.Length - 1;
            string archiveDirectoryKey = String.Empty;
            string actualDirectory = destinationRoot;
            bool keptBoth = false;

            for (int i = 0; i < directoryComponentCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                archiveDirectoryKey = archiveDirectoryKey.Length == 0
                    ? components[i]
                    : archiveDirectoryKey + "/" + components[i];

                if (IsUnderSkippedArchiveDirectory(archiveDirectoryKey, skippedDirectoryPrefixes))
                    return new ExtractionTarget { Skip = true, TargetPath = actualDirectory };

                string mappedDirectory;
                if (directoryMap.TryGetValue(archiveDirectoryKey, out mappedDirectory))
                {
                    actualDirectory = mappedDirectory;
                    continue;
                }

                string candidate = Path.Combine(actualDirectory, components[i]);
                EnsureNoExistingNestedReparsePoint(destinationRoot, actualDirectory);

                bool existingDirectory = Directory.Exists(candidate);
                bool existingFile = File.Exists(candidate);

                if (!existingDirectory && !existingFile)
                {
                    Directory.CreateDirectory(candidate);
                    createdDirectories.Add(candidate);
                    directoryMap[archiveDirectoryKey] = candidate;
                    actualDirectory = candidate;
                    continue;
                }

                if (createdDirectories.Contains(candidate))
                {
                    directoryMap[archiveDirectoryKey] = candidate;
                    actualDirectory = candidate;
                    continue;
                }

                string keepBothPath = CreateKeepBothPath(candidate, true);
                var request = new ArchiveOverwriteRequest
                {
                    TargetPath = candidate,
                    ArchiveEntryName = archiveDirectoryKey + "/",
                    ArchiveItemIsDirectory = true,
                    ExistingItemIsDirectory = existingDirectory,
                    ExistingItemIsReparsePoint = IsExistingReparsePoint(candidate),
                    SuggestedKeepBothPath = keepBothPath
                };

                ArchiveConflictResolution resolution = await ResolveArchiveConflictAsync(
                    request,
                    conflictState,
                    confirmConflictAsync).ConfigureAwait(false);

                if (resolution.Decision == ArchiveOverwriteDecision.Cancel)
                    throw new OperationCanceledException(cancellationToken);

                if (resolution.Decision == ArchiveOverwriteDecision.Skip)
                {
                    skippedDirectoryPrefixes.Add(archiveDirectoryKey);
                    return new ExtractionTarget { Skip = true, TargetPath = candidate };
                }

                if (resolution.Decision == ArchiveOverwriteDecision.KeepBoth)
                {
                    Directory.CreateDirectory(keepBothPath);
                    createdDirectories.Add(keepBothPath);
                    directoryMap[archiveDirectoryKey] = keepBothPath;
                    actualDirectory = keepBothPath;
                    keptBoth = true;
                    continue;
                }

                // For an archive folder, Overwrite means MERGE and is allowed only when
                // the existing target is also a real directory. Junctions/symlinks remain blocked.
                if (!existingDirectory || existingFile)
                    throw new IOException("Cannot merge a ZIP folder into a non-folder destination: " + candidate);

                EnsureNoExistingNestedReparsePoint(destinationRoot, candidate);
                directoryMap[archiveDirectoryKey] = candidate;
                actualDirectory = candidate;

                // MERGE is deliberately local. It creates a scope only for an optional
                // remembered FILE-conflict choice beneath this particular merged folder.
                conflictState.RegisterMergedFolder(archiveDirectoryKey, candidate);
            }

            if (isDirectoryEntry)
            {
                return new ExtractionTarget
                {
                    TargetPath = actualDirectory,
                    KeptBothConflict = keptBoth
                };
            }

            if (IsUnderSkippedArchiveDirectory(archiveDirectoryKey, skippedDirectoryPrefixes))
                return new ExtractionTarget { Skip = true, TargetPath = actualDirectory };

            string targetPath = Path.Combine(actualDirectory, components[components.Length - 1]);
            bool targetIsDirectory = Directory.Exists(targetPath);
            bool targetIsFile = File.Exists(targetPath);

            if (!targetIsDirectory && !targetIsFile)
            {
                return new ExtractionTarget
                {
                    TargetPath = targetPath,
                    KeptBothConflict = keptBoth
                };
            }

            string suggestedKeepBoth = CreateKeepBothPath(targetPath, false);
            MergedFolderConflictScope mergedScope = conflictState.FindNearestMergedFolderScope(archiveDirectoryKey);
            var fileRequest = new ArchiveOverwriteRequest
            {
                TargetPath = targetPath,
                ArchiveEntryName = entryFullName,
                ArchiveItemIsDirectory = false,
                ExistingItemIsDirectory = targetIsDirectory,
                ExistingItemIsReparsePoint = IsExistingReparsePoint(targetPath),
                SuggestedKeepBothPath = suggestedKeepBoth,
                MergedFolderScopePath = mergedScope == null ? null : mergedScope.TargetPath,
                MergedFolderScopeKey = mergedScope == null ? null : mergedScope.ArchiveDirectoryKey
            };

            ArchiveConflictResolution fileResolution = await ResolveArchiveConflictAsync(
                fileRequest,
                conflictState,
                confirmConflictAsync).ConfigureAwait(false);

            if (fileResolution.Decision == ArchiveOverwriteDecision.Cancel)
                throw new OperationCanceledException(cancellationToken);

            if (fileResolution.Decision == ArchiveOverwriteDecision.Skip)
                return new ExtractionTarget { Skip = true, TargetPath = targetPath, KeptBothConflict = keptBoth };

            if (fileResolution.Decision == ArchiveOverwriteDecision.KeepBoth)
            {
                return new ExtractionTarget
                {
                    TargetPath = suggestedKeepBoth,
                    KeptBothConflict = true
                };
            }

            if (!targetIsFile || targetIsDirectory)
                throw new IOException("Cannot replace a folder with a ZIP file: " + targetPath);

            return new ExtractionTarget
            {
                TargetPath = targetPath,
                Overwrite = true,
                KeptBothConflict = keptBoth
            };
        }

        private static async Task<ArchiveConflictResolution> ResolveArchiveConflictAsync(
            ArchiveOverwriteRequest request,
            ExtractionConflictState state,
            Func<ArchiveOverwriteRequest, Task<ArchiveConflictResolution>> confirmConflictAsync)
        {
            // Folder conflicts are always decided locally: MERGE / KEEP BOTH / SKIP / CANCEL.
            // Only file conflicts under a folder that the user explicitly MERGED can remember
            // a choice, and that remembered choice is scoped to that merged folder.
            if (!request.ArchiveItemIsDirectory && !String.IsNullOrEmpty(request.MergedFolderScopeKey))
            {
                ArchiveOverwriteDecision remembered;
                if (state.TryGetRememberedFileDecision(request.MergedFolderScopeKey, out remembered) &&
                    IsConflictDecisionCompatible(request, remembered))
                {
                    return new ArchiveConflictResolution
                    {
                        Decision = remembered,
                        ApplyToRemainingFileConflictsUnderMergedFolder = true
                    };
                }
            }

            ArchiveConflictResolution resolution = confirmConflictAsync == null
                ? new ArchiveConflictResolution { Decision = ArchiveOverwriteDecision.Skip }
                : await confirmConflictAsync(request).ConfigureAwait(false);

            if (resolution == null)
                resolution = new ArchiveConflictResolution { Decision = ArchiveOverwriteDecision.Cancel };

            if (!IsConflictDecisionCompatible(request, resolution.Decision))
                resolution.Decision = ArchiveOverwriteDecision.KeepBoth;

            if (!request.ArchiveItemIsDirectory &&
                resolution.ApplyToRemainingFileConflictsUnderMergedFolder &&
                !String.IsNullOrEmpty(request.MergedFolderScopeKey) &&
                resolution.Decision != ArchiveOverwriteDecision.Cancel)
            {
                state.SetRememberedFileDecision(request.MergedFolderScopeKey, resolution.Decision);
            }

            return resolution;
        }

        private static bool IsConflictDecisionCompatible(ArchiveOverwriteRequest request, ArchiveOverwriteDecision decision)
        {
            if (decision != ArchiveOverwriteDecision.Overwrite)
                return true;

            return request.ArchiveItemIsDirectory == request.ExistingItemIsDirectory &&
                   !request.ExistingItemIsReparsePoint;
        }


        private static bool IsExistingReparsePoint(string path)
        {
            try
            {
                if (!File.Exists(path) && !Directory.Exists(path))
                    return false;
                return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
            }
            catch
            {
                // If attributes cannot be inspected, do not allow a remembered destructive action.
                return true;
            }
        }

        private static bool IsUnderSkippedArchiveDirectory(string archiveDirectoryKey, ISet<string> skippedDirectoryPrefixes)
        {
            if (String.IsNullOrEmpty(archiveDirectoryKey))
                return false;

            foreach (string skipped in skippedDirectoryPrefixes)
            {
                if (String.Equals(archiveDirectoryKey, skipped, StringComparison.OrdinalIgnoreCase) ||
                    archiveDirectoryKey.StartsWith(skipped + "/", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string CreateKeepBothPath(string originalPath, bool isDirectory)
        {
            string directory = Path.GetDirectoryName(originalPath);
            if (String.IsNullOrEmpty(directory))
                directory = Directory.GetCurrentDirectory();

            string name = Path.GetFileName(originalPath);
            string stem = isDirectory ? name : Path.GetFileNameWithoutExtension(name);
            string extension = isDirectory ? String.Empty : Path.GetExtension(name);

            if (!isDirectory && String.IsNullOrEmpty(stem))
            {
                stem = name;
                extension = String.Empty;
            }

            for (int number = 1; number < Int32.MaxValue; number++)
            {
                string candidateName = stem + "(" + number.ToString() + ")" + extension;
                string candidate = Path.Combine(directory, candidateName);
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                    return candidate;
            }

            throw new IOException("Could not find an available name for: " + originalPath);
        }

        private sealed class ExtractionConflictState
        {
            private readonly Dictionary<string, MergedFolderConflictScope> mergedFolders =
                new Dictionary<string, MergedFolderConflictScope>(PathComparer);

            public void RegisterMergedFolder(string archiveDirectoryKey, string targetPath)
            {
                if (String.IsNullOrEmpty(archiveDirectoryKey) || String.IsNullOrEmpty(targetPath))
                    return;

                MergedFolderConflictScope existing;
                if (mergedFolders.TryGetValue(archiveDirectoryKey, out existing))
                {
                    existing.TargetPath = targetPath;
                    return;
                }

                mergedFolders[archiveDirectoryKey] = new MergedFolderConflictScope
                {
                    ArchiveDirectoryKey = archiveDirectoryKey,
                    TargetPath = targetPath
                };
            }

            public MergedFolderConflictScope FindNearestMergedFolderScope(string archiveDirectoryKey)
            {
                if (String.IsNullOrEmpty(archiveDirectoryKey))
                    return null;

                MergedFolderConflictScope nearest = null;
                foreach (MergedFolderConflictScope scope in mergedFolders.Values)
                {
                    if (!IsWithinArchiveDirectory(archiveDirectoryKey, scope.ArchiveDirectoryKey))
                        continue;

                    if (nearest == null || scope.ArchiveDirectoryKey.Length > nearest.ArchiveDirectoryKey.Length)
                        nearest = scope;
                }

                return nearest;
            }

            public bool TryGetRememberedFileDecision(
                string nearestMergedFolderKey,
                out ArchiveOverwriteDecision decision)
            {
                decision = ArchiveOverwriteDecision.Cancel;
                if (String.IsNullOrEmpty(nearestMergedFolderKey))
                    return false;

                MergedFolderConflictScope best = null;
                foreach (MergedFolderConflictScope scope in mergedFolders.Values)
                {
                    if (!scope.FileDecision.HasValue)
                        continue;
                    if (!IsWithinArchiveDirectory(nearestMergedFolderKey, scope.ArchiveDirectoryKey))
                        continue;

                    if (best == null || scope.ArchiveDirectoryKey.Length > best.ArchiveDirectoryKey.Length)
                        best = scope;
                }

                if (best == null)
                    return false;

                decision = best.FileDecision.Value;
                return true;
            }

            public void SetRememberedFileDecision(
                string mergedFolderKey,
                ArchiveOverwriteDecision decision)
            {
                MergedFolderConflictScope scope;
                if (String.IsNullOrEmpty(mergedFolderKey) ||
                    !mergedFolders.TryGetValue(mergedFolderKey, out scope))
                    return;

                scope.FileDecision = decision;
            }

            private static bool IsWithinArchiveDirectory(string candidateKey, string ancestorKey)
            {
                return String.Equals(candidateKey, ancestorKey, StringComparison.OrdinalIgnoreCase) ||
                       candidateKey.StartsWith(ancestorKey + "/", StringComparison.OrdinalIgnoreCase);
            }
        }

        private sealed class MergedFolderConflictScope
        {
            public string ArchiveDirectoryKey { get; set; }
            public string TargetPath { get; set; }
            public ArchiveOverwriteDecision? FileDecision { get; set; }
        }

        private sealed class ExtractionTarget
        {
            public string TargetPath { get; set; }
            public bool Skip { get; set; }
            public bool Overwrite { get; set; }
            public bool KeptBothConflict { get; set; }
        }

        private static ArchiveSafetyReport AnalyzeArchive(
            string zipFullPath,
            string destinationRoot,
            ArchiveThresholds thresholds,
            CancellationToken cancellationToken)
        {
            long expandedBytes = 0;
            long compressedBytes = 0;
            int fileCount = 0;
            int directoryCount = 0;

            using (var stream = new FileStream(zipFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Safety rules 1 + 2: invalid/escaping paths are always blocked, not user-overridable.
                    GetSafeTargetPath(destinationRoot, entry.FullName);

                    if (IsDirectoryEntry(entry))
                    {
                        directoryCount++;
                        continue;
                    }

                    fileCount++;
                    expandedBytes = CheckedAdd(expandedBytes, entry.Length);
                    compressedBytes = CheckedAdd(compressedBytes, entry.CompressedLength);
                }
            }

            double ratio;
            if (expandedBytes == 0)
                ratio = 1.0;
            else if (compressedBytes == 0)
                ratio = Double.PositiveInfinity;
            else
                ratio = (double)expandedBytes / (double)compressedBytes;

            var report = new ArchiveSafetyReport
            {
                ExpandedBytes = expandedBytes,
                CompressedBytes = compressedBytes,
                FileCount = fileCount,
                DirectoryCount = directoryCount,
                CompressionRatio = ratio
            };

            if (expandedBytes > thresholds.ExpandedSizeWarningBytes)
            {
                report.Issues.Add(new ArchiveSafetyIssue
                {
                    Type = ArchiveSafetyIssueType.ExpandedSize,
                    Message = "Expanded size exceeds the warning threshold."
                });
            }

            if (fileCount > thresholds.FileCountWarning)
            {
                report.Issues.Add(new ArchiveSafetyIssue
                {
                    Type = ArchiveSafetyIssueType.FileCount,
                    Message = "File count exceeds the warning threshold."
                });
            }

            if (ratio > thresholds.CompressionRatioWarning)
            {
                report.Issues.Add(new ArchiveSafetyIssue
                {
                    Type = ArchiveSafetyIssueType.CompressionRatio,
                    Message = "Compression ratio exceeds the warning threshold."
                });
            }

            return report;
        }

        private static List<CompressionPlanItem> BuildCompressionPlan(
            IEnumerable<string> sourcePaths,
            string destinationZipPath,
            CancellationToken cancellationToken)
        {
            var plan = new List<CompressionPlanItem>();
            var entryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawPath in sourcePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string sourcePath = Path.GetFullPath(rawPath);

                if (PathComparer.Equals(sourcePath, destinationZipPath))
                    throw new IOException("The destination ZIP cannot also be a source file.");

                if (File.Exists(sourcePath))
                {
                    AddCompressionFile(plan, entryNames, sourcePath, Path.GetFileName(sourcePath), destinationZipPath);
                    continue;
                }

                if (!Directory.Exists(sourcePath))
                    throw new FileNotFoundException("Source path not found.", sourcePath);

                string rootName = new DirectoryInfo(sourcePath).Name;
                if (String.IsNullOrEmpty(rootName))
                    rootName = "Archive";

                TraverseDirectoryForCompression(
                    new DirectoryInfo(sourcePath),
                    rootName,
                    destinationZipPath,
                    plan,
                    entryNames,
                    cancellationToken);
            }

            return plan;
        }

        private static void TraverseDirectoryForCompression(
            DirectoryInfo directory,
            string entryName,
            string destinationZipPath,
            IList<CompressionPlanItem> plan,
            ISet<string> entryNames,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedDirectoryName = NormalizeZipEntryName(entryName).TrimEnd('/') + "/";
            AddUniquePlanItem(plan, entryNames, new CompressionPlanItem
            {
                SourcePath = directory.FullName,
                EntryName = normalizedDirectoryName,
                IsDirectory = true,
                Length = 0,
                LastWriteTime = directory.LastWriteTime
            });

            FileSystemInfo[] children = directory.GetFileSystemInfos();
            Array.Sort(children, delegate(FileSystemInfo a, FileSystemInfo b)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
            });

            foreach (FileSystemInfo child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var childDirectory = child as DirectoryInfo;
                if (childDirectory != null)
                {
                    string childEntry = normalizedDirectoryName + childDirectory.Name;

                    // Do not recursively follow junctions/symlinks. This prevents cycles and accidental traversal
                    // outside the selected tree. The directory marker itself is still stored.
                    if ((childDirectory.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        AddUniquePlanItem(plan, entryNames, new CompressionPlanItem
                        {
                            SourcePath = childDirectory.FullName,
                            EntryName = NormalizeZipEntryName(childEntry).TrimEnd('/') + "/",
                            IsDirectory = true,
                            Length = 0,
                            LastWriteTime = childDirectory.LastWriteTime
                        });
                        continue;
                    }

                    TraverseDirectoryForCompression(
                        childDirectory,
                        childEntry,
                        destinationZipPath,
                        plan,
                        entryNames,
                        cancellationToken);
                    continue;
                }

                var file = child as FileInfo;
                if (file != null)
                {
                    string fileFullPath = Path.GetFullPath(file.FullName);
                    if (PathComparer.Equals(fileFullPath, destinationZipPath))
                        continue;

                    AddCompressionFile(
                        plan,
                        entryNames,
                        fileFullPath,
                        normalizedDirectoryName + file.Name,
                        destinationZipPath);
                }
            }
        }

        private static void AddCompressionFile(
            IList<CompressionPlanItem> plan,
            ISet<string> entryNames,
            string sourcePath,
            string entryName,
            string destinationZipPath)
        {
            string sourceFullPath = Path.GetFullPath(sourcePath);
            if (PathComparer.Equals(sourceFullPath, destinationZipPath))
                return;

            var info = new FileInfo(sourceFullPath);
            AddUniquePlanItem(plan, entryNames, new CompressionPlanItem
            {
                SourcePath = sourceFullPath,
                EntryName = NormalizeZipEntryName(entryName),
                IsDirectory = false,
                Length = info.Length,
                LastWriteTime = info.LastWriteTime
            });
        }

        private static void AddUniquePlanItem(
            IList<CompressionPlanItem> plan,
            ISet<string> entryNames,
            CompressionPlanItem item)
        {
            if (!entryNames.Add(item.EntryName))
                throw new IOException("Two selected items would create the same ZIP entry: " + item.EntryName);
            plan.Add(item);
        }

        private static string GetSafeTargetPath(string destinationRoot, string entryFullName)
        {
            if (entryFullName == null)
                throw new InvalidDataException("ZIP entry has no name.");

            string normalized = entryFullName
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (String.IsNullOrEmpty(normalized))
                return Path.GetFullPath(destinationRoot);

            ValidateWindowsEntryComponents(normalized, entryFullName);

            // Safety rule 2: absolute paths and drive-qualified paths are not user-overridable.
            if (Path.IsPathRooted(normalized) || IsDriveQualified(normalized))
                throw new InvalidDataException("ZIP entry uses an absolute path: " + entryFullName);

            string rootFullPath = Path.GetFullPath(destinationRoot);
            string rootPrefix = EnsureTrailingSeparator(rootFullPath);
            string candidate;

            try
            {
                candidate = Path.GetFullPath(Path.Combine(rootFullPath, normalized));
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                    throw new InvalidDataException("ZIP entry contains an invalid path: " + entryFullName, ex);
                throw;
            }

            // Safety rule 1: ../ or equivalent path traversal must never escape the chosen destination.
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) &&
                !String.Equals(candidate, rootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("ZIP entry escapes the selected destination: " + entryFullName);
            }

            return candidate;
        }


        private static void ValidateWindowsEntryComponents(string normalizedPath, string originalEntryName)
        {
            string[] components = normalizedPath.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            char[] invalid = Path.GetInvalidFileNameChars();

            foreach (string component in components)
            {
                if (component == "." || component == "..")
                    throw new InvalidDataException("ZIP entry contains a relative path component: " + originalEntryName);

                if (component.IndexOfAny(invalid) >= 0)
                    throw new InvalidDataException("ZIP entry contains characters that are unsafe or invalid on Windows: " + originalEntryName);

                if (component.EndsWith(" ", StringComparison.Ordinal) || component.EndsWith(".", StringComparison.Ordinal))
                    throw new InvalidDataException("ZIP entry contains a Windows-ambiguous trailing space or dot: " + originalEntryName);

                string deviceCandidate = component;
                int dot = deviceCandidate.IndexOf('.');
                if (dot >= 0)
                    deviceCandidate = deviceCandidate.Substring(0, dot);

                string upper = deviceCandidate.ToUpperInvariant();
                if (upper == "CON" || upper == "PRN" || upper == "AUX" || upper == "NUL" ||
                    upper == "COM1" || upper == "COM2" || upper == "COM3" || upper == "COM4" || upper == "COM5" ||
                    upper == "COM6" || upper == "COM7" || upper == "COM8" || upper == "COM9" ||
                    upper == "LPT1" || upper == "LPT2" || upper == "LPT3" || upper == "LPT4" || upper == "LPT5" ||
                    upper == "LPT6" || upper == "LPT7" || upper == "LPT8" || upper == "LPT9")
                {
                    throw new InvalidDataException("ZIP entry targets a reserved Windows device name: " + originalEntryName);
                }
            }
        }

        private static bool IsDriveQualified(string path)
        {
            return path.Length >= 2 && Char.IsLetter(path[0]) && path[1] == ':';
        }

        private static void EnsureNoExistingNestedReparsePoint(string destinationRoot, string targetDirectory)
        {
            string root = Path.GetFullPath(destinationRoot);
            string target = Path.GetFullPath(targetDirectory);
            string rootPrefix = EnsureTrailingSeparator(root);

            if (!target.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) &&
                !String.Equals(target, root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Extraction target escaped the selected destination.");
            }

            if (String.Equals(target, root, StringComparison.OrdinalIgnoreCase))
                return;

            string relative = target.Substring(rootPrefix.Length);
            string current = root;
            string[] parts = relative.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                current = Path.Combine(current, part);
                if (!Directory.Exists(current))
                    continue;

                FileAttributes attributes = File.GetAttributes(current);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Extraction path crosses an existing junction or symbolic link: " + current);
            }
        }

        private static void CommitTempFile(string tempPath, string finalPath, bool overwrite)
        {
            if (!File.Exists(finalPath))
            {
                File.Move(tempPath, finalPath);
                return;
            }

            if (!overwrite)
                throw new IOException("Destination already exists: " + finalPath);

            // Prefer atomic replacement on normal Windows file systems.
            try
            {
                File.Replace(tempPath, finalPath, null, true);
                return;
            }
            catch (PlatformNotSupportedException)
            {
            }
            catch (IOException)
            {
                // Some removable/non-NTFS file systems do not support File.Replace.
            }

            // Safe fallback: preserve the old file until the new file has been moved into place.
            string backupPath = CreateSiblingTempPath(finalPath, "backup");
            File.Move(finalPath, backupPath);
            try
            {
                File.Move(tempPath, finalPath);
                TryDeleteFile(backupPath);
            }
            catch
            {
                if (!File.Exists(finalPath) && File.Exists(backupPath))
                    File.Move(backupPath, finalPath);
                throw;
            }
        }

        private static string CreateSiblingTempPath(string finalPath, string purpose)
        {
            string directory = Path.GetDirectoryName(finalPath);
            if (String.IsNullOrEmpty(directory))
                directory = Directory.GetCurrentDirectory();
            string name = Path.GetFileName(finalPath);

            while (true)
            {
                string candidate = Path.Combine(
                    directory,
                    "." + name + ".ferry-" + purpose + "-" + Guid.NewGuid().ToString("N") + ".tmp");
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                    return candidate;
            }
        }

        private static bool IsDirectoryEntry(ZipArchiveEntry entry)
        {
            return entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                   entry.FullName.EndsWith("\\", StringComparison.Ordinal);
        }

        private static string EnsureDirectoryEntryName(string entryName)
        {
            string normalized = NormalizeZipEntryName(entryName);
            return normalized.EndsWith("/", StringComparison.Ordinal) ? normalized : normalized + "/";
        }

        private static string NormalizeZipEntryName(string entryName)
        {
            return entryName.Replace('\\', '/').TrimStart('/');
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                return path;
            return path + Path.DirectorySeparatorChar;
        }

        private static void TrySetEntryTime(ZipArchiveEntry entry, DateTime localTime)
        {
            try
            {
                entry.LastWriteTime = new DateTimeOffset(localTime);
            }
            catch
            {
                // ZIP timestamp range is limited; archive content is more important than metadata.
            }
        }

        private static void TrySetFileTime(string path, DateTimeOffset timestamp)
        {
            try
            {
                File.SetLastWriteTime(path, timestamp.LocalDateTime);
            }
            catch
            {
                // Timestamp restoration failure must not invalidate successfully extracted content.
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (!String.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static long CheckedSum(IEnumerable<long> values)
        {
            long sum = 0;
            foreach (long value in values)
                sum = CheckedAdd(sum, value);
            return sum;
        }

        private static long CheckedAdd(long left, long right)
        {
            checked
            {
                return left + right;
            }
        }

        private static ArchiveProgressInfo BuildProgress(
            ArchivePhase phase,
            string currentItem,
            long processedBytes,
            long totalBytes,
            long currentProcessedBytes,
            long currentTotalBytes,
            int processedFiles,
            int totalFiles,
            ThroughputEstimator estimator)
        {
            double speed = estimator.BytesPerSecond;
            TimeSpan? remaining = null;
            if (speed > 0.0 && totalBytes > processedBytes)
                remaining = TimeSpan.FromSeconds((totalBytes - processedBytes) / speed);
            else if (totalBytes <= processedBytes && totalBytes > 0)
                remaining = TimeSpan.Zero;

            return new ArchiveProgressInfo
            {
                Phase = phase,
                CurrentItem = currentItem,
                ProcessedBytes = processedBytes,
                TotalBytes = totalBytes,
                CurrentItemProcessedBytes = currentProcessedBytes,
                CurrentItemTotalBytes = currentTotalBytes,
                ProcessedFiles = processedFiles,
                TotalFiles = totalFiles,
                BytesPerSecond = speed,
                EstimatedRemaining = remaining
            };
        }

        private static void Report(IProgress<ArchiveProgressInfo> progress, ArchiveProgressInfo info)
        {
            if (progress != null)
                progress.Report(info);
        }

        private sealed class CompressionPlanItem
        {
            public string SourcePath { get; set; }
            public string EntryName { get; set; }
            public bool IsDirectory { get; set; }
            public long Length { get; set; }
            public DateTime LastWriteTime { get; set; }
        }

        private sealed class ThrottledProgressReporter
        {
            private readonly IProgress<ArchiveProgressInfo> _progress;
            private readonly int _minimumIntervalMilliseconds;
            private readonly Stopwatch _clock = Stopwatch.StartNew();
            private long _lastReportMilliseconds = -100000;

            public ThrottledProgressReporter(IProgress<ArchiveProgressInfo> progress, int minimumIntervalMilliseconds)
            {
                _progress = progress;
                _minimumIntervalMilliseconds = minimumIntervalMilliseconds;
            }

            public void Report(ArchiveProgressInfo info, bool force)
            {
                if (_progress == null)
                    return;

                long now = _clock.ElapsedMilliseconds;
                if (!force && now - _lastReportMilliseconds < _minimumIntervalMilliseconds)
                    return;

                _lastReportMilliseconds = now;
                _progress.Report(info);
            }
        }

        private sealed class ThroughputEstimator
        {
            private readonly Queue<Sample> _samples = new Queue<Sample>();
            private readonly Stopwatch _clock = Stopwatch.StartNew();
            private long _lastBytes;

            public double BytesPerSecond
            {
                get
                {
                    if (_samples.Count < 2)
                        return 0.0;

                    Sample first = _samples.Peek();
                    Sample last = null;
                    foreach (Sample sample in _samples)
                        last = sample;

                    if (last == null)
                        return 0.0;

                    double seconds = (last.Milliseconds - first.Milliseconds) / 1000.0;
                    if (seconds <= 0.05)
                        return 0.0;

                    long bytes = last.Bytes - first.Bytes;
                    return bytes > 0 ? bytes / seconds : 0.0;
                }
            }

            public void Add(long processedBytes)
            {
                long now = _clock.ElapsedMilliseconds;
                _lastBytes = processedBytes;
                _samples.Enqueue(new Sample { Milliseconds = now, Bytes = processedBytes });

                while (_samples.Count > 2 && now - _samples.Peek().Milliseconds > 5000)
                    _samples.Dequeue();
            }

            public void Reset(long processedBytes)
            {
                _samples.Clear();
                _lastBytes = processedBytes;
                _samples.Enqueue(new Sample { Milliseconds = _clock.ElapsedMilliseconds, Bytes = processedBytes });
            }

            private sealed class Sample
            {
                public long Milliseconds { get; set; }
                public long Bytes { get; set; }
            }
        }
    }
}
