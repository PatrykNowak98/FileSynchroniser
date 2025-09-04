using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FolderSync
{
    /// <summary>
    /// Main entry point for the FolderSync application.
    /// Performs one-way synchronization from source to replica folder.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
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

                // Initialize logger with configurable log file path
                var logger = new Logger(options.LogPath);
                
                logger.LogInfo("=== FolderSync Started ===");
                logger.LogInfo($"Source: {options.SourcePath}");
                logger.LogInfo($"Replica: {options.ReplicaPath}");

                logger.LogInfo($"Interval: {options.IntervalSeconds} seconds");
                logger.LogInfo($"Log file: {options.LogPath}");

                // Initialize sync service
                var syncService = new SyncService(logger);

                // Set up cancellation token for graceful shutdown (Ctrl+C)
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) => {
                    e.Cancel = true;
                    cts.Cancel();
                    logger.LogInfo("Shutdown requested by user...");
                };

                if (options.IntervalSeconds > 0)
                {
                    // Run in periodic mode
                    await RunPeriodicSync(syncService, options, cts.Token);
                }
                else
                {
                    // Run once
                    var stats = await syncService.SynchronizeAsync(options.SourcePath, options.ReplicaPath);
                    PrintSummary(stats, logger);
                }
                
                logger.LogInfo("=== FolderSync Completed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Run periodic synchronization with specified interval
        /// </summary>
        private static async Task RunPeriodicSync(SyncService syncService, SyncOptions options, CancellationToken cancellationToken)
        {
            var logger = new Logger(options.LogPath);
            logger.LogInfo($"Starting periodic sync every {options.IntervalSeconds} seconds");

            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.IntervalSeconds));

                // Run first sync immediately
                var stats = await syncService.SynchronizeAsync(options.SourcePath, options.ReplicaPath);
                PrintSummary(stats, logger);

                // Then run periodically
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    stats = await syncService.SynchronizeAsync(options.SourcePath, options.ReplicaPath);
                    PrintSummary(stats, logger);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInfo("Periodic sync stopped by user");
            }
        }

        /// <summary>
        /// Parse command line arguments for --source, --replica, --interval, and --log paths
        /// </summary>
        private static SyncOptions? ParseArguments(string[] args)
        {
            string? sourcePath = null;
            string? replicaPath = null;
            int intervalSeconds = 0; // 0 means run once
            string logPath = "sync.log";

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
                else if (args[i] == "--interval" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int interval) && interval > 0)
                    {
                        intervalSeconds = interval;
                    }
                    i++; // Skip next argument as it's the value
                }
                else if (args[i] == "--log" && i + 1 < args.Length)
                {
                    logPath = args[i + 1];
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
                ReplicaPath = Path.GetFullPath(replicaPath),
                IntervalSeconds = intervalSeconds,
                LogPath = logPath
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
            Console.WriteLine("  FolderSync --source <path> --replica <path> [--interval <seconds>] [--log <path>]");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --source     Source directory path");
            Console.WriteLine("  --replica    Replica directory path");
            Console.WriteLine("  --interval   Sync interval in seconds (optional, if not specified runs once)");
            Console.WriteLine("  --log        Log file path (optional, default: sync.log)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  FolderSync --source \"C:\\Source\" --replica \"D:\\Replica\"");
            Console.WriteLine("  FolderSync --source \"C:\\Source\" --replica \"D:\\Replica\" --interval 30 --log \"custom.log\"");
        }

        /// <summary>
        /// Print synchronization summary
        /// </summary>
        private static void PrintSummary(SyncStats stats, Logger logger)
        {
            var summary = $"Synchronization completed: " +
                         $"{stats.FilesCopied} files copied, " +
                         $"{stats.FilesUpdated} files updated, " +
                         $"{stats.FilesDeleted} files deleted, " +
                         $"{stats.DirectoriesCreated} directories created, " +
                         $"{stats.DirectoriesDeleted} directories deleted";
            
            if (stats.ErrorsEncountered > 0)
            {
                summary += $", {stats.ErrorsEncountered} errors encountered";
            }
            
            Console.WriteLine(summary);
            logger.LogInfo(summary);
        }
    }
}
