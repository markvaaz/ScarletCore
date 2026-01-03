# Event Types Reference

Complete reference for all built-in event types in ScarletCore.

## PrefixEvents

Events triggered **before** core game logic executes. Use these to intercept or modify behavior before it happens.

### Event Signature
```csharp
Action<NativeArray<Entity>>
```

### Available Events

| Event | Description | Use Case |
|-------|-------------|----------|
| `OnChatMessage` | Before chat message processing | Filter/modify chat, custom commands |
| `OnDealDamage` | Before damage is dealt | Damage modification, immunity checks |
| `OnDeath` | Before entity death | Prevent death, last-minute saves |
| `OnCastStarted` | Before ability cast starts | Prevent casting, modify cooldowns |
| `OnPreCastFinished` | Before cast finishes (pre-cast phase) | Cancel cast, modify targeting |
| `OnPostCastEnded` | Before post-cast phase ends | Effect modifications |
| `OnCastInterrupted` | Before cast interruption | Custom interrupt handling |
| `OnUnitSpawned` | Before unit spawns | Modify spawn, prevent spawning |
| `OnPlayerDowned` | Before player is incapacitated | Prevent down, revive mechanics |
| `OnWarEvent` | Before war event processing | Modify war mechanics |
| `OnReplaceAbilityOnSlot` | Before ability replacement | Prevent replacement, validation |
| `OnInteract` | Before interaction starts | Custom interaction logic |
| `OnInteractStop` | Before interaction stops | Cleanup, rewards |
| `OnShapeshift` | Before transformation | Custom forms, restrictions |
| `OnWaypointTeleport` | Before waypoint teleport | Teleport costs, restrictions |
| `OnDestroyTravelBuff` | Before travel buff removal | Custom travel mechanics |
| `OnInventoryChanged` | When inventory changes | Item tracking, validation |
| `OnMoveItem` | Before item movement | Prevent moves, custom logic |

### Example Usage

```csharp
// Prevent death under certain conditions
EventManager.On(PrefixEvents.OnDeath, (entities) => {
  foreach (var entity in entities) {
    var player = PlayerService.GetPlayerDataFromEntity(entity);
    if (player != null && HasImmortalityBuff(player)) {
      // Prevent death somehow (game-specific logic)
      Log.Message($"{player.Name} is immortal!");
    }
  }
});

// Modify damage before it's applied
EventManager.On(PrefixEvents.OnDealDamage, (entities) => {
  // Custom damage modifications
});
```

## PostfixEvents

Events triggered **after** core game logic executes. Use these for post-processing, monitoring, or reactions.

### Event Signature
```csharp
Action<NativeArray<Entity>>
```

### Available Events

PostfixEvents mirror all PrefixEvents with identical names. The difference is timing - postfix runs **after** the game's logic.

| Event | Description | Use Case |
|-------|-------------|----------|
| `OnChatMessage` | After chat message processing | Logging, analytics |
| `OnDealDamage` | After damage is dealt | Damage display, statistics |
| `OnDeath` | After entity death | Death penalties, rewards |
| `OnCastStarted` | After ability cast starts | Visual effects, notifications |
| `OnPreCastFinished` | After cast finishes | Apply effects |
| `OnPostCastEnded` | After post-cast phase | Cleanup |
| `OnCastInterrupted` | After cast interruption | Notifications |
| `OnUnitSpawned` | After unit spawns | Initialization, tracking |
| `OnPlayerDowned` | After player is downed | Death notifications |
| `OnWarEvent` | After war event | Rewards, announcements |
| `OnReplaceAbilityOnSlot` | After ability replacement | UI updates |
| `OnInteract` | After interaction starts | Feedback, effects |
| `OnInteractStop` | After interaction stops | Completion rewards |
| `OnShapeshift` | After transformation | Visual updates |
| `OnWaypointTeleport` | After waypoint teleport | Arrival effects |
| `OnDestroyTravelBuff` | After travel buff removal | Cleanup |
| `OnInventoryChanged` | After inventory changes | UI updates |
| `OnMoveItem` | After item movement | Sorting, organization |

