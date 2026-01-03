# BuffService

Comprehensive documentation for the `BuffService` helpers used by ScarletCore to apply and manage buffs on entities.

**Location**
- Service: `Services/BuffService.cs`

## Overview
`BuffService` is a convenience layer around the game's buff system. It provides safe helpers to apply, clean, query, modify and remove buffs while handling common pitfalls such as unwanted gameplay event components, lifetime/age management, and idempotent application.

## Table of Contents

- Basic Operations
- Clean / Safe Application
- Unique Application
- Queries & Removal
- Duration Management
- Examples
- Best Practices
- Performance & Thread Safety

---

## Basic Operations

### TryApplyBuff
```csharp
public static bool TryApplyBuff(Entity entity, PrefabGUID prefabGUID, float duration, out Entity buffEntity)
public static bool TryApplyBuff(Entity entity, PrefabGUID prefabGUID, float duration = 0)
```
Applies the specified buff prefab to `entity` and returns the created buff entity (first overload). `duration` is in seconds; use `-1` for permanent/indefinite. The method also removes a set of known gameplay-event components from the created buff to avoid triggering unwanted behaviors.

**Example:**
```csharp
// Apply buff for 30 seconds
BuffService.TryApplyBuff(targetEntity, someBuffGuid, 30f);

// Apply and get the buff entity
if (BuffService.TryApplyBuff(targetEntity, someBuffGuid, 30f, out var buffEntity)) {
  // work with buffEntity
}
```

---

## Clean / Safe Application

### TryApplyCleanBuff
```csharp
public static bool TryApplyCleanBuff(Entity entity, PrefabGUID prefabGUID, float duration, out Entity buffEntity)
public static bool TryApplyCleanBuff(Entity entity, PrefabGUID prefabGUID, float duration = 0)
```
Applies the buff and then "cleans" it by removing or clearing known gameplay-related components and buffers (spell mods, event listeners, sequences, absorb/amplify components, etc.). Use this when you need a minimal buff instance that won't trigger additional game logic.

**Example:**
```csharp
BuffService.TryApplyCleanBuff(targetEntity, minimalBuffGuid, 60f);
```

---

## Unique Application

### TryApplyUnique
```csharp
public static bool TryApplyUnique(Entity entity, PrefabGUID prefabGUID, float duration, out Entity buffEntity)
public static bool TryApplyUnique(Entity entity, PrefabGUID prefabGUID, float duration = 0)
```
Applies the buff only if the entity doesn't already have an instance. If a matching buff already exists the method returns `true` and provides the existing buff entity. Useful for idempotent application (starter buffs, persistent effects).

**Example:**
```csharp
BuffService.TryApplyUnique(targetEntity, onceOnlyBuffGuid, 0);
```

---

## Queries & Removal

### TryGetBuff
```csharp
public static bool TryGetBuff(Entity entity, PrefabGUID prefabGUID, out Entity buff)
```
Attempts to find and return the buff entity instance for the specified prefab applied to `entity`.

### HasBuff
```csharp
public static bool HasBuff(Entity entity, PrefabGUID prefabGUID)
```
Returns whether `entity` currently has the specified buff applied.

### TryRemoveBuff
```csharp
public static bool TryRemoveBuff(Entity entity, PrefabGUID prefabGUID)
```
Removes the buff instance if present. Uses `DestroyUtility.Destroy` internally to properly teardown the buff entity.

**Example:**
```csharp
if (BuffService.HasBuff(targetEntity, someBuffGuid)) {
  BuffService.TryRemoveBuff(targetEntity, someBuffGuid);
}
```

---

## Duration Management

### GetBuffRemainingDuration
```csharp
public static float GetBuffRemainingDuration(Entity entity, PrefabGUID prefabGUID)
```
Returns the remaining duration in seconds for the buff, or `-1` if the buff is missing or permanent/indefinite.

### ModifyBuffDuration
```csharp
public static bool ModifyBuffDuration(Entity entity, PrefabGUID prefabGUID, float newDuration)
```
Sets a new duration for an existing buff. When `newDuration <= 0` the method makes the buff permanent (if it has a `LifeTime` component). Returns `true` on success.

**Example:**
```csharp
// Extend to 120 seconds
BuffService.ModifyBuffDuration(targetEntity, someBuffGuid, 120f);

// Make permanent
BuffService.ModifyBuffDuration(targetEntity, someBuffGuid, -1);
```

---

## Examples

```csharp
// Apply a temporary buff for 30 seconds
BuffService.TryApplyBuff(targetEntity, someBuffGuid, 30f);

// Apply a cleaned buff (no gameplay event components)
BuffService.TryApplyCleanBuff(targetEntity, cleanBuffGuid, 60f);

// Ensure a buff is applied only once
BuffService.TryApplyUnique(targetEntity, uniqueBuffGuid, 0);

// Remove a buff
BuffService.TryRemoveBuff(targetEntity, someBuffGuid);

// Get remaining duration
float remaining = BuffService.GetBuffRemainingDuration(targetEntity, someBuffGuid);

// Modify duration
BuffService.ModifyBuffDuration(targetEntity, someBuffGuid, 120f);
```

---

## Best Practices

- Prefer `TryApplyCleanBuff` for effects that must not trigger extra gameplay logic (damage, sequences, event listeners).
- Use `TryApplyUnique` when you require idempotent buff application (avoid stacking duplicates).
- Always validate `Entity` and `PrefabGUID` inputs before calling these methods (ensure `PrefabGUID.GuidHash != 0`).
- These helpers interact with `GameSystems.EntityManager` and `BuffUtility` — call them from server-side/game-thread contexts only.

## Performance & Thread Safety

- Methods interact with Unity's ECS `EntityManager` and are not thread-safe. Call from the main thread.
- Prefer batching or using `TryApplyUnique` for repeated operations to avoid unnecessary allocations and entity creation.

## See also

- `Services/BuffService.cs` — implementation reference.
- `BuffUtility`, `DestroyUtility`, `GameSystems.DebugEventsSystem` — underlying helpers used by the service.

