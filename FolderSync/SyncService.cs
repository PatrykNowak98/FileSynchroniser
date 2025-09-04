using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FolderSync.Utilities;

namespace FolderSync
{
    /// <summary>
    /// Service responsible for performing folder synchronization
    /// </summary>
    public class SyncService
    {
        private readonly Logger _logger;

        /// <summary>
        /// Initialize sync service with logger
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public SyncService(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Perform asynchronous synchronization from source to replica with deletion support
        /// </summary>
        /// <param name="sourcePath">Source directory path</param>
        /// <param name="replicaPath">Replica directory path</param>
        /// <returns>Statistics about the synchronization operation</returns>
        public async Task<SyncStats> SynchronizeAsync(string sourcePath, string replicaPath)
        {
            return await Task.Run(() => SynchronizeOnce(sourcePath, replicaPath));
        }

        /// <summary>
        /// Perform one-time synchronization from source to replica with deletion support
        /// </summary>
        /// <param name="sourcePath">Source directory path</param>
        /// <param name="replicaPath">Replica directory path</param>
        /// <returns>Statistics about the synchronization operation</returns>
        public SyncStats SynchronizeOnce(string sourcePath, string replicaPath)
        {
            var stats = new SyncStats();

            try
            {
                // Validate paths
                if (!PathHelper.IsValidPath(sourcePath))
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
                }

                // Ensure replica directory exists
                if (!Directory.Exists(replicaPath))
                {
                    Directory.CreateDirectory(replicaPath);
                    _logger.LogInfo($"Created replica directory: {replicaPath}");
                    stats.DirectoriesCreated++;
                }

                // Perform iterative synchronization to avoid recursion
                SynchronizeDirectoryIterative(sourcePath, replicaPath, stats);

                // Remove files/directories from replica that no longer exist in source
                RemoveDeletedItems(sourcePath, replicaPath, stats);

                _logger.LogInfo($"Synchronization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Synchronization failed: {ex.Message}");
                stats.ErrorsEncountered++;
            }

            return stats;
        }

