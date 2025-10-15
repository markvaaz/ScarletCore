using Unity.Entities;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using System.Reflection;
using ProjectM;
using Unity.Mathematics;
using ScarletCore.Services;
using Stunlock.Core;
using ScarletCore.Utils;

namespace ScarletCore.Data;

/// <summary>
/// Represents comprehensive player data and provides convenient access to player-related information.
/// This class serves as a wrapper around Unity ECS entities to simplify player data management.
/// </summary>
/// <summary>
/// Creates a new instance of PlayerData.
/// </summary>
public class PlayerData() {
  /// <summary>
  /// The underlying Unity ECS entity representing the user.
  /// This is the primary entity that contains all player-related components.
  /// </summary>
  public Entity UserEntity;

  /// <summary>
  /// Gets the User component from the UserEntity.
  /// Contains core player information like character name, platform ID, and connection status.
  /// </summary>
  public User User => UserEntity.Read<User>();

  /// <summary>
  /// Cached player name to avoid repeated string conversions.
  /// Null until first access, then cached for performance.
  /// </summary>
  private string _name = null;

  /// <summary>
  /// Gets the network identifier for this player.
  /// Used for network communication and player identification.
  /// </summary>
  public NetworkId NetworkId => UserEntity.Read<NetworkId>();

  /// <summary>
  /// Gets the player's character name with lazy loading and caching.
  /// Converts the FixedString64Bytes to string only when needed and caches the clean result (without tags).
  /// </summary>
  public string Name {
    get {
      // Lazy load and cache the clean name to avoid repeated string conversions
      if (string.IsNullOrEmpty(_name)) {
        var fullName = User.CharacterName.ToString();
        _name = ExtractCleanName(fullName);
      }
      return _name;
    }
  }

  /// <summary>
  /// Extracts the clean player name by removing any tags in square brackets.
  /// Tags are always in the format [TAG] and are removed from the beginning of the name.
  /// </summary>
  /// <param name="fullName">The full name that may contain tags (e.g., "[Vaaz] Mark")</param>
  /// <returns>The clean name without tags (e.g., "Mark")</returns>
  private static string ExtractCleanName(string fullName) {
    if (string.IsNullOrEmpty(fullName)) return fullName;

    var trimmed = fullName.Trim();

    // Check if the name starts with a tag in square brackets
    if (trimmed.StartsWith("[")) {
      var closingBracketIndex = trimmed.IndexOf(']');
      if (closingBracketIndex > 0 && closingBracketIndex < trimmed.Length - 1) {
        // Extract everything after the closing bracket and trim whitespace
        return trimmed.Substring(closingBracketIndex + 1).Trim();
      }
    }

    // Return the original name if no tag pattern is found
    return trimmed;
  }

  /// <summary>
  /// Manually sets the cached player name.
  /// Automatically extracts the clean name by removing tags if present.
  /// Useful when the name is known from external sources or needs to be updated.
  /// </summary>
  /// <param name="name">The name to cache (tags will be automatically removed)</param>
  public void SetName(string name) {
    _name = ExtractCleanName(name);
  }

  /// <summary>
  /// Gets the currently cached name without triggering lazy loading.
  /// Returns null if the name hasn't been loaded or set yet.
  /// </summary>
  public string CachedName => _name;

  /// <summary>
  /// Gets the full character name including any tags from the entity data.
  /// This always returns the raw name as stored in the entity, including [TAG] prefixes.
  /// </summary>
  public string FullName => User.CharacterName.ToString();

  /// <summary>
  /// Gets the player's character entity in the game world.
  /// This entity represents the actual vampire character that moves around.
  /// </summary>
  public Entity CharacterEntity => User.LocalCharacter._Entity;

  /// <summary>
  /// Gets the platform-specific user ID (Steam ID, etc.).
  /// Used for cross-platform player identification and external integrations.
  /// </summary>
  public ulong PlatformId => User.PlatformId;

