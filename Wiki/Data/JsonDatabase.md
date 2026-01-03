# JsonDatabase (Human-readable JSON storage)

`JsonDatabase` provides JSON-based persistent storage with automatic caching. Best for configuration files, player data, and scenarios where human readability and manual editing are important.

## Overview

Each plugin gets its own database folder stored in `BepInEx/config/<PluginGuid>/`. Files are stored as formatted JSON, making them easy to read and edit manually.

```csharp
var db = new JsonDatabase("MyPluginGuid");
```

## Basic Operations

### Save data
```csharp
var config = new GameConfig { MaxPlayers = 10, EnablePvP = true };
db.Save("config", config);
// Creates: BepInEx/config/MyPluginGuid/config.json
```

### Load data
```csharp
var config = db.Load<GameConfig>("config");
if (config != null) {
  // Use config data
}
```

### Get with automatic caching
The cache is automatically updated when files change on disk:

```csharp
// First call loads from file and caches
var data = db.Get<PlayerData>("players/alice");

// Subsequent calls return from cache (unless file changed)
var cached = db.Get<PlayerData>("players/alice"); // Fast!
```

### Get or create
If data doesn't exist, create it:

```csharp
var settings = db.GetOrCreate("settings", () => new GameSettings {
  Difficulty = "Normal",
  Volume = 0.8f
});

// Or with default constructor
var defaults = db.GetOrCreate<GameSettings>("settings");
```

### Check if exists
```csharp
if (db.Has("players/alice")) {
  // File exists
}

// Also works for directories
if (db.Has("players")) {
  // Directory exists
}
```

### Delete data
```csharp
bool deleted = db.Delete("players/alice");
// Also removes from cache
```

## File Organization

### Using folders
Organize data with folder structures:

```csharp
// Save to folders
db.Save("players/alice", playerData);
db.Save("players/bob", otherPlayerData);
db.Save("guilds/dragons", guildData);
```

### List files in folder
```csharp
// Get all files in root
string[] allFiles = db.GetFilesInFolder();

// Get files in specific folder
string[] players = db.GetFilesInFolder("players");

// Include subdirectories
string[] allPlayers = db.GetFilesInFolder("players", includeSubdirectories: true);

// Returns paths like: ["players/alice", "players/bob"]
```

### List directories
```csharp
// Get all directories in root
string[] folders = db.GetDirectoriesInFolder();

// Get subdirectories
string[] subFolders = db.GetDirectoriesInFolder("players", includeSubdirectories: true);
```

### Get full file path
```csharp
string fullPath = db.GetFullPath("config");
// Returns: "BepInEx/config/MyPluginGuid/config.json"
```

## Cache Management

### Clear cache
Force reload from disk on next access:

```csharp
// Clear specific file cache
db.ClearCache("players/alice");

// Clear all cache for this database
db.ClearCache();
```

### Save all cached data
Write all cached changes back to disk:

```csharp
db.SaveAll();
```

This is useful when making multiple in-memory changes before persisting.

## Backups

### Enable automatic backups
Backups are triggered on server save events:

```csharp
db.EnableAutoBackup();

// Or specify custom backup location
db.EnableAutoBackup("/custom/backup/path");
```

### Configure backup retention
Control how many backups to keep (default: 50):

```csharp
db.MaxBackups = 10; // Keep only 10 recent backups
```

Older backups are automatically deleted when the limit is exceeded.

### Manual backup
Create a backup ZIP archive on demand:

```csharp
string backupPath = await db.CreateBackup();
Log.Message($"Backup created at: {backupPath}");

// With custom location and save name
string customBackup = await db.CreateBackup("/my/backups", "manual-backup");
// Creates: /my/backups/MyPluginGuid Backups/MyPluginGuid_backup_manual-backup_2026-01-02_14-30-00.zip
```

### Restore from backup
Restore database from a backup ZIP file:

```csharp
// Restore, merging with existing files
bool success = await db.RestoreFromBackup("/path/to/backup.zip");

// Restore, clearing existing data first
bool success = await db.RestoreFromBackup("/path/to/backup.zip", clearExisting: true);

if (success) {
  Log.Message("Database restored successfully");
}
```

### Disable automatic backups
```csharp
db.DisableAutoBackup();
```

## Cleanup

### Plugin unload
When your plugin is unloaded:

```csharp
db.UnregisterAssembly();
```

This disables auto-backup and cleans up resources.

## Advanced Features

### JSON serialization options
The database uses `System.Text.Json` with these settings:
- **WriteIndented**: `true` (pretty-printed for readability)
- **ReferenceHandler**: `IgnoreCycles` (handles circular references safely)

### Automatic cache invalidation
The cache automatically detects when files are modified externally and reloads them:

```csharp
var data = db.Get<Config>("config");
// User manually edits config.json on disk
var updated = db.Get<Config>("config"); // Automatically reloads!
```

## When to use JsonDatabase

✅ **Use JsonDatabase when:**
- You want human-readable, editable configuration files
- Users need to manually edit data
- Data structure is relatively simple
- You need folder-based organization
- Caching and backup support are valuable
- Files need to be version controlled

❌ **Don't use JsonDatabase when:**
- You need complex queries (use `Database` with LiteDB)
- Performance with very large datasets is critical
- You don't need human readability
- Data needs to be shared across mods (use `SharedDatabase`)

## Complete Example

```csharp
public class MyPlugin : BasePlugin {
  private JsonDatabase _db;

  public override void Load() {
    _db = new JsonDatabase(MyPluginInfo.PLUGIN_GUID);
    _db.EnableAutoBackup();
    _db.MaxBackups = 20;

    // Load or create configuration
    var config = _db.GetOrCreate("config", () => new PluginConfig {
      EnableFeature = true,
      MaxPlayers = 10,
      ServerName = "My Server"
    });

    // Save player data with folder organization
    _db.Save("players/user123", new PlayerData {
      Name = "Alice",
      Level = 50,
      Gold = 1000
    });

    // List all player files
    var playerFiles = _db.GetFilesInFolder("players");
    Log.Message($"Found {playerFiles.Length} players");

    // Get cached data (fast)
    var alice = _db.Get<PlayerData>("players/user123");

    // Save all changes
    _db.SaveAll();
  }

  public override bool Unload() {
    _db?.UnregisterAssembly();
    return true;
  }
}
```

## Comparison with Database (LiteDB)

| Feature | JsonDatabase | Database (LiteDB) |
|---------|--------------|-------------------|
| Human-readable | ✅ Yes (JSON) | ❌ No (binary) |
| Manual editing | ✅ Easy | ❌ Not possible |
| Query support | ❌ No | ✅ Yes (LINQ) |
| Performance | 🟡 Good | ✅ Excellent |
| Large datasets | 🟡 Moderate | ✅ Excellent |
| Caching | ✅ Automatic | ❌ N/A |
| Folder organization | ✅ Yes | 🟡 Via key naming |
| Backups | ✅ ZIP archives | ✅ File copy |
