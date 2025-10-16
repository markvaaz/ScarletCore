using ScarletCore.Data;
using System.Collections.Generic;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using System.Linq;
using System;
using ScarletCore.Utils;
using ScarletCore.Systems;
using ProjectM;

namespace ScarletCore.Services;

/// <summary>
/// Manages player data caching and retrieval with multiple indexing strategies for optimal performance.
/// Handles player lifecycle including connection, disconnection, and name changes.
/// </summary>
public static class PlayerService {
  public static string[] Tags = ["🄰", "🄱", "🄲", "🄳", "🄴", "🄵", "🄶", "🄷", "🄸", "🄹", "🄺", "🄻", "🄼", "🄽", "🄾", "🄿", "🅀", "🅁", "🅂", "🅃", "🅄", "🅅", "🅆", "🅇", "🅈", "🅉", "🅊", "🅋", "🅌", "🅍", "🅎", "🅏", "🅐", "🅑", "🅒", "🅓", "🅔", "🅕", "🅖", "🅗", "🅘", "🅙", "🅚", "🅛", "🅜", "🅝", "🅞", "🅟", "🅠", "🅡", "🅢", "🅣", "🅤", "🅥", "🅦", "🅧", "🅨", "🅩", "🅰", "🅱", "🅲", "🅳", "🅴", "🅵", "🅶", "🅷", "🅸", "🅹", "🅺", "🅻", "🅼", "🅽", "🅾", "🅿", "🆀", "🆁", "🆂", "🆃", "🆄", "🆅", "🆆", "🆇", "🆈", "🆉", "🆊", "🆋", "🆌", "🆍", "🆎", "🆏", "🆐", "🆑", "🆒", "🆓", "🆔", "🆕", "🆖", "🆗", "🆘", "🆙", "🆚", "🆛", "🆜", "🆝", "🆞", "🆟", "🆠", "🆡", "🆢", "🆣", "🆤", "🆥", "🆦", "🆧", "🆨", "🆩", "🆪", "🆫", "🆬", "🆭"];

  /// <summary>
  /// Dictionary for fast player lookup by character name (case-insensitive)
  /// </summary>
  public static readonly Dictionary<string, PlayerData> PlayerNames = [];

  /// <summary>
  /// Dictionary for fast player lookup by platform ID (Steam ID, etc.)
  /// </summary>
  public static readonly Dictionary<ulong, PlayerData> PlayerIds = [];

  /// <summary>
  /// Dictionary for fast player lookup by network ID (active connections only)
  /// </summary>
  public static readonly Dictionary<NetworkId, PlayerData> PlayerNetworkIds = [];

  /// <summary>
  /// List of players who haven't set their character name yet (new accounts)
  /// </summary>
  private static readonly List<PlayerData> UnnamedPlayers = [];

  /// <summary>
  /// Complete list of all known players (online and offline)
  /// </summary>
  public static readonly List<PlayerData> AllPlayers = [];

  /// <summary>
  /// Extracts the clean player name by removing any tags from the beginning of the name.
  /// Supports two tag formats:
  /// 1. Square bracket tags: [TAG] or [🆘] or [🅀🄰]
  /// 2. Direct character tags: 🆘 or 🅀🄰 (using characters from Tags array)
  /// </summary>
  /// <param name="fullName">The full name that may contain tags (e.g., "[Vaaz] Mark", "🆘 Mark", "🅀🄰 Mark")</param>
  /// <returns>The clean name without tags (e.g., "Mark")</returns>
  private static string ExtractCleanName(string fullName) {
    if (string.IsNullOrEmpty(fullName)) return fullName;

    var trimmed = fullName.Trim();

    // First, check if the name starts with a tag in square brackets
    if (trimmed.StartsWith("[")) {
      var closingBracketIndex = trimmed.IndexOf(']');
      if (closingBracketIndex > 0 && closingBracketIndex < trimmed.Length - 1) {
        // Extract everything after the closing bracket and trim whitespace
        return trimmed.Substring(closingBracketIndex + 1).Trim();
      }
    }

    // Second, check if the name starts with direct tag characters (without brackets)
    int tagEndIndex = 0;
    for (int i = 0; i < trimmed.Length; i++) {
      string currentChar = trimmed[i].ToString();

      // Check if current character is in the Tags array
      if (Tags.Contains(currentChar)) {
        tagEndIndex = i + 1;
      } else {
        // Stop when we hit a character that's not a tag character
        break;
      }
    }

    // If we found tag characters at the beginning, remove them
    if (tagEndIndex > 0) {
      return trimmed.Substring(tagEndIndex).Trim();
    }

    // Return the original name if no tag pattern is found
    return trimmed;
  }

