namespace ScarletCore.Events;

public enum ServerEvents {
  OnInitialize
}

public enum PlayerEvents {
  PlayerJoined,
  PlayerLeft,
  PlayerKicked,
  PlayerBanned,
  CharacterCreated
}

public enum PrefixEvents {
  OnChatMessage,
  OnDealDamage,
  OnDeath,
  OnCastStarted,
  OnPreCastFinished,
  OnPostCastEnded,
  OnCastInterrupted,
  OnUnitSpawned,
  OnPlayerDowned,
  OnWarEvent,
  OnReplaceAbilityOnSlot,
  OnInteract,
  OnShapeshift,
  OnWaypointTeleport,
  OnDestroyTravelBuff,
  OnInventoryChanged
}

public enum PostfixEvents {
  OnChatMessage,
  OnDealDamage,
  OnDeath,
  OnCastStarted,
  OnPreCastFinished,
  OnPostCastEnded,
  OnCastInterrupted,
  OnUnitSpawned,
  OnPlayerDowned,
  OnWarEvent,
  OnReplaceAbilityOnSlot,
  OnInteract,
  OnShapeshift,
  OnWaypointTeleport,
  OnDestroyTravelBuff,
  OnInventoryChanged
}