        /// <summary>
        /// Perform directory synchronization using iterative approach (stack-based DFS)
        /// </summary>
        /// <param name="sourcePath">Source directory path</param>
        /// <param name="replicaPath">Replica directory path</param>
        /// <param name="stats">Statistics to update</param>
        private void SynchronizeDirectoryIterative(string sourcePath, string replicaPath, SyncStats stats)
        {
            // Use stack for iterative directory traversal
            var directoriesToProcess = new Stack<(string source, string replica)>();
            directoriesToProcess.Push((sourcePath, replicaPath));

            while (directoriesToProcess.Count > 0)
            {
                var (currentSource, currentReplica) = directoriesToProcess.Pop();

                try
                {
                    // Process files in current directory
                    ProcessFilesInDirectory(currentSource, currentReplica, stats);

                    // Process subdirectories
                    var sourceDirectories = Directory.EnumerateDirectories(currentSource);
                    foreach (var sourceSubDir in sourceDirectories)
                    {
                        var dirName = Path.GetFileName(sourceSubDir);
                        var replicaSubDir = Path.Combine(currentReplica, dirName);

                        // Create replica subdirectory if it doesn't exist
                        if (!Directory.Exists(replicaSubDir))
                        {
                            Directory.CreateDirectory(replicaSubDir);
                            _logger.LogInfo($"Created directory: {replicaSubDir}");
                            stats.DirectoriesCreated++;
                        }

                        // Add to stack for processing
                        directoriesToProcess.Push((sourceSubDir, replicaSubDir));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing directory {currentSource}: {ex.Message}");
                    stats.ErrorsEncountered++;
                }
            }
        }

        /// <summary>
        /// Process all files in a single directory
        /// </summary>
        /// <param name="sourceDir">Source directory</param>
        /// <param name="replicaDir">Replica directory</param>
        /// <param name="stats">Statistics to update</param>
        private void ProcessFilesInDirectory(string sourceDir, string replicaDir, SyncStats stats)
        {
            var sourceFiles = Directory.EnumerateFiles(sourceDir);
            
            foreach (var sourceFile in sourceFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(sourceFile);
                    var replicaFile = Path.Combine(replicaDir, fileName);

                    if (ShouldCopyFile(sourceFile, replicaFile))
                    {
                        bool fileExisted = File.Exists(replicaFile);
                        File.Copy(sourceFile, replicaFile, true);
                        
                        if (fileExisted)
                        {
                            _logger.LogInfo($"Updated file: {replicaFile}");
                            stats.FilesUpdated++;
                        }
                        else
                        {
                            _logger.LogInfo($"Copied file: {replicaFile}");
                            stats.FilesCopied++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error copying file {sourceFile}: {ex.Message}");
                    stats.ErrorsEncountered++;
                }
            }
        }

        /// <summary>
        /// Determine if a file should be copied based on existence, size, and timestamp
        /// </summary>
        /// <param name="sourceFile">Source file path</param>
        /// <param name="replicaFile">Replica file path</param>
        /// <returns>True if file should be copied</returns>
        private bool ShouldCopyFile(string sourceFile, string replicaFile)
        {
            try
            {
                // If replica file doesn't exist, copy it
                if (!File.Exists(replicaFile))
                {
                    return true;
                }

                var sourceInfo = new FileInfo(sourceFile);
                var replicaInfo = new FileInfo(replicaFile);

                // Compare file size and last write time
                return sourceInfo.Length != replicaInfo.Length ||
                       sourceInfo.LastWriteTime != replicaInfo.LastWriteTime;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error comparing files {sourceFile} and {replicaFile}: {ex.Message}");
                return true; // Default to copying on error
            }
        }

        /// <summary>
        /// Remove files and directories from replica that no longer exist in source
        /// </summary>
        /// <param name="sourcePath">Source directory path</param>
        /// <param name="replicaPath">Replica directory path</param>
        /// <param name="stats">Statistics to update</param>
        private void RemoveDeletedItems(string sourcePath, string replicaPath, SyncStats stats)
        {
            try
            {
                // Remove files that don't exist in source
                RemoveDeletedFiles(sourcePath, replicaPath, stats);

                // Remove directories that don't exist in source
                RemoveDeletedDirectories(sourcePath, replicaPath, stats);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing deleted items: {ex.Message}");
                stats.ErrorsEncountered++;
            }
        }

        /// <summary>
        /// Remove files from replica that don't exist in source
        /// </summary>
        private void RemoveDeletedFiles(string sourcePath, string replicaPath, SyncStats stats)
        {
            // Use stack for iterative traversal
            var directoriesToCheck = new Stack<(string source, string replica)>();
            directoriesToCheck.Push((sourcePath, replicaPath));

            while (directoriesToCheck.Count > 0)
            {
                var (currentSource, currentReplica) = directoriesToCheck.Pop();

                try
                {
                    if (!Directory.Exists(currentReplica))
                        continue;

                    // Check files in current replica directory
                    var replicaFiles = Directory.EnumerateFiles(currentReplica);
                    foreach (var replicaFile in replicaFiles)
                    {
                        var fileName = Path.GetFileName(replicaFile);
                        var sourceFile = Path.Combine(currentSource, fileName);

                        if (!File.Exists(sourceFile))
                        {
                            try
                            {
                                File.Delete(replicaFile);
                                _logger.LogInfo($"Deleted file: {replicaFile}");
                                stats.FilesDeleted++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Error deleting file {replicaFile}: {ex.Message}");
                                stats.ErrorsEncountered++;
                            }
                        }
                    }

                    // Add subdirectories to check
                    if (Directory.Exists(currentSource))
                    {
                        var replicaSubDirs = Directory.EnumerateDirectories(currentReplica);
                        foreach (var replicaSubDir in replicaSubDirs)
                        {
                            var dirName = Path.GetFileName(replicaSubDir);
                            var sourceSubDir = Path.Combine(currentSource, dirName);
                            directoriesToCheck.Push((sourceSubDir, replicaSubDir));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error checking files in {currentReplica}: {ex.Message}");
                    stats.ErrorsEncountered++;
                }
            }
        }

        /// <summary>
        /// Remove directories from replica that don't exist in source
        /// </summary>
        private void RemoveDeletedDirectories(string sourcePath, string replicaPath, SyncStats stats)
        {
            try
            {
                if (!Directory.Exists(replicaPath))
                    return;

                var replicaDirectories = Directory.EnumerateDirectories(replicaPath);
                var directoriesToDelete = new List<string>();

                foreach (var replicaDir in replicaDirectories)
                {
                    var dirName = Path.GetFileName(replicaDir);
                    var sourceDir = Path.Combine(sourcePath, dirName);

                    if (!Directory.Exists(sourceDir))
                    {
                        directoriesToDelete.Add(replicaDir);
                    }
                    else
                    {
                        // Recursively check subdirectories
                        RemoveDeletedDirectories(sourceDir, replicaDir, stats);
                    }
                }

                // Delete directories that don't exist in source
                foreach (var dirToDelete in directoriesToDelete)
                {
                    try
                    {
                        Directory.Delete(dirToDelete, true);
                        _logger.LogInfo($"Deleted directory: {dirToDelete}");
                        stats.DirectoriesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error deleting directory {dirToDelete}: {ex.Message}");
                        stats.ErrorsEncountered++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking directories in {replicaPath}: {ex.Message}");
                stats.ErrorsEncountered++;
            }
        }
    }
}
