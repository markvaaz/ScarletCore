# SharedDatabase (Cross-mod data sharing)

`SharedDatabase` is a static, shared database accessible across all assemblies. It allows mods to share data without hard dependencies on each other.

## Overview

A single LiteDB database (`ScarletCore_SharedData`) is shared by all mods. Data is organized using namespaces: `"ModName/key"` format prevents collisions between different mods.

```csharp
// Save data
SharedDatabase.Set("MyMod", "players/alice", playerProfile);

// Retrieve data
var profile = SharedDatabase.Get<PlayerProfile>("MyMod", "players/alice");
```

## Basic Usage

### Set (save) data
```csharp
var config = new ServerConfig { MaxPlayers = 100 };
SharedDatabase.Set("MyModName", "config/server", config);
```

### Get (retrieve) data
```csharp
var config = SharedDatabase.Get<ServerConfig>("MyModName", "config/server");
if (config != null) {
  Log.Message($"Max players: {config.MaxPlayers}");
}
```

### Check if key exists
```csharp
if (SharedDatabase.Has("MyModName", "config/server")) {
  // Data exists
}
```

### Get or create
If data doesn't exist, create it:

```csharp
var players = SharedDatabase.GetOrCreate("MyModName", "players", () => {
  return new Dictionary<string, PlayerData>();
});
```

Or with default constructor:

```csharp
var cache = SharedDatabase.GetOrCreate<GameCache>("MyModName", "cache");
```

### Delete data
```csharp
SharedDatabase.Delete("MyModName", "key");
```

## Namespacing

### Two-part key structure
Every operation requires a namespace (first parameter):

```csharp
SharedDatabase.Set(
  "MyMod",           // Namespace — identifies your mod
  "players/alice",   // Key — identifies the data within your namespace
  playerProfile      // Data to save
);
```

This prevents key collisions between different mods using the shared database.

### Choosing a namespace
Use your mod's name or GUID as the namespace:

```csharp
SharedDatabase.Set("ScarletCore", "settings/main", settings);
SharedDatabase.Set("VampireCosmetics", "skins/default", skinConfig);
SharedDatabase.Set("EnhancedCombat", "abilities/warrior", abilityTree);
```

### Organizing keys within your namespace
Structure keys with slashes to organize data:

```csharp
// Players and related data
SharedDatabase.Set("MyMod", "players/alice", aliceData);
SharedDatabase.Set("MyMod", "players/bob", bobData);

// Settings
SharedDatabase.Set("MyMod", "settings/server", serverConfig);
SharedDatabase.Set("MyMod", "settings/game", gameConfig);

// Temporary/cache data
SharedDatabase.Set("MyMod", "cache/rankings", rankings);
SharedDatabase.Set("MyMod", "cache/stats", stats);
```

## Advanced Features

### List all keys in namespace
Retrieve all keys for your namespace:

```csharp
var keys = SharedDatabase.GetAllKeys("MyMod");
foreach (var key in keys) {
  Log.Message($"Stored key: {key}");
}
```

### Get keys by prefix
Retrieve keys that start with a specific prefix:

```csharp
// Get all player keys
var playerKeys = SharedDatabase.GetKeysByPrefix("MyMod", "players/");
// Returns: ["players/alice", "players/bob", ...]

// Get all cache keys
var cacheKeys = SharedDatabase.GetKeysByPrefix("MyMod", "cache/");
```

### Get all data by prefix
Retrieve all data entries that match a prefix:

```csharp
// Get all player data
var players = SharedDatabase.GetAllByPrefix<PlayerProfile>("MyMod", "players/");
// Returns: { "players/alice": aliceData, "players/bob": bobData, ... }

// Get all settings
var settings = SharedDatabase.GetAllByPrefix<ConfigData>("MyMod", "settings/");
```

### Query with predicates
Filter data using LINQ expressions:

```csharp
// Find all keys containing "admin"
var adminData = SharedDatabase.Query<PlayerProfile>(
  "MyMod",
  key => key.Contains("admin")
);

// Find all keys starting with "player/" and containing specific pattern
var activePlayers = SharedDatabase.QueryWithKeys<PlayerData>(
  "MyMod",
  key => key.StartsWith("players/") && key.EndsWith("_active")
);
// Returns: Dictionary with keys and values
```

### Get all data for namespace
Retrieve all data entries in your namespace:

```csharp
// Get all data as list
var allData = SharedDatabase.GetAll<PlayerProfile>("MyMod");

// Get all data with keys
var allDataWithKeys = SharedDatabase.GetAllWithKeys<PlayerProfile>("MyMod");
foreach (var kvp in allDataWithKeys) {
  Log.Message($"Key: {kvp.Key}, Value: {kvp.Value}");
}
```

