using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using ProjectM.Scripting;
using System;
using ScarletCore.Utils;
using ScarletCore.Systems;

namespace ScarletCore.Services;

/// <summary>
/// Provides utility methods for managing entity inventories
/// Handles inventory operations like adding, removing, and checking items.
/// </summary>
public class InventoryService {
  private static ServerGameManager GameManager = GameSystems.ServerGameManager;
  private static EntityManager EntityManager = GameSystems.EntityManager;

  /// <summary>
  /// Checks if an entity's inventory is completely empty.
  /// </summary>
  /// <param name="entity">The entity to check</param>
  /// <returns>True if the inventory is empty, false otherwise</returns>
  public static bool IsInventoryEmpty(Entity entity) {
    return InventoryUtilities.IsInventoryEmpty(EntityManager, entity);
  }

  /// <summary>
  /// Attempts to get the item at a specific slot in an entity's inventory.
  /// </summary>
  /// <param name="entity">The entity whose inventory will be checked</param>
  /// <param name="slot">The slot index</param>
  /// <param name="item">The item found, if any</param>
  /// <returns>True if an item exists at the slot, false otherwise</returns>
  public static bool TryGetItemAtSlot(Entity entity, int slot, out InventoryBuffer item) {
    return InventoryUtilities.TryGetItemAtSlot(EntityManager, entity, slot, out item);
  }

  /// <summary>
  /// Removes the item at a specific slot in an entity's inventory.
  /// </summary>
  /// <param name="entity">The entity whose inventory will be modified</param>
  /// <param name="slot">The slot index</param>
  public static void RemoveItemAtSlot(Entity entity, int slot) {
    if (!TryGetItemAtSlot(entity, slot, out var item)) return;
    // Optionally: protect invalid slots if needed, here there is no extra check
    InventoryUtilitiesServer.TryRemoveItemAtIndex(EntityManager, entity, item.ItemType, item.Amount, slot, true);
  }

  /// <summary>
  /// Adds an item to a specific slot in the inventory, setting amount and MaxAmountOverride.
  /// </summary>
  /// <param name="entity">The entity whose inventory will be modified</param>
  /// <param name="slot">The slot index</param>
  /// <param name="prefabGUID">The GUID of the item</param>
  /// <param name="amount">The amount of the item</param>
  /// <param name="maxAmount">The value for MaxAmountOverride</param>
  public static void AddWithMaxAmount(Entity entity, int slot, PrefabGUID prefabGUID, int amount, int maxAmount) {
    var response = GameManager.TryAddInventoryItem(entity, prefabGUID, 1, new(slot), false);
    var slotIndex = response.Slot;
    var items = GetInventoryItems(entity);
    var item = items[slotIndex];
    item.MaxAmountOverride = maxAmount;
    item.Amount = amount;
    items[slotIndex] = item;
  }

  /// <summary>
  /// Forces all items in an entity's inventory to have MaxAmountOverride equal to their current amount.
  /// </summary>
  /// <param name="entity">The entity whose inventory will be adjusted</param>
  public static void ForceAllSlotsMaxAmount(Entity entity) {
    var items = GetInventoryItems(entity);
    for (int i = 0; i < items.Length; i++) {
      var existingItem = items[i];
      existingItem.MaxAmountOverride = existingItem.Amount;
      items[i] = existingItem;
    }
  }

  /// <summary>
  /// Checks if the specified entity has an inventory component.
  /// </summary>
  /// <param name="entity">The entity to check for inventory</param>
  /// <returns>True if the entity has an inventory, false otherwise</returns>
  public static bool HasInventory(Entity entity) {
    return InventoryUtilities.TryGetInventoryEntity(EntityManager, entity, out _);
  }

  /// <summary>
  /// Adds items to an entity's inventory if there's space available.
  /// </summary>
  /// <param name="entity">The entity to add items to</param>
  /// <param name="guid">The GUID of the item to add</param>
  /// <param name="amount">The quantity of items to add</param>
  public static bool AddItem(Entity entity, PrefabGUID guid, int amount) {
    // Don't add items if inventory is full
    if (GameManager.HasFullInventory(entity)) return false;

    try {
      return GameManager.TryAddInventoryItem(entity, guid, amount);
    } catch (Exception e) {
      Log.Error(e);
      return false;
    }
  }

  /// <summary>
  /// Checks if an entity has at least one of the specified item.
  /// </summary>
  /// <param name="entity">The entity to check</param>
  /// <param name="guid">The GUID of the item to look for</param>
  /// <returns>True if the entity has the item, false otherwise</returns>
  public static bool HasItem(Entity entity, PrefabGUID guid) {
    return GameManager.GetInventoryItemCount(entity, guid) > 0;
  }

