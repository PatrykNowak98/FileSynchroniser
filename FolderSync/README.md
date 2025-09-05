# FolderSync

A robust command-line folder synchronization tool that performs one-way synchronization from a source directory to a replica directory.

## Features

- **One-way synchronization** from source to replica
- **Periodic synchronization** with configurable intervals
- **Complete file management**: copies new files, updates modified files, deletes removed files
- **Directory structure management**: creates and removes directories as needed
- **Advanced file comparison**: supports both timestamp/size and MD5 hash verification
- **Professional CLI**: built with robust argument parsing and validation
- **Comprehensive logging** to both console and configurable file
- **Cross-platform compatibility** with proper path handling
- **Graceful error handling** and shutdown (Ctrl+C support)
- **Unit tested** with comprehensive test coverage

## Usage

```bash
FolderSync --source <source_path> --replica <replica_path> [options]
```

### Arguments

| Argument | Short | Description | Required | Default |
|----------|-------|-------------|----------|---------|
| `--source` | `-s` | Source directory path | ✅ | - |
| `--replica` | `-r` | Replica directory path | ✅ | - |
| `--interval` | `-i` | Sync interval in seconds (0 = run once) | ❌ | 0 |
| `--log` | `-l` | Log file path | ❌ | sync.log |
| `--use-md5` | | Use MD5 hash for deeper file verification | ❌ | false |

### Examples

**One-time sync:**
```bash
FolderSync --source "C:\MyDocuments" --replica "D:\Backup"
# Or using short aliases:
FolderSync -s "C:\MyDocuments" -r "D:\Backup"
```

**Periodic sync with custom settings:**
```bash
FolderSync --source "C:\Projects" --replica "D:\Backup\Projects" --interval 300 --log "project_sync.log"
```

**High-accuracy sync with MD5 verification:**
```bash
FolderSync -s "C:\Important" -r "D:\Critical_Backup" --use-md5 --log "C:\Logs\critical.log"
```

**Help and usage information:**
```bash
FolderSync --help
```

## Installation & Setup

### Prerequisites
- .NET 9.0 Runtime or SDK
- Windows, Linux, or macOS

### Download & Run
1. Download the latest release from the repository
2. Extract to your preferred directory
3. Run from command line:
   ```bash
   dotnet FolderSync.dll --source "path/to/source" --replica "path/to/replica"
   ```

### Build from Source
1. Clone the repository
2. Navigate to project directory: `cd FolderSync`
3. Build: `dotnet build --configuration Release`
4. Run: `dotnet run -- --source "path/to/source" --replica "path/to/replica"`

## Technical Details

### File Support
- **Universal file type support**: Works with any file type (documents, media, archives, executables, databases, etc.)
- **Binary-safe operations**: Treats all files as binary data for exact byte-for-byte copying
- **No file size limitations**: Handles files from 0 bytes to multi-gigabyte files
- **Preserves file attributes**: Maintains original timestamps and handles read-only files
- **Extension agnostic**: Works with files that have no extension or custom extensions

### File Comparison Modes
1. **Standard Mode** (default): Compares file size and last modified timestamp
2. **MD5 Mode** (`--use-md5`): Additional MD5 hash comparison for bit-perfect accuracy

### Architecture
- **Iterative directory traversal** using stack-based DFS to avoid recursion issues
- **Thread-safe logging** with consistent timestamp formatting
- **Cross-platform path operations** using proper normalization
- **Asynchronous execution** with PeriodicTimer for efficient periodic operations
- **Graceful shutdown handling** with CancellationToken support

### Error Handling
- Comprehensive error handling around all file operations
- Continues synchronization even when individual files fail
- Detailed error logging with timestamps
- Graceful handling of permissions, locked files, and network issues

### Performance Considerations
- Uses efficient directory enumeration for better memory usage
- Stack-based traversal prevents issues with deep directory structures
- Optional MD5 verification only when requested (slower but more accurate)
- Efficient batch operations for directory creation and deletion

## Common Use Cases

### Personal Backup
```bash
# Daily document backup
FolderSync -s "C:\Users\YourName\Documents" -r "D:\Backups\Documents" -i 86400
```

### Development Projects
```bash
# Continuous project sync to external drive
FolderSync -s "C:\Dev\MyProject" -r "E:\ProjectBackup" -i 300 --use-md5
```

### Media Libraries
```bash
# Photo library synchronization
FolderSync -s "C:\Photos" -r "\\NAS\PhotoBackup" -l "photo_sync.log"
```

### Server Backups
```bash
# Critical file backup with verification
FolderSync -s "C:\ImportantData" -r "D:\CriticalBackup" --use-md5 -l "C:\Logs\backup.log"
```

## Logging

All operations are logged with timestamps in the format:
```
[2025-09-04 14:48:49] INFO: Copied file: D:\Backup\document.txt
[2025-09-04 14:48:49] INFO: Deleted file: D:\Backup\old_file.txt
[2025-09-04 14:48:49] INFO: Synchronization completed: 2 files copied, 1 files deleted
```

Log files are automatically created if they don't exist, and log directories are created as needed.

## Troubleshooting

### Common Issues

**Permission Errors:**
- Run as administrator if accessing system directories
- Check file/folder permissions on both source and replica
- Ensure antivirus is not blocking file operations

**Performance Issues:**
- Use standard mode instead of MD5 for faster sync
- Consider longer intervals for large directories
- Check available disk space and network connection

**File Lock Errors:**
- Application continues with other files when individual files are locked
- Check logs for specific error details
- Close applications that might be using files

### Getting Help
- Use `FolderSync --help` for quick reference
- Check log files for detailed error information
- Ensure paths are correct and accessible

## License

This project is created for educational and professional use, demonstrating best practices in software development including CLI design, testing, error handling, and documentation.
