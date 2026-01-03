# EventManager Examples

Complete examples demonstrating various EventManager patterns and use cases.

## Basic Event Subscription

### Player Events

```csharp
using ScarletCore.Events;
using ScarletCore.Services;
using ScarletCore.Utils;

public class PlayerTracker {
  public void Initialize() {
    // Track when players join
    EventManager.On(PlayerEvents.PlayerJoined, OnPlayerJoined);
    
    // Track when players leave
    EventManager.On(PlayerEvents.PlayerLeft, OnPlayerLeft);
    
    // Track character creation
    EventManager.On(PlayerEvents.CharacterCreated, OnCharacterCreated);
  }
  
  private void OnPlayerJoined(PlayerData player) {
    Log.Message($"[Tracker] {player.CharacterName} joined the server");
    Log.Message($"[Tracker] Steam ID: {player.SteamID}");
    
    // Send welcome message
    player.SendMessage($"Welcome, {player.CharacterName}!");
  }
  
  private void OnPlayerLeft(PlayerData player) {
    Log.Message($"[Tracker] {player.CharacterName} left the server");
  }
  
  private void OnCharacterCreated(PlayerData player) {
    Log.Message($"[Tracker] New character created: {player.CharacterName}");
    
    // Give starter bonus
    player.SendMessage("Welcome! Here's a starter bonus!");
  }
  
  public void Cleanup() {
    EventManager.Off(PlayerEvents.PlayerJoined, OnPlayerJoined);
    EventManager.Off(PlayerEvents.PlayerLeft, OnPlayerLeft);
    EventManager.Off(PlayerEvents.CharacterCreated, OnCharacterCreated);
  }
}
```

### Server Events

```csharp
public class ServerMonitor {
  public void Initialize() {
    // Run once on server initialization
    EventManager.Once(ServerEvents.OnInitialize, () => {
      Log.Message("Server is ready!");
      InitializeDatabase();
    });
    
    // Track every save
    EventManager.On(ServerEvents.OnSave, OnServerSave);
  }
  
  private void OnServerSave(string saveName) {
    Log.Message($"Server saving: {saveName}");
    
    // Create database backup
    CreateBackup(saveName);
  }
  
  private void InitializeDatabase() {
    // One-time setup
    Log.Message("Initializing database...");
  }
  
  private void CreateBackup(string saveName) {
    // Backup logic
    Log.Message($"Creating backup for: {saveName}");
  }
}
```

## Priority-Based Execution

### Validation → Processing → Logging

```csharp
public class CombatSystem {
  public void Initialize() {
    EventManager.On(PrefixEvents.OnDealDamage, ValidateDamage);
    EventManager.On(PrefixEvents.OnDealDamage, ProcessDamage);
    EventManager.On(PrefixEvents.OnDealDamage, LogDamage);
  }
  
  // Runs FIRST - validates before processing
  [EventPriority(EventPriority.First)]
  private void ValidateDamage(NativeArray<Entity> entities) {
    Log.Message("1. Validating damage...");
    // Validation logic
  }
  
  // Runs SECOND - normal priority
  [EventPriority(EventPriority.Normal)]
  private void ProcessDamage(NativeArray<Entity> entities) {
    Log.Message("2. Processing damage...");
    // Main damage processing
  }
  
  // Runs LAST - log after everything
  [EventPriority(EventPriority.Last)]
  private void LogDamage(NativeArray<Entity> entities) {
    Log.Message("3. Logging damage...");
    // Logging/analytics
  }
}
```

### Multiple Mods with Priorities

```csharp
// Mod A: Core combat mod (runs first)
public class CoreCombatMod {
  [EventPriority(EventPriority.VeryHigh)]
  private void OnDamage(NativeArray<Entity> entities) {
    Log.Message("CoreCombat: Applying base damage");
  }
}

// Mod B: Armor mod (runs second)
public class ArmorMod {
  [EventPriority(EventPriority.High)]
  private void OnDamage(NativeArray<Entity> entities) {
    Log.Message("ArmorMod: Calculating armor reduction");
  }
}

// Mod C: Damage display (runs last)
public class DamageDisplay {
  [EventPriority(EventPriority.Last)]
  private void OnDamage(NativeArray<Entity> entities) {
    Log.Message("DamageDisplay: Showing damage numbers");
  }
}
```

