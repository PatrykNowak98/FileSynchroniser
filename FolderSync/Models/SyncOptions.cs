namespace FolderSync
{
    /// <summary>
    /// Configuration options for synchronization
    /// </summary>
    public class SyncOptions
    {
        /// <summary>
        /// Path to the source directory
        /// </summary>
        public required string SourcePath { get; set; }

        /// <summary>
        /// Path to the replica directory
        /// </summary>
        public required string ReplicaPath { get; set; }

        /// <summary>
        /// Synchronization interval in seconds
        /// </summary>
        public int IntervalSeconds { get; set; } = 0;

        /// <summary>
        /// Log file path
        /// </summary>
        public string LogPath { get; set; } = "sync.log";
    }
}
