# FolderSync

A command-line folder synchronization tool that performs one-way synchronization from a source directory to a replica directory.

## Features

- One-way synchronization from source to replica
- Copies new files and updates modified files
- Creates necessary directory structure in replica
- Comprehensive logging to both console and file
- Cross-platform path handling
- Error handling

## Usage

```bash
FolderSync --source <source_path> --replica <replica_path>
```

### Arguments

- `--source`: Path to the source directory
- `--replica`: Path to the replica directory

### Example

```bash
FolderSync --source "C:\MyDocuments" --replica "D:\Backup\MyDocuments"
```

## Build Instructions

1. Ensure you have .NET SDK installed
2. Navigate to the project directory
3. Build the project:

```bash
dotnet build
```

4. Run the application:

```bash
dotnet run -- --source "path/to/source" --replica "path/to/replica"
```

## Development Status

- ✅ Day 1: Basic one-time sync functionality   

## Technical Details

- Uses iterative directory traversal (stack-based DFS) to avoid recursion issues
- Compares files by size and timestamp for efficiency
- Thread-safe logging with timestamp formatting
- Cross-platform path operations using Path.Combine
FolderSync --source <source_path> --replica <replica_path> [--interval <seconds>] [--log <path>]
```

### Arguments

- `--source`: Path to the source directory
- `--replica`: Path to the replica directory  
- `--interval`: Sync interval in seconds (optional, if not specified runs once)
- `--log`: Log file path (optional, default: sync.log)

### Examples

**One-time sync:**
```bash
FolderSync --source "C:\MyDocuments" --replica "D:\Backup\MyDocuments"
```

**Periodic sync every 30 seconds with custom log file:**
```bash
FolderSync --source "C:\MyDocuments" --replica "D:\Backup\MyDocuments" --interval 30 --log "backup.log"
```

## Build Instructions

1. Ensure you have .NET SDK installed
2. Navigate to the project directory
3. Build the project:

```bash
dotnet build
```

4. Run the application:

```bash
dotnet run -- --source "path/to/source" --replica "path/to/replica"
```

## Development Status

- ✅ Day 1: Basic one-time sync functionality
- ✅ Day 2: Periodic sync + deletion + enhanced CLI
- ⏳ Day 3: Robustness, testing, and polish

## Technical Details

- Uses iterative directory traversal (stack-based DFS) to avoid recursion issues
- Compares files by size and timestamp for efficiency
- Thread-safe logging with timestamp formatting
- Cross-platform path operations using Path.Combine
- Asynchronous periodic execution with PeriodicTimer
- Graceful shutdown handling with CancellationToken
- Complete replica synchronization (copies, updates, deletions)
