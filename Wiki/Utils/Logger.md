# Log (Logger)

`Log` is a static utility class that provides simple, centralized logging for ScarletCore mods and systems. It wraps the BepInEx logging system, making it easy to write debug, info, warning, error, and fatal messages to the console and log files. It also includes helpers for logging PlayerData and entity components for debugging purposes.

## Overview

- All methods are static and can be called from anywhere in your mod or ScarletCore internals.
- Uses the main plugin's log instance (`Plugin.LogInstance`) for output.
- Supports all standard log levels: Debug, Info, Warning, Error, Fatal, and more (see below).
- Includes helpers for logging PlayerData and entity components.

## Log Levels

The following log levels are available (from the `LogLevel` enum):

- `None` — No level selected.
- `Fatal` — A fatal error has occurred, which cannot be recovered from.
- `Error` — An error has occurred, but can be recovered from.
- `Warning` — A warning has been produced, but does not necessarily mean that something wrong has happened.
- `Message` — An important message that should be displayed to the user.
- `Info` — A message of low importance.
- `Debug` — A message that would likely only interest a developer.
- `All` — All log levels.

## Example Usage

```csharp
using ScarletCore.Utils;

Log.Info("Hello from my mod!");
Log.Debug($"Value: {value}");
Log.Warning("Something might be wrong");
Log.Error("An error occurred");
Log.Fatal("Critical failure!");

// Log with a specific log level
Log.LogLevel(LogLevel.Info, "This is an info message");
Log.LogLevel(LogLevel.Warning, "This is a warning");
Log.LogLevel(LogLevel.Fatal, "This is a fatal error");

// Log all components of an entity
Log.Components(entity);

// Log PlayerData details
Log.Player(playerData);
```

## API Reference

- `Log.Debug(object message)` — Log a debug message
- `Log.Info(object message)` — Log an info message
- `Log.Warning(object message)` — Log a warning message
- `Log.Error(object message)` — Log an error message
- `Log.Fatal(object message)` — Log a fatal error message
- `Log.LogLevel(LogLevel level, object message)` — Log a message with a specific log level
- `Log.Components(Entity entity)` — Log all component types attached to an entity
- `Log.Player(PlayerData playerData)` — Log detailed information about a PlayerData instance

## Notes

- All log methods are safe to call even if the logger is not initialized (they will do nothing).
- Use `Log.Player` and `Log.Components` for advanced debugging and development.
- Log output is visible in the BepInEx console and log files.

---

For more details, see the source code in `Utils/Logger.cs`.
