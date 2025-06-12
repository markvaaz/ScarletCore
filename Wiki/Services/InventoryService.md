# InventoryService

InventoryService provides utility methods for managing entity inventories, handling operations like adding, removing, and checking items in V Rising.

## Overview

```csharp
using ScarletCore.Services;
using ProjectM;
using Stunlock.Core;

// Add items to an inventory
InventoryService.AddItem(entity, itemGUID, 10);

// Check if entity has specific item
bool hasItem = InventoryService.HasItem(entity, itemGUID);
```

## Features

- Item addition and removal operations
- Inventory capacity and status checking
- Bulk operations for multiple items
- Item transfer between entities
- Inventory clearing and management
- Built-in error handling and validation

## Methods

### HasInventory
Checks if the specified entity has an inventory component.

```csharp
if (InventoryService.HasInventory(entity)) {
  Log.Info("Entity has an inventory");
}
```

**Parameters:**
- `entity` - The entity to check for inventory

**Returns:** True if the entity has an inventory, false otherwise

### AddItem
Adds items to an entity's inventory if there's space available.

```csharp
InventoryService.AddItem(entity, itemGUID, 5);
```

**Parameters:**
- `entity` - The entity to add items to
- `guid` - The GUID of the item to add
- `amount` - The quantity of items to add

**Behavior:**
- Does not add items if inventory is full
- Includes error handling for failed operations

### HasItem
Checks if an entity has at least one of the specified item.

```csharp
if (InventoryService.HasItem(entity, itemGUID)) {
  Log.Info("Entity has the item");
}
```

**Parameters:**
- `entity` - The entity to check
- `guid` - The GUID of the item to look for

**Returns:** True if the entity has the item, false otherwise

### HasAmount
Checks if an entity has at least the specified amount of an item.

```csharp
if (InventoryService.HasAmount(entity, itemGUID, 10)) {
  Log.Info("Entity has enough items");
}
```

**Parameters:**
- `entity` - The entity to check
- `guid` - The GUID of the item to check
- `amount` - The minimum amount required

**Returns:** True if the entity has enough of the item, false otherwise

### RemoveItem
Removes a specified amount of items from an entity's inventory.

```csharp
bool removed = InventoryService.RemoveItem(entity, itemGUID, 3);
if (removed) {
  Log.Info("Items removed successfully");
}
```

**Parameters:**
- `entity` - The entity to remove items from
- `guid` - The GUID of the item to remove
- `amount` - The quantity to remove

**Returns:** True if the removal was successful, false otherwise

**Behavior:**
- Verifies entity has inventory and sufficient items before removal

### GetInventoryItems
Gets the inventory buffer containing all items for the specified entity.

```csharp
var items = InventoryService.GetInventoryItems(entity);
if (items.IsCreated) {
  foreach (var item in items) {
    Log.Info($"Item: {item.ItemType}, Amount: {item.Amount}");
  }
}
```

**Parameters:**
- `entity` - The entity whose inventory to retrieve

**Returns:** A dynamic buffer of inventory items, or default if no inventory exists

### ClearInventory
Removes all items from an entity's inventory, making it completely empty.

```csharp
InventoryService.ClearInventory(entity);
```

**Parameters:**
- `entity` - The entity whose inventory to clear

### GetInventorySize
Gets the maximum capacity of an entity's inventory.

```csharp
int maxSlots = InventoryService.GetInventorySize(entity);
Log.Info($"Inventory has {maxSlots} total slots");
```

**Parameters:**
- `entity` - The entity to check

**Returns:** The total number of slots in the inventory

### GetItemAmount
Gets the exact amount of a specific item in an entity's inventory.

```csharp
int amount = InventoryService.GetItemAmount(entity, itemGUID);
Log.Info($"Player has {amount} of this item");
```

**Parameters:**
- `entity` - The entity to check
- `guid` - The GUID of the item to count

**Returns:** The total quantity of the specified item

### IsFull
Checks if an entity's inventory is completely full.

```csharp
if (InventoryService.IsFull(entity)) {
  Log.Info("Inventory is full");
}
```

**Parameters:**
- `entity` - The entity to check

**Returns:** True if the inventory is full, false otherwise

## Bulk Operations

### AddItems
Adds multiple items to an entity's inventory in a single operation.

```csharp
var itemsToAdd = new Dictionary<PrefabGUID, int> {
  { itemGUID1, 10 },
  { itemGUID2, 5 }
};

var failedItems = InventoryService.AddItems(entity, itemsToAdd);
if (failedItems.Count > 0) {
  Log.Info("Some items couldn't be added due to full inventory");
}
```

**Parameters:**
- `entity` - The entity to add items to
- `items` - Dictionary of items where key is PrefabGUID and value is amount

**Returns:** Dictionary of items that couldn't be added (if inventory became full)

### RemoveItems
Removes multiple items from an entity's inventory in a single operation.

```csharp
var itemsToRemove = new Dictionary<PrefabGUID, int> {
  { itemGUID1, 5 },
  { itemGUID2, 3 }
};

var failedItems = InventoryService.RemoveItems(entity, itemsToRemove);
```

**Parameters:**
- `entity` - The entity to remove items from
- `items` - Dictionary of items where key is PrefabGUID and value is amount