  /// <summary>
  /// Gets whether the player is currently connected to the server.
  /// Useful for checking player availability before performing operations.
  /// </summary>
  public bool IsOnline => User.IsConnected;
  /// <summary>
  /// Gets whether the player has administrator privileges.
  /// Used for permission checks and admin-only functionality.
  /// </summary>
  public bool IsAdmin => User.IsAdmin;

  /// <summary>
  /// Gets the current world position of the player's character.
  /// Returns the 3D coordinates where the character is located in the game world.
  /// </summary>
  public float3 Position => CharacterEntity.Position();

  /// <summary>
  /// Gets the player's current equipment level rounded to the nearest integer.
  /// Calculates the full level from all equipped items and converts to int.
  /// </summary>
  public int Level => Convert.ToInt32(GetEquipment().GetFullLevel());

  /// <summary>
  /// Gets the date and time when the player connected to the server.
  /// Converts from UTC ticks to local DateTime for easier handling.
  /// </summary>
  public DateTime ConnectedSince => new DateTime(User.TimeLastConnected, DateTimeKind.Utc).ToLocalTime();

  /// <summary>
  /// Gets the player's clan name if they belong to one.
  /// Returns null if the player is not in a clan or clan data is unavailable.
  /// </summary>
  public string ClanName {
    get {
      var clan = User.ClanEntity.GetEntityOnServer();

      if (Entity.Null.Equals(clan) || !clan.Has<ClanTeam>()) return null; // No clan data available

      var clanName = clan.Read<ClanTeam>().Name.ToString();

      return string.IsNullOrEmpty(clanName) ? null : clanName;
    }
  }

  /// <summary>
  /// Gets the player's inventory items as a dynamic buffer.
  /// Provides access to all items currently in the player's inventory.
  /// </summary>
  /// <returns>A dynamic buffer containing all inventory items</returns>
  public DynamicBuffer<InventoryBuffer> GetInventory() {
    return InventoryService.GetInventoryItems(CharacterEntity);
  }

  /// <summary>
  /// Attempts to give an item to the player's inventory.
  /// Checks if inventory has space before adding the item.
  /// </summary>
  /// <param name="itemGuid">The GUID of the item to give</param>
  /// <param name="amount">The quantity of items to give (default: 1)</param>
  /// <returns>True if the item was successfully added, false if inventory is full</returns>
  public bool TryGiveItem(PrefabGUID itemGuid, int amount = 1) {
    if (InventoryService.IsFull(CharacterEntity)) return false;
    InventoryService.AddItem(CharacterEntity, itemGuid, amount);
    return true;
  }


  /// <summary>
  /// Attempts to remove an item from the player's inventory.
  /// Returns false if the inventory is empty or if there are not enough items.
  /// </summary>
  /// <param name="itemGuid">The GUID of the item to remove</param>
  /// <param name="amount">The quantity of items to remove (default: 1)</param>
  /// <returns>True if the item was successfully removed, false otherwise</returns>
  public bool TryRemoveItem(PrefabGUID itemGuid, int amount = 1) {
    if (InventoryService.IsInventoryEmpty(CharacterEntity)) return false;
    return InventoryService.RemoveItem(CharacterEntity, itemGuid, amount);
  }

  /// <summary>
  /// Gets the player's current equipment data.
  /// Provides access to all equipped items and their stats.
  /// </summary>
  /// <returns>The Equipment component containing all equipped items</returns>
  public Equipment GetEquipment() {
    return CharacterEntity.Read<Equipment>();
  }

  /// <summary>
  /// Casts a specific ability for the player.
  /// Triggers the specified ability if the player has it available.
  /// </summary>
  /// <param name="abilityGroup">The GUID of the ability group to cast</param>
  public void CastAbility(PrefabGUID abilityGroup) {
    AbilityService.CastAbility(CharacterEntity, abilityGroup);
  }