  /// <summary>
  /// Initializes the player service by loading all existing users from the entity manager
  /// and populating the various lookup caches
  /// </summary>
  internal static void Initialize() {
    // Clear any existing cache data to ensure clean state
    ClearCache();

    // Create entity query to find all User entities (both active and disabled)
    EntityQueryBuilder queryBuilder = new(Allocator.Temp);
    queryBuilder.AddAll(ComponentType.ReadOnly<User>());
    queryBuilder.WithOptions(EntityQueryOptions.IncludeDisabled);

    EntityQuery query = GameSystems.EntityManager.CreateEntityQuery(ref queryBuilder);

    try {
      // Get all user entities and populate the cache
      var userEntities = query.ToEntityArray(Allocator.Temp);

      foreach (var entity in userEntities) {
        // Add each user to the cache system
        SetPlayerCache(entity, true);
      }
    } catch (Exception e) {
      Log.Error(e);
    } finally {
      // Always dispose of temporary allocations to prevent memory leaks
      query.Dispose();
      queryBuilder.Dispose();
    }
  }

  /// <summary>
  /// Clears all cached player data and resets the lookup indexes.
  /// This is useful for resetting the service state, such as during server restarts or mod reloads.
  /// </summary>
  internal static void ClearCache() {
    PlayerNames.Clear();
    PlayerIds.Clear();
    PlayerNetworkIds.Clear();
    UnnamedPlayers.Clear();
    AllPlayers.Clear();
  }

  /// <summary>
  /// Creates or updates player data in the cache system with multiple indexing strategies.
  /// Handles new player registration, name changes, and online/offline state management.
  /// </summary>
  /// <param name="userEntity">The user entity containing player data</param>
  /// <param name="isOffline">Whether the player is going offline (affects NetworkId caching)</param>
  /// <returns>The PlayerData object for the user</returns>
  internal static PlayerData SetPlayerCache(Entity userEntity, bool isOffline = false) {
    // Extract core data from the user entity
    var networkId = userEntity.Read<NetworkId>();
    var userData = userEntity.Read<User>();
    var fullName = userData.CharacterName.Value;
    var cleanName = ExtractCleanName(fullName);

    // Check if this is a new player we haven't seen before
    if (!PlayerIds.ContainsKey(userData.PlatformId)) {
      PlayerData newData = new();

      // Handle new players based on whether they have a character name set
      if (string.IsNullOrEmpty(cleanName)) {
        // New player with no character name - add to unnamed list for later processing
        // This happens when players create an account but haven't chosen a character name yet
        UnnamedPlayers.Add(newData);
      } else {
        // New player with a name - add directly to the named player index using clean name
        PlayerNames[cleanName.ToLower()] = newData;
        newData.SetName(cleanName);
      }

      // Add to all primary indexes
      PlayerIds[userData.PlatformId] = newData;
      AllPlayers.Add(newData);
    }

    // Get the existing player data from cache
    var playerData = PlayerIds[userData.PlatformId];

    // Update the entity reference (important as entities can change between sessions)
    playerData.UserEntity = userEntity;

    // Manage network ID indexing based on online/offline status
    if (isOffline) {
      // Remove network ID when going offline as it may change on reconnection
      PlayerNetworkIds.Remove(networkId);
    } else {
      // Add/update network ID for online players (used for fast lookups during gameplay)
      PlayerNetworkIds[networkId] = playerData;
    }

    // Detect name changes by comparing cached name vs current clean name
    // CachedName returns the internal _name field before any lazy loading occurs
    var nameChanged = !string.IsNullOrEmpty(playerData.CachedName) && playerData.Name != cleanName;

    // Detect when an unnamed player has finally set their character name
    // This happens when TryGetByName triggers lazy loading or when player sets name for first time
    var nameIsNoLongerEmpty = string.IsNullOrEmpty(playerData.CachedName) && !string.IsNullOrEmpty(cleanName);

    // Handle name changes and transitions from unnamed to named
    if (nameChanged || nameIsNoLongerEmpty) {
      // Remove old name from the lookup index (if it existed)
      PlayerNames.Remove(playerData.Name.ToLower());

      // Update the internal cached name with clean name
      playerData.SetName(cleanName);

      // Add new clean name to the lookup index
      PlayerNames[cleanName.ToLower()] = playerData;

      // Remove from unnamed players list (handles both name changes and first-time naming)
      UnnamedPlayers.Remove(playerData);
    }

    return playerData;
  }