## Custom Events

### Simple Custom Event

```csharp
// Define your data
public class RewardData {
  public string RewardType;
  public int Amount;
  public PlayerData Player;
}

public class RewardSystem {
  public void GiveReward(PlayerData player, string type, int amount) {
    var data = new RewardData {
      RewardType = type,
      Amount = amount,
      Player = player
    };
    
    // Emit custom event
    EventManager.Emit("RewardMod:OnReward", data);
  }
}

public class RewardListener {
  public void Initialize() {
    // Subscribe to the custom event
    EventManager.On("RewardMod:OnReward", OnReward);
  }
  
  private void OnReward(RewardData data) {
    Log.Message($"{data.Player.CharacterName} received {data.Amount} {data.RewardType}");
    
    // Update UI, statistics, etc.
  }
}
```

### Cross-Mod Communication

```csharp
// Mod A: Quest System (emits events)
public class QuestMod {
  public class QuestCompletedData {
    public string QuestId;
    public string QuestName;
    public PlayerData Player;
    public int ExpReward;
  }
  
  private void CompleteQuest(PlayerData player, string questId) {
    var data = new QuestCompletedData {
      QuestId = questId,
      QuestName = "Dragon Slayer",
      Player = player,
      ExpReward = 1000
    };
    
    // Other mods can listen to this
    EventManager.Emit("QuestMod:QuestCompleted", data);
  }
}

// Mod B: Economy Mod (listens to quest events)
public class EconomyMod {
  public void Initialize() {
    // Listen to quest completions without depending on QuestMod
    EventManager.On("QuestMod:QuestCompleted", OnQuestCompleted);
  }
  
  private void OnQuestCompleted(QuestMod.QuestCompletedData data) {
    // Give gold bonus
    int goldReward = data.ExpReward / 10;
    Log.Message($"Economy: Giving {goldReward} gold to {data.Player.CharacterName}");
  }
}

// Mod C: Statistics Mod (also listens)
public class StatsMod {
  public void Initialize() {
    EventManager.On("QuestMod:QuestCompleted", OnQuestCompleted);
  }
  
  private void OnQuestCompleted(QuestMod.QuestCompletedData data) {
    // Track quest statistics
    Log.Message($"Stats: Recording quest completion for {data.Player.CharacterName}");
  }
}
```

### Event Chains

```csharp
public class EventChainExample {
  public void Initialize() {
    // First event triggers second, second triggers third
    EventManager.On("Chain:Step1", OnStep1);
    EventManager.On("Chain:Step2", OnStep2);
    EventManager.On("Chain:Step3", OnStep3);
  }
  
  public void StartChain() {
    EventManager.Emit("Chain:Step1", "Starting chain...");
  }
  
  private void OnStep1(string data) {
    Log.Message($"Step 1: {data}");
    EventManager.Emit("Chain:Step2", "Step 1 complete");
  }
  
  private void OnStep2(string data) {
    Log.Message($"Step 2: {data}");
    EventManager.Emit("Chain:Step3", "Step 2 complete");
  }
  
  private void OnStep3(string data) {
    Log.Message($"Step 3: {data}");
    Log.Message("Chain complete!");
  }
}
```

## Advanced Patterns

### Event Broadcasting System

```csharp
public class BroadcastSystem {
  public class BroadcastData {
    public string Message;
    public string Sender;
    public BroadcastLevel Level;
  }
  
  public enum BroadcastLevel {
    Info,
    Warning,
    Critical
  }
  
  public void Broadcast(string message, string sender, BroadcastLevel level) {
    var data = new BroadcastData {
      Message = message,
      Sender = sender,
      Level = level
    };
    
    EventManager.Emit("Server:Broadcast", data);
  }
  
  public void Initialize() {
    EventManager.On("Server:Broadcast", OnBroadcast);
  }
  
  private void OnBroadcast(BroadcastData data) {
    var color = data.Level switch {
      BroadcastLevel.Info => "white",
      BroadcastLevel.Warning => "yellow",
      BroadcastLevel.Critical => "red",
      _ => "white"
    };
    
    // Send to all players
    foreach (var player in PlayerService.GetAllPlayers()) {
      player.SendMessage($"<color={color}>[{data.Sender}] {data.Message}</color>");
    }
  }
}
```

