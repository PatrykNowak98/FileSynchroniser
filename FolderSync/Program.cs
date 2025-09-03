using System;
using System.IO;

namespace FolderSync
{
    /// <summary>
    /// Main entry point for the FolderSync application.
    /// Performs one-way synchronization from source to replica folder.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Parse command line arguments
                var options = ParseArguments(args);
                if (options == null)
                {
                    ShowUsage();
                    return;
                }

                // Initialize logger with hardcoded log file for now
                var logger = new Logger("sync.log");
                
                logger.LogInfo("=== FolderSync Started ===");
                logger.LogInfo($"Source: {options.SourcePath}");
                logger.LogInfo($"Replica: {options.ReplicaPath}");

                // Initialize and run sync service
                var syncService = new SyncService(logger);
                var stats = syncService.SynchronizeOnce(options.SourcePath, options.ReplicaPath);

                // Print summary
                PrintSummary(stats, logger);
                
                logger.LogInfo("=== FolderSync Completed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Parse command line arguments for --source and --replica paths
        /// </summary>
        private static SyncOptions? ParseArguments(string[] args)
        {
            string sourcePath = " ";
            string replicaPath = " ";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--source" && i + 1 < args.Length)
                {
                    sourcePath = args[i + 1];
                    i++; // Skip next argument as it's the value
                }
                else if (args[i] == "--replica" && i + 1 < args.Length)
                {
                    replicaPath = args[i + 1];
                    i++; // Skip next argument as it's the value
                }
            }

            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(replicaPath))
            {
                return null;
            }

            return new SyncOptions
            {
                SourcePath = Path.GetFullPath(sourcePath),
                ReplicaPath = Path.GetFullPath(replicaPath)
            };
        }

        /// <summary>
        /// Display usage information
        /// </summary>
        private static void ShowUsage()
        {
            Console.WriteLine("FolderSync - One-way folder synchronization tool");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  FolderSync --source <path> --replica <path>");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --source   Source directory path");
            Console.WriteLine("  --replica  Replica directory path");
        }

        /// <summary>
        /// Print synchronization summary
        /// </summary>
        private static void PrintSummary(SyncStats stats, Logger logger)
        {
            var summary = $"Synchronization completed: " +
                         $"{stats.FilesCopied} files copied, " +
                         $"{stats.FilesUpdated} files updated, " +
                         $"{stats.DirectoriesCreated} directories created";
            
            Console.WriteLine(summary);
            logger.LogInfo(summary);
        }
    }
}