### Example Usage

```csharp
// Log all deaths
EventManager.On(PostfixEvents.OnDeath, (entities) => {
  foreach (var entity in entities) {
    var player = PlayerService.GetPlayerDataFromEntity(entity);
    if (player != null) {
      Log.Message($"{player.Name} died");
      SaveDeathStatistics(player);
    }
  }
});

// Show damage numbers after damage is dealt
EventManager.On(PostfixEvents.OnDealDamage, (entities) => {
  // Display damage numbers to players
});
```

## PlayerEvents

Player lifecycle and state change events.

### PlayerJoined

**Signature:** `Action<PlayerData>`

Triggered when a player connects to the server.

```csharp
EventManager.On(PlayerEvents.PlayerJoined, (player) => {
  Log.Message($"{player.Name} joined");
  Log.Message($"SteamID: {player.SteamID}");
  player.SendMessage("Welcome to the server!");
});
```

### PlayerLeft

**Signature:** `Action<PlayerData>`

Triggered when a player disconnects from the server.

```csharp
EventManager.On(PlayerEvents.PlayerLeft, (player) => {
  Log.Message($"{player.Name} left");
  SavePlayerData(player);
});
```

### PlayerKicked

**Signature:** `Action<PlayerData>`

Triggered when a player is kicked.

```csharp
EventManager.On(PlayerEvents.PlayerKicked, (player) => {
  Log.Message($"{player.Name} was kicked");
  LogModerationAction(player, "kick");
});
```

### PlayerBanned

**Signature:** `Action<PlayerData>`

Triggered when a player is banned.

```csharp
EventManager.On(PlayerEvents.PlayerBanned, (player) => {
  Log.Message($"{player.Name} was banned");
  LogModerationAction(player, "ban");
});
```

### CharacterCreated

**Signature:** `Action<PlayerData>`

Triggered when a new character is created.

```csharp
EventManager.On(PlayerEvents.CharacterCreated, (player) => {
  Log.Message($"New character: {player.Name}");
  GiveStarterBonus(player);
});
```

### CharacterRenamed

**Signature:** `Action<PlayerData>`

Triggered when a character is renamed.

```csharp
EventManager.On(PlayerEvents.CharacterRenamed, (player) => {
  Log.Message($"Character renamed: {player.Name}");
  UpdateDatabase(player);
});
```

## ServerEvents

Server-wide lifecycle events.

### OnInitialize

**Signature:** `Action`

Triggered once when the server is initialized and ready.

```csharp
EventManager.On(ServerEvents.OnInitialize, () => {
  Log.Message("Server initialized!");
  InitializeDatabase();
  LoadConfiguration();
});

// Or use Once for one-time setup
EventManager.Once(ServerEvents.OnInitialize, () => {
  Log.Message("One-time initialization");
});
```

### OnSave

**Signature:** `Action` or `Action<string>`

Triggered when the server state is being saved. Optionally provides the save name.

```csharp
// Without parameter
EventManager.On(ServerEvents.OnSave, () => {
  Log.Message("Server is saving");
  CreateBackup();
});

// With save name parameter
EventManager.On(ServerEvents.OnSave, (saveName) => {
  Log.Message($"Saving: {saveName}");
  CreateBackup(saveName);
});
```

## CommandEvents (Internal)

Command execution lifecycle events. These are primarily used internally by the command system.

### OnBeforeExecute

**Signature:** `Action<PlayerData, CommandInfo, string[]>`

Triggered before a command is executed.

```csharp
// Internal use only
EventManager.On(CommandEvents.OnBeforeExecute, (player, commandInfo, args) => {
  Log.Message($"Executing: {commandInfo.Name}");
});
```

### OnAfterExecute

**Signature:** `Action<PlayerData, CommandInfo, string[]>`

Triggered after a command is executed.

```csharp
// Internal use only
EventManager.On(CommandEvents.OnAfterExecute, (player, commandInfo, args) => {
  Log.Message($"Completed: {commandInfo.Name}");
});
```

