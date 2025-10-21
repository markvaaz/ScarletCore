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
  /// Extracts the clean player name by removing all tags from the beginning of the name.
  /// Tags are separated by spaces and the last word is considered the actual name.
  /// Examples: "tag1 Mark" → "Mark", "tag3 tag2 tag1 Player" → "Player"
  /// </summary>
  /// <param name="fullName">The full name that may contain tags (e.g., "tag1 Mark", "tag3 tag2 tag1 Player")</param>
  /// <returns>The clean name without tags (e.g., "Mark", "Player")</returns>
  public static string ExtractCleanName(string fullName) {
    if (string.IsNullOrEmpty(fullName)) return fullName;

    var trimmed = fullName.Trim();

    // Split by spaces and take the last part as the actual name
    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // If only one part, it's just a name without tags
    if (parts.Length <= 1) return trimmed;

    // Return the last part (the actual name)
    return parts[parts.Length - 1];
  }

  /// <summary>
  /// Extracts all tags from a full name, returning them in order from left to right.
  /// </summary>
  /// <param name="fullName">The full name that may contain tags</param>
  /// <returns>Array of tags, or empty array if no tags</returns>
  public static string[] ExtractTags(string fullName) {
    if (string.IsNullOrEmpty(fullName)) return [];

    var trimmed = fullName.Trim();
    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // If only one part or no parts, no tags exist
    if (parts.Length <= 1) return [];

    // Return all parts except the last one (which is the name)
    var tags = new string[parts.Length - 1];
    Array.Copy(parts, tags, parts.Length - 1);
    return tags;
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
        // New player with a name - set the name first, then add to cache
        newData.SetName(cleanName);
        PlayerNames[cleanName.ToLower()] = newData;
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

    // Get the old cached name before any updates (avoid lazy loading side effects)
    var oldCachedName = playerData.CachedName;

    // Detect name changes by comparing old cached name vs current clean name
    var nameChanged = !string.IsNullOrEmpty(oldCachedName) && oldCachedName != cleanName;

    // Detect when an unnamed player has finally set their character name
    var nameIsNoLongerEmpty = string.IsNullOrEmpty(oldCachedName) && !string.IsNullOrEmpty(cleanName);

    // Handle name changes and transitions from unnamed to named
    if (nameChanged || nameIsNoLongerEmpty) {
      // Remove old name from the lookup index using the cached old name
      if (!string.IsNullOrEmpty(oldCachedName)) {
        PlayerNames.Remove(oldCachedName.ToLower());
      }

      // Update the internal cached name with new clean name
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
  /// Automatically extracts clean names by removing tags.
  /// </summary>
  /// <param name="name">The character name to search for (case-insensitive, tags will be removed)</param>
  /// <param name="playerData">The found player data, or null if not found</param>
  /// <returns>True if player was found, false otherwise</returns>
  public static bool TryGetByName(string name, out PlayerData playerData) {
    // Extract clean name from the search query (remove tags if present)
    var cleanSearchName = ExtractCleanName(name);

    // Normalize to lowercase for case-insensitive comparison
    var normalizedSearchName = cleanSearchName.ToLower();

    // First, try to get the player from the named players index (fastest lookup)
    if (PlayerNames.TryGetValue(normalizedSearchName, out playerData)) {
      return true;
    }

    // If not found in named players and no unnamed players exist, player doesn't exist
    if (UnnamedPlayers.Count == 0) {
      playerData = null;
      return false;
    }

    // Search through unnamed players to see if any have the target clean name
    // This triggers lazy loading of names for unnamed players
    foreach (var unnamedPlayer in UnnamedPlayers.ToList()) {
      var unnamedPlayerFullName = unnamedPlayer.User.CharacterName.ToString();
      var unnamedPlayerCleanName = ExtractCleanName(unnamedPlayerFullName);

      // Skip if still empty
      if (string.IsNullOrEmpty(unnamedPlayerCleanName)) continue;

      if (unnamedPlayerCleanName.ToLower() == normalizedSearchName) {
        // Found the player - promote them to named players
        playerData = unnamedPlayer;

        // Update the cached name with clean name
        playerData.SetName(unnamedPlayerCleanName);

        // Move player from unnamed to named index for future fast lookups
        PlayerNames[unnamedPlayerCleanName.ToLower()] = playerData;
        UnnamedPlayers.Remove(playerData);

        return true;
      }
    }

    // Player not found
    playerData = null;
    return false;
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

    if (!player.CharacterEntity.Has<AttachedBuffer>()) return;

    var attachedBuffer = player.CharacterEntity.ReadBuffer<AttachedBuffer>();

    foreach (var entry in attachedBuffer) {
      if (entry.PrefabGuid.GuidHash != -892362184) continue;
      var icon = entry.Entity.Read<PlayerMapIcon>();
      icon.UserName = newName;
      entry.Entity.Write(icon);
    }
  }

  /// <summary>
  /// Sets a name tag for the specified player at the given index.
  /// Tags are indexed from right to left (closest to name = index 0).
  /// Format: "tag2 tag1 tag0 Name" where tag0 is index 0, tag1 is index 1, etc.
  /// </summary>
  /// <param name="player">The player to set the tag for</param>
  /// <param name="tag">The tag to set (cannot contain spaces)</param>
  /// <param name="tagIndex">The index position (0 = closest to name, 1 = second closest, etc.)</param>
  /// <returns>True if the tag was set successfully, false if validation failed</returns>
  public static bool SetNameTag(PlayerData player, string tag, int tagIndex = 0) {
    if (player == null || string.IsNullOrEmpty(tag)) return false;

    // Validate that tag doesn't contain spaces (required for multiple tag support)
    if (tag.Contains(' ')) return false;

    if (tagIndex < 0) return false;

    // Get current clean name and existing tags
    var cleanName = ExtractCleanName(player.FullName);
    var existingTags = ExtractTags(player.FullName).ToList();

    // Extend the tags list if necessary to accommodate the new index
    while (existingTags.Count <= tagIndex) {
      existingTags.Add("");
    }

    // Set the tag at the specified index
    existingTags[tagIndex] = tag;

    // Remove empty tags from the end to keep the list clean
    while (existingTags.Count > 0 && string.IsNullOrEmpty(existingTags[existingTags.Count - 1])) {
      existingTags.RemoveAt(existingTags.Count - 1);
    }

    // Build the new name: "tag2 tag1 tag0 Name" (reverse order)
    var tagParts = new List<string>();
    for (int i = existingTags.Count - 1; i >= 0; i--) {
      if (!string.IsNullOrEmpty(existingTags[i])) {
        tagParts.Add(existingTags[i]);
      }
    }

    // Combine tags and name
    string newFullName;
    if (tagParts.Count > 0) {
      newFullName = $"{string.Join(" ", tagParts)} {cleanName}";
    } else {
      newFullName = cleanName;
    }

    // Rename the player with the new tagged name
    var newName = new FixedString64Bytes(newFullName);
    RenamePlayer(player, newName);
    return true;
  }

  /// <summary>
  /// Removes a specific tag by index from the player's name.
  /// Tags are indexed from right to left (closest to name = index 0).
  /// </summary>
  /// <param name="player">The player to remove the tag from</param>
  /// <param name="tagIndex">The index of the tag to remove (0 = closest to name)</param>
  /// <returns>True if a tag was removed, false if no tag at that index</returns>
  public static bool RemoveNameTag(PlayerData player, int tagIndex) {
    if (player == null || tagIndex < 0) return false;

    // Get current clean name and existing tags
    var cleanName = ExtractCleanName(player.FullName);
    var existingTags = ExtractTags(player.FullName).ToList();

    // Check if the index is valid
    if (tagIndex >= existingTags.Count) return false;

    // Remove the tag at the specified index
    existingTags.RemoveAt(tagIndex);

    // Build the new name: "tag2 tag1 tag0 Name" (reverse order)
    var tagParts = new List<string>();
    for (int i = existingTags.Count - 1; i >= 0; i--) {
      if (!string.IsNullOrEmpty(existingTags[i])) {
        tagParts.Add(existingTags[i]);
      }
    }

    // Combine tags and name
    string newFullName;
    if (tagParts.Count > 0) {
      newFullName = $"{string.Join(" ", tagParts)} {cleanName}";
    } else {
      newFullName = cleanName;
    }

    // Rename the player with the new name
    var newName = new FixedString64Bytes(newFullName);
    RenamePlayer(player, newName);
    return true;
  }

  /// <summary>
  /// Removes all tags from the player's name, leaving only the clean name.
  /// </summary>
  /// <param name="player">The player to remove all tags from</param>
  public static void RemoveAllNameTags(PlayerData player) {
    if (player == null) return;

    // Get the current clean name (without any existing tags)
    var cleanName = ExtractCleanName(player.FullName);

    // If the clean name is the same as full name, no tags exist
    if (cleanName == player.FullName.Trim()) return;

    // Rename the player with just the clean name
    var newName = new FixedString64Bytes(cleanName);
    RenamePlayer(player, newName);
  }

  /// <summary>
  /// Gets a specific tag by index from the player's name.
  /// Tags are indexed from right to left (closest to name = index 0).
  /// </summary>
  /// <param name="player">The player to get the tag from</param>
  /// <param name="tagIndex">The index of the tag to get (0 = closest to name)</param>
  /// <returns>The tag at the specified index, or null if no tag at that index</returns>
  public static string GetNameTag(PlayerData player, int tagIndex) {
    if (player == null || tagIndex < 0) return null;

    var existingTags = ExtractTags(player.FullName);

    if (tagIndex >= existingTags.Length) return null;

    return existingTags[tagIndex];
  }

  /// <summary>
  /// Gets all tags from the player's name.
  /// Returns tags in order from closest to name (index 0) to farthest (highest index).
  /// </summary>
  /// <param name="player">The player to get tags from</param>
  /// <returns>Array of tags, or empty array if no tags</returns>
  public static string[] GetAllNameTags(PlayerData player) {
    if (player == null) return [];

    return ExtractTags(player.FullName);
  }
}


