using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        /// <param name="useMd5Verification">Whether to use MD5 for file comparison</param>
        /// <returns>Statistics about the synchronization operation</returns>
        public async Task<SyncStats> SynchronizeAsync(string sourcePath, string replicaPath, bool useMd5Verification = false)
        {
            return await Task.Run(() => SynchronizeOnce(sourcePath, replicaPath, useMd5Verification));
        }

        /// <summary>
        /// Perform one-time synchronization from source to replica with deletion support
        /// </summary>
        /// <param name="sourcePath">Source directory path</param>
        /// <param name="replicaPath">Replica directory path</param>
        /// <param name="useMd5Verification">Whether to use MD5 for file comparison</param>
        /// <returns>Statistics about the synchronization operation</returns>
        public SyncStats SynchronizeOnce(string sourcePath, string replicaPath, bool useMd5Verification = false)
        {
            var stats = new SyncStats();

            try
            {
                if (!PathHelper.IsValidPath(sourcePath))
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
                }

                if (!Directory.Exists(replicaPath))
                {
                    Directory.CreateDirectory(replicaPath);
                    _logger.LogInfo($"Created replica directory: {replicaPath}");
                    stats.DirectoriesCreated++;
                }

                SynchronizeDirectoryIterative(sourcePath, replicaPath, stats, useMd5Verification);
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
        /// <param name="useMd5Verification">Whether to use MD5 for file comparison</param>
        private void SynchronizeDirectoryIterative(string sourcePath, string replicaPath, SyncStats stats, bool useMd5Verification)
        {
            var directoriesToProcess = new Stack<(string source, string replica)>();
            directoriesToProcess.Push((sourcePath, replicaPath));

            while (directoriesToProcess.Count > 0)
            {
                var (currentSource, currentReplica) = directoriesToProcess.Pop();

                try
                {
                    ProcessFilesInDirectory(currentSource, currentReplica, stats, useMd5Verification);

                    var sourceDirectories = Directory.EnumerateDirectories(currentSource);
                    foreach (var sourceSubDir in sourceDirectories)
                    {
                        var dirName = Path.GetFileName(sourceSubDir);
                        var replicaSubDir = Path.Combine(currentReplica, dirName);

                        if (!Directory.Exists(replicaSubDir))
                        {
                            Directory.CreateDirectory(replicaSubDir);
                            _logger.LogInfo($"Created directory: {replicaSubDir}");
                            stats.DirectoriesCreated++;
                        }

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
        /// <param name="useMd5Verification">Whether to use MD5 for file comparison</param>
        private void ProcessFilesInDirectory(string sourceDir, string replicaDir, SyncStats stats, bool useMd5Verification)
        {
            var sourceFiles = Directory.EnumerateFiles(sourceDir);
            
            foreach (var sourceFile in sourceFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(sourceFile);
                    var replicaFile = Path.Combine(replicaDir, fileName);

                    if (ShouldCopyFile(sourceFile, replicaFile, useMd5Verification))
                    {
                        bool fileExisted = File.Exists(replicaFile);
                        
                        if (fileExisted && File.GetAttributes(replicaFile).HasFlag(FileAttributes.ReadOnly))
                        {
                            File.SetAttributes(replicaFile, FileAttributes.Normal);
                        }
                        
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
        /// Determine if a file should be copied based on existence, size, timestamp, and optionally MD5
        /// </summary>
        /// <param name="sourceFile">Source file path</param>
        /// <param name="replicaFile">Replica file path</param>
        /// <param name="useMd5Verification">Whether to use MD5 for comparison</param>
        /// <returns>True if file should be copied</returns>
        private bool ShouldCopyFile(string sourceFile, string replicaFile, bool useMd5Verification)
        {
            try
            {
                if (!File.Exists(replicaFile))
                {
                    return true;
                }

                var sourceInfo = new FileInfo(sourceFile);
                var replicaInfo = new FileInfo(replicaFile);

                bool sizeOrTimeChanged = sourceInfo.Length != replicaInfo.Length ||
                                       sourceInfo.LastWriteTime != replicaInfo.LastWriteTime;

                if (useMd5Verification && !sizeOrTimeChanged)
                {
                    return !FilesHaveSameMd5Hash(sourceFile, replicaFile);
                }

                return sizeOrTimeChanged;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error comparing files {sourceFile} and {replicaFile}: {ex.Message}");
                return true;
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
                RemoveDeletedFiles(sourcePath, replicaPath, stats);
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
            var directoriesToCheck = new Stack<(string source, string replica)>();
            directoriesToCheck.Push((sourcePath, replicaPath));

            while (directoriesToCheck.Count > 0)
            {
                var (currentSource, currentReplica) = directoriesToCheck.Pop();

                try
                {
                    if (!Directory.Exists(currentReplica))
                        continue;

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
                        RemoveDeletedDirectories(sourceDir, replicaDir, stats);
                    }
                }

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

        /// <summary>
        /// Compare two files using MD5 hash
        /// </summary>
        /// <param name="file1Path">Path to first file</param>
        /// <param name="file2Path">Path to second file</param>
        /// <returns>True if files have the same MD5 hash</returns>
        private bool FilesHaveSameMd5Hash(string file1Path, string file2Path)
        {
            try
            {
                using var md5 = MD5.Create();
                
                byte[] hash1, hash2;
                
                using (var stream1 = File.OpenRead(file1Path))
                {
                    hash1 = md5.ComputeHash(stream1);
                }
                
                using (var stream2 = File.OpenRead(file2Path))
                {
                    hash2 = md5.ComputeHash(stream2);
                }

                return hash1.SequenceEqual(hash2);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error computing MD5 hash for {file1Path} and {file2Path}: {ex.Message}");
                return false;
            }
        }
    }
}