  /// <summary>
  /// Replaces the player's ability in a specific slot with a new ability.
  /// </summary>
  /// <param name="newAbilityGuid">The GUID of the new ability to assign</param>
  /// <param name="abilitySlotIndex">The slot index to replace (default: 0)</param>
  /// <param name="priority">The priority for the ability replacement (default: 99)</param>
  public void ReplaceAbilityOnSlot(PrefabGUID newAbilityGuid, int abilitySlotIndex = 0, int priority = 99) {
    AbilityService.ReplaceNpcAbilityOnSlot(CharacterEntity, newAbilityGuid, abilitySlotIndex, priority);
  }

  /// <summary>
  /// Attempts to apply a buff to the player's character with a specific duration.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to apply</param>
  /// <param name="duration">Duration in seconds (-1 for permanent/indefinite)</param>
  /// <param name="buffEntity">Output parameter for the applied buff entity</param>
  /// <returns>True if the buff was successfully applied, false otherwise</returns>
  public bool TryApplyBuff(PrefabGUID prefabGUID, float duration, out Entity buffEntity) {
    return BuffService.TryApplyBuff(CharacterEntity, prefabGUID, duration, out buffEntity);
  }

  /// <summary>
  /// Attempts to apply a buff to the player's character with an optional duration.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to apply</param>
  /// <param name="duration">Duration in seconds (default: 0)</param>
  /// <returns>True if the buff was successfully applied, false otherwise</returns>
  public bool TryApplyBuff(PrefabGUID prefabGUID, float duration = 0) {
    return BuffService.TryApplyBuff(CharacterEntity, prefabGUID, duration, out _);
  }

  /// <summary>
  /// Attempts to apply a 'clean' buff to the player's character, removing extra gameplay components.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to apply</param>
  /// <param name="duration">Duration in seconds (-1 for permanent/indefinite)</param>
  /// <param name="buffEntity">Output parameter for the applied buff entity</param>
  /// <returns>True if the clean buff was successfully applied, false otherwise</returns>
  public bool TryApplyCleanBuff(PrefabGUID prefabGUID, float duration, out Entity buffEntity) {
    return BuffService.TryApplyCleanBuff(CharacterEntity, prefabGUID, duration, out buffEntity);
  }

  /// <summary>
  /// Attempts to apply a 'clean' buff to the player's character with an optional duration.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to apply</param>
  /// <param name="duration">Duration in seconds (default: 0)</param>
  /// <returns>True if the clean buff was successfully applied, false otherwise</returns>
  public bool TryApplyCleanBuff(PrefabGUID prefabGUID, float duration = 0) {
    return BuffService.TryApplyCleanBuff(CharacterEntity, prefabGUID, duration);
  }

  /// <summary>
  /// Grants administrator privileges to the player.
  /// </summary>
  public void MakeAdmin() {
    AdminService.AddAdmin(this);
  }

  /// <summary>
  /// Removes administrator privileges from the player.
  /// </summary>
  public void RemoveAdmin() {
    AdminService.RemoveAdmin(this);
  }

  /// <summary>
  /// Kicks the player from the server.
  /// </summary>
  public void Kick() {
    KickBanService.Kick(this);
  }

  /// <summary>
  /// Bans the player from the server.
  /// </summary>
  public void Ban() {
    KickBanService.Ban(this);
  }

  /// <summary>
  /// Unbans the player, allowing them to reconnect to the server.
  /// </summary>
  public void Unban() {
    KickBanService.Unban(this);
  }

  /// <summary>
  /// Attempts to add another player to this player's clan.
  /// </summary>
  /// <param name="player">The player to add to the clan</param>
  /// <returns>True if the player was added, false otherwise</returns>
  public bool TryAddMemberToClan(PlayerData player) {
    return ClanService.TryAddMember(player, ClanName);
  }

  /// <summary>
  /// Gets a list of all members in this player's clan.
  /// </summary>
  /// <returns>List of PlayerData for each clan member</returns>
  public List<PlayerData> GetClanMembers() {
    return ClanService.GetMembers(ClanName);
  }

  /// <summary>
  /// Attempts to remove this player from their current clan.
  /// </summary>
  /// <returns>True if the player was removed, false otherwise</returns>
  public bool TryRemoveFromClan() {
    return ClanService.TryRemoveFromClan(this);
  }

