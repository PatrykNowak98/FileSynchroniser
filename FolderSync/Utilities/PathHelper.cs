using System;
using System.IO;

namespace FolderSync.Utilities
{
    /// <summary>
    /// Helper class for path operations
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Validate that a path exists and is accessible
        /// </summary>
        /// <param name="path">Path to validate</param>
        /// <returns>True if path is valid and accessible</returns>
        public static bool IsValidPath(string path)
        {
            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get relative path from source to target
        /// </summary>
        /// <param name="sourcePath">Source directory path</param>
        /// <param name="targetPath">Target file/directory path</param>
        /// <returns>Relative path</returns>
        public static string GetRelativePath(string sourcePath, string targetPath)
        {
            var sourceUri = new Uri(Path.GetFullPath(sourcePath) + Path.DirectorySeparatorChar);
            var targetUri = new Uri(Path.GetFullPath(targetPath));
            
            return Uri.UnescapeDataString(sourceUri.MakeRelativeUri(targetUri).ToString())
                      .Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Safely combine path components
        /// </summary>
        /// <param name="paths">Path components to combine</param>
        /// <returns>Combined path</returns>
        public static string SafeCombine(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return string.Empty;

            string result = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                result = Path.Combine(result, paths[i]);
            }
            return result;
        }
    }
}
