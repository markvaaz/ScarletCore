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
using ScarletCore.Events;

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
    if (!PlayerIds.TryGetValue(userData.PlatformId, out PlayerData playerData)) {
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

      playerData = newData;

      // Add to all primary indexes
      PlayerIds[userData.PlatformId] = playerData;
      AllPlayers.Add(newData);
    }

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

      if (unnamedPlayerCleanName.Equals(normalizedSearchName, StringComparison.CurrentCultureIgnoreCase)) {
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

  /// <summary>
  /// Renames the specified player to a new character name.
  /// </summary>
  /// <param name="player">The player whose character name will be changed.</param>
  /// <param name="newName">The new name to assign to the player (as FixedString64Bytes).</param>
  public static void RenamePlayer(PlayerData player, FixedString64Bytes newName) {
    var des = GameSystems.DebugEventsSystem;

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

    EventManager.Emit(PlayerEvents.CharacterRenamed, player);

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
  /// Sets a name tag for the specified player at tag index 0 (closest to name).
  /// This is a legacy method that maintains backwards compatibility.
  /// For multi-tag support, use SetTagAtIndex instead.
  /// </summary>
  /// <param name="player">The player to set the tag for</param>
  /// <param name="tag">The tag to set (any text is allowed)</param>
  /// <returns>True if the tag was set successfully, false if player or tag is null/empty</returns>
  public static bool SetNameTag(PlayerData player, string tag) {
    return SetTagAtIndex(player, tag, 0);
  }


  /// <summary>
  /// Sets a tag at a specific index in the player's name, allowing for multiple tags or custom tag placement.
  /// </summary>
  /// <param name="player">The player whose name tag will be set.</param>
  /// <param name="tag">The tag text to insert into the player's name.</param>
  /// <param name="tagIndex">The index at which to insert the tag (0-based).</param>
  /// <returns>True if the tag was successfully set; otherwise, false.</returns>
  public static bool SetNameTag(PlayerData player, string tag, int tagIndex) {
    return SetTagAtIndex(player, tag, tagIndex);
  }

  /// <summary>
  /// Removes any existing tag from the player's name, leaving only the clean name.
  /// This removes ALL tags. For removing specific tags, use RemoveTagAtIndex or RemoveTagByText.
  /// </summary>
  /// <param name="player">The player to remove the tag from</param>
  public static void RemoveNameTag(PlayerData player) {
    RemoveAllTags(player);
  }

  /// <summary>
  /// Removes a tag at a specific index. Tags at higher indices (left side) shift down to fill the gap.
  /// </summary>
  /// <param name="player">The player to remove the tag from</param>
  /// <param name="tagIndex">The index of the tag to remove</param>
  /// <returns>True if tag was removed, false if player is null or index is invalid</returns>
  public static bool RemoveNameTag(PlayerData player, int tagIndex) {
    if (player == null || tagIndex < 0) return false;

    var cleanName = ExtractCleanName(player.FullName);
    var tags = ExtractTags(player.FullName);

    // Check if index exists
    if (tagIndex >= tags.Count) return false;

    // Remove the tag at the specified index
    tags.RemoveAt(tagIndex);

    // Build and apply the new name
    var newFullName = BuildFullName(cleanName, tags);
    var newName = new FixedString64Bytes(newFullName);
    RenamePlayer(player, newName);

    return true;
  }

  /// <summary>
  /// Removes the first occurrence of a tag that matches the specified text (case-insensitive).
  /// Searches from tag0 (rightmost) to higher indices (leftmost).
  /// </summary>
  /// <param name="player">The player to remove the tag from</param>
  /// <param name="tagText">The text of the tag to remove</param>
  /// <returns>True if tag was found and removed, false otherwise</returns>
  public static bool RemoveNameTag(PlayerData player, string tagText) {
    if (player == null || string.IsNullOrWhiteSpace(tagText)) return false;

    var cleanName = ExtractCleanName(player.FullName);
    var tags = ExtractTags(player.FullName);

    var normalizedTagText = tagText.Trim().ToLower();

    // Find the first tag that matches (searching from tag0 upwards)
    var indexToRemove = -1;
    for (int i = 0; i < tags.Count; i++) {
      if (tags[i].Equals(normalizedTagText, StringComparison.CurrentCultureIgnoreCase)) {
        indexToRemove = i;
        break;
      }
    }

    // Tag not found
    if (indexToRemove == -1) return false;

    // Remove the tag
    tags.RemoveAt(indexToRemove);

    // Build and apply the new name
    var newFullName = BuildFullName(cleanName, tags);
    var newName = new FixedString64Bytes(newFullName);
    RenamePlayer(player, newName);

    return true;
  }

  /// <summary>
  /// Removes all tags from a player, leaving only the clean name.
  /// </summary>
  /// <param name="player">The player to remove all tags from</param>
  /// <returns>True if successful, false if player is null</returns>
  public static bool RemoveAllTags(PlayerData player) {
    if (player == null) return false;

    var cleanName = ExtractCleanName(player.FullName);

    // If the clean name is the same as full name, no tags exist
    if (cleanName == player.FullName.Trim()) return true;

    // Rename the player with just the clean name
    var newName = new FixedString64Bytes(cleanName);
    RenamePlayer(player, newName);

    return true;
  }


  /// <summary>
  /// Extracts the clean player name by removing all tags from the beginning of the name.
  /// Tags are considered everything before the last word (the actual name).
  /// </summary>
  /// <param name="fullName">The full name that may contain tags (e.g., "ADM GOD S Mark")</param>
  /// <returns>The clean name without any tags (e.g., "Mark")</returns>
  public static string ExtractCleanName(string fullName) {
    if (string.IsNullOrEmpty(fullName)) return fullName;

    var trimmed = fullName.Trim();

    // Find the last space - everything after it is the actual name
    var lastSpaceIndex = trimmed.LastIndexOf(' ');

    // If no space found, return the whole name (no tags present)
    if (lastSpaceIndex == -1) return trimmed;

    // Return everything after the last space (the actual name)
    return trimmed[(lastSpaceIndex + 1)..].Trim();
  }

  /// <summary>
  /// Extracts all tags from the full name as a list.
  /// Tags are indexed from right to left (tag0 is closest to the name).
  /// </summary>
  /// <param name="fullName">The full name with tags</param>
  /// <returns>List of tags indexed from right to left (tag0 first)</returns>
  public static List<string> ExtractTags(string fullName) {
    if (string.IsNullOrEmpty(fullName)) return [];

    var trimmed = fullName.Trim();
    var lastSpaceIndex = trimmed.LastIndexOf(' ');

    // No tags if no space found
    if (lastSpaceIndex == -1) return [];

    // Get everything before the last space (all tags)
    var tagsSection = trimmed[..lastSpaceIndex].Trim();

    if (string.IsNullOrEmpty(tagsSection)) return [];

    // Split by space and reverse to get right-to-left ordering
    var tags = tagsSection.Split([' '], StringSplitOptions.RemoveEmptyEntries);
    var tagList = new List<string>(tags);
    tagList.Reverse(); // Reverse to get tag0 first (rightmost tag)

    return tagList;
  }

  /// <summary>
  /// Builds a full name from clean name and tags list.
  /// Tags list should be ordered as tag0, tag1, tag2... (right to left priority)
  /// </summary>
  /// <param name="cleanName">The base player name</param>
  /// <param name="tags">List of tags ordered as tag0, tag1, tag2...</param>
  /// <returns>Full name with tags in correct format</returns>
  private static string BuildFullName(string cleanName, List<string> tags) {
    if (tags == null || tags.Count == 0) return cleanName;

    // Reverse tags to get left-to-right display order
    var reversedTags = new List<string>(tags);
    reversedTags.Reverse();

    // Join: "tag2 tag1 tag0 Name"
    return string.Join(" ", reversedTags) + " " + cleanName;
  }

  /// <summary>
  /// Gets a specific tag by index from a player's name.
  /// Index 0 is the tag immediately left of the name (rightmost tag).
  /// </summary>
  /// <param name="player">The player to get the tag from</param>
  /// <param name="tagIndex">The index of the tag (0 = closest to name)</param>
  /// <param name="tag">The tag value if found</param>
  /// <returns>True if tag exists at that index, false otherwise</returns>
  public static bool TryGetTag(PlayerData player, int tagIndex, out string tag) {
    tag = null;
    if (player == null || tagIndex < 0) return false;

    var tags = ExtractTags(player.FullName);

    if (tagIndex >= tags.Count) return false;

    tag = tags[tagIndex];
    return true;
  }

  /// <summary>
  /// Gets all tags from a player as a list ordered by index (tag0, tag1, tag2...).
  /// </summary>
  /// <param name="player">The player to get tags from</param>
  /// <returns>List of tags ordered by index</returns>
  public static List<string> GetAllTags(PlayerData player) {
    if (player == null) return [];
    return ExtractTags(player.FullName);
  }

  /// <summary>
  /// Sets a tag at a specific index. If a tag already exists at that index, it will be replaced.
  /// Tags at higher indices (left side) are pushed further left to make room.
  /// </summary>
  /// <param name="player">The player to set the tag for</param>
  /// <param name="tagIndex">The index where to set the tag (0 = closest to name)</param>
  /// <param name="tag">The tag text to set</param>
  /// <returns>True if successful, false if player or tag is null/empty or index is negative</returns>
  public static bool SetTagAtIndex(PlayerData player, string tag, int tagIndex) {
    if (player == null || string.IsNullOrWhiteSpace(tag) || tagIndex < 0) return false;

    var cleanName = ExtractCleanName(player.FullName);
    var tags = ExtractTags(player.FullName);

    // Ensure the list is large enough to accommodate the index
    while (tags.Count <= tagIndex) {
      tags.Add(null); // Placeholder for empty slots
    }

    // Set the tag at the specified index
    tags[tagIndex] = tag.Trim();

    // Remove any null placeholders that may have been created
    tags = [.. tags.Where(t => !string.IsNullOrEmpty(t))];

    // Build and apply the new name
    var newFullName = BuildFullName(cleanName, tags);
    var newName = new FixedString64Bytes(newFullName);
    RenamePlayer(player, newName);

    return true;
  }
}