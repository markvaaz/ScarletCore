# ScarletCore Extensions

## Overview

ScarletCore provides three main extension classes that simplify working with Unity ECS entities, localization, and player data retrieval. These extensions reduce boilerplate code and provide a more fluent, readable API for common operations.

- **ECSExtensions** — Entity Component System helpers (Read, Write, Has, Add, Remove, etc.)
- **LocalizationExtensions** — String localization with server/player language context
- **PlayerDataExtensions** — Retrieve PlayerData from various types (Entity, string, ulong, NetworkId, User)

## Table of Contents

- [ECSExtensions](#ecsextensions)
  - [Component Read/Write](#component-readwrite)
  - [Component Checking & Manipulation](#component-checking--manipulation)
  - [Buffers](#buffers)
  - [Entity State & Queries](#entity-state--queries)
  - [Entity Helpers](#entity-helpers)
  - [Position & Team](#position--team)
  - [Buff & Faction](#buff--faction)
  - [Localization Helper](#localization-helper)
- [LocalizationExtensions](#localizationextensions)
- [PlayerDataExtensions](#playerdataextensions)
- [Examples](#examples)
- [Best Practices](#best-practices)
- [Performance Considerations](#performance-considerations)

---

## ECSExtensions

Extension methods for working with Unity ECS entities and components. All methods are defined in the `ScarletCore` namespace.

### Component Read/Write

#### Read

```csharp
public static T Read<T>(this Entity entity) where T : struct
```

Reads a component from the entity.

**Returns:** Component data

**Example:**
```csharp
var health = entity.Read<Health>();
Log.Info($"Health: {health.Value}/{health.MaxHealth}");
```

#### Write

```csharp
public static void Write<T>(this Entity entity, T componentData) where T : struct
```

Writes component data to the entity.

**Example:**
```csharp
var health = entity.Read<Health>();
health.Value = 100;
entity.Write(health);
```

#### With

```csharp
public static void With<T>(this Entity entity, WithRefHandler<T> action) where T : struct
public delegate void WithRefHandler<T>(ref T item)
```

Reads a component, allows modification via delegate (by reference), then writes it back. Very convenient for modifying components inline.

**Example:**
```csharp
// Modify health without manual Read/Write
entity.With((ref Health health) => {
    health.Value -= 50;
});

// Modify position
entity.With((ref LocalTransform transform) => {
    transform.Position += new float3(0, 10, 0);
});
```

#### AddWith

```csharp
public static void AddWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
```

Adds the component if it doesn't exist, then allows modification via delegate.

**Example:**
```csharp
entity.AddWith((ref CustomData data) => {
    data.Value = 42;
});
```

#### HasWith

```csharp
public static void HasWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
```

If the entity has the component, allows modification via delegate.

**Example:**
```csharp
entity.HasWith((ref Stunned stunned) => {
    stunned.Duration -= deltaTime;
});
```

---

### Component Checking & Manipulation

#### Has

```csharp
public static bool Has<T>(this Entity entity) where T : struct
```

Checks if the entity has a component.

**Returns:** `true` if component exists

**Example:**
```csharp
if (entity.Has<Health>()) {
    // Entity is damageable
}
```

#### TryGetComponent

```csharp
public static bool TryGetComponent<T>(this Entity entity, out T componentData) where T : struct
```

Tries to get component data. Safer than `Read` when component might not exist.

**Returns:** `true` if component exists, outputs data

**Example:**
```csharp
if (entity.TryGetComponent(out Health health)) {
    Log.Info($"Health: {health.Value}");
}
```

#### Add

```csharp
public static void Add<T>(this Entity entity) where T : struct
```

Adds a component to the entity if it doesn't already exist.

**Example:**
```csharp
entity.Add<Immortal>();
```

#### Remove

```csharp
public static void Remove<T>(this Entity entity) where T : struct
```

Removes a component from the entity if it exists.

**Example:**
```csharp
entity.Remove<Stunned>();
```

---

### Buffers

#### ReadBuffer

```csharp
public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct
```

Reads a dynamic buffer from the entity.

**Example:**
```csharp
var buffer = entity.ReadBuffer<AbilityGroupSlotBuffer>();
foreach (var slot in buffer) {
    Log.Info($"Slot: {slot.SlotId}");
}
```

#### AddBuffer

```csharp
public static DynamicBuffer<T> AddBuffer<T>(this Entity entity) where T : struct
```

Adds a dynamic buffer to the entity.

**Example:**
```csharp
var buffer = entity.AddBuffer<CustomBufferElement>();
buffer.Add(new CustomBufferElement { Value = 10 });
```

#### TryGetBuffer

```csharp
public static bool TryGetBuffer<T>(this Entity entity, out DynamicBuffer<T> dynamicBuffer) where T : struct
```

Tries to get a dynamic buffer using ServerGameManager.

**Example:**
```csharp
if (entity.TryGetBuffer(out DynamicBuffer<BuffBuffer> buffs)) {
    Log.Info($"Entity has {buffs.Length} buffs");
}
```

---

### Entity State & Queries

#### Exists

```csharp
public static bool Exists(this Entity entity)
```

Checks if the entity exists in EntityManager and is not null.

**Example:**
```csharp
if (entity.Exists()) {
    // Safe to use entity
}
```

#### IsNull

```csharp
public static bool IsNull(this Entity entity)
```

Checks if the entity is `Entity.Null`.

**Example:**
```csharp
if (!entity.IsNull()) {
    // Entity reference is valid
}
```

#### IsDisabled

```csharp
public static bool IsDisabled(this Entity entity)
```

Checks if the entity has the `Disabled` component.

**Example:**
```csharp
if (!entity.IsDisabled()) {
    ProcessEntity(entity);
}
```

#### IsPlayer

```csharp
public static bool IsPlayer(this Entity entity)
```

Checks if the entity represents a player character.

**Example:**
```csharp
if (entity.IsPlayer()) {
    Log.Info("Entity is a player");
}
```

#### TryGetPlayer

```csharp
public static bool TryGetPlayer(this Entity entity, out Entity player)
```

Tries to get the player entity if the entity represents a player character.

**Example:**
```csharp
if (entity.TryGetPlayer(out var player)) {
    // Use player entity
}
```

#### IsPlayerOwned

```csharp
public static bool IsPlayerOwned(this Entity entity)
```

Checks if the entity is owned by a player (has `EntityOwner` pointing to a player).

**Example:**
```csharp
if (entity.IsPlayerOwned()) {
    Log.Info("This entity belongs to a player");
}
```

#### Destroy

```csharp
public static void Destroy(this Entity entity, bool immediate = false)
```

Destroys the entity. If `immediate` is true, destroys immediately; otherwise uses `DestroyUtility`.

**Example:**
```csharp
entity.Destroy(); // Standard destruction
entity.Destroy(immediate: true); // Immediate destruction
```

---

### Entity Helpers

#### GetPrefabGuid

```csharp
public static PrefabGUID GetPrefabGuid(this Entity entity)
```

Gets the PrefabGUID component, or `PrefabGUID.Empty` if not found.

**Example:**
```csharp
var prefab = entity.GetPrefabGuid();
Log.Info($"Prefab: {prefab.GuidHash}");
```

#### GetGuidHash

```csharp
public static int GetGuidHash(this Entity entity)
```

Gets the GuidHash from the PrefabGUID component.

**Example:**
```csharp
var hash = entity.GetGuidHash();
```

#### GetOwner

```csharp
public static Entity GetOwner(this Entity entity)
```

Gets the owner entity from `EntityOwner` component, or `Entity.Null` if not found.

**Example:**
```csharp
var owner = entity.GetOwner();
if (owner.Exists()) {
    Log.Info($"Owner: {owner}");
}
```

#### GetUserEntity

```csharp
public static Entity GetUserEntity(this Entity entity)
```

Gets the user entity associated with the entity (from `PlayerCharacter.UserEntity` or if entity is a `User`).

**Example:**
```csharp
var userEntity = characterEntity.GetUserEntity();
```

#### TryGetAttached

```csharp
public static bool TryGetAttached(this Entity entity, out Entity attached)
```

Tries to get the parent entity attached via the `Attach` component.

**Example:**
```csharp
if (entity.TryGetAttached(out var parent)) {
    Log.Info($"Attached to: {parent}");
}
```

#### TryGetTeamEntity

```csharp
public static bool TryGetTeamEntity(this Entity entity, out Entity teamEntity)
```

Tries to get the team entity from `TeamReference`.

**Example:**
```csharp
if (entity.TryGetTeamEntity(out var team)) {
    Log.Info($"Team entity: {team}");
}
```

#### GetBuffTarget

```csharp
public static Entity GetBuffTarget(this Entity entity)
```

Gets the buff target entity for the specified entity.

**Example:**
```csharp
var target = entity.GetBuffTarget();
```

---

### Position & Team

#### Position

```csharp
public static float3 Position(this Entity entity)
```

Gets the position from `Translation` component, or `float3.zero` if not found.

**Example:**
```csharp
var pos = entity.Position();
Log.Info($"Entity position: {pos}");
```

#### SetPosition

```csharp
public static void SetPosition(this Entity entity, float3 position)
```

Sets the position in `Translation` and `LastTranslation` components.

**Example:**
```csharp
entity.SetPosition(new float3(100, 0, 100));
```

#### AimPosition

```csharp
public static float3 AimPosition(this Entity entity)
```

Gets the aim position from `EntityInput` component.

**Example:**
```csharp
var aimPos = playerEntity.AimPosition();
```

#### GetTileCoord

```csharp
public static int2 GetTileCoord(this Entity entity)
```

Gets the tile coordinates from `TilePosition` component.

**Example:**
```csharp
var tile = entity.GetTileCoord();
```

#### SetTeam

```csharp
public static void SetTeam(this Entity entity, Entity teamSource)
```

Sets the team and team reference based on another entity.

**Example:**
```csharp
minion.SetTeam(player);
```

#### IsAllies

```csharp
public static bool IsAllies(this Entity entity, Entity player)
```

Checks if the entity is allied with the specified player entity.

**Example:**
```csharp
if (entity.IsAllies(playerEntity)) {
    // Friendly entity
}
```

---

### Buff & Faction

#### HasBuff

```csharp
public static bool HasBuff(this Entity entity, PrefabGUID buffPrefabGuid)
```

Checks if the entity has a specific buff.

**Example:**
```csharp
if (entity.HasBuff(Prefabs.Buff_InCombat)) {
    Log.Info("Entity is in combat");
}
```

#### SetFaction

```csharp
public static void SetFaction(this Entity entity, PrefabGUID factionPrefabGuid)
```

Sets the faction reference of the entity.

**Example:**
```csharp
entity.SetFaction(Prefabs.Faction_Players);
```

#### GetUnitLevel

```csharp
public static int GetUnitLevel(this Entity entity)
```

Gets the unit level from `UnitLevel` component.

**Example:**
```csharp
var level = entity.GetUnitLevel();
Log.Info($"Enemy level: {level}");
```

---

### Localization Helper

#### LocalizedName

```csharp
public static string LocalizedName(this PrefabGUID prefabGuid)
```

Gets the localized name for a PrefabGUID.

**Example:**
```csharp
var name = itemPrefab.LocalizedName();
MessageService.SendMessage(player, $"You obtained {name}");
```

---

## LocalizationExtensions

Extension methods for localizing strings using server or player language context.

### Localize (Server Language)

```csharp
public static string Localize(this string key, params object[] parameters)
```

Localizes a key using the server's default language with optional format parameters.

**Parameters:**
- `key`: Translation key
- `parameters`: Format parameters (optional)

**Returns:** Localized string

**Example:**
```csharp
var text = "help_message".Localize();
var formatted = "player_join".Localize(playerName);
```

### Localize (Player Language)

```csharp
public static string Localize(this string key, PlayerData player, params object[] parameters)
```

Localizes a key using the player's preferred language (falls back to server default if not available).

**Parameters:**
- `key`: Translation key
- `player`: PlayerData for language context
- `parameters`: Format parameters (optional)

**Returns:** Localized string in player's language

**Example:**
```csharp
var msg = "welcome_message".Localize(playerData);
var greeting = "hello_player".Localize(playerData, playerData.Name);
MessageService.SendMessage(playerData, greeting);
```

**Technical Note:** The extension attempts to detect the originating assembly to support composite keys (e.g., `"MyMod:custom_key"`), allowing other mods to register their own translation keys.

---

## PlayerDataExtensions

Extension methods for retrieving PlayerData from various types.

### GetPlayerData (from string)

```csharp
public static PlayerData GetPlayerData(this string playerName)
```

Gets PlayerData by player name.

**Example:**
```csharp
var player = "Marcos".GetPlayerData();
if (player != null) {
    MessageService.SendMessage(player, "Hello!");
}
```

### GetPlayerData (from ulong)

```csharp
public static PlayerData GetPlayerData(this ulong playerId)
```

Gets PlayerData by platform ID.

**Example:**
```csharp
var player = 123456789UL.GetPlayerData();
```

### GetPlayerData (from Entity)

```csharp
public static PlayerData GetPlayerData(this Entity entity)
```

Gets PlayerData from an entity (works with `User` or `PlayerCharacter` entities).

**Example:**
```csharp
var player = characterEntity.GetPlayerData();
if (player != null) {
    Log.Info($"Player: {player.Name}");
}
```

### GetPlayerData (from NetworkId)

```csharp
public static PlayerData GetPlayerData(this NetworkId networkId)
```

Gets PlayerData by network ID.

**Example:**
```csharp
var player = networkId.GetPlayerData();
```

### GetPlayerData (from User)

```csharp
public static PlayerData GetPlayerData(this User user)
```

Gets PlayerData from a User component.

**Example:**
```csharp
var userComponent = userEntity.Read<User>();
var player = userComponent.GetPlayerData();
```

---

## Examples

### Example 1: Modify Entity Health

```csharp
// Using With for inline modification
enemy.With((ref Health health) => {
    health.Value = math.max(0, health.Value - 50);
});

// Traditional way (more verbose)
var health = enemy.Read<Health>();
health.Value -= 50;
enemy.Write(health);
```

### Example 2: Safe Component Access

```csharp
// Check existence before reading
if (entity.Has<Health>()) {
    var health = entity.Read<Health>();
    // Use health
}

// Or use TryGetComponent
if (entity.TryGetComponent(out Health health)) {
    Log.Info($"Health: {health.Value}");
}
```

### Example 3: Entity Queries and Checks

```csharp
var entities = EntityLookupService.QueryAll<Enemy>();

foreach (var entity in entities) {
    if (!entity.Exists()) continue;
    if (entity.IsDisabled()) continue;
    
    if (entity.IsPlayerOwned()) {
        Log.Info("Enemy is player-owned (pet/minion)");
    }
    
    var level = entity.GetUnitLevel();
    Log.Info($"Enemy level: {level}");
}

entities.Dispose();
```

### Example 4: Localized Messages

```csharp
// Server language
var welcomeMsg = "welcome_server".Localize();
MessageService.SendSystemMessage(welcomeMsg);

// Player language
var players = PlayerService.GetOnlinePlayers();
foreach (var player in players) {
    var personalMsg = "welcome_personal".Localize(player, player.Name);
    MessageService.SendMessage(player, personalMsg);
}
```

### Example 5: PlayerData Retrieval

```csharp
// From entity
var player = characterEntity.GetPlayerData();

// From name
var targetPlayer = "Eve".GetPlayerData();
if (targetPlayer != null) {
    MessageService.SendMessage(targetPlayer, "You've been mentioned!");
}

// From ID
ulong platformId = 123456789UL;
var playerById = platformId.GetPlayerData();
```

### Example 6: Entity Team Management

```csharp
// Copy team from player to spawned entity
var spawnedEntity = SpawnerService.Spawn(prefab, position);
spawnedEntity.SetTeam(playerEntity);

// Check if allied
if (enemy.IsAllies(playerEntity)) {
    Log.Info("Enemy is actually friendly");
}
```

### Example 7: Buff Management

```csharp
// Check for buffs
if (player.HasBuff(Prefabs.Buff_InCombat)) {
    MessageService.SendMessage(player.GetPlayerData(), "You cannot rest while in combat");
    return;
}

// Iterate through all buffs
if (entity.TryGetBuffer(out DynamicBuffer<BuffBuffer> buffs)) {
    foreach (var buff in buffs) {
        var buffName = buff.PrefabGuid.LocalizedName();
        Log.Info($"Buff: {buffName}");
    }
}
```

### Example 8: Position and Movement

```csharp
// Get current position
var currentPos = entity.Position();

// Teleport to new position
var targetPos = new float3(100, 0, 100);
entity.SetPosition(targetPos);

// Get player's aim position (for abilities)
var aimPos = playerEntity.AimPosition();
SpawnerService.Spawn(projectilePrefab, aimPos);
```

### Example 9: Component Conditional Operations

```csharp
// Add component if missing, then modify
entity.AddWith((ref CustomData data) => {
    data.Score += 100;
});

// Modify only if exists
entity.HasWith((ref Stunned stunned) => {
    stunned.Duration = math.max(0, stunned.Duration - deltaTime);
});
```

### Example 10: Entity Cleanup and Destruction

```csharp
// Find expired entities
var expiredEntities = EntityLookupService.QueryAll<TemporaryEntity>();

foreach (var entity in expiredEntities) {
    if (!entity.Exists()) continue;
    
    if (entity.TryGetComponent(out ExpirationTimer timer) && timer.Remaining <= 0) {
        // Standard destruction (queued)
        entity.Destroy();
        
        // Or immediate destruction
        // entity.Destroy(immediate: true);
    }
}

expiredEntities.Dispose();
```

---

## Best Practices

### 1. Prefer `With` for Component Modifications

```csharp
// ✅ Good - Concise and safe
entity.With((ref Health health) => {
    health.Value -= damage;
});

// ❌ Verbose
var health = entity.Read<Health>();
health.Value -= damage;
entity.Write(health);
```

### 2. Always Check Entity Existence

```csharp
// ✅ Good
if (entity.Exists()) {
    var health = entity.Read<Health>();
}

// ❌ Bad - Potential crash
var health = entity.Read<Health>(); // Crash if entity destroyed!
```

### 3. Use `TryGetComponent` for Optional Components

```csharp
// ✅ Good - Safe
if (entity.TryGetComponent(out Health health)) {
    ProcessHealth(health);
}

// ❌ Bad - Exception if component missing
if (entity.Has<Health>()) {
    var health = entity.Read<Health>(); // Still two operations
}
```

### 4. Use `GetPlayerData` Extensions for Convenience

```csharp
// ✅ Good - Simple
var player = "Marcos".GetPlayerData();

// ❌ Verbose
PlayerData player = null;
PlayerService.TryGetByName("Marcos", out player);
```

### 5. Localize Player-Facing Messages

```csharp
// ✅ Good - Player's language
var msg = "error_insufficient_funds".Localize(playerData);
MessageService.SendMessage(playerData, msg);

// ❌ Bad - Hard-coded English
MessageService.SendMessage(playerData, "You don't have enough gold");
```

### 6. Dispose Buffers When Done

```csharp
// ✅ Good - ReadBuffer returns reference, no disposal needed
var buffs = entity.ReadBuffer<BuffBuffer>();
foreach (var buff in buffs) { }
// No disposal needed

// ⚠️ Note: TryGetBuffer also doesn't require disposal
if (entity.TryGetBuffer(out DynamicBuffer<BuffBuffer> buffs)) {
    // Use buffs
}
```

### 7. Use Appropriate Destruction Method

```csharp
// ✅ Good - Standard destruction (queued)
entity.Destroy();

// ✅ Good - Immediate when necessary (careful!)
entity.Destroy(immediate: true);

// ❌ Bad - Direct EntityManager call (less safe)
GameSystems.EntityManager.DestroyEntity(entity);
```

### 8. Cache PrefabGUID Lookups

```csharp
// ✅ Good - Cache if used multiple times
var prefabGuid = entity.GetPrefabGuid();
if (prefabGuid == Prefabs.Item_Gold) { }
if (prefabGuid == Prefabs.Item_Silver) { }

// ❌ Bad - Repeated calls
if (entity.GetPrefabGuid() == Prefabs.Item_Gold) { }
if (entity.GetPrefabGuid() == Prefabs.Item_Silver) { }
```

---

## Performance Considerations

### Component Access

- `Read<T>()` — Fast, direct component access
- `Write<T>()` — Fast, direct component write
- `With<T>()` — Slight overhead (one extra call), but cleaner code
- `TryGetComponent<T>()` — Two operations (Has + Read), use when component is optional

### Entity Checks

- `Exists()` — Fast, two checks (null + EntityManager.Exists)
- `Has<T>()` — Fast, uses Il2Cpp type resolution (cached internally by Unity)
- `IsPlayer()` — Same as `Has<PlayerCharacter>()`

### Localization

- `Localize()` — Involves dictionary lookups and string formatting; avoid in hot loops
- Cache localized strings when used repeatedly
- Server language lookup is faster than player language (no fallback logic)

### PlayerData Retrieval

- All `GetPlayerData()` methods use internal PlayerService dictionaries (fast O(1) lookups)
- Entity-based lookup requires component read first, slightly slower

### Buffer Access

- `ReadBuffer<T>()` — Returns reference to existing buffer (no allocation)
- `TryGetBuffer<T>()` — Uses ServerGameManager (slightly slower)
- Buffers are not copied; modifications affect the original

### Best Performance Tips

1. **Cache component data** in variables when used multiple times
2. **Avoid repeated `Localize()`** calls — cache results
3. **Use `With()`** for single modifications, but not in tight loops
4. **Batch entity operations** instead of processing one-by-one
5. **Check `Exists()` before any component access** in delayed callbacks

---

## Common Pitfalls

### 1. Forgetting to Check Existence

```csharp
// ❌ Crash if entity was destroyed
ActionScheduler.Delayed(() => {
    entity.Write(new Health { Value = 100 });
}, 5f);

// ✅ Safe
ActionScheduler.Delayed(() => {
    if (entity.Exists()) {
        entity.Write(new Health { Value = 100 });
    }
}, 5f);
```

### 2. Modifying Structs Without Writing Back

```csharp
// ❌ Changes lost! Structs are value types
var health = entity.Read<Health>();
health.Value = 100; // Only modifies local copy!

// ✅ Write back
var health = entity.Read<Health>();
health.Value = 100;
entity.Write(health);

// ✅ Or use With
entity.With((ref Health health) => {
    health.Value = 100;
});
```

### 3. Using `Has` Before `Read` Unnecessarily

```csharp
// ❌ Two operations
if (entity.Has<Health>()) {
    var health = entity.Read<Health>();
}

// ✅ Use TryGetComponent
if (entity.TryGetComponent(out Health health)) {
    // Use health
}
```

### 4. Not Handling Null PlayerData

```csharp
// ❌ Potential null reference
var player = "UnknownPlayer".GetPlayerData();
MessageService.SendMessage(player, "Hi"); // Crash if null!

// ✅ Check first
var player = playerName.GetPlayerData();
if (player != null) {
    MessageService.SendMessage(player, "Hi");
}
```

### 5. Immediate Destruction Without Consideration

```csharp
// ❌ Can cause issues if other systems reference entity
entity.Destroy(immediate: true);

// ✅ Use standard destruction unless you have a specific reason
entity.Destroy();
```

---

## Related Documentation

- [GameSystems](Systems/GameSystems.md) — Core ECS systems access
- [EntityLookupService](Services/EntityLookupService.md) — Entity queries
- [PlayerService](Services/PlayerService.md) — Player management
- [Localizer](Localization/Localizer.md) — Localization system
- [Logger](Utils/Logger.md) — Logging utilities

---

## Requirements

- **Unity.Entities** — ECS framework
- **Il2CppInterop.Runtime** — Il2Cpp type interop
- **ProjectM** — V Rising game assemblies
- **ScarletCore.Services** — PlayerService, Localizer
- **ScarletCore.Systems** — GameSystems

---

## Technical Notes

### WithRefHandler Delegate

The `WithRefHandler<T>` delegate allows passing structs by reference for in-place modification:

```csharp
public delegate void WithRefHandler<T>(ref T item);
```

This enables the `With`, `AddWith`, and `HasWith` pattern which is both performant and clean.

### Localization Assembly Detection

`LocalizationExtensions` uses stack trace analysis to detect the calling assembly, allowing mods to register composite translation keys (e.g., `"ModName:key"`) without explicit registration.

### PlayerData Caching

`PlayerDataExtensions` leverages PlayerService's internal caching, making repeated lookups very fast. PlayerService maintains dictionaries indexed by platform ID, name, and network ID.
