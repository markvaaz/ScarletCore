# Settings (BepInEx configuration management)

`Settings` provides a simplified API for managing plugin configuration using BepInEx's native config system. Best for plugin settings that users can edit via `.cfg` files.

## Overview

Settings are stored in `BepInEx/config/<PluginGuid>.cfg` files. This integrates directly with BepInEx's configuration UI and allows users to edit settings without restarting.

```csharp
var settings = new Settings("MyPlugin", pluginInstance);
```

You need a reference to your plugin instance (usually `this` in your plugin class).

## Basic Usage

### Add a setting
```csharp
settings.Add("Server", "MaxPlayers", 100, "Maximum number of players");
```

Creates an entry in the `[Server]` section with key `MaxPlayers` and default value `100`.

### Get a setting value
```csharp
var entry = settings.Get<int>("Server", "MaxPlayers");
var maxPlayers = entry.Value;
```

### Get with type inference
# Settings (BepInEx configuration management)

`Settings` provides a simplified API for managing plugin configuration using BepInEx's native config system. Best for plugin settings that users can edit via `.cfg` files.

## Overview

Settings are stored in `BepInEx/config/<PluginGuid>.cfg` files. This integrates directly with BepInEx's configuration UI and allows users to edit settings without restarting.

```csharp
var settings = new Settings("MyPlugin", pluginInstance);
```

You need a reference to your plugin instance (usually `this` in your plugin class).

## Basic Usage

### Add a setting
```csharp
settings.Add("Server", "MaxPlayers", 100, "Maximum number of players");
```

Creates an entry in the `[Server]` section with key `MaxPlayers` and default value `100` (note: keys are stored flat internally — see "Notes").

### Get a setting value
```csharp
// Use the key provided when adding the entry (flat key)
var maxPlayers = settings.Get<int>("MaxPlayers");
```

The `Get<T>(string key)` method returns the value `T` directly (not a `ConfigEntry<T>`).

### Set a setting value
```csharp
settings.Set<int>("MaxPlayers", 120);
```

### Check if setting exists
```csharp
// Use the flat key
if (settings.Has("MaxPlayers")) {
  // Setting exists
}
```

### Save to disk
```csharp
settings.Save();  // Writes to `.cfg` file via BepInEx
```

## Sections (Organization)

Group related settings using sections. Each section appears as a `[Section]` header in the `.cfg` file.

### Using sections (adding)
```csharp
var serverSection = settings.Section("Server");
serverSection.Add("MaxPlayers", 100, "Maximum player count")
             .Add("PvpEnabled", true, "Enable PvP")
             .Add("MaxLevel", 50, "Maximum player level");
```

Note: when retrieving values, use the plain key you added above:
```csharp
var maxPlayers = settings.Get<int>("MaxPlayers");
```

This creates:
```
[Server]
MaxPlayers = 100 ; Maximum player count
PvpEnabled = true ; Enable PvP
MaxLevel = 50 ; Maximum player level
```

### Fluent chaining
Use the section fluent API to add multiple settings at once.

```csharp
settings.Section("Game")
  .Add("Difficulty", "Normal", "Game difficulty (Easy/Normal/Hard)")
  .Add("AutoSave", true, "Enable auto-save")
  .Add("SaveInterval", 5, "Save interval in minutes");

settings.Section("Logging")
  .Add("LogLevel", "Info", "Log level (Debug/Info/Warning/Error)")
  .Add("LogFile", "latest.log", "Log file name");
```

## Configuration File Format

Settings are stored in standard BepInEx `.cfg` format (human-editable):

```
[Server]
MaxPlayers = 100 ; Maximum number of players
PvpEnabled = True
MaxLevel = 50

[Game]
Difficulty = Normal ; Game difficulty (Easy/Normal/Hard)
AutoSave = True

[Logging]
LogLevel = Info ; Log level (Debug/Info/Warning/Error)
LogFile = latest.log
```

Users can edit these values directly in the `.cfg` file and changes take effect on next load or dynamically if your plugin supports it.

## Accessing Configuration Entries

### Getting values
The library provides `Get<T>(string key)` to return the value. Example:

```csharp
var enabled = settings.Get<bool>("Enabled");
var maxConn = settings.Get<int>("MaxConnections");
```

### Updating values
Use `Set<T>(string key, T value)` to update a value programmatically.

```csharp
settings.Set<int>("MaxConnections", 200);
```

### Iterating keys and values
`Keys` is a flat collection of string keys; `Values` contains the underlying BepInEx `ConfigEntryBase` values.

```csharp
foreach (var key in settings.Keys) {
  var value = settings.Get<object>(key);
  Log.Message($"{key} = {value}");
}

foreach (var entry in settings.Values) {
  Log.Message($"Entry: {entry.Definition.Key} = {entry.BoxedValue}");
}
```

### Subscribing to changes
`Get<T>` returns the raw value; if you need to subscribe to `SettingChanged`, locate the underlying `ConfigEntry<T>` in `settings.Values` and cast:

```csharp
var entry = settings.Values.OfType<ConfigEntry<int>>().FirstOrDefault(e => e.Definition.Key == "MaxConnections");
if (entry != null) {
  entry.SettingChanged += (s, e) => {
    UpdateServerMaxConnections(entry.Value);
    Log.Message("Max connections updated");
  };
}
```

> Note: the API stores entries by their `key` string only. If you add the same `key` under multiple sections, they will collide. Prefer unique keys across your plugin.

## Type Support

Settings support any serializable type via BepInEx `ConfigEntry<T>`:

```csharp
settings.Add("General", "Name", "MyServer", "Server name");
settings.Add("General", "Port", 8080, "Server port");
settings.Add("General", "Enabled", true, "Is server enabled");

// Enums
settings.Add("General", "Difficulty", GameDifficulty.Normal, "Difficulty level");

// Complex types (serialized as JSON/text)
settings.Add("General", "AllowedUsers", "alice;bob;charlie", "Semicolon-separated user list");
```

## Lifecycle Management

### Dispose
Clean up resources when plugin unloads:

```csharp
public void OnDestroy() {
  settings.Dispose();
}
```

This clears all entries and clears the cached config file reference.

## When to use Settings

✅ **Use Settings when:**
- You're storing plugin configuration/options
- Users should edit settings in BepInEx config files
- Values are simple and non-complex
- Integration with BepInEx UI is desired
- You need hot-reload support

❌ **Don't use Settings when:**
- You need complex data structures (use JsonDatabase or Database)
- Data needs query/filter capabilities (use Database)
- Data should be shared across mods (use SharedDatabase)
- You want structured hierarchical data (use JsonDatabase)

## Common Patterns

### Plugin initialization with settings
```csharp
var settings = new Settings("MyPlugin", this);

settings.Section("Server")
  .Add("Enabled", true, "Enable server features")
  .Add("MaxConnections", 100, "Max concurrent connections");

settings.Section("Debug")
  .Add("LogLevel", "Info", "Logging level")
  .Add("Verbose", false, "Enable verbose logging");

settings.Save();
```

### Reading configuration at startup
```csharp
var enabled = settings.Get<bool>("Enabled");
var maxConn = settings.Get<int>("MaxConnections");

if (enabled) {
  InitializeServer(maxConn);
}
```

### Watch for changes
```csharp
var entry = settings.Values.OfType<ConfigEntry<int>>().FirstOrDefault(e => e.Definition.Key == "MaxConnections");
if (entry != null) {
  entry.SettingChanged += (s, e) => {
    UpdateServerMaxConnections(entry.Value);
    Log.Message("Max connections updated");
  };
}
```