## Custom Events

Create your own events with any signature.

### Parameterless Custom Event

```csharp
// Subscribe
EventManager.On("MyMod:SimpleEvent", () => {
  Log.Message("Simple event triggered!");
});

// Emit
EventManager.Emit("MyMod:SimpleEvent");
```

### Single Parameter Custom Event

```csharp
// Subscribe
EventManager.On("MyMod:MessageEvent", (string message) => {
  Log.Message($"Received: {message}");
});

// Emit
EventManager.Emit("MyMod:MessageEvent", "Hello World");
```

### Complex Data Custom Event

```csharp
// Define data structure
public class CustomEventData {
  public string Name;
  public int Value;
  public PlayerData Player;
}

// Subscribe
EventManager.On("MyMod:ComplexEvent", (CustomEventData data) => {
  Log.Message($"{data.Player.Name}: {data.Name} = {data.Value}");
});

// Emit
var eventData = new CustomEventData {
  Name = "Score",
  Value = 100,
  Player = playerData
};
EventManager.Emit("MyMod:ComplexEvent", eventData);
```

### Multi-Parameter Custom Event

```csharp
// Subscribe using a single tuple parameter
EventManager.On("MyMod:MultiParam", ((string, int, bool) data) => {
  var (name, value, flag) = data;
  Log.Message($"{name}: {value}, {flag}");
});

// Emit with tuple
EventManager.Emit("MyMod:MultiParam", ("Test", 42, true));
```

## Event Type Comparison

| Event Category | When | Signature | Thread-Safe | Use Case |
|---------------|------|-----------|-------------|----------|
| PrefixEvents | Before game logic | `Action<NativeArray<Entity>>` | No (main thread) | Intercept, prevent, modify |
| PostfixEvents | After game logic | `Action<NativeArray<Entity>>` | No (main thread) | React, log, effects |
| PlayerEvents | Player lifecycle | `Action<PlayerData>` | No (main thread) | Player tracking, welcome |
| ServerEvents | Server lifecycle | `Action` or `Action<string>` | No (main thread) | Initialization, saves |
| CommandEvents | Command execution | `Action<PlayerData, CommandInfo, string[]>` | No (main thread) | Internal command system |
| Custom Events | User-defined | Any delegate | Yes (locked) | Cross-mod, custom logic |

## Best Practices

### Choosing the Right Event Type

```csharp
// ✅ Use PrefixEvents to PREVENT or MODIFY
EventManager.On(PrefixEvents.OnDeath, (entities) => {
  // Check if death should be prevented
  // Modify death behavior
});

// ✅ Use PostfixEvents to REACT or LOG
EventManager.On(PostfixEvents.OnDeath, (entities) => {
  // Death already happened, now react
  // Log statistics, show effects
});

// ✅ Use PlayerEvents for PLAYER LIFECYCLE
EventManager.On(PlayerEvents.PlayerJoined, (player) => {
  // Player just joined
  // Send welcome message, load data
});

// ✅ Use ServerEvents for SERVER LIFECYCLE
EventManager.On(ServerEvents.OnSave, (saveName) => {
  // Server is saving
  // Create backups, save data
});

// ✅ Use Custom Events for MOD INTEGRATION
EventManager.On("EconomyMod:Transaction", (transactionData) => {
  // Another mod emitted this
  // React to economy events
});
```

### Event Naming Conventions

```csharp
// ✅ Good: Namespaced with mod name
"MyMod:OnPlayerKill"
"EconomyMod:OnTransaction"
"QuestMod:QuestCompleted"

// ❌ Bad: Generic, collision-prone
"OnKill"
"Transaction"
"QuestDone"
```

### Type Safety

```csharp
// ✅ Good: Strongly typed
public class RewardData {
  public string Type;
  public int Amount;
}
EventManager.On("MyMod:Reward", (RewardData data) => { });

// ❌ Bad: Using object or dynamic
EventManager.On("MyMod:Reward", (object data) => {
  // No type safety, error-prone
});
```
