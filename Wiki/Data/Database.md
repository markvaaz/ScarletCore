# Database (Structured storage with LiteDB)

`Database` provides type-safe, structured storage using LiteDB — an embedded document database. Best for complex data, relationships, and when you need query capabilities.

## Overview

Each plugin gets its own database file stored in `BepInEx/config/<PluginGuid>/`. The database handles serialization, type safety, and circular reference detection automatically.

```csharp
var db = new Database("MyPluginGuid");
```

## Basic Operations

### Save data
```csharp
var player = new PlayerProfile { Name = "Alice", Level = 42 };
db.Set("players/alice", player);
```

### Retrieve data
```csharp
var player = db.Get<PlayerProfile>("players/alice");
if (player != null) {
  // Use player data
}
```

### Check if key exists
```csharp
if (db.Has("players/alice")) {
  // Key exists
}
```

### Get or create
If data doesn't exist, create it using a factory function:

```csharp
var settings = db.GetOrCreate("config/game", () => new GameSettings());
```

Or with default constructor:

```csharp
var settings = db.GetOrCreate<GameSettings>("config/game");
```

### Delete data
```csharp
bool deleted = db.Delete("players/alice");
```

### Get database statistics
```csharp
int totalEntries = db.Count();
string[] allKeys = db.GetAllKeys();
```

## Querying Data

### Query by key pattern
Filter entries using LINQ expressions on keys:

```csharp
// Get all players (keys starting with "players/")
var players = db.Query<PlayerProfile>(x => x.StartsWith("players/"));

// Get players with specific pattern
var activePlayers = db.Query<PlayerProfile>(x => x.StartsWith("players/") && x.Contains("active"));
```

### Query with keys
Get both keys and values in query results:

```csharp
var playerData = db.QueryWithKeys<PlayerProfile>(x => x.StartsWith("players/"));
foreach (var kvp in playerData) {
  Log.Message($"Key: {kvp.Key}, Player: {kvp.Value.Name}");
}
```

### Get all data
Retrieve all entries of a specific type:

```csharp
// Get all values
List<PlayerProfile> allPlayers = db.GetAll<PlayerProfile>();

// Get all with keys
Dictionary<string, PlayerProfile> allWithKeys = db.GetAllWithKeys<PlayerProfile>();
```

### Get by prefix
Convenient method for prefix-based queries:

```csharp
// Get all keys with prefix
string[] playerKeys = db.GetKeysByPrefix("players/");

// Get all data with prefix
Dictionary<string, PlayerProfile> players = db.GetAllByPrefix<PlayerProfile>("players/");
```

## Advanced Features

### Circular reference detection
The system automatically detects and prevents circular references:

```csharp
var player = new Player { Name = "Bob" };
player.SelfReference = player;  // Circular!
db.Set("players/bob", player);  // Throws InvalidOperationException with path info
```

Error message will indicate the exact path where the circular reference was detected.

### Data persistence
Ensure all pending writes are committed to disk:

```csharp
db.Checkpoint();
```

### Clear entire database
⚠️ **Warning:** This deletes all data permanently!

```csharp
db.Clear();
```

## Backups

### Enable automatic backups
Backups are triggered automatically on server save events:

```csharp
db.EnableAutoBackup();

// Or specify custom backup location
db.EnableAutoBackup("/custom/backup/path");
```

### Configure backup retention
Control how many backup copies to keep (default: 50):

```csharp
db.MaxBackups = 10;  // Keep only 10 recent backups
```

Older backups beyond this limit are automatically deleted.

### Manual backup
Create a backup on demand:

```csharp
string backupPath = await db.CreateBackup();
Log.Message($"Backup created at: {backupPath}");

// With custom location and save name
string customBackup = await db.CreateBackup("/my/backups", "manual-backup");
```

### Restore from backup
⚠️ **Warning:** Requires server restart to take effect!

```csharp
bool success = await db.RestoreFromBackup("/path/to/backup.db");
if (success) {
  Log.Message("Database restored. Restart required.");
}
```

### Disable automatic backups
```csharp
db.DisableAutoBackup();
```

## Cleanup

### Proper disposal
The database implements `IDisposable`:

```csharp
using (var db = new Database("MyPluginGuid")) {
  // Use database
} // Automatically disposed

// Or manually
db.Dispose();
```

### Plugin unload
When your plugin is unloaded:

```csharp
db.UnregisterAssembly();
```

This disables auto-backup and properly disposes resources.

## When to use Database

✅ **Use Database when:**
- You have complex data structures with relationships
- You need to query or filter data efficiently
- You want type-safe, validated storage
- Performance with large datasets matters
- You need automatic backup support
- Data doesn't need to be human-readable

❌ **Don't use Database when:**
- Data is simple key-value pairs (consider `JsonDatabase`)
- You want human-readable file format (use `JsonDatabase`)
- Users need to edit files manually (use `Settings`)
- Data needs to be shared across mods (use `SharedDatabase`)

## Complete Example

```csharp
public class MyPlugin : BasePlugin {
  private Database _db;

  public override void Load() {
    _db = new Database(MyPluginInfo.PLUGIN_GUID);
    _db.EnableAutoBackup();
    _db.MaxBackups = 20;

    // Initialize default data
    var config = _db.GetOrCreate("config", () => new PluginConfig {
      EnableFeature = true,
      MaxPlayers = 10
    });

    // Store player data
    _db.Set("players/user123", new PlayerData {
      Name = "Alice",
      Level = 50,
      LastSeen = DateTime.Now
    });

    // Query active players
    var activePlayers = _db.Query<PlayerData>(x => 
      x.StartsWith("players/") && 
      x.Contains("active")
    );

    Log.Message($"Found {activePlayers.Count} active players");
  }

  public override bool Unload() {
    _db?.UnregisterAssembly();
    return true;
  }
}
```