# AdminService

Documentation for the `AdminService` used by ScarletCore to manage administrator privileges and permissions.

**Location**
# AdminService

Comprehensive documentation for the `AdminService` helpers used by ScarletCore to manage administrator privileges and permissions.

**Location**
- Service: `Services/AdminService.cs`

## Overview
`AdminService` exposes helpers to add, remove and check admin privileges for players. It performs ECS interactions (creating auth/deauth events, adding/removing components) and updates the local admin list persisted by `GameSystems.AdminAuthSystem`.

## Table of Contents

- Overview
- Public APIs
- Internal commands & initialization
- Examples
- Best Practices
- Performance & Thread Safety
- See also

---

## Public APIs

### AddAdmin
```csharp
public static void AddAdmin(PlayerData playerData)
public static void AddAdmin(string playerName)
public static void AddAdmin(ulong playerId)
```
- Adds admin privileges to a player. Overloads accept `PlayerData`, player name or platform id. When the player is online `AddAdmin(ulong)` creates an ECS `AdminAuthEvent` entity and associates it with the player's character and user entities. The method also adds the id to `GameSystems.AdminAuthSystem._LocalAdminList` and calls `Save()`/`Refresh()`.

### RemoveAdmin
```csharp
public static void RemoveAdmin(PlayerData playerData)
public static void RemoveAdmin(string playerName)
public static void RemoveAdmin(ulong playerId)
```
- Removes admin privileges. When online the method removes the `AdminUser` component from the user's entity (if present), refreshes user data, creates a `DeauthAdminEvent` ECS entity, and removes the id from the local admin list (persisting changes).

### IsAdmin
```csharp
public static bool IsAdmin(ulong platformId)
public static bool IsAdmin(string playerName)
```
- `IsAdmin(ulong)` checks `GameSystems.AdminAuthSystem._LocalAdminList` for the platform id.
- `IsAdmin(string)` resolves the player name via `PlayerService` and then checks by id; logs a warning if the player cannot be resolved.

---

## Internal commands & initialization

The service also contains internal command handlers and startup initialization logic (marked `internal`):

- `Initialize()` — Subscribes to `PlayerEvents.PlayerJoined` and auto-assigns admin when a per-player `auto_admin_<platformId>` flag is set in the plugin database.
- `[Command("autoadmin", ...)]` — Toggles the calling player's auto-admin setting. Admin-only command.
- `[Command("toggleadmin", ...)]` — Toggles admin status for a target player (requires `admin.toggle` permission). Resolves player name if provided; otherwise operates on the sender.

These are internal and decorated with the project's `Command` attribute; they rely on `CommandContext` and `PlayerService` for lookups and replies.

---

## Examples

```csharp
// Promote by platform id
AdminService.AddAdmin(12345678901234567UL);

// Promote an online player by name
AdminService.AddAdmin("SomePlayerName");

// Demote by platform id
AdminService.RemoveAdmin(12345678901234567UL);

// Check admin
if (AdminService.IsAdmin(player.PlatformId)) {
  // perform admin-only logic
}

// Toggle auto-admin in command handler (internal usage example)
// [Command("autoadmin")] will flip the auto-admin flag saved in Plugin.Database
```

---

## Best Practices

- Prefer using `PlayerData` or platform id overloads (`ulong`) when available; name-based overloads require an online player lookup and will log a warning if the player isn't found.
- Changes that affect ECS components or create events should be executed in server-side game logic (main thread).
- Use `RoleService` methods to manage roles consistently — `AddAdmin(ulong)` uses `RoleService.AddRoleToPlayer` and `RemoveAdmin(ulong)` uses `RoleService.RemoveRoleFromPlayer` to keep role state consistent.
- After modifying the local admin list the service calls `Save()` and `Refresh()` on `GameSystems.AdminAuthSystem._LocalAdminList` to persist and apply changes immediately.

---

## Performance & Thread Safety

- Methods interact with ECS `EntityManager` and game systems; they are not thread-safe. Call from the main thread.
- Name-based lookups (`AddAdmin(string)`, `RemoveAdmin(string)`, `IsAdmin(string)`) perform `PlayerService` lookups which are O(1) for online players but will fail for offline players; prefer platform id where possible.

---

## See also

- `Services/AdminService.cs` — implementation reference.
- `RoleService` — role management helpers used by admin operations.
- `GameSystems.AdminAuthSystem` — local admin list and persistence.
