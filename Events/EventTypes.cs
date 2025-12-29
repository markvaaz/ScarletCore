namespace ScarletCore.Events;

/// <summary>
/// Defines server-wide events for global lifecycle and persistence operations.
/// </summary>
public enum ServerEvents {
  /// <summary>
  /// Triggered when the server is initialized and ready.
  /// </summary>
  OnInitialize,
  /// <summary>
  /// Triggered when the server state is saved.
  /// </summary>
  OnSave
}

/// <summary>
/// Defines events related to player actions and state changes.
/// </summary>
public enum PlayerEvents {
  /// <summary>
  /// Triggered when a player joins the server.
  /// </summary>
  PlayerJoined,
  /// <summary>
  /// Triggered when a player leaves the server.
  /// </summary>
  PlayerLeft,
  /// <summary>
  /// Triggered when a player is kicked from the server.
  /// </summary>
  PlayerKicked,
  /// <summary>
  /// Triggered when a player is banned from the server.
  /// </summary>
  PlayerBanned,
  /// <summary>
  /// Triggered when a new character is created for a player.
  /// </summary>
  CharacterCreated,
  /// <summary>
  /// Triggered when a player's character is renamed.
  /// </summary>
  CharacterRenamed
}

/// <summary>
/// Defines prefix events that occur before core game logic, allowing interception or modification.
/// </summary>
public enum PrefixEvents {
  /// <summary>Triggered before a chat message is processed.</summary>
  OnChatMessage,
  /// <summary>Triggered before damage is dealt to an entity.</summary>
  OnDealDamage,
  /// <summary>Triggered before an entity's death is processed.</summary>
  OnDeath,
  /// <summary>Triggered before an ability cast starts.</summary>
  OnCastStarted,
  /// <summary>Triggered before an ability's cast is finished (pre-cast phase).</summary>
  OnPreCastFinished,
  /// <summary>Triggered before an ability's post-cast phase ends.</summary>
  OnPostCastEnded,
  /// <summary>Triggered before an ability cast is interrupted.</summary>
  OnCastInterrupted,
  /// <summary>Triggered before a unit is spawned in the world.</summary>
  OnUnitSpawned,
  /// <summary>Triggered before a player is downed (incapacitated).</summary>
  OnPlayerDowned,
  /// <summary>Triggered before a war event is processed.</summary>
  OnWarEvent,
  /// <summary>Triggered before an ability is replaced in a slot.</summary>
  OnReplaceAbilityOnSlot,
  /// <summary>Triggered before an entity interaction starts.</summary>
  OnInteract,
  /// <summary>Triggered before an entity interaction stops.</summary>
  OnInteractStop,
  /// <summary>Triggered before a shapeshift transformation occurs.</summary>
  OnShapeshift,
  /// <summary>Triggered before a player teleports using a waypoint.</summary>
  OnWaypointTeleport,
  /// <summary>Triggered before a travel buff is destroyed.</summary>
  OnDestroyTravelBuff,
  /// <summary>Triggered when a player's inventory changes.</summary>
  OnInventoryChanged,
  /// <summary>Triggered before an item is moved in the inventory.</summary>
  OnMoveItem
}

/// <summary>
/// Defines postfix events that occur after core game logic, allowing for post-processing or monitoring.
/// </summary>
public enum PostfixEvents {
  /// <summary>Triggered after a chat message is processed.</summary>
  OnChatMessage,
  /// <summary>Triggered after damage is dealt to an entity.</summary>
  OnDealDamage,
  /// <summary>Triggered after an entity's death is processed.</summary>
  OnDeath,
  /// <summary>Triggered after an ability cast starts.</summary>
  OnCastStarted,
  /// <summary>Triggered after an ability's cast is finished (pre-cast phase).</summary>
  OnPreCastFinished,
  /// <summary>Triggered after an ability's post-cast phase ends.</summary>
  OnPostCastEnded,
  /// <summary>Triggered after an ability cast is interrupted.</summary>
  OnCastInterrupted,
  /// <summary>Triggered after a unit is spawned in the world.</summary>
  OnUnitSpawned,
  /// <summary>Triggered after a player is downed (incapacitated).</summary>
  OnPlayerDowned,
  /// <summary>Triggered after a war event is processed.</summary>
  OnWarEvent,
  /// <summary>Triggered after an ability is replaced in a slot.</summary>
  OnReplaceAbilityOnSlot,
  /// <summary>Triggered after an entity interaction starts.</summary>
  OnInteract,
  /// <summary>Triggered after an entity interaction stops.</summary>
  OnInteractStop,
  /// <summary>Triggered after a shapeshift transformation occurs.</summary>
  OnShapeshift,
  /// <summary>Triggered after a player teleports using a waypoint.</summary>
  OnWaypointTeleport,
  /// <summary>Triggered after a travel buff is destroyed.</summary>
  OnDestroyTravelBuff,
  /// <summary>Triggered after a player's inventory changes.</summary>
  OnInventoryChanged,
  /// <summary>Triggered after an item is moved in the inventory.</summary>
  OnMoveItem
}