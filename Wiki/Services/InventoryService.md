# InventoryService Documentation

## Overview

`InventoryService` is a comprehensive utility service for managing entity inventories in the game. It provides methods for adding, removing, checking, transferring, and manipulating items in entity inventories using Unity's ECS and the game's inventory system.

## Table of Contents

- [Basic Operations](#basic-operations)
- [Item Management](#item-management)
- [Inventory Queries](#inventory-queries)
- [Advanced Operations](#advanced-operations)
- [Batch Operations](#batch-operations)
- [Item Transfer System](#item-transfer-system)
- [Examples](#examples)
- [Best Practices](#best-practices)

---

## Basic Operations

### Checking Inventory Status

#### HasInventory
```csharp
public static bool HasInventory(Entity entity)
```
Checks if an entity has an inventory component.

**Example:**
```csharp
if (InventoryService.HasInventory(playerEntity)) {
    // Entity has an inventory
}
```

#### IsInventoryEmpty
```csharp
public static bool IsInventoryEmpty(Entity entity)
```
Checks if an entity's inventory is completely empty.

**Example:**
```csharp
if (InventoryService.IsInventoryEmpty(chestEntity)) {
    Log.Info("Chest is empty");
}
```

#### IsFull
```csharp
public static bool IsFull(Entity entity)
```
Checks if an entity's inventory is completely full.

**Example:**
```csharp
if (InventoryService.IsFull(playerEntity)) {
    ServerChatUtils.SendSystemMessageToClient(entityManager, user, "Your inventory is full!");
}
```

#### GetInventorySize
```csharp
public static int GetInventorySize(Entity entity)
```
Gets the maximum capacity (total slots) of an entity's inventory.

**Example:**
```csharp
int maxSlots = InventoryService.GetInventorySize(playerEntity);
Log.Info($"Inventory has {maxSlots} slots");
```

---

## Item Management

### Adding Items

#### AddItem
```csharp
public static bool AddItem(Entity entity, PrefabGUID guid, int amount)
public static bool AddItem(Entity entity, PrefabGUID guid, int amount, out InventoryBuffer item)
```
Adds items to an entity's inventory if there's space available.

**Parameters:**
- `entity` - The entity to add items to
- `guid` - The GUID of the item to add
- `amount` - The quantity of items to add
- `item` (out) - The resulting InventoryBuffer item if successful

**Returns:** `true` if items were added successfully, `false` otherwise

**Example:**
```csharp
var ironOre = new PrefabGUID(1234567);
if (InventoryService.AddItem(playerEntity, ironOre, 50)) {
    Log.Info("Added 50 iron ore");
}

// With item reference
if (InventoryService.AddItem(playerEntity, ironOre, 50, out var item)) {
    Log.Info($"Added item to slot with amount: {item.Amount}");
}
```

#### AddItemAtSlot
```csharp
public static bool AddItemAtSlot(Entity entity, PrefabGUID guid, int amount, int slot)
public static bool AddItemAtSlot(Entity entity, PrefabGUID guid, int amount, int slot, out InventoryBuffer item)
```
Adds items to a specific slot in an entity's inventory.

**Example:**
```csharp
var sword = new PrefabGUID(9876543);
if (InventoryService.AddItemAtSlot(playerEntity, sword, 1, 0)) {
    Log.Info("Added sword to first slot");
}
```

#### AddWithMaxAmount
```csharp
public static bool AddWithMaxAmount(Entity entity, PrefabGUID guid, int amount, int maxAmount)
public static bool AddWithMaxAmount(Entity entity, PrefabGUID guid, int slot, int amount, int maxAmount)
```
Adds an item with a custom `MaxAmountOverride` value. Useful for custom stacking limits.

**Example:**
```csharp
var customItem = new PrefabGUID(1111111);
// Add 10 items but set max stack to 999
if (InventoryService.AddWithMaxAmount(playerEntity, customItem, 10, 999)) {
    Log.Info("Added custom item with special stack size");
}
```

### Removing Items

#### RemoveItem
```csharp
public static bool RemoveItem(Entity entity, PrefabGUID guid, int amount)
```
Removes a specified amount of items from an entity's inventory.

**Example:**
```csharp
var gold = new PrefabGUID(2222222);
if (InventoryService.RemoveItem(playerEntity, gold, 100)) {
    Log.Info("Removed 100 gold");
} else {
    Log.Info("Not enough gold");
}
```

#### RemoveItemAtSlot
```csharp
public static void RemoveItemAtSlot(Entity entity, int slot)
public static void RemoveItemAtSlot(Entity entity, int slot, int amount)
```
Removes items from a specific slot.

**Example:**
```csharp
// Remove all items at slot 5
InventoryService.RemoveItemAtSlot(playerEntity, 5);

// Remove 10 items from slot 5
InventoryService.RemoveItemAtSlot(playerEntity, 5, 10);
```

#### ClearSlot
```csharp
public static void ClearSlot(Entity entity, int slot)
```
Clears all items from a specific inventory slot.

**Example:**
```csharp
InventoryService.ClearSlot(chestEntity, 3);
```

#### ClearInventory
```csharp
public static void ClearInventory(Entity entity)
```
Removes all items from an entity's inventory, making it completely empty.

**Example:**
```csharp
InventoryService.ClearInventory(playerEntity);
Log.Info("Inventory cleared");
```

### Checking Items

#### HasItem
```csharp
public static bool HasItem(Entity entity, PrefabGUID guid)
```
Checks if an entity has at least one of the specified item.

**Example:**
```csharp
var key = new PrefabGUID(3333333);
if (InventoryService.HasItem(playerEntity, key)) {
    // Player has the key
    OpenDoor();
}
```

#### HasAmount
```csharp
public static bool HasAmount(Entity entity, PrefabGUID guid, int amount)
```
Checks if an entity has at least the specified amount of an item.

**Example:**
```csharp
var wood = new PrefabGUID(4444444);
if (InventoryService.HasAmount(playerEntity, wood, 50)) {
    // Player has at least 50 wood
    CraftItem();
}
```

#### GetItemAmount
```csharp
public static int GetItemAmount(Entity entity, PrefabGUID guid)
```
Gets the exact total amount of a specific item in an entity's inventory.

**Example:**
```csharp
var stone = new PrefabGUID(5555555);
int stoneCount = InventoryService.GetItemAmount(playerEntity, stone);
Log.Info($"Player has {stoneCount} stone");
```

---

## Inventory Queries

### Slot Operations

#### TryGetItemSlot
```csharp
public static bool TryGetItemSlot(Entity entity, PrefabGUID guid, out int slot)
```
Attempts to find the slot index of a specific item.

**Example:**
```csharp
var potion = new PrefabGUID(6666666);
if (InventoryService.TryGetItemSlot(playerEntity, potion, out int slot)) {
    Log.Info($"Potion found at slot {slot}");
}
```

#### TryGetItemAtSlot
```csharp
public static bool TryGetItemAtSlot(Entity entity, int slot, out InventoryBuffer item)
```
Attempts to get the item at a specific slot.

**Example:**
```csharp
if (InventoryService.TryGetItemAtSlot(playerEntity, 0, out var item)) {
    Log.Info($"Slot 0 contains: {item.ItemType.GuidHash}, Amount: {item.Amount}");
}
```

#### GetEmptySlotIndex
```csharp
public static int GetEmptySlotIndex(Entity entity)
```
Returns the index of the first empty slot, or -1 if none available.

**Example:**
```csharp
int emptySlot = InventoryService.GetEmptySlotIndex(playerEntity);
if (emptySlot != -1) {
    Log.Info($"First empty slot is at index {emptySlot}");
}
```

#### GetEmptySlotsCount
```csharp
public static int GetEmptySlotsCount(Entity entity)
```
Returns the total number of empty slots.

**Example:**
```csharp
int freeSlots = InventoryService.GetEmptySlotsCount(playerEntity);
Log.Info($"Player has {freeSlots} free inventory slots");
```

### Getting Inventory Data

#### GetInventoryItems
```csharp
public static DynamicBuffer<InventoryBuffer> GetInventoryItems(Entity entity)
```
Gets the inventory buffer containing all items.

**Example:**
```csharp
var items = InventoryService.GetInventoryItems(playerEntity);
for (int i = 0; i < items.Length; i++) {
    var item = items[i];
    if (!item.ItemType.Equals(PrefabGUID.Empty)) {
        Log.Info($"Slot {i}: {item.ItemType.GuidHash} x{item.Amount}");
    }
}
```

#### TryGetInventoryEntity
```csharp
public static bool TryGetInventoryEntity(Entity entity, out Entity inventoryEntity)
```
Attempts to get the inventory entity associated with the given entity.

**Example:**
```csharp
if (InventoryService.TryGetInventoryEntity(playerEntity, out var invEntity)) {
    // Work with inventory entity directly
}
```

---

## Advanced Operations

### Inventory Size Modification

#### ModifyInventorySize
```csharp
public static void ModifyInventorySize(Entity entity, int newSize)
```
Changes the inventory size. If reducing size, excess items are dropped.

**Example:**
```csharp
// Expand inventory to 50 slots
InventoryService.ModifyInventorySize(playerEntity, 50);

// Items in slots 40+ will be dropped if reducing to 40
InventoryService.ModifyInventorySize(playerEntity, 40);
```

### Special Operations

#### CreateDropItem
```csharp
public static void CreateDropItem(Entity entity, PrefabGUID guid, int amount = 1)
```
Creates a dropped item in the world at the entity's position.

**Example:**
```csharp
var loot = new PrefabGUID(7777777);
InventoryService.CreateDropItem(playerEntity, loot, 10);
```

#### CopyInventory
```csharp
public static void CopyInventory(Entity sourceEntity, Entity targetEntity)
```
Copies all items from one entity's inventory to another.

**Example:**
```csharp
// Copy player inventory to a storage chest
InventoryService.CopyInventory(playerEntity, chestEntity);
```

#### ForceAllSlotsMaxAmount
```csharp
public static void ForceAllSlotsMaxAmount(Entity entity)
```
**⚠️ DANGER!** Forces all items to have `MaxAmountOverride` equal to their current amount.

**Example:**
```csharp
// This makes items unstackable beyond their current amount
InventoryService.ForceAllSlotsMaxAmount(playerEntity);
```

#### AutoSort
```csharp
public static void AutoSort(Entity storage)
```
Automatically sorts the inventory of the specified storage entity using the game's built-in sort utility.

**Example:**
```csharp
// Sort a chest inventory
InventoryService.AutoSort(chestEntity);

// Sort player inventory
InventoryService.AutoSort(playerEntity);
```

---

## Batch Operations

### Batch Add/Remove

#### AddItems
```csharp
public static Dictionary<PrefabGUID, int> AddItems(Entity entity, Dictionary<PrefabGUID, int> items)
```
Adds multiple items in a single operation.

**Returns:** Dictionary of items that couldn't be added (if inventory became full)

**Example:**
```csharp
var itemsToAdd = new Dictionary<PrefabGUID, int> {
    { new PrefabGUID(1111), 50 },  // 50 wood
    { new PrefabGUID(2222), 30 },  // 30 stone
    { new PrefabGUID(3333), 10 }   // 10 iron
};

var failedItems = InventoryService.AddItems(playerEntity, itemsToAdd);
if (failedItems.Count > 0) {
    Log.Info("Some items couldn't be added - inventory full");
}
```

#### RemoveItems
```csharp
public static Dictionary<PrefabGUID, int> RemoveItems(Entity entity, Dictionary<PrefabGUID, int> items)
```
Removes multiple items in a single operation.

**Returns:** Dictionary of items that couldn't be removed (insufficient quantity)

**Example:**
```csharp
var itemsToRemove = new Dictionary<PrefabGUID, int> {
    { new PrefabGUID(1111), 20 },  // 20 wood
    { new PrefabGUID(2222), 15 }   // 15 stone
};

var failedItems = InventoryService.RemoveItems(playerEntity, itemsToRemove);
if (failedItems.Count > 0) {
    Log.Info("Some items couldn't be removed - not enough");
}
```

### Batch Queries

#### HasItems
```csharp
public static bool HasItems(Entity entity, Dictionary<PrefabGUID, int> items)
```
Checks if an entity has all specified items in required amounts.

**Example:**
```csharp
var craftingRecipe = new Dictionary<PrefabGUID, int> {
    { new PrefabGUID(1111), 20 },  // 20 wood
    { new PrefabGUID(2222), 10 },  // 10 iron
    { new PrefabGUID(3333), 5 }    // 5 leather
};

if (InventoryService.HasItems(playerEntity, craftingRecipe)) {
    // Player has all materials, can craft
    CraftItem();
}
```

#### GetItemAmounts
```csharp
public static Dictionary<PrefabGUID, int> GetItemAmounts(Entity entity, List<PrefabGUID> itemGuids)
```
Gets the amounts of multiple items.

**Example:**
```csharp
var itemsToCheck = new List<PrefabGUID> {
    new PrefabGUID(1111),
    new PrefabGUID(2222),
    new PrefabGUID(3333)
};

var amounts = InventoryService.GetItemAmounts(playerEntity, itemsToCheck);
foreach (var kvp in amounts) {
    Log.Info($"Item {kvp.Key.GuidHash}: {kvp.Value}");
}
```

#### GiveItemSet
```csharp
public static Dictionary<PrefabGUID, int> GiveItemSet(Entity entity, Dictionary<PrefabGUID, int> itemSet, bool clearFirst = false)
```
Gives a complete item set to an entity (useful for starter kits, admin tools).

**Example:**
```csharp
var starterKit = new Dictionary<PrefabGUID, int> {
    { new PrefabGUID(1111), 100 }, // 100 wood
    { new PrefabGUID(2222), 50 },  // 50 stone
    { new PrefabGUID(3333), 1 }    // 1 sword
};

// Clear inventory first, then add items
var failed = InventoryService.GiveItemSet(playerEntity, starterKit, clearFirst: true);
```

---

## Item Transfer System

The transfer system handles both unique items (weapons, armor with ItemEntity) and stackable items (resources).

### Basic Transfer

#### TransferItem (by slot)
```csharp
public static bool TransferItem(Entity fromInventory, Entity toInventory, int fromSlot, int amount = 0)
```
Moves an item from a specific slot in one inventory to another.

- **Unique items** (with ItemEntity): Moves only from the specific slot
- **Stackable items** (without ItemEntity): Collects the required amount from any slots

**Example:**
```csharp
// Move item from player slot 5 to chest
if (InventoryService.TransferItem(playerInv, chestInv, 5)) {
    Log.Info("Item transferred");
}

// Move specific amount
if (InventoryService.TransferItem(playerInv, chestInv, 5, 50)) {
    Log.Info("50 items transferred");
}
```

#### TransferItem (by PrefabGUID)
```csharp
public static bool TransferItem(Entity fromInventory, Entity toInventory, PrefabGUID itemPrefab, int amount = 0)
```
Finds the item by GUID and transfers it.

**Example:**
```csharp
var ironOre = new PrefabGUID(1234567);
if (InventoryService.TransferItem(playerInv, chestInv, ironOre, 100)) {
    Log.Info("100 iron ore transferred");
}
```

### Entity-Based Transfer

#### TransferItemBetweenEntities
```csharp
public static bool TransferItemBetweenEntities(Entity fromEntity, Entity toEntity, int fromSlot, int amount = 0)
public static bool TransferItemBetweenEntities(Entity fromEntity, Entity toEntity, PrefabGUID itemPrefab, int amount = 0)
```
Higher-level transfer methods that work with entity owners (includes validation).

**Example:**
```csharp
// Transfer by slot
if (InventoryService.TransferItemBetweenEntities(player, chest, 3)) {
    Log.Info("Item from player slot 3 moved to chest");
}

// Transfer by item type
var gold = new PrefabGUID(9999);
if (InventoryService.TransferItemBetweenEntities(player, chest, gold, 500)) {
    Log.Info("500 gold transferred to chest");
}
```

### Swap Items

#### SwapItemBetweenSlots
```csharp
public static bool SwapItemBetweenSlots(Entity fromEntity, Entity toEntity, int fromSlot, int toSlot)
```
Swaps items between two slots (can be same or different inventories).

**Features:**
- Works within the same inventory
- Works between different inventories
- Properly updates ItemEntity.ContainerEntity for unique items

**Example:**
```csharp
// Swap slots within same inventory
if (InventoryService.SwapItemBetweenSlots(player, player, 0, 5)) {
    Log.Info("Swapped slots 0 and 5");
}

// Swap between different inventories
if (InventoryService.SwapItemBetweenSlots(player, chest, 2, 8)) {
    Log.Info("Swapped player slot 2 with chest slot 8");
}
```

---

## Examples

### Example 1: Simple Item Shop

```csharp
public class SimpleShop {
    private static PrefabGUID GOLD = new PrefabGUID(12345);
    private static PrefabGUID SWORD = new PrefabGUID(54321);
    
    public static bool BuyItem(Entity player, PrefabGUID itemToBuy, int price) {
        // Check if player has enough gold
        if (!InventoryService.HasAmount(player, GOLD, price)) {
            Log.Info("Not enough gold!");
            return false;
        }
        
        // Check if player has inventory space
        if (InventoryService.IsFull(player)) {
            Log.Info("Inventory is full!");
            return false;
        }
        
        // Remove gold
        if (!InventoryService.RemoveItem(player, GOLD, price)) {
            return false;
        }
        
        // Add item
        if (InventoryService.AddItem(player, itemToBuy, 1)) {
            Log.Info("Item purchased successfully!");
            return true;
        }
        
        // Refund if adding failed
        InventoryService.AddItem(player, GOLD, price);
        return false;
    }
}
```

### Example 2: Crafting System

```csharp
public class CraftingSystem {
    public static bool CraftItem(Entity player, PrefabGUID result, Dictionary<PrefabGUID, int> recipe) {
        // Check if player has all required materials
        if (!InventoryService.HasItems(player, recipe)) {
            Log.Info("Missing required materials!");
            return false;
        }
        
        // Check if player has space for result
        if (InventoryService.IsFull(player)) {
            Log.Info("Inventory is full!");
            return false;
        }
        
        // Remove materials
        var failed = InventoryService.RemoveItems(player, recipe);
        if (failed.Count > 0) {
            Log.Error("Failed to remove materials!");
            return false;
        }
        
        // Add crafted item
        if (InventoryService.AddItem(player, result, 1)) {
            Log.Info("Item crafted successfully!");
            return true;
        }
        
        return false;
    }
}
```

### Example 3: Inventory Transfer UI

```csharp
public class InventoryTransferUI {
    public static void TransferItemToStorage(Entity player, Entity storage, int slotIndex) {
        if (!InventoryService.TryGetItemAtSlot(player, slotIndex, out var item)) {
            Log.Info("No item at this slot!");
            return;
        }
        
        if (InventoryService.IsFull(storage)) {
            Log.Info("Storage is full!");
            return;
        }
        
        if (InventoryService.TransferItemBetweenEntities(player, storage, slotIndex)) {
            Log.Info($"Transferred {item.ItemType.GuidHash} to storage");
        } else {
            Log.Info("Transfer failed!");
        }
    }
    
    public static void TransferAllItems(Entity from, Entity to) {
        var items = InventoryService.GetInventoryItems(from);
        int transferred = 0;
        
        for (int i = items.Length - 1; i >= 0; i--) {
            if (!items[i].ItemType.Equals(PrefabGUID.Empty)) {
                if (InventoryService.TransferItemBetweenEntities(from, to, i)) {
                    transferred++;
                }
            }
        }
        
        Log.Info($"Transferred {transferred} items");
    }
}
```

### Example 4: Item Sorting

```csharp
public class InventorySorter {
    public static void SortPlayerInventory(Entity player) {
        // Use the built-in sort function
        InventoryService.AutoSort(player);
        Log.Info("Inventory sorted");
    }
    
    public static void SortAllStorageChests(List<Entity> chests) {
        foreach (var chest in chests) {
            if (InventoryService.HasInventory(chest)) {
                InventoryService.AutoSort(chest);
            }
        }
        Log.Info($"Sorted {chests.Count} storage chests");
    }
    
    // Custom sort by item type if needed
    public static void CustomSortInventoryByItemType(Entity entity) {
        var items = InventoryService.GetInventoryItems(entity);
        var itemList = new List<InventoryBuffer>();
        
        // Collect all non-empty items
        for (int i = 0; i < items.Length; i++) {
            if (!items[i].ItemType.Equals(PrefabGUID.Empty)) {
                itemList.Add(items[i]);
            }
        }
        
        // Sort by ItemType GUID
        itemList.Sort((a, b) => a.ItemType.GuidHash.CompareTo(b.ItemType.GuidHash));
        
        // Clear inventory
        for (int i = 0; i < items.Length; i++) {
            InventoryService.ClearSlot(entity, i);
        }
        
        // Re-add sorted items
        for (int i = 0; i < itemList.Count; i++) {
            var item = itemList[i];
            InventoryService.AddItemAtSlot(entity, item.ItemType, item.Amount, i);
        }
    }
}
```

### Example 5: Starter Kit System

```csharp
public class StarterKitSystem {
    private static Dictionary<PrefabGUID, int> _warriorKit = new() {
        { new PrefabGUID(1001), 1 },   // Iron Sword
        { new PrefabGUID(1002), 1 },   // Iron Armor
        { new PrefabGUID(1003), 10 },  // Health Potions
        { new PrefabGUID(1004), 100 }  // Gold
    };
    
    private static Dictionary<PrefabGUID, int> _mageKit = new() {
        { new PrefabGUID(2001), 1 },   // Magic Staff
        { new PrefabGUID(2002), 1 },   // Mage Robe
        { new PrefabGUID(2003), 10 },  // Mana Potions
        { new PrefabGUID(2004), 100 }  // Gold
    };
    
    public static void GiveStarterKit(Entity player, string kitType) {
        var kit = kitType.ToLower() switch {
            "warrior" => _warriorKit,
            "mage" => _mageKit,
            _ => null
        };
        
        if (kit == null) {
            Log.Error($"Unknown kit type: {kitType}");
            return;
        }
        
        var failed = InventoryService.GiveItemSet(player, kit, clearFirst: false);
        
        if (failed.Count > 0) {
            Log.Info($"Some items couldn't be added - inventory might be full");
        } else {
            Log.Info($"Successfully gave {kitType} starter kit!");
        }
    }
}
```

---

## Best Practices

### 1. Always Check Before Operations

```csharp
// Good
if (InventoryService.HasInventory(entity)) {
    InventoryService.AddItem(entity, item, amount);
}

// Bad - may crash
InventoryService.AddItem(entity, item, amount);
```

### 2. Validate Inventory Space

```csharp
// Good
if (!InventoryService.IsFull(player)) {
    InventoryService.AddItem(player, item, amount);
} else {
    NotifyPlayerInventoryFull();
}

// Better - AddItem already checks this
if (InventoryService.AddItem(player, item, amount)) {
    // Success
} else {
    // Failed - might be full or other issue
}
```

### 3. Use Batch Operations When Possible

```csharp
// Good - single operation
var items = new Dictionary<PrefabGUID, int> { ... };
InventoryService.AddItems(player, items);

// Less efficient - multiple operations
foreach (var item in items) {
    InventoryService.AddItem(player, item.Key, item.Value);
}
```

### 4. Handle Return Values

```csharp
// Good
if (InventoryService.RemoveItem(player, item, amount)) {
    // Successfully removed
    PerformAction();
} else {
    // Failed - not enough items
    NotifyPlayer("Not enough items!");
}

// Bad - ignoring failure
InventoryService.RemoveItem(player, item, amount);
PerformAction(); // Might execute even if removal failed
```

### 5. Use Appropriate Transfer Methods

```csharp
// For inventory entities
InventoryService.TransferItem(invEntity1, invEntity2, slot);

// For entity owners (with validation)
InventoryService.TransferItemBetweenEntities(player, chest, slot);

// Choose based on what you have access to
```

### 6. Be Careful with ModifyInventorySize

```csharp
// Items will be dropped if reducing size
int currentSize = InventoryService.GetInventorySize(player);
int newSize = 30;

if (newSize < currentSize) {
    // Warn player that items will be dropped
    NotifyPlayer("Reducing inventory size - items may be dropped!");
}

InventoryService.ModifyInventorySize(player, newSize);
```

---

## Performance Considerations

- **GetInventoryItems** returns a `DynamicBuffer` reference - iterate efficiently
- **Batch operations** are more efficient than individual operations in loops
- **Transfer operations** validate existence and space - don't duplicate checks
- **ClearInventory** iterates all items - for large inventories, consider direct buffer manipulation
- Avoid calling **GetItemAmount** in loops - cache the value if needed multiple times

---

## Thread Safety

⚠️ **Warning:** This service is **NOT thread-safe**. All methods must be called from the main thread as they interact with Unity's EntityManager and game systems.

---

## Common Pitfalls

### 1. Entity vs Inventory Entity Confusion

Some methods work with the entity owner, others with the inventory entity itself:

```csharp
// Works with entity owner
InventoryService.AddItem(playerEntity, item, amount);

// Works with inventory entity
var items = InventoryService.GetInventoryItems(inventoryEntity);

// Use TryGetInventoryEntity to get the inventory entity
if (InventoryService.TryGetInventoryEntity(playerEntity, out var invEntity)) {
    // Now you have the actual inventory entity
}
```

### 2. Not Checking Return Values

```csharp
// Wrong - assumes success
InventoryService.AddItem(player, item, 100);
DeductPlayerGold(100);

// Correct - verify success
if (InventoryService.AddItem(player, item, 100)) {
    DeductPlayerGold(100);
}
```

### 3. Forgetting Amount = 0 Behavior

```csharp
// amount = 0 means "move all"
InventoryService.TransferItem(from, to, slot, 0); // Moves ALL items at slot

// Specify amount to move partial
InventoryService.TransferItem(from, to, slot, 50); // Moves only 50
```

---

## Requirements

- Unity ECS (Entities package)
- Game-specific systems: `ServerGameManager`, `GameSystems`
- ProjectM namespace (game-specific)
- Stunlock.Core namespace (game-specific)

---

## License

Part of the ScarletCore framework.