  /// <summary>
  /// Checks if an entity has at least the specified amount of an item.
  /// </summary>
  /// <param name="entity">The entity to check</param>
  /// <param name="guid">The GUID of the item to check</param>
  /// <param name="amount">The minimum amount required</param>
  /// <returns>True if the entity has enough of the item, false otherwise</returns>
  public static bool HasAmount(Entity entity, PrefabGUID guid, int amount) {
    return GameManager.GetInventoryItemCount(entity, guid) >= amount;
  }

  /// <summary>
  /// Removes a specified amount of items from an entity's inventory.
  /// </summary>
  /// <param name="entity">The entity to remove items from</param>
  /// <param name="guid">The GUID of the item to remove</param>
  /// <param name="amount">The quantity to remove</param>
  /// <returns>True if the removal was successful, false otherwise</returns>
  public static bool RemoveItem(Entity entity, PrefabGUID guid, int amount) {
    // Check if entity has an inventory
    if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, entity, out var inventoryEntity)) return false;

    // Verify the entity has enough items to remove
    if (GetItemAmount(entity, guid) < amount) return false;

    GameManager.TryRemoveInventoryItem(inventoryEntity, guid, amount);

    return true;
  }

  /// <summary>
  /// Gets the inventory buffer containing all items for the specified entity.
  /// </summary>
  /// <param name="entity">The entity whose inventory to retrieve</param>
  /// <returns>A dynamic buffer of inventory items, or default if no inventory exists</returns>
  public static DynamicBuffer<InventoryBuffer> GetInventoryItems(Entity entity) {
    if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, entity, out var inventoryEntity)) return default;
    return EntityManager.GetBuffer<InventoryBuffer>(inventoryEntity);
  }

  /// <summary>
  /// Removes all items from an entity's inventory, making it completely empty.
  /// </summary>
  /// <param name="entity">The entity whose inventory to clear</param>
  public static void ClearInventory(Entity entity) {
    // Check if entity has an inventory
    if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, entity, out var inventoryEntity)) return;

    // Get all items in the inventory
    var inventoryBuffer = GetInventoryItems(entity);

    // Remove each item from the inventory
    foreach (var item in inventoryBuffer) {
      GameManager.TryRemoveInventoryItem(inventoryEntity, item.ItemType, item.Amount);
    }
  }

  /// <summary>
  /// Gets the maximum capacity of an entity's inventory.
  /// </summary>
  /// <param name="entity">The entity to check</param>
  /// <returns>The total number of slots in the inventory</returns>
  public static int GetInventorySize(Entity entity) {
    return InventoryUtilities.GetInventorySize(EntityManager, entity);
  }

  /// <summary>
  /// Gets the exact amount of a specific item in an entity's inventory.
  /// </summary>
  /// <param name="entity">The entity to check</param>
  /// <param name="guid">The GUID of the item to count</param>
  /// <returns>The total quantity of the specified item</returns>
  public static int GetItemAmount(Entity entity, PrefabGUID guid) {
    return GameManager.GetInventoryItemCount(entity, guid);
  }

  /// <summary>
  /// Checks if an entity's inventory is completely full.
  /// </summary>
  /// <param name="entity">The entity to check</param>
  /// <returns>True if the inventory is full, false otherwise</returns>
  public static bool IsFull(Entity entity) {
    return GameManager.HasFullInventory(entity);
  }

  #region Bulk Operations

  /// <summary>
  /// Adds multiple items to an entity's inventory in a single operation.
  /// </summary>
  /// <param name="entity">The entity to add items to</param>
  /// <param name="items">Dictionary of items where key is PrefabGUID and value is amount</param>
  /// <returns>Dictionary of items that couldn't be added (if inventory became full)</returns>
  public static Dictionary<PrefabGUID, int> AddItems(Entity entity, Dictionary<PrefabGUID, int> items) {
    var failedItems = new Dictionary<PrefabGUID, int>();

    foreach (var item in items) {
      if (GameManager.HasFullInventory(entity)) {
        // If inventory is full, add remaining items to failed list
        failedItems[item.Key] = item.Value;
        continue;
      }

      try {
        GameManager.TryAddInventoryItem(entity, item.Key, item.Value);
      } catch (Exception e) {
        Log.Error($"Failed to add item {item.Key} amount {item.Value}: {e}");
        failedItems[item.Key] = item.Value;
      }
    }

    return failedItems;
  }

  /// <summary>
  /// Removes multiple items from an entity's inventory in a single operation.
  /// </summary>
  /// <param name="entity">The entity to remove items from</param>
  /// <param name="items">Dictionary of items where key is PrefabGUID and value is amount</param>
  /// <returns>Dictionary of items that couldn't be removed (insufficient quantity)</returns>
  public static Dictionary<PrefabGUID, int> RemoveItems(Entity entity, Dictionary<PrefabGUID, int> items) {
    var failedItems = new Dictionary<PrefabGUID, int>();

    // Check if entity has an inventory
    if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, entity, out var inventoryEntity)) {
      return items; // Return all items as failed if no inventory
    }

    foreach (var item in items) {
      var availableAmount = GetItemAmount(entity, item.Key);

      if (availableAmount < item.Value) {
        failedItems[item.Key] = item.Value - availableAmount;
        // Remove what we can
        if (availableAmount > 0) {
          GameManager.TryRemoveInventoryItem(inventoryEntity, item.Key, availableAmount);
        }
      } else {
        GameManager.TryRemoveInventoryItem(inventoryEntity, item.Key, item.Value);
      }
    }

    return failedItems;
  }

  /// <summary>
  /// Checks if an entity has all the specified items in the required amounts.
  /// </summary>
  /// <param name="entity">The entity to check</param>
  /// <param name="items">Dictionary of items where key is PrefabGUID and value is required amount</param>
  /// <returns>True if entity has all items in sufficient quantities, false otherwise</returns>
  public static bool HasItems(Entity entity, Dictionary<PrefabGUID, int> items) {
    foreach (var item in items) {
      if (!HasAmount(entity, item.Key, item.Value)) {
        return false;
      }
    }
    return true;
  }

  /// <summary>
  /// Gets the amounts of multiple items in an entity's inventory.
  /// </summary>
  /// <param name="entity">The entity to check</param>
  /// <param name="itemGuids">List of item GUIDs to check</param>
  /// <returns>Dictionary with item GUIDs as keys and current amounts as values</returns>
  public static Dictionary<PrefabGUID, int> GetItemAmounts(Entity entity, List<PrefabGUID> itemGuids) {
    var amounts = new Dictionary<PrefabGUID, int>();

    foreach (var guid in itemGuids) {
      amounts[guid] = GetItemAmount(entity, guid);
    }

    return amounts;
  }

  /// <summary>
  /// Gives a complete item set to an entity (useful for starter kits, admin tools, etc.).
  /// </summary>
  /// <param name="entity">The entity to give items to</param>
  /// <param name="itemSet">Dictionary of items where key is PrefabGUID and value is amount</param>
  /// <param name="clearFirst">Whether to clear inventory before adding items</param>
  /// <returns>Dictionary of items that couldn't be added</returns>
  public static Dictionary<PrefabGUID, int> GiveItemSet(Entity entity, Dictionary<PrefabGUID, int> itemSet, bool clearFirst = false) {
    if (clearFirst) {
      ClearInventory(entity);
    }

    return AddItems(entity, itemSet);
  }

  /// <summary>
  /// Transfers items from one entity's inventory to another.
  /// </summary>
  /// <param name="fromEntity">Source entity</param>
  /// <param name="toEntity">Destination entity</param>
  /// <param name="items">Dictionary of items to transfer</param>
  /// <returns>Dictionary of items that couldn't be transferred</returns>
  public static Dictionary<PrefabGUID, int> TransferItems(Entity fromEntity, Entity toEntity, Dictionary<PrefabGUID, int> items) {
    var failedItems = new Dictionary<PrefabGUID, int>();

    // First, try to remove items from source
    var removeFailed = RemoveItems(fromEntity, items);

    // Calculate what was actually removed
    var actuallyRemoved = new Dictionary<PrefabGUID, int>();
    foreach (var item in items) {
      var failedAmount = removeFailed.ContainsKey(item.Key) ? removeFailed[item.Key] : 0;
      var removedAmount = item.Value - failedAmount;
      if (removedAmount > 0) {
        actuallyRemoved[item.Key] = removedAmount;
      }
    }

    // Add the removed items to destination
    if (actuallyRemoved.Count > 0) {
      var addFailed = AddItems(toEntity, actuallyRemoved);

      // If some items couldn't be added to destination, return them to source
      if (addFailed.Count > 0) {
        AddItems(fromEntity, addFailed);
        failedItems = addFailed;
      }
    }

    return failedItems;
  }

  #endregion
}