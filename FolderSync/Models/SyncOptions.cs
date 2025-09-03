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
    }
}