  /// <summary>
  /// Reveals the entire map for the player.
  /// </summary>
  public void RevealMap() {
    RevealMapService.RevealFullMap(this);
  }

  /// <summary>
  /// Hides the entire map for the player.
  /// </summary>
  public void HideMap() {
    RevealMapService.HideFullMap(this);
  }

  /// <summary>
  /// Teleports the player to a specific position in the world.
  /// Instantly moves the player's character to the specified coordinates.
  /// </summary>
  /// <param name="position">The target position to teleport to</param>
  public void TeleportTo(float3 position) {
    TeleportService.TeleportToPosition(CharacterEntity, position);
  }

  /// <summary>
  /// Sends a chat or system message to the player.
  /// Ignores empty or whitespace-only messages.
  /// </summary>
  /// <param name="message">The message text to send</param>
  public void SendMessage(string message) {
    if (string.IsNullOrWhiteSpace(message)) return;
    MessageService.Send(this, message);
  }

  /// <summary>
  /// Dictionary to store custom data from different mods.
  /// Uses the calling assembly name as the key to prevent conflicts between mods.
  /// </summary>
  private Dictionary<string, object> CustomData { get; set; } = [];

  /// <summary>
  /// Sets or updates custom data for the calling mod.
  /// Each mod gets its own namespace based on the assembly name to prevent conflicts.
  /// </summary>
  /// <typeparam name="T">The type of data to store</typeparam>
  /// <param name="value">The data value to store</param>
  /// <returns>The stored value for method chaining</returns>
  /// <example>
  /// // Store custom mod data
  /// var playerLevel = playerData.SetData(25);
  /// var customConfig = playerData.SetData(new MyModConfig());
  /// </example>
  public T SetData<T>(T value) {
    // Use the calling assembly name as the key to prevent mod data conflicts
    var key = Assembly.GetCallingAssembly().GetName().Name;

    // Update existing data or add new entry
    if (CustomData.ContainsKey(key)) {
      CustomData[key] = value;
    } else {
      CustomData.Add(key, value);
    }
    return value;
  }

  /// <summary>
  /// Retrieves custom data for the calling mod.
  /// Returns the default value for the type if no data is found or type mismatch occurs.
  /// </summary>
  /// <typeparam name="T">The expected type of the stored data</typeparam>
  /// <returns>The stored data or default(T) if not found</returns>
  /// <example>
  /// // Retrieve custom mod data
  /// var playerLevel = playerData.GetData&lt;int&gt;();
  /// var customConfig = playerData.GetData&lt;MyModConfig&gt;();
  /// </example>
  public T GetData<T>() {
    // Use the calling assembly name as the key to retrieve mod-specific data
    var key = Assembly.GetCallingAssembly().GetName().Name;

    // Try to get the data and cast it to the expected type
    if (CustomData.TryGetValue(key, out var value) && value is T typedValue) {
      return typedValue;
    }

    // Return default value if data not found or type mismatch
    return default;
  }

  /// <summary>
  /// Retrieves custom data from a specific assembly/mod.
  /// Allows mods to access data stored by other mods if they know the assembly name.
  /// Returns the default value for the type if no data is found or type mismatch occurs.
  /// </summary>
  /// <typeparam name="T">The expected type of the stored data</typeparam>
  /// <param name="assemblyName">The name of the assembly/mod to retrieve data from</param>
  /// <returns>The stored data or default(T) if not found</returns>
  /// <example>
  /// // Retrieve data from another mod
  /// var otherModConfig = playerData.GetDataFrom&lt;SomeConfig&gt;("OtherModName");
  /// var sharedPlayerLevel = playerData.GetDataFrom&lt;int&gt;("SharedLevelingMod");
  /// </example>
  public T GetDataFrom<T>(string assemblyName) {
    // Try to get the data and cast it to the expected type
    if (CustomData.TryGetValue(assemblyName, out var value) && value is T typedValue) {
      return typedValue;
    }

    // Return default value if data not found or type mismatch
    return default;
  }
}