**Returns:** Dictionary of items that couldn't be removed (insufficient quantity)

### HasItems
Checks if an entity has all the specified items in the required amounts.

```csharp
var requiredItems = new Dictionary<PrefabGUID, int> {
  { itemGUID1, 10 },
  { itemGUID2, 5 }
};

if (InventoryService.HasItems(entity, requiredItems)) {
  Log.Info("Entity has all required items");
}
```

**Parameters:**
- `entity` - The entity to check
- `items` - Dictionary of items where key is PrefabGUID and value is required amount

**Returns:** True if entity has all items in sufficient quantities, false otherwise

### GetItemAmounts
Gets the amounts of multiple items in an entity's inventory.

```csharp
var itemGuids = new List<PrefabGUID> { itemGUID1, itemGUID2 };
var amounts = InventoryService.GetItemAmounts(entity, itemGuids);

foreach (var item in amounts) {
  Log.Info($"Item {item.Key}: {item.Value}");
}
```

**Parameters:**
- `entity` - The entity to check
- `itemGuids` - List of item GUIDs to check

**Returns:** Dictionary with item GUIDs as keys and current amounts as values

### GiveItemSet
Gives a complete item set to an entity.

```csharp
var itemSet = new Dictionary<PrefabGUID, int> {
  { itemGUID1, 10 },
  { itemGUID2, 5 }
};

var failedItems = InventoryService.GiveItemSet(entity, itemSet, clearFirst: true);
```

**Parameters:**
- `entity` - The entity to give items to
- `itemSet` - Dictionary of items where key is PrefabGUID and value is amount
- `clearFirst` - Whether to clear inventory before adding items

**Returns:** Dictionary of items that couldn't be added

### TransferItems
Transfers items from one entity's inventory to another.

```csharp
var itemsToTransfer = new Dictionary<PrefabGUID, int> {
  { itemGUID1, 5 }
};

var failedItems = InventoryService.TransferItems(fromEntity, toEntity, itemsToTransfer);
```

**Parameters:**
- `fromEntity` - Source entity
- `toEntity` - Destination entity
- `items` - Dictionary of items to transfer

**Returns:** Dictionary of items that couldn't be transferred

**Behavior:**
- Automatically returns items to source if destination inventory is full

### GetInventorySummary
Gets a summary of all items in an entity's inventory.

```csharp
var summary = InventoryService.GetInventorySummary(entity);
foreach (var item in summary) {
  Log.Info($"Item {item.Key}: {item.Value}");
}
```

**Parameters:**
- `entity` - The entity to analyze

**Returns:** Dictionary with all items and their quantities

### GetFreeSlots
Counts the number of free slots in an entity's inventory.

```csharp
int freeSlots = InventoryService.GetFreeSlots(entity);
Log.Info($"Inventory has {freeSlots} free slots");
```

**Parameters:**
- `entity` - The entity to check

**Returns:** Number of free inventory slots

## Usage Examples

### Basic Item Management
```csharp
using ScarletCore.Services;
using ProjectM;
using Stunlock.Core;

var itemGUID = new PrefabGUID(-484591467); // Example item GUID

// Check if entity has inventory
if (InventoryService.HasInventory(entity)) {
  // Add items
  InventoryService.AddItem(entity, itemGUID, 10);
  
  // Check if has items
  if (InventoryService.HasItem(entity, itemGUID)) {
    Log.Info("Item added successfully");
  }
  
  // Get exact amount
  int amount = InventoryService.GetItemAmount(entity, itemGUID);
  Log.Info($"Player has {amount} items");
  
  // Remove some items
  if (InventoryService.RemoveItem(entity, itemGUID, 5)) {
    Log.Info("Items removed successfully");
  }
}
```

### Inventory Status Checking
```csharp
// Check inventory capacity
int totalSlots = InventoryService.GetInventorySize(entity);
int freeSlots = InventoryService.GetFreeSlots(entity);
Log.Info($"Inventory: {freeSlots}/{totalSlots} slots free");

// Check if inventory is full
if (InventoryService.IsFull(entity)) {
  Log.Info("Inventory is full, cannot add more items");
}

// Get complete inventory summary
var summary = InventoryService.GetInventorySummary(entity);
Log.Info($"Inventory contains {summary.Count} different item types");
```

### Bulk Operations
```csharp
// Add multiple items
var itemsToAdd = new Dictionary<PrefabGUID, int> {
  { new PrefabGUID(-484591467), 10 },
  { new PrefabGUID(-1531666018), 5 }
};

var failed = InventoryService.AddItems(entity, itemsToAdd);
if (failed.Count == 0) {
  Log.Info("All items added successfully");
}

// Check multiple items at once
var requiredItems = new Dictionary<PrefabGUID, int> {
  { new PrefabGUID(-484591467), 5 },
  { new PrefabGUID(-1531666018), 2 }
};

if (InventoryService.HasItems(entity, requiredItems)) {
  Log.Info("Player has all required items for crafting");
}
```

## Important Notes

- **Inventory validation** - Methods check for inventory existence before performing operations
- **Capacity handling** - Add operations respect inventory capacity limits
- **Bulk operations** - Return dictionaries of failed operations for error handling
- **Transfer safety** - Transfer operations automatically handle rollback on failure
- **Error handling** - All operations include built-in exception handling
