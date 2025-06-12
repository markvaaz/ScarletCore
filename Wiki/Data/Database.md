# Database

The Database class provides JSON-based data persistence for V Rising mods. Each database instance creates its own folder and manages file operations with caching.

## Overview

```csharp
using ScarletCore.Data;

var db = new Database("MyModName");
```

## Features

- JSON serialization with caching
- Temporary in-memory storage
- Automatic directory management
- Cache invalidation based on file timestamps

## Methods

### Save
Saves data to a JSON file.

```csharp
db.Save<GuildData>("guild_123", guildData);
```

**Parameters:**
- `path` - File name without extension
- `data` - Object to serialize

### Load
Loads data from a JSON file.

```csharp
var data = db.Load<GuildData>("guild_123");
```

**Parameters:**
- `path` - File name without extension

**Returns:** Deserialized object or default value

### Get
Gets data from cache or loads from file if not cached.

```csharp
var data = db.Get<GuildData>("guild_123");
```

**Parameters:**
- `path` - File name without extension

**Returns:** Cached or loaded data

### Has
Checks if a data file exists.

```csharp
bool exists = db.Has("guild_123");
```

**Parameters:**
- `path` - File name without extension

**Returns:** True if file exists

### Delete
Deletes a data file and removes from cache.

```csharp
bool deleted = db.Delete("guild_123");
```

**Parameters:**
- `path` - File name without extension

**Returns:** True if successfully deleted

### ClearCache
Clears cached data.

```csharp
db.ClearCache();           // Clear all cache
db.ClearCache("guild_123"); // Clear specific file cache
```

**Parameters:**
- `path` - Optional specific path to clear

### SaveAll
Saves all cached data to files.

```csharp
db.SaveAll();
```

## Temporary Storage

Access temporary in-memory storage that persists only during runtime.

```csharp
var tempData = db.Temp;
```

### Temp Methods

#### Set
Stores data in memory only.

```csharp
db.Temp.Set<string>("session_token", "abc123");
```

#### Get
Retrieves temporary data.

```csharp
var token = db.Temp.Get<string>("session_token");
```

#### Has
Checks if temporary data exists.

```csharp
bool exists = db.Temp.Has("session_token");
```

#### Remove
Removes temporary data.

```csharp
bool removed = db.Temp.Remove("session_token");
```

#### Clear
Clears all temporary data.

```csharp
db.Temp.Clear();
```

#### GetKeys
Gets all temporary data keys.

```csharp
var keys = db.Temp.GetKeys();
```

## File Storage

Files are stored in:
```
BepInEx/config/{DatabaseName}/{path}.json
```

## Caching Behavior

- Data is cached after load/save operations
- Cache is invalidated when files are modified externally
- Cache timestamps track file modification times
- Temporary storage exists only in memory

## Example Usage

```csharp
using ScarletCore.Data;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("markvaaz.ScarletCore")]
public class Plugin : BasePlugin {
    public static Database Database { get; private set; }

    public override void Load() {
        Database = new Database(MyPluginInfo.PLUGIN_GUID);
        
        // Use database for persistent storage
        Database.Save("hooks/enabled", true);
        var isEnabled = Database.Load<bool>("hooks/enabled");
        
        // Use temporary storage for runtime data
        Database.Temp.Set("session_active", true);
    }
}
```
