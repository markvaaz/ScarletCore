using Unity.Entities;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Stunlock.Core;
using ScarletCore.Localization;
using ProjectM;
using ScarletCore.Commanding;
using System.Reflection;
using ProjectM.Terrain;

namespace ScarletCore.Services;

/// <summary>
/// Represents comprehensive player data and provides convenient access to player-related information.
/// This class serves as a wrapper around Unity ECS entities to simplify player data management.
/// </summary>
public class PlayerData {
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
      if(string.IsNullOrEmpty(_name)) {
        var fullName = User.CharacterName.ToString();
        _name = PlayerService.ExtractCleanName(fullName);
      }
      return _name;
    }
  }

  /// <summary>
  /// Manually sets the cached player name.
  /// Automatically extracts the clean name by removing tags if present.
  /// Useful when the name is known from external sources or needs to be updated.
  /// </summary>
  /// <param name="name">The name to cache (tags will be automatically removed)</param>
  public void SetName(string name) {
    _name = PlayerService.ExtractCleanName(name);
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
  public DateTime LastConnected => new DateTime(User.TimeLastConnected, DateTimeKind.Utc).ToLocalTime();

  /// <summary>
  /// Gets the world region type where the player's character is currently located.
  /// Returns WorldRegionType.None if the player is not in any defined region.
  /// </summary>
  public WorldRegionType CurrentRegion => UserEntity.Read<CurrentWorldRegion>().CurrentRegion;

  /// <summary>
  /// Gets the player's preferred localization language.
  /// Attempts to retrieve the player's explicit language setting and, if it is not set (<see cref="Language.None"/>), falls back to the current server language.
  /// </summary>
  /// <returns>The player's language preference, or the server's current language if the player has not chosen one.</returns>
  public Language Language {
    get {
      var lang = Localizer.GetPlayerLanguage(this);
      if(lang == Language.None) {
        lang = Localizer.CurrentServerLanguage;
      }
      return lang;
    }
  }

  /// <summary>
  /// Gets the player's clan name if they belong to one.
  /// Returns null if the player is not in a clan or clan data is unavailable.
  /// </summary>
  public string ClanName {
    get {
      var clan = User.ClanEntity.GetEntityOnServer();

      if(Entity.Null.Equals(clan) || !clan.Has<ClanTeam>()) return null; // No clan data available

      var clanName = clan.Read<ClanTeam>().Name.ToString();

      return string.IsNullOrEmpty(clanName) ? null : clanName;
    }
  }

  /// <summary>
  /// Gets the player's current input state including movement, abilities, and actions.
  /// Provides access to the EntityInput component which contains all player input data.
  /// </summary>
  public EntityInput Input => CharacterEntity.Read<EntityInput>();


  /// <summary>
  /// Sets a tag at a specific index in the player's name, allowing for multiple tags or custom tag placement.
  /// </summary>
  /// <param name="tag">The tag text to insert into the player's name.</param>
  /// <param name="tagIndex">The index at which to insert the tag (0-based).</param>
  /// <returns>True if the tag was successfully set; otherwise, false.</returns>
  public bool SetNameTag(string tag, int tagIndex) {
    return PlayerService.SetTagAtIndex(this, tag, tagIndex);
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
    if(InventoryService.IsFull(CharacterEntity)) return false;
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
    if(InventoryService.IsInventoryEmpty(CharacterEntity)) return false;
    return InventoryService.RemoveItem(CharacterEntity, itemGuid, amount);
  }

  /// <summary>
  /// Checks if the player's inventory is completely empty.
  /// </summary>
  public bool IsInventoryEmpty() {
    return InventoryService.IsInventoryEmpty(CharacterEntity);
  }

  /// <summary>
  /// Checks if the player's inventory is completely full.
  /// </summary>
  public bool IsInventoryFull() {
    return InventoryService.IsFull(CharacterEntity);
  }

  /// <summary>
  /// Checks if the player has at least one of the specified item.
  /// </summary>
  public bool HasItem(PrefabGUID itemGuid) {
    return InventoryService.HasItem(CharacterEntity, itemGuid);
  }

  /// <summary>
  /// Checks if the player has at least <paramref name="amount"/> of the specified item.
  /// </summary>
  public bool HasAmount(PrefabGUID itemGuid, int amount) {
    return InventoryService.HasAmount(CharacterEntity, itemGuid, amount);
  }

  /// <summary>
  /// Returns the total quantity of the specified item in the player's inventory.
  /// </summary>
  public int GetItemAmount(PrefabGUID itemGuid) {
    return InventoryService.GetItemAmount(CharacterEntity, itemGuid);
  }

  /// <summary>
  /// Returns the total number of slots in the player's inventory.
  /// </summary>
  public int GetInventorySize() {
    return InventoryService.GetInventorySize(CharacterEntity);
  }

  /// <summary>
  /// Returns the number of empty slots in the player's inventory.
  /// </summary>
  public int GetEmptySlotCount() {
    return InventoryService.GetEmptySlotsCount(CharacterEntity);
  }

  /// <summary>
  /// Finds the first empty slot index in the player's inventory, or -1 if the inventory is full.
  /// </summary>
  public int GetEmptySlotIndex() {
    return InventoryService.GetEmptySlotIndex(CharacterEntity);
  }

  /// <summary>
  /// Tries to find the slot index occupied by the specified item.
  /// </summary>
  public bool TryGetItemSlot(PrefabGUID itemGuid, out int slot) {
    return InventoryService.TryGetItemSlot(CharacterEntity, itemGuid, out slot);
  }

  /// <summary>
  /// Tries to get the inventory entry at a specific slot index.
  /// </summary>
  public bool TryGetItemAtSlot(int slot, out InventoryBuffer item) {
    return InventoryService.TryGetItemAtSlot(CharacterEntity, slot, out item);
  }

  /// <summary>
  /// Adds an item directly into a specific inventory slot.
  /// </summary>
  /// <returns>True if the item was placed into the slot successfully.</returns>
  public bool AddItemAtSlot(PrefabGUID itemGuid, int amount, int slot) {
    return InventoryService.AddItemAtSlot(CharacterEntity, itemGuid, amount, slot);
  }

  /// <summary>
  /// Removes the entire stack from a specific inventory slot.
  /// </summary>
  public void RemoveItemAtSlot(int slot) {
    InventoryService.RemoveItemAtSlot(CharacterEntity, slot);
  }

  /// <summary>
  /// Removes a specific amount from a given inventory slot.
  /// </summary>
  public void RemoveItemAtSlot(int slot, int amount) {
    InventoryService.RemoveItemAtSlot(CharacterEntity, slot, amount);
  }

  /// <summary>
  /// Clears the item at the specified slot, leaving it empty.
  /// </summary>
  public void ClearInventorySlot(int slot) {
    InventoryService.ClearSlot(CharacterEntity, slot);
  }

  /// <summary>
  /// Removes all items from the player's inventory.
  /// </summary>
  public void ClearInventory() {
    InventoryService.ClearInventory(CharacterEntity);
  }

  /// <summary>
  /// Drops an item from the player's inventory at their current position.
  /// </summary>
  public void DropItem(PrefabGUID itemGuid, int amount = 1) {
    InventoryService.CreateDropItem(CharacterEntity, itemGuid, amount);
  }

  /// <summary>
  /// Adds multiple items to the player's inventory.
  /// Returns a dictionary of items that could not be added (overflow).
  /// </summary>
  public Dictionary<PrefabGUID, int> AddItems(Dictionary<PrefabGUID, int> items) {
    return InventoryService.AddItems(CharacterEntity, items);
  }

  /// <summary>
  /// Removes multiple items from the player's inventory.
  /// Returns a dictionary of items that could not be removed (deficit).
  /// </summary>
  public Dictionary<PrefabGUID, int> RemoveItems(Dictionary<PrefabGUID, int> items) {
    return InventoryService.RemoveItems(CharacterEntity, items);
  }

  /// <summary>
  /// Checks whether the player has all items in the specified dictionary at the required amounts.
  /// </summary>
  public bool HasItems(Dictionary<PrefabGUID, int> items) {
    return InventoryService.HasItems(CharacterEntity, items);
  }

  /// <summary>
  /// Returns the current amount of each requested item in the player's inventory.
  /// </summary>
  public Dictionary<PrefabGUID, int> GetItemAmounts(List<PrefabGUID> itemGuids) {
    return InventoryService.GetItemAmounts(CharacterEntity, itemGuids);
  }

  /// <summary>
  /// Gives a full set of items to the player.
  /// Returns items that could not be added due to insufficient space.
  /// </summary>
  /// <param name="itemSet">Items to give.</param>
  /// <param name="clearFirst">If true, clears the inventory before giving items.</param>
  public Dictionary<PrefabGUID, int> GiveItemSet(Dictionary<PrefabGUID, int> itemSet, bool clearFirst = false) {
    return InventoryService.GiveItemSet(CharacterEntity, itemSet, clearFirst);
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
    AbilityService.ReplaceAbilityOnSlotHard(CharacterEntity, newAbilityGuid, abilitySlotIndex, priority);
  }

  /// <summary>
  /// Performs a temporary (soft) replacement of ability slots for the player.
  /// Uses an internal replace buff managed by the buff system, so replacements last only as long as the buff is active.
  /// </summary>
  /// <param name="abilities">Array of replacement entries: (PrefabGUID, slot index, priority, copy cooldown)</param>
  public void ReplaceAbilityOnSlotSoft((PrefabGUID PrefabGUID, int Slot, int Priority, bool CopyCooldown)[] abilities) {
    AbilityService.ReplaceAbilityOnSlotSoft(CharacterEntity, abilities);
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
  /// Attempts to apply a raw buff to the player's character, preserving gameplay event components.
  /// Unlike TryApplyBuff, this does NOT strip CreateGameplayEventsOnSpawn or GameplayEventListeners.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to apply</param>
  /// <param name="duration">Duration in seconds (-1 for permanent/indefinite)</param>
  /// <param name="buffEntity">Output parameter for the applied buff entity</param>
  /// <returns>True if the buff was successfully applied, false otherwise</returns>
  public bool TryApplyRawBuff(PrefabGUID prefabGUID, float duration, out Entity buffEntity) {
    return BuffService.TryApplyRawBuff(CharacterEntity, prefabGUID, duration, out buffEntity);
  }

  /// <summary>
  /// Attempts to apply a raw buff to the player's character with an optional duration.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to apply</param>
  /// <param name="duration">Duration in seconds (default: 0)</param>
  /// <returns>True if the buff was successfully applied, false otherwise</returns>
  public bool TryApplyRawBuff(PrefabGUID prefabGUID, float duration = 0) {
    return BuffService.TryApplyRawBuff(CharacterEntity, prefabGUID, duration, out _);
  }

  /// <summary>
  /// Attempts to apply a buff to the player only if it is not already active.
  /// If the buff already exists it will not be reapplied or modified.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to apply</param>
  /// <param name="duration">Duration in seconds (-1 for permanent/indefinite)</param>
  /// <param name="buffEntity">Output parameter for the existing or newly applied buff entity</param>
  /// <returns>True if the buff was applied or already exists, false if application failed</returns>
  public bool TryApplyUniqueBuff(PrefabGUID prefabGUID, float duration, out Entity buffEntity) {
    return BuffService.TryApplyUnique(CharacterEntity, prefabGUID, duration, out buffEntity);
  }

  /// <summary>
  /// Attempts to apply a buff to the player only if it is not already active.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to apply</param>
  /// <param name="duration">Duration in seconds (default: 0)</param>
  /// <returns>True if the buff was applied or already exists, false if application failed</returns>
  public bool TryApplyUniqueBuff(PrefabGUID prefabGUID, float duration = 0) {
    return BuffService.TryApplyUnique(CharacterEntity, prefabGUID, duration, out _);
  }

  /// <summary>
  /// Checks whether the player currently has a specific buff applied.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to check</param>
  /// <returns>True if the buff is currently active on the player, false otherwise</returns>
  public bool HasBuff(PrefabGUID prefabGUID) {
    return BuffService.HasBuff(CharacterEntity, prefabGUID);
  }

  /// <summary>
  /// Attempts to retrieve the entity for a specific buff applied to the player.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to look up</param>
  /// <param name="buffEntity">Output parameter that will contain the buff entity if found</param>
  /// <returns>True if the buff was found, false otherwise</returns>
  public bool TryGetBuff(PrefabGUID prefabGUID, out Entity buffEntity) {
    return BuffService.TryGetBuff(CharacterEntity, prefabGUID, out buffEntity);
  }

  /// <summary>
  /// Removes a specific buff from the player if it is currently active.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to remove</param>
  /// <returns>True if the buff was found and removed, false otherwise</returns>
  public bool TryRemoveBuff(PrefabGUID prefabGUID) {
    return BuffService.TryRemoveBuff(CharacterEntity, prefabGUID);
  }

  /// <summary>
  /// Gets the remaining duration of a buff currently active on the player.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to query</param>
  /// <returns>Remaining duration in seconds, or -1 if the buff does not exist or is permanent</returns>
  public float GetBuffRemainingDuration(PrefabGUID prefabGUID) {
    return BuffService.GetBuffRemainingDuration(CharacterEntity, prefabGUID);
  }

  /// <summary>
  /// Modifies the remaining duration of a buff currently active on the player.
  /// </summary>
  /// <param name="prefabGUID">The GUID of the buff to modify</param>
  /// <param name="newDuration">The new duration in seconds</param>
  /// <returns>True if the duration was successfully modified, false otherwise</returns>
  public bool ModifyBuffDuration(PrefabGUID prefabGUID, float newDuration) {
    return BuffService.ModifyBuffDuration(CharacterEntity, prefabGUID, newDuration);
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
  /// Checks whether the player is currently banned from the server.
  /// </summary>
  public bool IsBanned() {
    return KickBanService.IsBanned(this);
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
  /// Tries to get the current clan leader of this player's clan.
  /// </summary>
  /// <param name="leader">The clan leader, or null if not found.</param>
  /// <returns>True if a leader was found, false otherwise.</returns>
  public bool TryGetClanLeader(out PlayerData leader) {
    leader = null;
    return !string.IsNullOrEmpty(ClanName) && ClanService.TryGetClanLeader(ClanName, out leader);
  }

  /// <summary>
  /// Attempts to change this player's role inside their clan.
  /// </summary>
  /// <param name="newRole">The new clan role to assign.</param>
  /// <returns>True if the role was changed successfully, false otherwise.</returns>
  public bool TryChangeClanRole(ClanRoleEnum newRole) {
    return ClanService.TryChangeClanRole(this, newRole);
  }

  /// <summary>
  /// Sets a clan tag in the player's display name.
  /// </summary>
  /// <param name="clanTag">The tag text to apply.</param>
  public void SetClanTag(string clanTag) {
    ClanService.SetTagForPlayer(this, clanTag);
  }

  /// <summary>
  /// Removes the clan tag from the player's display name.
  /// </summary>
  public void RemoveClanTag() {
    ClanService.RemoveTagFromPlayer(this);
  }

  /// <summary>
  /// Reveals the entire map for the player.
  /// </summary>
  public void RevealMap() {
    MapService.RevealFullMap(this);
  }

  /// <summary>
  /// Hides the entire map for the player.
  /// </summary>
  public void HideMap() {
    MapService.HideFullMap(this);
  }

  /// <summary>
  /// Reveals a circular area of the map around the given position.
  /// </summary>
  /// <param name="centerPos">World-space center of the reveal area.</param>
  /// <param name="radius">Radius in world units to reveal.</param>
  public void RevealMapRadius(float3 centerPos, float radius) {
    MapService.RevealMapRadius(this, centerPos, radius);
  }

  /// <summary>
  /// Reveals a rectangular area of the map around the given position.
  /// </summary>
  /// <param name="centerPos">World-space center of the reveal area.</param>
  /// <param name="width">Width in world units of the rectangle.</param>
  /// <param name="height">Height in world units of the rectangle.</param>
  public void RevealMapRectangle(float3 centerPos, float width, float height) {
    MapService.RevealMapRectangle(this, centerPos, width, height);
  }

  /// <summary>
  /// Gets the display name of the world region the player is currently in.
  /// </summary>
  public string GetCurrentRegionDisplayName() {
    return MapService.GetRegionDisplayName(CurrentRegion, Language);
  }

  /// <summary>
  /// Returns all online players within <paramref name="radius"/> world units of this player, excluding themselves.
  /// Uses the <see cref="PlayerSpatialHash"/> for efficient lookup. Does not allocate — reuse the list for hot paths.
  /// </summary>
  /// <param name="radius">Search radius in world units.</param>
  /// <param name="results">List to fill with nearby players. Cleared before use.</param>
  public void GetNearbyPlayers(float radius, List<PlayerData> results) {
    PlayerSpatialHash.QueryNearby(this, radius, results);
  }

  /// <summary>
  /// Returns all online players within <paramref name="radius"/> world units of this player, excluding themselves.
  /// Allocates a new list. Prefer the overload that accepts a <see cref="List{T}"/> for hot paths.
  /// </summary>
  /// <param name="radius">Search radius in world units.</param>
  public List<PlayerData> GetNearbyPlayers(float radius) {
    return PlayerSpatialHash.QueryNearby(this, radius);
  }

  /// <summary>
  /// Applies an array of stat modifiers to the player using the specified modifier buff.
  /// Safe to call at high frequency — rapid consecutive calls are coalesced into a single cycle.
  /// </summary>
  /// <param name="modifierBuff">The prefab GUID of the modifier buff to use</param>
  /// <param name="modifiers">Array of modifiers to apply</param>
  public void ApplyStatModifiers(PrefabGUID modifierBuff, Modifier[] modifiers) {
    StatModifierService.ApplyModifiers(CharacterEntity, modifierBuff, modifiers);
  }

  /// <summary>
  /// Removes all stat modifiers applied via the specified modifier buff and cancels any pending re-apply.
  /// </summary>
  /// <param name="modifierBuff">The prefab GUID of the modifier buff to remove</param>
  public void RemoveStatModifiers(PrefabGUID modifierBuff) {
    StatModifierService.RemoveModifiers(CharacterEntity, modifierBuff);
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
  /// Teleports the player to another player's current position.
  /// </summary>
  /// <param name="target">The target player to teleport to</param>
  /// <returns>True if teleportation was successful, false otherwise</returns>
  public bool TeleportTo(PlayerData target) {
    return TeleportService.TeleportToEntity(CharacterEntity, target.CharacterEntity);
  }

  /// <summary>
  /// Returns the world-space distance between this player and another player.
  /// </summary>
  /// <param name="other">The other player to measure distance to</param>
  /// <returns>Distance in world units, or -1 if either entity is invalid</returns>
  public float GetDistanceTo(PlayerData other) {
    return TeleportService.GetDistance(CharacterEntity, other.CharacterEntity);
  }

  /// <summary>
  /// Sends a chat or system message to the player.
  /// Ignores empty or whitespace-only messages.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="message">The message text to send</param>
  public void SendMessage(string message) {
    if(string.IsNullOrWhiteSpace(message)) return;
    message = ProcessPlaceholders(message);
    MessageService.Send(this, message);
  }

  /// <summary>
  /// Sends a raw message to the player without any placeholder processing.
  /// </summary>
  /// <param name="message">The raw message text to send.</param>
  public void SendRawMessage(string message) {
    if(string.IsNullOrWhiteSpace(message)) return;
    MessageService.SendRaw(this, message);
  }

  /// <summary>
  /// Displays a scrolling combat text (SCT) notification above the player.
  /// </summary>
  /// <param name="prefab">The prefab GUID for the SCT icon or effect.</param>
  /// <param name="assetGuid">The asset GUID string for the SCT.</param>
  /// <param name="color">The color of the SCT value.</param>
  /// <param name="value">The numeric value to display.</param>
  public void SendSCT(PrefabGUID prefab, string assetGuid, float3 color, int value) {
    MessageService.SendSCT(this, prefab, assetGuid, color, value);
  }

  /// <summary>
  /// Sends an error-styled message to the player using <see cref="MessageService.SendError(PlayerData, string)"/>.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="message">The message text to send.</param>
  public void SendErrorMessage(string message) {
    message = ProcessPlaceholders(message);
    MessageService.SendError(this, message);
  }

  /// <summary>
  /// Sends an informational-styled message to the player using <see cref="MessageService.SendInfo(PlayerData, string)"/>.
  /// Ignores empty or whitespace-only messages.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="message">The message text to send.</param>
  public void SendInfoMessage(string message) {
    message = ProcessPlaceholders(message);
    MessageService.SendInfo(this, message);
  }

  /// <summary>
  /// Sends a success-styled message to the player using <see cref="MessageService.SendSuccess(PlayerData, string)"/>.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="message">The message text to send.</param>
  public void SendSuccessMessage(string message) {
    message = ProcessPlaceholders(message);
    MessageService.SendSuccess(this, message);
  }

  /// <summary>
  /// Sends a warning-styled message to the player using <see cref="MessageService.SendWarning(PlayerData, string)"/>.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="message">The message text to send.</param>
  public void SendWarningMessage(string message) {
    message = ProcessPlaceholders(message);
    MessageService.SendWarning(this, message);
  }

  /// <summary>
  /// Sends a localized message to the player by looking up the given localization key and formatting with arguments.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="key">The localization key to look up.</param>
  /// <param name="args">Optional formatting arguments.</param>
  public void SendLocalizedMessage(string key, params object[] args) {
    var assembly = Assembly.GetCallingAssembly();
    var localized = Localizer.Get(this, key, assembly, args);
    localized = ProcessPlaceholders(localized);
    MessageService.Send(this, localized);
  }

  /// <summary>
  /// Sends a localized error message to the player by looking up the given key and formatting with arguments.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="key">The localization key to look up.</param>
  /// <param name="args">Optional formatting arguments.</param>
  public void SendLocalizedErrorMessage(string key, params object[] args) {
    var assembly = Assembly.GetCallingAssembly();
    var localized = Localizer.Get(this, key, assembly, args);
    localized = ProcessPlaceholders(localized);
    MessageService.SendError(this, localized);
  }

  /// <summary>
  /// Sends a localized informational message to the player by looking up the given key and formatting with arguments.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="key">The localization key to look up.</param>
  /// <param name="args">Optional formatting arguments.</param>
  public void SendLocalizedInfoMessage(string key, params object[] args) {
    var assembly = Assembly.GetCallingAssembly();
    var localized = Localizer.Get(this, key, assembly, args);
    localized = ProcessPlaceholders(localized);
    MessageService.SendInfo(this, localized);
  }

  /// <summary>
  /// Sends a localized success message to the player by looking up the given key and formatting with arguments.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="key">The localization key to look up.</param>
  /// <param name="args">Optional formatting arguments.</param>
  public void SendLocalizedSuccessMessage(string key, params object[] args) {
    var assembly = Assembly.GetCallingAssembly();
    var localized = Localizer.Get(this, key, assembly, args);
    localized = ProcessPlaceholders(localized);
    MessageService.SendSuccess(this, localized);
  }

  /// <summary>
  /// Sends a localized warning message to the player by looking up the given key and formatting with arguments.
  /// Supports placeholders: {playerName} and PrefabGuid(number).
  /// </summary>
  /// <param name="key">The localization key to look up.</param>
  /// <param name="args">Optional formatting arguments.</param>
  public void SendLocalizedWarningMessage(string key, params object[] args) {
    var assembly = Assembly.GetCallingAssembly();
    var localized = Localizer.Get(this, key, assembly, args);
    localized = ProcessPlaceholders(localized);
    MessageService.SendWarning(this, localized);
  }

  /// <summary>
  /// Processes placeholders in the message text.
  /// Replaces {playerName} with the player's name and PrefabGuid(number) with localized item names.
  /// </summary>
  /// <param name="message">The message text containing placeholders.</param>
  /// <returns>The message with placeholders replaced.</returns>
  private string ProcessPlaceholders(string message) {
    if(string.IsNullOrEmpty(message)) return message;

    // Replace {playerName} with player's name
    message = message.Replace("{playerName}", Name);

    // Replace PrefabGuid(...) with localized name
    var prefabPattern = System.Text.RegularExpressions.Regex.Matches(message, @"PrefabGuid\((-?\d+)\)");
    foreach(System.Text.RegularExpressions.Match match in prefabPattern) {
      if(int.TryParse(match.Groups[1].Value, out int guidValue)) {
        var prefabGuid = new PrefabGUID(guidValue);
        var localizedName = prefabGuid.LocalizedName(Language);
        message = message.Replace(match.Value, localizedName);
      }
    }

    return message;
  }

  // ========== Role Management ==========

  /// <summary>
  /// Adds a role to this player.
  /// </summary>
  /// <param name="roleName">Name of the role to add</param>
  /// <returns>True if the role was successfully added, false if the role doesn't exist or player already has it</returns>
  public bool AddRole(string roleName) {
    return RoleService.AddRoleToPlayer(this, roleName);
  }

  /// <summary>
  /// Removes a role from this player.
  /// </summary>
  /// <param name="roleName">Name of the role to remove</param>
  /// <returns>True if the role was successfully removed, false if player didn't have the role</returns>
  public bool RemoveRole(string roleName) {
    return RoleService.RemoveRoleFromPlayer(this, roleName);
  }

  /// <summary>
  /// Checks if this player has a specific role.
  /// </summary>
  /// <param name="roleName">Name of the role to check</param>
  /// <returns>True if the player has the role, false otherwise</returns>
  public bool HasRole(string roleName) {
    return RoleService.PlayerHasRole(this, roleName);
  }

  /// <summary>
  /// Gets all role names assigned to this player.
  /// </summary>
  /// <returns>List of role names</returns>
  public List<string> GetRoles() {
    return RoleService.GetPlayerRoles(this);
  }

  /// <summary>
  /// Gets all Role objects assigned to this player, ordered by priority (highest first).
  /// </summary>
  /// <returns>List of Role objects</returns>
  public List<Role> GetRoleObjects() {
    return RoleService.GetPlayerRoleObjects(this);
  }

  /// <summary>
  /// Gets the highest priority role for this player.
  /// </summary>
  /// <returns>The primary role, or null if player has no roles</returns>
  public Role GetPrimaryRole() {
    return RoleService.GetPlayerPrimaryRole(this);
  }

  /// <summary>
  /// Checks if this player has a specific permission (from any of their roles).
  /// </summary>
  /// <param name="permission">The permission to check</param>
  /// <returns>True if the player has the permission, false otherwise</returns>
  public bool HasPermission(string permission) {
    return RoleService.PlayerHasPermission(this, permission);
  }

  /// <summary>
  /// Clears all roles from this player.
  /// </summary>
  public void ClearRoles() {
    RoleService.ClearPlayerRoles(this);
  }

  /// <summary>
  /// Attempts to execute a command as if it were sent by this player.
  /// The command can be provided with or without the command prefix.
  /// </summary>
  /// <param name="commandText">The command text to execute (e.g., "teleport spawn" or ".teleport spawn")</param>
  /// <returns>True if the command was found and executed, false otherwise</returns>
  public bool TryExecuteCommand(string commandText) {
    return CommandHandler.TryExecuteCommand(this, commandText);
  }
}