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
  /// Creates a dropped item in the world from an entity's position.
  /// </summary>
  /// <param name="entity">The entity to drop the item from</param>
  /// <param name="prefabGUID">The GUID of the item to drop</param>
  /// <param name="amount">The amount of the item to drop (default is 1)</param>
  public static void CreateDropItem(Entity entity, PrefabGUID prefabGUID, int amount = 1) {
    InventoryUtilitiesServer.CreateDropItem(EntityManager, entity, prefabGUID, amount, Entity.Null);
  }

  /// <summary>
  /// Copies all items from one entity's inventory to another.
  /// </summary>
  /// <param name="sourceEntity">The entity to copy items from</param>
  /// <param name="targetEntity">The entity to copy items to</param>
  public static void CopyInventory(Entity sourceEntity, Entity targetEntity) {
    InventoryUtilitiesServer.CopyInventory(EntityManager, sourceEntity, targetEntity);
  }

  /// <summary>
  /// Modifies the inventory size of an entity, setting minimum, maximum, and new size.
  /// </summary>
  /// <param name="entity">The entity whose inventory size will be modified</param>
  /// <param name="minSize">Minimum inventory size</param>
  /// <param name="maxSize">Maximum inventory size</param>
  /// <param name="newSize">New inventory size to set</param>
  public static void ModifyInventorySize(Entity entity, int minSize, int maxSize, int newSize) {
    InventoryUtilitiesServer.ModifyInventorySize(EntityManager, entity, minSize, maxSize, newSize);
  }

  /// <summary>
  /// Attempts to get the slot index of a specific item in an entity's inventory.
  /// </summary>
  /// <param name="entity">The entity to search</param>
  /// <param name="prefabGUID">The GUID of the item to find</param>
  /// <param name="slot">Output parameter for the slot index</param>
  /// <returns>True if the item was found and slot index retrieved, false otherwise</returns>
  public static bool TryGetItemSlot(Entity entity, PrefabGUID prefabGUID, out int slot) {
    return InventoryUtilities.TryGetItemSlot(EntityManager, entity, prefabGUID, out slot);
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
    RemoveItemAtSlot(entity, slot, 0); // Default to removing all items at the slot
  }

  public static void RemoveItemAtSlot(Entity entity, int slot, int amount) {
    if (!TryGetItemAtSlot(entity, slot, out var item)) return;
    InventoryUtilitiesServer.TryRemoveItemAtIndex(EntityManager, entity, item.ItemType, amount <= 0 ? item.Amount : amount, slot, true);
  }

  public static void ClearSlot(Entity entity, int slot) {
    InventoryUtilitiesServer.ClearSlot(EntityManager, entity, slot);
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

    if (TryGetItemAtSlot(entity, slotIndex, out var item)) {
      item.MaxAmountOverride = maxAmount;
      item.Amount = amount;
      return;
    }
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
    return TryGetInventoryEntity(entity, out _);
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
    if (!TryGetInventoryEntity(entity, out var inventoryEntity)) return false;

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
    if (!TryGetInventoryEntity(entity, out var inventoryEntity)) return default;
    return EntityManager.GetBuffer<InventoryBuffer>(inventoryEntity);
  }

  /// <summary>
  /// Removes all items from an entity's inventory, making it completely empty.
  /// </summary>
  /// <param name="entity">The entity whose inventory to clear</param>
  public static void ClearInventory(Entity entity) {
    // Check if entity has an inventory
    if (!TryGetInventoryEntity(entity, out var inventoryEntity)) return;

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

  /// <summary>
  /// Returns the index of the first empty slot in the entity's inventory, or -1 if none is available or inventory does not exist.
  /// </summary>
  /// <param name="entity">The entity whose inventory will be checked</param>
  /// <returns>The index of the first empty slot, or -1 if no empty slot is found or inventory is missing</returns>
  public static int GetEmptySlot(Entity entity) {
    if (!HasInventory(entity)) {
      return -1;
    }

    var inventoryBuffer = GetInventoryItems(entity);

    for (int i = 0; i < inventoryBuffer.Length; i++) {
      if (inventoryBuffer[i].ItemType.Equals(PrefabGUID.Empty)) {
        return i;
      }
    }

    return -1;
  }

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
    if (!TryGetInventoryEntity(entity, out var inventoryEntity)) {
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
  /// Attempts to get the inventory entity associated with the given entity.
  /// </summary>
  /// <param name="entity">The entity to check for an inventory</param>
  /// <param name="inventoryEntity">The resulting inventory entity if found, or Entity.Null if not</param>
  /// <returns>True if the inventory entity was found, false otherwise</returns>
  public static bool TryGetInventoryEntity(Entity entity, out Entity inventoryEntity) {
    if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, entity, out inventoryEntity)) {
      inventoryEntity = Entity.Null;
      return false;
    }
    return true;
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
  /// Moves an item from a specific slot in one inventory to another inventory.
  /// For unique items (with ItemEntity): moves only from the specific slot.
  /// For stackable items (without ItemEntity): collects the required amount from any slots containing the item.
  /// </summary>
  /// <param name="fromInventory">Source inventory entity</param>
  /// <param name="toInventory">Destination inventory entity</param>
  /// <param name="fromSlot">Slot index in the source inventory</param>
  /// <param name="amount">Amount to move (0 = move all from slot for unique items, or all available for stackable items)</param>
  /// <returns>True if the item was successfully moved, false otherwise</returns>
  public static bool TransferItem(Entity fromInventory, Entity toInventory, int fromSlot, int amount = 0) {
    try {
      // Use existing method to get item at slot
      if (!TryGetItemAtSlot(fromInventory, fromSlot, out var itemEntry)) {
        return false;
      }

      var hasItemEntity = !itemEntry.ItemEntity.GetEntityOnServer().Equals(Entity.Null);

      if (hasItemEntity) {
        // For unique items, move only from the specific slot
        return MoveUniqueItem(fromInventory, toInventory, fromSlot, itemEntry);
      } else {
        // For stackable items, collect from any slots if needed
        return MoveStackableItemFromAnySlot(fromInventory, toInventory, itemEntry.ItemType, amount > 0 ? amount : itemEntry.Amount);
      }

    } catch (Exception e) {
      Log.Error($"Error moving item between inventories: {e}");
      return false;
    }
  }

  /// <summary>
  /// Moves stackable items by collecting the required amount from any slots in the inventory.
  /// </summary>
  private static bool MoveStackableItemFromAnySlot(Entity fromInventory, Entity toInventory, PrefabGUID itemType, int requestedAmount) {
    try {
      // Check if source inventory has enough of the item
      var totalAvailable = GetItemAmountInInventory(fromInventory, itemType);

      if (totalAvailable < requestedAmount) {
        Log.Warning($"Not enough items available. Requested: {requestedAmount}, Available: {totalAvailable}");
        return false;
      }

      // Try to add the items to destination inventory first
      if (!AddItemToInventoryDirect(toInventory, itemType, requestedAmount, out var transferredAmount)) {
        Log.Warning("Failed to add items to destination inventory");
        return false;
      }

      if (transferredAmount <= 0) {
        Log.Warning("No items were transferred");
        return false;
      }

      // Now remove the transferred amount from source inventory
      RemoveItem(fromInventory, itemType, transferredAmount);

      return true;

    } catch (Exception e) {
      Log.Error($"Error moving stackable item from any slot: {e}");
      return false;
    }
  }

  /// <summary>
  /// Gets the total amount of a specific item type in an inventory.
  /// </summary>
  private static int GetItemAmountInInventory(Entity inventory, PrefabGUID itemType) {
    try {
      var inventoryItems = GetInventoryItems(inventory);
      if (inventoryItems.Equals(default)) return 0;

      int totalAmount = 0;
      for (int i = 0; i < inventoryItems.Length; i++) {
        if (inventoryItems[i].ItemType.Equals(itemType)) {
          totalAmount += inventoryItems[i].Amount;
        }
      }

      return totalAmount;
    } catch (Exception e) {
      Log.Error($"Error getting item amount in inventory: {e}");
      return 0;
    }
  }

  /// <summary>
  /// Moves an item between inventories by item prefab.
  /// Finds the slot of the item in the source inventory and moves the specified amount to the destination inventory.
  /// </summary>
  /// <param name="fromInventory">Source inventory entity</param>
  /// <param name="toInventory">Destination inventory entity</param>
  /// <param name="itemPrefab">The PrefabGUID of the item to move</param>
  /// <param name="amount">Amount to move (0 = move all)</param>
  /// <returns>True if the item was successfully moved, false otherwise</returns>
  public static bool TransferItem(Entity fromInventory, Entity toInventory, PrefabGUID itemPrefab, int amount = 0) {
    if (!TryGetItemSlot(fromInventory, itemPrefab, out var fromSlot)) return false;
    return TransferItem(fromInventory, toInventory, fromSlot, amount);
  }

  /// <summary>
  /// Moves a unique item (with ItemEntity) between inventories.
  /// </summary>
  private static bool MoveUniqueItem(Entity fromInventory, Entity toInventory, int fromSlot, InventoryBuffer itemEntry) {
    try {
      var inventoryItems = GetInventoryItems(toInventory);

      if (inventoryItems.Equals(default)) {
        Log.Error("Destination inventory buffer not found");
        return false;
      }

      int emptySlot = GetEmptySlot(toInventory);

      if (emptySlot == -1) {
        return false;
      }

      inventoryItems[emptySlot] = itemEntry;

      var itemEntity = itemEntry.ItemEntity.GetEntityOnServer();

      if (!itemEntity.Has<InventoryItem>()) return false;

      itemEntity.With((ref InventoryItem inventoryItem) => {
        inventoryItem.ContainerEntity = toInventory;
      });

      ClearSlot(fromInventory, fromSlot);

      return true;

    } catch (Exception e) {
      Log.Error($"Error moving unique item: {e}");
      return false;
    }
  }

  /// <summary>
  /// Helper method to add items directly to inventory and return transferred amount.
  /// Extends existing AddItem functionality.
  /// </summary>
  private static bool AddItemToInventoryDirect(Entity inventory, PrefabGUID itemGuid, int amount, out int transferredAmount) {
    transferredAmount = 0;

    try {
      // Create temporary item entry
      var moveEntry = new InventoryBuffer {
        ItemType = itemGuid,
        Amount = amount
      };

      var addItemSettings = new AddItemSettings() {
        EntityManager = EntityManager,
        ItemDataMap = GameManager.ItemLookupMap
      };

      var addItemResponse = InventoryUtilitiesServer.TryAddItem(addItemSettings, inventory, moveEntry);

      if (addItemResponse.Success) {
        transferredAmount = amount - addItemResponse.RemainingAmount;
        return true;
      }

      return false;
    } catch (Exception e) {
      Log.Error($"Error adding item to inventory: {e}");
      return false;
    }
  }

  /// <summary>
  /// Helper method to update the amount of an item in a specific slot.
  /// Complements existing slot management methods.
  /// </summary>
  private static void UpdateSlotAmount(Entity inventory, int slot, int newAmount) {
    try {
      if (TryGetItemAtSlot(inventory, slot, out var item)) {
        var inventoryItems = GetInventoryItems(inventory);
        if (!inventoryItems.Equals(default) && slot < inventoryItems.Length) {
          var updatedEntry = item;
          updatedEntry.Amount = newAmount;
          inventoryItems[slot] = updatedEntry;
        }
      }
    } catch (Exception e) {
      Log.Error($"Error updating slot amount: {e}");
    }
  }

  /// <summary>
  /// Moves an item between inventories with additional validation and error handling.
  /// This is a wrapper method that provides extra safety checks using existing HasInventory method.
  /// </summary>
  /// <param name="fromEntity">Entity that owns the source inventory</param>
  /// <param name="toEntity">Entity that owns the destination inventory</param>
  /// <param name="fromSlot">Slot index in the source inventory</param>
  /// <param name="amount">Amount to move (0 = move all)</param>
  /// <returns>True if the item was successfully moved, false otherwise</returns>
  public static bool TransferItemBetweenEntities(Entity fromEntity, Entity toEntity, int fromSlot, int amount = 0) {
    try {
      // Use existing method to check if entities have inventories
      if (!HasInventory(fromEntity)) {
        Log.Error("Source entity has no inventory");
        return false;
      }

      if (!HasInventory(toEntity)) {
        Log.Error("Destination entity has no inventory");
        return false;
      }

      if (!TryGetInventoryEntity(fromEntity, out var fromInventory)) {
        Log.Error("Failed to get source inventory entity");
        return false;
      }

      if (!TryGetInventoryEntity(toEntity, out var toInventory)) {
        Log.Error("Failed to get destination inventory entity");
        return false;
      }

      return TransferItem(fromInventory, toInventory, fromSlot, amount);
    } catch (Exception e) {
      Log.Error($"Error moving item between entities: {e}");
      return false;
    }
  }

  /// <summary>
  /// Moves an item between inventories by item prefab, with additional validation and error handling.
  /// Finds the slot of the item in the source inventory and moves the specified amount to the destination inventory.
  /// </summary>
  /// <param name="fromInventory">Entity that owns the source inventory</param>
  /// <param name="toInventory">Entity that owns the destination inventory</param>
  /// <param name="itemPrefab">The PrefabGUID of the item to move</param>
  /// <param name="amount">Amount to move (0 = move all)</param>
  /// <returns>True if the item was successfully moved, false otherwise</returns>
  public static bool TransferItemBetweenEntities(Entity fromInventory, Entity toInventory, PrefabGUID itemPrefab, int amount = 0) {
    if (!TryGetItemSlot(fromInventory, itemPrefab, out var fromSlot)) return false;
    return TransferItemBetweenEntities(fromInventory, toInventory, fromSlot, amount);
  }
}