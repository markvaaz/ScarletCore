# Data Examples (Practical use cases)

Common patterns and real-world examples showing how to use each storage system.

## Choosing the right storage

### Scenario: Store game settings
**Best choice:** Settings (BepInEx config)

```csharp
var settings = new Settings("MyGameMod", pluginInstance);

settings.Section("Difficulty")
  .Add("Level", "Normal", "Game difficulty (Easy/Normal/Hard)")
  .Add("DamageMultiplier", 1.0f, "Enemy damage multiplier")
  .Add("HealthMultiplier", 1.0f, "Enemy health multiplier");

settings.Section("Features")
  .Add("EnablePvP", true, "Enable PvP combat")
  .Add("EnableEvents", true, "Enable world events");

settings.Save();

// Read at startup
var difficulty = settings.Get<string>("Difficulty", "Level").Value;
InitializeGame(difficulty);
```

Users can edit `.cfg` file directly — no code changes needed.

### Scenario: Store player profiles and statistics
**Best choice:** Database (LiteDB) or JsonDatabase

#### Using Database (with queries):
```csharp
var db = new Database("PlayerMod");

// Save player profile
var profile = new PlayerProfile {
  Id = playerId,
  Name = "Alice",
  Level = 42,
  CreatedAt = DateTime.UtcNow,
  Stats = new PlayerStats { Health = 100, Mana = 50 }
};

db.Set($"players/{playerId}", profile);

// Later: retrieve player
var loadedProfile = db.Get<PlayerProfile>($"players/{playerId}");

// Enable backups
db.EnableAutoBackup();
db.MaxBackups = 20;
```

#### Using JsonDatabase (human-readable):
```csharp
var db = new JsonDatabase("PlayerMod");

var profile = new PlayerProfile {
  Name = "Alice",
  Level = 42,
  Stats = new PlayerStats { Health = 100, Mana = 50 }
};

db.Set($"players/{playerId}", profile);

// File location: BepInEx/config/PlayerMod/players/{playerId}.json
// Players can inspect/edit files manually
```

Both approaches with automatic backups:

```csharp
db.EnableAutoBackup();  // Backup on server save
db.MaxBackups = 50;     // Keep 50 backup copies
```

### Scenario: Cache expensive calculations
**Best choice:** JsonDatabase (in-memory cache)

```csharp
var db = new JsonDatabase("StatsCache");

// Only calculate once, then use cached version
var rankings = db.GetOrCreate("rankings", () => {
  Log.Message("Calculating rankings...");  // Expensive operation
  return CalculatePlayerRankings();
});

// Next call returns cached version instantly
var rankings2 = db.Get<List<PlayerRanking>>("rankings");

// On data change, update cache
UpdatePlayerRanking("alice", 1000);
rankings = CalculatePlayerRankings();
db.Set("rankings", rankings);  // Clears cache and writes new data
```

### Scenario: Share data between mods
**Best choice:** SharedDatabase

```csharp
// Mod A (Economy system) stores currency data
public class EconomyMod {
  public void SavePlayerBalance(string playerId, decimal balance) {
    SharedDatabase.Set("EconomyMod", $"balance/{playerId}", balance);
  }
}

// Mod B (Shop system) reads currency without dependency
public class ShopMod {
  public void PurchaseItem(string playerId, Item item) {
    var balance = SharedDatabase.Get<decimal>("EconomyMod", $"balance/{playerId}");
    if (balance >= item.Cost) {
      // Process purchase
      SharedDatabase.Set("EconomyMod", $"balance/{playerId}", 
        balance - item.Cost);
    }
  }
}
```

### Scenario: Complex relational data
**Best choice:** Database (LiteDB)

```csharp
var db = new Database("GameMod");

// Save with relationships
var guild = new Guild {
  Id = guildId,
  Name = "Dragon Slayers",
  Members = new List<GuildMember> {
    new GuildMember { PlayerId = "alice", Role = "Leader" },
    new GuildMember { PlayerId = "bob", Role = "Officer" },
    new GuildMember { PlayerId = "charlie", Role = "Member" }
  },
  Treasury = new GuildTreasury { Gold = 10000, Items = new List<Item>() }
};

db.Set($"guilds/{guildId}", guild);
```

## Storage Comparison Table

| Use Case | Best Choice | Reason |
|----------|------------|--------|
| Plugin settings | Settings | BepInEx integration, editable `.cfg` files |
| Player profiles | Database | Complex structure, type-safe |
| Simple KV data | JsonDatabase | Human-readable, cached |
| Cache data | JsonDatabase | In-memory cache, fast access |
| Cross-mod data | SharedDatabase | No dependencies, namespaced |
| Query/filter | Database | Full query support |
| World/game state | Database | Complex relationships |

## Complete Plugin Example

```csharp
public class MyGamePlugin : BasePlugin {
  private Settings _settings;
  private JsonDatabase _cache;
  private Database _gameData;

  public override void Load() {
    // Configuration
    _settings = new Settings("MyGamePlugin", this);
    _settings.Section("Game")
      .Add("Difficulty", "Normal", "Difficulty level")
      .Add("EnablePvP", true, "Enable PvP");
    _settings.Save();

    // Game state (complex, queryable)
    _gameData = new Database("MyGamePlugin_Data");
    _gameData.EnableAutoBackup();

    // Cache (fast, simple)
    _cache = new JsonDatabase("MyGamePlugin_Cache");
    _cache.EnableAutoBackup();

    Log.Message("Plugin loaded successfully");
  }

  public void SaveGameState() {
    var gameState = new GameState {
      ActivePlayers = GetActivePlayers(),
      WorldBosses = GetWorldBosses(),
      SavedAt = DateTime.UtcNow
    };

    _gameData.Set("world/state", gameState);
  }

  public void UpdatePlayerRankings() {
    var rankings = CalculateRankings();
    _cache.Set("cache/rankings", rankings);
  }

  public void OnDestroy() {
    _settings.Dispose();
    // Databases auto-save on shutdown
  }
}
```

## Tips

✅ **Use Settings for configuration** — users expect `.cfg` files
✅ **Use Database for game state** — relationships matter
✅ **Use JsonDatabase for cache** — human-readable backup
✅ **Use SharedDatabase for mod interop** — avoid hard dependencies
✅ **Enable backups** — protect against data loss
✅ **Organize keys** — use namespacing (players/, cache/, etc.)

❌ **Don't mix concerns** — keep config separate from game data
❌ **Don't save to Settings** — only for configuration
❌ **Don't use SharedDatabase for everything** — defeats privacy
❌ **Don't ignore backups** — implement them from day one