### State Change Tracking

```csharp
public class PlayerStateTracker {
  private readonly Dictionary<ulong, PlayerState> _playerStates = new();
  
  public class PlayerState {
    public bool IsAlive = true;
    public bool IsInCombat = false;
    public DateTime LastActivity = DateTime.Now;
  }
  
  public void Initialize() {
    EventManager.On(PlayerEvents.PlayerJoined, OnPlayerJoined);
    EventManager.On(PrefixEvents.OnDeath, OnPlayerDeath);
    EventManager.On(PrefixEvents.OnDealDamage, OnCombatAction);
  }
  
  private void OnPlayerJoined(PlayerData player) {
    _playerStates[player.SteamID] = new PlayerState();
    CheckStateChange(player.SteamID);
  }
  
  private void OnPlayerDeath(NativeArray<Entity> entities) {
    // Update state and emit custom event
    foreach (var entity in entities) {
      var player = PlayerService.GetPlayerDataFromEntity(entity);
      if (player != null && _playerStates.ContainsKey(player.SteamID)) {
        _playerStates[player.SteamID].IsAlive = false;
        EventManager.Emit("StateTracker:PlayerDied", player);
      }
    }
  }
  
  private void OnCombatAction(NativeArray<Entity> entities) {
    // Track combat state
    foreach (var entity in entities) {
      var player = PlayerService.GetPlayerDataFromEntity(entity);
      if (player != null && _playerStates.ContainsKey(player.SteamID)) {
        var state = _playerStates[player.SteamID];
        if (!state.IsInCombat) {
          state.IsInCombat = true;
          EventManager.Emit("StateTracker:EnteredCombat", player);
        }
        state.LastActivity = DateTime.Now;
      }
    }
  }
  
  private void CheckStateChange(ulong steamId) {
    // Emit state change events
  }
}
```

### Cooldown System

```csharp
public class CooldownSystem {
  private readonly Dictionary<string, DateTime> _cooldowns = new();
  
  public class CooldownData {
    public string AbilityName;
    public PlayerData Player;
    public TimeSpan Duration;
  }
  
  public void Initialize() {
    EventManager.On("Cooldown:Start", OnCooldownStart);
    EventManager.On("Cooldown:Check", OnCooldownCheck);
  }
  
  private void OnCooldownStart(CooldownData data) {
    var key = $"{data.Player.SteamID}_{data.AbilityName}";
    _cooldowns[key] = DateTime.Now.Add(data.Duration);
    
    Log.Message($"Cooldown started: {data.AbilityName} for {data.Duration.TotalSeconds}s");
  }
  
  private void OnCooldownCheck(CooldownData data) {
    var key = $"{data.Player.SteamID}_{data.AbilityName}";
    
    if (!_cooldowns.TryGetValue(key, out var endTime)) {
      EventManager.Emit("Cooldown:Ready", data);
      return;
    }
    
    if (DateTime.Now >= endTime) {
      _cooldowns.Remove(key);
      EventManager.Emit("Cooldown:Ready", data);
    } else {
      var remaining = endTime - DateTime.Now;
      EventManager.Emit("Cooldown:NotReady", new {
        Data = data,
        Remaining = remaining
      });
    }
  }
}
```

### Plugin System with Events

```csharp
public class PluginLoader {
  public interface IPlugin {
    string Name { get; }
    void OnLoad();
    void OnUnload();
  }
  
  private readonly List<IPlugin> _plugins = new();
  
  public void LoadPlugin(IPlugin plugin) {
    _plugins.Add(plugin);
    
    // Notify all listeners
    EventManager.Emit("PluginLoader:PluginLoaded", new {
      PluginName = plugin.Name,
      Plugin = plugin
    });
    
    plugin.OnLoad();
  }
  
  public void UnloadAll() {
    foreach (var plugin in _plugins) {
      EventManager.Emit("PluginLoader:PluginUnloading", new {
        PluginName = plugin.Name,
        Plugin = plugin
      });
      
      plugin.OnUnload();
    }
    _plugins.Clear();
  }
}

// Example plugin
public class MyPlugin : PluginLoader.IPlugin {
  public string Name => "MyAwesomePlugin";
  
  public void OnLoad() {
    EventManager.On(PlayerEvents.PlayerJoined, OnPlayerJoin);
  }
  
  public void OnUnload() {
    EventManager.Off(PlayerEvents.PlayerJoined, OnPlayerJoin);
  }
  
  private void OnPlayerJoin(PlayerData player) {
    Log.Message($"[{Name}] Player joined: {player.CharacterName}");
  }
}
```