### Count entries
Count entries in your namespace:

```csharp
// Count entries in your namespace
int count = SharedDatabase.Count("MyMod");

// Count all entries across all namespaces
int totalCount = SharedDatabase.CountAll();
```

### List all database names
Get all namespaces currently in use:

```csharp
var databases = SharedDatabase.GetAllDatabaseNames();
// Returns: ["MyMod", "OtherMod", "ThirdMod", ...]
```

### Clear namespace
Clear all data for your namespace:

```csharp
int deleted = SharedDatabase.ClearDatabase("MyMod");
Log.Message($"Deleted {deleted} entries");
```

### Async operations
Non-blocking I/O: SharedDatabase methods are synchronous. Backup and restore operations are asynchronous via `CreateBackup` and `RestoreFromBackup`.

```csharp
// Create backup (async)
string backupPath = await SharedDatabase.CreateBackup();

// Restore from backup (async)
bool ok = await SharedDatabase.RestoreFromBackup(backupPath);
```

### Circular reference detection
Like Database, SharedDatabase detects circular references:

```csharp
var node = new Node { Value = 1 };
node.Self = node;
SharedDatabase.Set("MyMod", "nodes/cycle", node);  // Throws with error info
```

## Backup & Recovery

### Enable automatic backups
```csharp
SharedDatabase.EnableAutoBackup();

// Custom backup location
SharedDatabase.EnableAutoBackup("/path/to/backups");
```

### Configure backup limit
```csharp
SharedDatabase.MaxBackups = 20;  // Keep 20 recent backups
```

### Manual backup
```csharp
string backupPath = await SharedDatabase.CreateBackup("/custom/path");
Log.Message($"Backup created at: {backupPath}");
```

## Cross-Mod Access Example

**Mod A** saves player rankings:

```csharp
// Inside ModA
var rankings = new Dictionary<string, int> { 
  { "alice", 1000 }, 
  { "bob", 950 } 
};
SharedDatabase.Set("ModA", "rankings/all", rankings);
```

**Mod B** reads the rankings (no dependency needed):

```csharp
// Inside ModB — can read ModA's data without dependency!
var rankings = SharedDatabase.Get<Dictionary<string, int>>("ModA", "rankings/all");
if (rankings != null) {
  Log.Message($"Top player: {rankings.First()}");
}
```

## Thread Safety

All operations are thread-safe using internal locking:

```csharp
// Multiple threads can safely read/write
SharedDatabase.Set("MyMod", "key", data);  // Thread-safe
var data = SharedDatabase.Get("MyMod", "key");  // Thread-safe
```

## When to use SharedDatabase

✅ **Use SharedDatabase when:**
- Multiple mods need to share data
- Mods shouldn't have hard dependencies
- Data is accessed from different assemblies
- You want centralized storage for multiple mods
- You need to allow optional integration between mods

❌ **Don't use SharedDatabase when:**
- Data is only used by your mod (use Database or JsonDatabase)
- You need human-readable files (use JsonDatabase)
- It's plugin configuration (use Settings)
- Performance is critical for single-mod access (use Database directly)

## Common Patterns

### Allow other mods to extend your data
```csharp
// ModA stores base data
var baseFeatures = new List<string> { "spawn", "heal" };
SharedDatabase.Set("ModA", "features/base", baseFeatures);

// ModB adds to ModA's data
var baseFeatures = SharedDatabase.Get<List<string>>("ModA", "features/base") 
  ?? new List<string>();
baseFeatures.Add("teleport");
SharedDatabase.Set("ModA", "features/base", baseFeatures);
```

### Version checking
```csharp
SharedDatabase.Set("MyMod", "meta/version", "1.5.0");

// Other mods can check compatibility
var version = SharedDatabase.Get<string>("MyMod", "meta/version");
if (version != "1.5.0") {
  Log.Warning("Version mismatch");
}
```

### Cache shared calculations
```csharp
var rankings = SharedDatabase.GetOrCreate("Analysis", "cache/rankings", () => {
  return CalculateRankings();  // Expensive operation
});
```

### Mod registry
```csharp
// Register your mod
SharedDatabase.Set("Registry", "mods/mymod", new {
  Name = "MyAwesomeMod",
  Version = "1.0.0",
  Author = "YourName"
});

// Other mods can discover your mod
var myMod = SharedDatabase.Get("Registry", "mods/mymod");
```