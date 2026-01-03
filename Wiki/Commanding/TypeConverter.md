# Type Conversion (Parameter Types & Parsing)

The TypeConverter is responsible for converting string arguments typed by players into strongly-typed C# parameters. It validates input format and provides automatic type detection for command overloading.

## How it works

When a player types a command, the system:
1. Splits the message into tokens (respecting quoted strings)
2. Matches tokens to method parameters
3. Attempts to convert each token to the target parameter type
4. Assigns a priority score based on conversion success
5. Selects the best matching command based on total score

## Type Priority System

When multiple commands match the same input, TypeConverter uses priority scores to select the best fit:

| Type | Priority | Description |
|------|----------|-------------|
| `int`, `long`, `uint`, `ulong`, `short`, `byte` | 100 | Exact integer match |
| `bool` | 95 | `true`/`false` or `1`/`0` |
| `float`, `double` | 90 | Decimal numbers |
| `enum` | 85 | Case-insensitive enum member |
| `PrefabGUID` | 80 | Valid prefab GUID |
| `PlayerData` (numeric ID) | 75 | Player by SteamID |
| `float2`, `float3`, `float4`, `quaternion` | 70 | Vector types |
| `PlayerData` (name) | 60 | Player by name |
| `string` | 10 | Matches anything (lowest priority) |

Higher priority = more specific type = preferred when overloading.

## Supported Types & Formats

### Primitives

**String**
- Accepts any text input
- Use quotes for multi-word strings: `"Hello World"`
- Lowest priority (10) — used as fallback

```csharp
// Example
[Command("say", Language.English)]
public static void Say(CommandContext ctx, string message) { }
// Usage: .say "Hello everyone"
```

**Integers** (`int`, `long`, `uint`, `ulong`, `short`, `byte`)
- Plain numeric text: `123`, `-456`, `999999`
- Priority: 100

```csharp
[Command("spawn", Language.English)]
public static void Spawn(CommandContext ctx, PrefabGUID prefab, int count) { }
// Usage: .spawn -123456789 5
```

**Floats/Doubles** (`float`, `double`)
- Decimal numbers using `.` (invariant culture): `3.14`, `-0.5`, `100.0`
- Priority: 90

```csharp
[Command("speed", Language.English)]
public static void Speed(CommandContext ctx, float multiplier) { }
// Usage: .speed 2.5
```

**Boolean** (`bool`)
- Accepts: `true`, `false`, `1`, `0` (case-insensitive)
- Priority: 95

```csharp
[Command("pvp", Language.English)]
public static void TogglePvP(CommandContext ctx, bool enabled) { }
// Usage: .pvp true
// Usage: .pvp 1
```

### Game Types

**PlayerData**
- Can be specified by **name** or **numeric SteamID**
- Numeric ID priority: 75, Name priority: 60
- Automatically resolves to the PlayerData object

```csharp
[Command("kick", Language.English)]
public static void Kick(CommandContext ctx, PlayerData player) { }
// Usage: .kick PlayerName
// Usage: .kick "Player With Spaces"
// Usage: .kick 76561198012345678
```

**PrefabGUID**
- Integer GUID value for game prefabs
- Priority: 80

```csharp
[Command("give", Language.English)]
public static void Give(CommandContext ctx, PrefabGUID itemGuid) { }
// Usage: .give -123456789
```

### Vector Types

**float2** (`Unity.Mathematics.float2`)
- Two comma-separated floats: `x,y`
- Spaces around commas are optional: `100,200` or `100, 200`
- Priority: 70

```csharp
[Command("setpos2d", Language.English)]
public static void SetPos2D(CommandContext ctx, float2 position) { }
// Usage: .setpos2d 100,200
// Usage: .setpos2d 100.5, 200.75
```

**float3** (`Unity.Mathematics.float3`)
- Three comma-separated floats: `x,y,z`
- Priority: 70

```csharp
[Command("tp", Language.English)]
public static void Teleport(CommandContext ctx, float3 coords) { }
// Usage: .tp 100,200,50
// Usage: .tp 100.5, 200.0, 50.25
```

**float4** (`Unity.Mathematics.float4`)
- Four comma-separated floats: `x,y,z,w`
- Priority: 70

```csharp
[Command("setrotation", Language.English)]
public static void SetRotation(CommandContext ctx, float4 rotation) { }
// Usage: .setrotation 0,0,0,1
```

**quaternion** (`Unity.Mathematics.quaternion`)
- Four comma-separated floats: `x,y,z,w`
- Priority: 70

```csharp
[Command("rotate", Language.English)]
public static void Rotate(CommandContext ctx, quaternion rotation) { }
// Usage: .rotate 0,0,0,1
```

### Enums

**Any enum type**
- Case-insensitive member name matching
- Priority: 85

```csharp
public enum GameMode { PvE, PvP, Mixed }

[Command("gamemode", Language.English)]
public static void SetGameMode(CommandContext ctx, GameMode mode) { }
// Usage: .gamemode PvP
// Usage: .gamemode pvp (case-insensitive)
```

## Conversion Behavior

### Success
When conversion succeeds, the parameter receives the converted value and the command gets the type's priority score.

### Failure
When conversion fails:
- The command is disqualified from matching (unless `allowTypeFailure` is enabled internally)
- The system tries other command overloads
- If no commands match, the player receives an error with usage information

### Optional Parameters
Optional parameters with default values don't require input:

```csharp
[Command("spawn", Language.English)]
public static void Spawn(CommandContext ctx, PrefabGUID prefab, int count = 1) { }
// Usage: .spawn -123456789      (count defaults to 1)
// Usage: .spawn -123456789 5    (count = 5)
```

## Friendly Type Names (in usage messages)

When the system generates usage help, it displays user-friendly names:

| Type | Friendly Name |
|------|---------------|
| `string` | `text` |
| `int`, `long`, etc. | `number` |
| `float`, `double` | `decimal` |
| `bool` | `true/false` |
| `PlayerData` | `player` |
| `PrefabGUID` | `prefabGuid` |
| `float2` | `x,y` |
| `float3` | `x,y,z` |
| `float4`, `quaternion` | `x,y,z,w` |

Example usage message: `.teleport <player:player> <coords:x,y,z>`

## Tips for Command Authors

✅ **Use specific types** instead of strings when possible (better type safety and priority)
✅ **Provide clear usage strings** showing expected format (especially for vectors)
✅ **Use optional parameters** for flexibility
✅ **Test with various inputs** to ensure proper conversion

❌ **Don't use `string` when `PlayerData` or `PrefabGUID` would be more appropriate**
❌ **Don't forget commas** in vector types (they're required, not optional)