  /// <summary>
  /// Gets all players with admin privileges
  /// </summary>
  /// <returns>List of admin players</returns>
  public static List<PlayerData> GetAdmins() {
    return [.. AllPlayers.Where(p => p.IsAdmin)];
  }

  /// <summary>
  /// Gets all currently connected players
  /// </summary>
  /// <returns>List of online players</returns>
  public static List<PlayerData> GetAllConnected() {
    List<PlayerData> connectedPlayers = [];
    foreach (var player in AllPlayers) {
      if (player.IsOnline) {
        connectedPlayers.Add(player);
      }
    }

    return connectedPlayers;
  }

  /// <summary>
  /// Attempts to retrieve a player by their platform ID (Steam ID, etc.)
  /// </summary>
  /// <param name="platformId">The platform ID to search for</param>
  /// <param name="playerData">The found player data, or null if not found</param>
  /// <returns>True if player was found, false otherwise</returns>
  public static bool TryGetById(ulong platformId, out PlayerData playerData) {
    return PlayerIds.TryGetValue(platformId, out playerData);
  }

  /// <summary>
  /// Attempts to retrieve a player by their network ID (only works for online players)
  /// </summary>
  /// <param name="networkId">The network ID to search for</param>
  /// <param name="playerData">The found player data, or null if not found</param>
  /// <returns>True if player was found, false otherwise</returns>
  public static bool TryGetByNetworkId(NetworkId networkId, out PlayerData playerData) {
    return PlayerNetworkIds.TryGetValue(networkId, out playerData);
  }

  /// <summary>
  /// Attempts to retrieve a player by their character name.
  /// Also handles discovery of unnamed players who have set their name since last update.
  /// Automatically extracts clean names by removing tags in square brackets.
  /// </summary>
  /// <param name="name">The character name to search for (case-insensitive, tags will be removed)</param>
  /// <param name="playerData">The found player data, or null if not found</param>
  /// <returns>True if player was found, false otherwise</returns>
  public static bool TryGetByName(string name, out PlayerData playerData) {
    // Extract clean name from the search query (remove tags if present)
    var cleanSearchName = ExtractCleanName(name);

    // First, try to get the player from the named players index (fastest lookup)
    if (PlayerNames.TryGetValue(cleanSearchName.ToLower(), out playerData)) {
      return true;
    }

    // If not found in named players and no unnamed players exist, player doesn't exist
    if (UnnamedPlayers.Count == 0) return false;

    // Search through unnamed players to see if any have the target clean name
    // This triggers lazy loading of names for unnamed players
    playerData = UnnamedPlayers.FirstOrDefault(p => ExtractCleanName(p.Name).ToLower() == cleanSearchName.ToLower());

    var exist = playerData != null;

    // If found in unnamed players, promote them to named players
    if (exist) {
      // Move player from unnamed to named index for future fast lookups using clean name
      var playerCleanName = ExtractCleanName(playerData.Name);
      PlayerNames[playerCleanName.ToLower()] = playerData;
      UnnamedPlayers.Remove(playerData);

      // Update the cached name to be the clean name
      playerData.SetName(playerCleanName);
    }

    return exist;
  }

  public static void RenamePlayer(PlayerData player, FixedString64Bytes newName) {
    var des = GameSystems.DebugEventsSystem;
    var networkId = player.NetworkId;

    var renameEvent = new RenameUserDebugEvent {
      NewName = newName,
      Target = player.NetworkId
    };

    var fromCharacter = new FromCharacter {
      User = player.UserEntity,
      Character = player.CharacterEntity
    };

    des.RenameUser(fromCharacter, renameEvent);

    SetPlayerCache(player.UserEntity);

    var attachedBuffer = player.CharacterEntity.ReadBuffer<AttachedBuffer>();

    foreach (var entry in attachedBuffer) {
      if (entry.PrefabGuid.GuidHash != -892362184) continue;
      var icon = entry.Entity.Read<PlayerMapIcon>();
      icon.UserName = newName;
      entry.Entity.Write(icon);
    }
  }
}


