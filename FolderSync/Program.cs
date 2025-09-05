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
        static async Task<int> Main(string[] args)
        {
            try
            {
                var options = ParseArguments(args);
                if (options == null)
                {
                    ShowUsage();
                    return 1;
                }

                if (!await ValidateOptions(options))
                {
                    return 1;
                }

                var logger = new Logger(options.LogPath);
                
                logger.LogInfo("=== FolderSync Started ===");
                logger.LogInfo($"Source: {options.SourcePath}");
                logger.LogInfo($"Replica: {options.ReplicaPath}");
                logger.LogInfo($"Interval: {options.IntervalSeconds} seconds");
                logger.LogInfo($"Log file: {options.LogPath}");
                logger.LogInfo($"MD5 verification: {options.UseMd5Verification}");

                var syncService = new SyncService(logger);

                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) => {
                    e.Cancel = true;
                    cts.Cancel();
                    logger.LogInfo("Shutdown requested by user...");
                };

                if (options.IntervalSeconds > 0)
                {
                    await RunPeriodicSync(syncService, options, cts.Token);
                }
                else
                {
                    var stats = await syncService.SynchronizeAsync(options.SourcePath, options.ReplicaPath, options.UseMd5Verification);
                    PrintSummary(stats, logger);
                }
                
                logger.LogInfo("=== FolderSync Completed ===");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Parse command line arguments with improved error handling and validation
        /// </summary>
        private static SyncOptions? ParseArguments(string[] args)
        {
            if (args.Length == 0)
                return null;

            if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "/?"))
            {
                return null;
            }

            string? sourcePath = null;
            string? replicaPath = null;
            int intervalSeconds = 0;
            string logPath = "sync.log";
            bool useMd5 = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--source" or "-s":
                        if (i + 1 < args.Length)
                        {
                            sourcePath = args[++i];
                        }
                        break;

                    case "--replica" or "-r":
                        if (i + 1 < args.Length)
                        {
                            replicaPath = args[++i];
                        }
                        break;

                    case "--interval" or "-i":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int interval))
                        {
                            intervalSeconds = Math.Max(0, interval);
                            i++;
                        }
                        break;

                    case "--log" or "-l":
                        if (i + 1 < args.Length)
                        {
                            logPath = args[++i];
                        }
                        break;

                    case "--use-md5":
                        useMd5 = true;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(replicaPath))
            {
                Console.WriteLine("Error: Both --source and --replica arguments are required.");
                return null;
            }

            return new SyncOptions
            {
                SourcePath = sourcePath.Trim('"'),
                ReplicaPath = replicaPath.Trim('"'),
                IntervalSeconds = intervalSeconds,
                LogPath = logPath.Trim('"'),
                UseMd5Verification = useMd5
            };
        }

        /// <summary>
        /// Validate options and create necessary directories
        /// </summary>
        private static async Task<bool> ValidateOptions(SyncOptions options)
        {
            try
            {
                if (!Directory.Exists(options.SourcePath))
                {
                    Console.WriteLine($"Error: Source directory does not exist: {options.SourcePath}");
                    return false;
                }

                options.SourcePath = Path.GetFullPath(options.SourcePath);
                options.ReplicaPath = Path.GetFullPath(options.ReplicaPath);
                options.LogPath = Path.GetFullPath(options.LogPath);

                var logDir = Path.GetDirectoryName(options.LogPath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating options: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Display comprehensive usage information
        /// </summary>
        private static void ShowUsage()
        {
            Console.WriteLine("FolderSync - Professional one-way folder synchronization tool");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  FolderSync --source <path> --replica <path> [options]");
            Console.WriteLine();
            Console.WriteLine("REQUIRED ARGUMENTS:");
            Console.WriteLine("  --source, -s <path>     Source directory path");
            Console.WriteLine("  --replica, -r <path>    Replica directory path");
            Console.WriteLine();
            Console.WriteLine("OPTIONAL ARGUMENTS:");
            Console.WriteLine("  --interval, -i <sec>    Sync interval in seconds (default: 0 = run once)");
            Console.WriteLine("  --log, -l <path>        Log file path (default: sync.log)");
            Console.WriteLine("  --use-md5               Enable MD5 hash verification for file comparison");
            Console.WriteLine("  --help, -h              Show this help information");
            Console.WriteLine();
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("  # One-time synchronization");
            Console.WriteLine("  FolderSync --source \"C:\\Documents\" --replica \"D:\\Backup\"");
            Console.WriteLine();
            Console.WriteLine("  # Periodic sync every 5 minutes with custom log");
            Console.WriteLine("  FolderSync -s \"C:\\Projects\" -r \"D:\\ProjectBackup\" -i 300 -l \"backup.log\"");
            Console.WriteLine();
            Console.WriteLine("  # High-accuracy sync with MD5 verification");
            Console.WriteLine("  FolderSync -s \"C:\\Important\" -r \"E:\\Critical\" --use-md5");
            Console.WriteLine();
            Console.WriteLine("FEATURES:");
            Console.WriteLine("  • Copies new and modified files from source to replica");
            Console.WriteLine("  • Removes files and directories deleted from source");
            Console.WriteLine("  • Creates directory structure as needed");
            Console.WriteLine("  • Comprehensive logging with timestamps");
            Console.WriteLine("  • Graceful shutdown with Ctrl+C");
            Console.WriteLine("  • Cross-platform path handling");
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

                var stats = await syncService.SynchronizeAsync(options.SourcePath, options.ReplicaPath, options.UseMd5Verification);
                PrintSummary(stats, logger);

                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    stats = await syncService.SynchronizeAsync(options.SourcePath, options.ReplicaPath, options.UseMd5Verification);
                    PrintSummary(stats, logger);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInfo("Periodic sync stopped gracefully");
            }
        }

        /// <summary>
        /// Print synchronization summary with enhanced formatting
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
