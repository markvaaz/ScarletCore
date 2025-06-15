using ScarletCore.Data;
using System.Collections.Generic;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using System.Linq;
using System;
using ScarletCore.Utils;
using ScarletCore.Systems;

namespace ScarletCore.Services;

/// <summary>
/// Manages player data caching and retrieval with multiple indexing strategies for optimal performance.
/// Handles player lifecycle including connection, disconnection, and name changes.
/// </summary>
public static class PlayerService {

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
        SetPlayerCache(entity);
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
    var name = userData.CharacterName.Value;

    // Check if this is a new player we haven't seen before
    if (!PlayerIds.ContainsKey(userData.PlatformId)) {
      PlayerData newData = new();

      // Handle new players based on whether they have a character name set
      if (string.IsNullOrEmpty(name)) {
        // New player with no character name - add to unnamed list for later processing
        // This happens when players create an account but haven't chosen a character name yet
        UnnamedPlayers.Add(newData);
      } else {
        // New player with a name - add directly to the named player index
        PlayerNames[name.ToLower()] = newData;
        newData.SetName(name);
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

    // Detect name changes by comparing cached name vs current entity name
    // CachedName returns the internal _name field before any lazy loading occurs
    var nameChanged = !string.IsNullOrEmpty(playerData.CachedName) && playerData.Name != name;

    // Detect when an unnamed player has finally set their character name
    // This happens when TryGetByName triggers lazy loading or when player sets name for first time
    var nameIsNoLongerEmpty = string.IsNullOrEmpty(playerData.CachedName) && !string.IsNullOrEmpty(name);

    // Handle name changes and transitions from unnamed to named
    if (nameChanged || nameIsNoLongerEmpty) {
      // Remove old name from the lookup index (if it existed)
      PlayerNames.Remove(playerData.Name.ToLower());

      // Update the internal cached name
      playerData.SetName(name);

      // Add new name to the lookup index
      PlayerNames[name.ToLower()] = playerData;

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
    return [.. PlayerNetworkIds.Values];
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
  /// </summary>
  /// <param name="name">The character name to search for (case-insensitive)</param>
  /// <param name="playerData">The found player data, or null if not found</param>
  /// <returns>True if player was found, false otherwise</returns>
  public static bool TryGetByName(string name, out PlayerData playerData) {
    // First, try to get the player from the named players index (fastest lookup)
    if (PlayerNames.TryGetValue(name.ToLower(), out playerData)) {
      return true;
    }

    // If not found in named players and no unnamed players exist, player doesn't exist
    if (UnnamedPlayers.Count == 0) return false;

    // Search through unnamed players to see if any have the target name
    // This triggers lazy loading of names for unnamed players
    playerData = UnnamedPlayers.FirstOrDefault(p => p.Name.ToLower() == name.ToLower());

    var exist = playerData != null;

    // If found in unnamed players, promote them to named players
    if (exist) {
      // Move player from unnamed to named index for future fast lookups
      PlayerNames[name.ToLower()] = playerData;
      UnnamedPlayers.Remove(playerData);
    }

    return exist;
  }
}


