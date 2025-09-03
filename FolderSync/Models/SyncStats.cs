namespace FolderSync
{
    /// <summary>
    /// Statistics collected during synchronization process
    /// </summary>
    public class SyncStats
    {
        /// <summary>
        /// Number of files copied from source to replica
        /// </summary>
        public int FilesCopied { get; set; } = 0;

        /// <summary>
        /// Number of files updated in replica
        /// </summary>
        public int FilesUpdated { get; set; } = 0;

        /// <summary>
        /// Number of files deleted from replica
        /// </summary>
        public int FilesDeleted { get; set; } = 0;

        /// <summary>
        /// Number of directories created in replica
        /// </summary>
        public int DirectoriesCreated { get; set; } = 0;

        /// <summary>
        /// Number of directories deleted from replica
        /// </summary>
        public int DirectoriesDeleted { get; set; } = 0;

        /// <summary>
        /// Number of errors encountered during synchronization
        /// </summary>
        public int ErrorsEncountered { get; set; } = 0;

        /// <summary>
        /// Reset all statistics to zero
        /// </summary>
        public void Reset()
        {
            FilesCopied = 0;
            FilesUpdated = 0;
            FilesDeleted = 0;
            DirectoriesCreated = 0;
            DirectoriesDeleted = 0;
            ErrorsEncountered = 0;
        }
    }
}
