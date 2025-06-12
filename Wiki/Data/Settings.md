# Settings

The Settings class provides BepInEx configuration management for V Rising mods. It simplifies creating, reading, and managing configuration entries with a fluent API.

## Overview

```csharp
using ScarletCore.Data;

var settings = new Settings(MyPluginInfo.PLUGIN_GUID, this);
```

## Features

- BepInEx configuration integration
- Fluent API for section management
- Type-safe configuration entries
- Automatic file persistence
- Configuration validation

## Methods

### Add
Adds a new configuration entry.

```csharp
settings.Add("General", "EnableFeature", true, "Enable main feature");
```

**Parameters:**
- `section` - Configuration section name
- `key` - Configuration key identifier
- `defaultValue` - Default value if not found
- `description` - Description of the setting

### Get
Retrieves the value of a configuration entry.

```csharp
var isEnabled = settings.Get<bool>("EnableFeature");
```

**Parameters:**
- `key` - Configuration key to retrieve

**Returns:** The configuration value

### Set
Sets the value of an existing configuration entry.

```csharp
settings.Set("EnableFeature", false);
```

**Parameters:**
- `key` - Configuration key to update
- `value` - New value to set

### Has
Checks if a configuration entry exists.

```csharp
bool exists = settings.Has("EnableFeature");
```

**Parameters:**
- `key` - Configuration key to check

**Returns:** True if the key exists

### Save
Saves the configuration to disk. (the saving is normaly done automatically by BepInEx)

```csharp
settings.Save();
```

### Dispose
Cleans up resources and clears entries.

```csharp
settings.Dispose();
```

## Section Management

Use the Section method for fluent configuration setup.

```csharp
settings.Section("General")
  .Add("EnableFeature", true, "Enable main feature")
  .Add("MaxItems", 100, "Maximum number of items");
```

### Section Methods

#### Add
Adds a configuration entry to the section and returns the section for chaining.

```csharp
section.Add("MaxPlayers", 50, "Maximum player count");
```

**Parameters:**
- `key` - Configuration key identifier
- `defaultValue` - Default value
- `description` - Description of the setting

**Returns:** SettingsSection for method chaining

## Properties

### Keys
Gets all configuration entry keys.

```csharp
var keys = settings.Keys;
```

### Values
Gets all configuration entry values.

```csharp
var values = settings.Values;
```

## File Storage

Configuration files are stored in:
```
BepInEx/config/{PluginGuid}.cfg
```

## Best Practices

### Organization
Organize settings into logical sections:
- **General** - Core plugin settings (URLs, intervals, toggles)
- **Customization** - User-facing text and formatting
- **Admin** - Administrative feature toggles
- **Features** - Specific feature configurations

### Method Chaining
Use fluent API for cleaner configuration setup:
```csharp
Settings.Section("General")
  .Add("Setting1", defaultValue1, "Description 1")
  .Add("Setting2", defaultValue2, "Description 2")
  .Add("Setting3", defaultValue3, "Description 3");
```

### Reload Support
Implement settings reload functionality:
```csharp
public static void ReloadSettings() {
  Settings.Dispose();
  LoadSettings();
}
```

### Descriptive Keys and Descriptions
Use clear, descriptive configuration keys and detailed descriptions:
```csharp
generalSection.Add("MessageInterval", 0.2f, "Interval in seconds between sending messages.\nUseful to prevent rate limiting.");
```

## Example Usage

```csharp
using ScarletCore.Data;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("markvaaz.ScarletCore")]
public class Plugin : BasePlugin {
  public static Settings Settings { get; private set; }

  public override void Load() {
    Settings = new Settings(MyPluginInfo.PLUGIN_GUID, this);
    
    LoadSettings();
  }

  public override bool Unload() {
    Settings.Dispose();
    return true;
  }

  public static void LoadSettings() {
    // General configuration
    var generalSection = Settings.Section("General");
    generalSection
      .Add("AdminWebhookURL", "null", "Admin Webhook URL for notifications")
      .Add("EnableBatching", true, "Enable message batching to avoid rate limiting")
      .Add("MessageInterval", 0.2f, "Interval in seconds between messages");

    // Customization settings
    var customSection = Settings.Section("Customization");
    customSection
      .Add("LoginMessageFormat", "{playerName} has joined the game.", "Login message format")
      .Add("GlobalPrefix", "[Global] {playerName}:", "Global chat prefix")
      .Add("ClanPrefix", "[Clan][{clanName}] {playerName}:", "Clan chat prefix");

    // Feature toggles
    var adminSection = Settings.Section("Admin");
    adminSection
      .Add("AdminGlobalMessages", true, "Send global messages to admin webhook")
      .Add("AdminLoginMessages", true, "Send login messages to admin webhook")
      .Add("AdminPvpMessages", true, "Send PvP messages to admin webhook");
  }

  public static void ReloadSettings() {
    Settings.Dispose();
    LoadSettings();
  }
}
```