## Cleanup Patterns

### Manual Cleanup

```csharp
public class ManagedEventSystem : IDisposable {
  private Action<PlayerData> _joinHandler;
  private Action<PlayerData> _leaveHandler;
  
  public void Initialize() {
    _joinHandler = OnPlayerJoined;
    _leaveHandler = OnPlayerLeft;
    
    EventManager.On(PlayerEvents.PlayerJoined, _joinHandler);
    EventManager.On(PlayerEvents.PlayerLeft, _leaveHandler);
  }
  
  private void OnPlayerJoined(PlayerData player) {
    Log.Message($"Player joined: {player.CharacterName}");
  }
  
  private void OnPlayerLeft(PlayerData player) {
    Log.Message($"Player left: {player.CharacterName}");
  }
  
  public void Dispose() {
    EventManager.Off(PlayerEvents.PlayerJoined, _joinHandler);
    EventManager.Off(PlayerEvents.PlayerLeft, _leaveHandler);
  }
}
```

### Assembly-Wide Cleanup

```csharp
public class MyMod {
  public void Load() {
    // Register various handlers
    EventManager.On(PlayerEvents.PlayerJoined, OnPlayerJoin);
    EventManager.On(ServerEvents.OnSave, OnSave);
    EventManager.On("MyMod:CustomEvent", OnCustomEvent);
  }
  
  public void Unload() {
    // Unregister ALL handlers from this assembly at once
    int removed = EventManager.UnregisterAssembly();
    Log.Message($"Removed {removed} event handlers");
  }
  
  private void OnPlayerJoin(PlayerData player) { }
  private void OnSave(string saveName) { }
  private void OnCustomEvent(object data) { }
}
```

## Testing and Debugging

### Event Monitoring

```csharp
public class EventMonitor {
  public void Initialize() {
    // Monitor all event subscriptions
    MonitorAllEvents();
  }
  
  private void MonitorAllEvents() {
    var stats = EventManager.GetEventStatistics();
    
    Log.Message("=== Event Statistics ===");
    foreach (var kvp in stats) {
      Log.Message($"{kvp.Key}: {kvp.Value} subscribers");
    }
    
    // Check specific events
    int joinCount = EventManager.GetSubscriberCount(PlayerEvents.PlayerJoined);
    Log.Message($"PlayerJoined: {joinCount} subscribers");
  }
  
  public void ListCustomEvents() {
    var events = EventManager.GetRegisteredEvents();
    
    Log.Message("=== Registered Custom Events ===");
    foreach (var eventName in events) {
      int count = EventManager.GetSubscriberCount(eventName);
      Log.Message($"{eventName}: {count} subscribers");
    }
  }
}
```

### Event Testing

```csharp
public class EventTester {
  public void TestCustomEvent() {
    int callCount = 0;
    
    // Subscribe
    EventManager.On("Test:Event", (string data) => {
      callCount++;
      Log.Message($"Received: {data} (call #{callCount})");
    });
    
    // Emit multiple times
    EventManager.Emit("Test:Event", "Test 1");
    EventManager.Emit("Test:Event", "Test 2");
    EventManager.Emit("Test:Event", "Test 3");
    
    Log.Message($"Total calls: {callCount}");
    
    // Cleanup
    EventManager.ClearEvent("Test:Event");
  }
  
  public void TestOnce() {
    int callCount = 0;
    
    EventManager.Once("Test:OnceEvent", () => {
      callCount++;
      Log.Message($"Called once! Count: {callCount}");
    });
    
    // Emit multiple times
    EventManager.Emit("Test:OnceEvent");
    EventManager.Emit("Test:OnceEvent");
    EventManager.Emit("Test:OnceEvent");
    
    Log.Message($"Total calls (should be 1): {callCount}");
  }
}
```
