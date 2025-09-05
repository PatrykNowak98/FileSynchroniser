using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FolderSync.Tests
{
    /// <summary>
    /// Unit tests for FolderSync synchronization behavior
    /// </summary>
    public class SyncServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _sourceDir;
        private readonly string _replicaDir;
        private readonly Logger _logger;
        private readonly SyncService _syncService;

        public SyncServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"FolderSyncTest_{Guid.NewGuid()}");
            _sourceDir = Path.Combine(_tempDir, "Source");
            _replicaDir = Path.Combine(_tempDir, "Replica");
            
            Directory.CreateDirectory(_sourceDir);
            Directory.CreateDirectory(_replicaDir);

            var logPath = Path.Combine(_tempDir, "test.log");
            _logger = new Logger(logPath);
            _syncService = new SyncService(_logger);
        }

        [Fact]
        public async Task Sync_Should_Copy_NewFile_To_Replica()
        {
            var sourceFile = Path.Combine(_sourceDir, "newfile.txt");
            var replicaFile = Path.Combine(_replicaDir, "newfile.txt");
            await File.WriteAllTextAsync(sourceFile, "Test content");

            var stats = await _syncService.SynchronizeAsync(_sourceDir, _replicaDir);

            Assert.True(File.Exists(replicaFile));
            Assert.Equal("Test content", await File.ReadAllTextAsync(replicaFile));
            Assert.Equal(1, stats.FilesCopied);
            Assert.Equal(0, stats.FilesUpdated);
        }

        [Fact]
        public async Task Sync_Should_Update_ModifiedFile()
        {
            var sourceFile = Path.Combine(_sourceDir, "modified.txt");
            var replicaFile = Path.Combine(_replicaDir, "modified.txt");
            
            await File.WriteAllTextAsync(sourceFile, "Original content");
            await File.WriteAllTextAsync(replicaFile, "Original content");
            
            await Task.Delay(10);
            
            await File.WriteAllTextAsync(sourceFile, "Modified content");

            var stats = await _syncService.SynchronizeAsync(_sourceDir, _replicaDir);

            Assert.Equal("Modified content", await File.ReadAllTextAsync(replicaFile));
            Assert.Equal(0, stats.FilesCopied);
            Assert.Equal(1, stats.FilesUpdated);
        }

        [Fact]
        public async Task Sync_Should_Delete_RemovedFile()
        {
            var replicaFile = Path.Combine(_replicaDir, "to_delete.txt");
            await File.WriteAllTextAsync(replicaFile, "Content to delete");

            var stats = await _syncService.SynchronizeAsync(_sourceDir, _replicaDir);

            Assert.False(File.Exists(replicaFile));
            Assert.Equal(1, stats.FilesDeleted);
        }

        [Fact]
        public async Task Sync_Should_Handle_ReadOnlyFile_Overwrite()
        {
            var sourceFile = Path.Combine(_sourceDir, "readonly.txt");
            var replicaFile = Path.Combine(_replicaDir, "readonly.txt");
            
            await File.WriteAllTextAsync(replicaFile, "Old content");
            await Task.Delay(10);
            await File.WriteAllTextAsync(sourceFile, "New content");
            File.SetAttributes(replicaFile, FileAttributes.ReadOnly);

            var stats = await _syncService.SynchronizeAsync(_sourceDir, _replicaDir);

            Assert.Equal("New content", await File.ReadAllTextAsync(replicaFile));
            Assert.Equal(1, stats.FilesUpdated);
        }

        [Fact]
        public async Task Sync_Should_Create_Directory_Structure()
        {
            var sourceSubDir = Path.Combine(_sourceDir, "SubDirectory");
            var sourceNestedDir = Path.Combine(sourceSubDir, "Nested");
            Directory.CreateDirectory(sourceNestedDir);
            
            var sourceFile = Path.Combine(sourceNestedDir, "nested_file.txt");
            await File.WriteAllTextAsync(sourceFile, "Nested content");

            var expectedReplicaDir = Path.Combine(_replicaDir, "SubDirectory", "Nested");
            var expectedReplicaFile = Path.Combine(expectedReplicaDir, "nested_file.txt");

            var stats = await _syncService.SynchronizeAsync(_sourceDir, _replicaDir);

            Assert.True(Directory.Exists(expectedReplicaDir));
            Assert.True(File.Exists(expectedReplicaFile));
            Assert.Equal("Nested content", await File.ReadAllTextAsync(expectedReplicaFile));
            Assert.Equal(2, stats.DirectoriesCreated);
            Assert.Equal(1, stats.FilesCopied);
        }

        [Fact]
        public async Task Sync_Should_Delete_Empty_Directories()
        {
            var replicaSubDir = Path.Combine(_replicaDir, "EmptyDir");
            Directory.CreateDirectory(replicaSubDir);

            var stats = await _syncService.SynchronizeAsync(_sourceDir, _replicaDir);

            Assert.False(Directory.Exists(replicaSubDir));
            Assert.Equal(1, stats.DirectoriesDeleted);
        }

        [Fact]
        public async Task Sync_With_MD5_Should_Detect_Content_Changes()
        {
            var sourceFile = Path.Combine(_sourceDir, "md5test.txt");
            var replicaFile = Path.Combine(_replicaDir, "md5test.txt");
            
            await File.WriteAllTextAsync(sourceFile, "Original content");
            await File.WriteAllTextAsync(replicaFile, "Original content");
            
            var originalInfo = new FileInfo(sourceFile);
            var replicaInfo = new FileInfo(replicaFile);
            
            await File.WriteAllTextAsync(sourceFile, "Changed content!");
            File.SetLastWriteTime(sourceFile, originalInfo.LastWriteTime);
            File.SetLastWriteTime(replicaFile, replicaInfo.LastWriteTime);

            var stats = await _syncService.SynchronizeAsync(_sourceDir, _replicaDir, useMd5Verification: true);

            Assert.Equal("Changed content!", await File.ReadAllTextAsync(replicaFile));
            Assert.Equal(1, stats.FilesUpdated);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                foreach (var file in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(_tempDir, true);
            }
        }
    }
}
