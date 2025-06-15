using System;
using System.Collections.Generic;
using Stunlock.Network;
using ScarletCore.Data;
using ProjectM.Network;
using Unity.Entities;
using Unity.Collections;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Gameplay.WarEvents;
using ProjectM.Shared.WarEvents;
using ScarletCore.Utils;
namespace ScarletCore.Events;

/// <summary>
/// Central event system for ScarletCore that allows other mods to subscribe to various game events
/// </summary>
public static class EventManager {
  #region System Events

  /// <summary>
  /// Event fired when ScarletCore initializes
  /// </summary>
  public static event EventHandler<InitializeEventArgs> OnInitialize;
  #endregion

  #region Chat Events

  /// <summary>
  /// Event fired when a chat message is sent
  /// </summary>
  public static event EventHandler<ChatMessageEventArgs> OnChatMessage;

  #endregion
  #region User Connection Events

  /// <summary>
  /// Event fired when a user successfully connects to the server
  /// </summary>
  public static event EventHandler<UserConnectedEventArgs> OnUserConnected;

  /// <summary>
  /// Event fired when a user disconnects from the server
  /// </summary>
  public static event EventHandler<UserDisconnectedEventArgs> OnUserDisconnected;

  /// <summary>
  /// Event fired when a user is kicked from the server
  /// </summary>
  public static event EventHandler<UserKickedEventArgs> OnUserKicked;

  /// <summary>
  /// Event fired when a user is banned from the server
  /// </summary>
  public static event EventHandler<UserBannedEventArgs> OnUserBanned;

  #endregion
  #region Death Events
  /// <summary>
  /// Event fired when entities die
  /// </summary>
  public static event EventHandler<DeathEventArgs> OnAnyDeath;

  /// <summary>
  /// Event fired when unfiltered deaths occur
  /// </summary>
  public static event EventHandler<DeathEventArgs> OnOtherDeath;

  /// <summary>
  /// Event fired when players die
  /// </summary>
  public static event EventHandler<DeathEventArgs> OnPlayerDeath;
  /// <summary>
  /// Event fired when V Blood units die
  /// </summary>
  public static event EventHandler<DeathEventArgs> OnVBloodDeath;

  /// <summary>
  /// Event fired when servants die
  /// </summary>
  public static event EventHandler<DeathEventArgs> OnServantDeath;
  #endregion

  #region Unit Spawn Events

  /// <summary>
  /// Event fired when units spawn
  /// </summary>
  public static event EventHandler<UnitSpawnEventArgs> OnUnitSpawn;

  #endregion

  #region Damage Events

  /// <summary>
  /// Event fired when damage is dealt
  /// </summary>
  public static event EventHandler<DamageEventArgs> OnDealDamage;

  #endregion

  #region War Events

  /// <summary>
  /// Event fired when war events start
  /// </summary>
  public static event EventHandler<WarEventStartedEventArgs> OnWarEventsStarted;

  /// <summary>
  /// Event fired when all war events end
  /// </summary>
  public static event EventHandler OnWarEventsEnded;

  #endregion

  #region Player Downed Events

  /// <summary>
  /// Event fired when players/vampires are downed
  /// </summary>
  public static event EventHandler<PlayerDownedEventArgs> OnPlayerDowned;

  #endregion

  #region Event Invocation Methods

  /// <summary>
  /// Invokes the OnInitialize event safely
  /// </summary>
  /// <param name="isFirstInitialization">Whether this is the first initialization</param>
  internal static void InvokeInitialize() {
    try {
      var args = new InitializeEventArgs();
      OnInitialize?.Invoke(null, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnInitialize event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnChatMessage event safely
  /// </summary>
  /// <param name="player">The PlayerData of the sender</param>
  /// <param name="message">The content of the chat message</param>
  /// <param name="messageType">The type of chat message (All, Global, Whisper, etc.)</param>
  /// <param name="targetPlayer">The PlayerData of the target user (if applicable, e.g., for whispers)</param>
  /// <returns>The event args with cancellation state</returns>
  internal static ChatMessageEventArgs InvokeChatMessage(PlayerData player, string message, ChatMessageType messageType, PlayerData targetPlayer = null, ChatMessageSystem instance = null) {
    try {
      var args = new ChatMessageEventArgs(player, message, messageType, targetPlayer);
      OnChatMessage?.Invoke(instance, args);

      return args;
    } catch (Exception ex) {
      Log.Error($"Error invoking OnChatMessage event: {ex}");
      return new ChatMessageEventArgs(player, message, messageType, targetPlayer);
    }
  }

  /// <summary>
  /// Invokes the OnUserConnected event safely
  /// </summary>
  /// <param name="player">The PlayerData of the connected user</param>
  internal static void InvokeUserConnected(PlayerData player, ServerBootstrapSystem instance = null) {
    try {
      var args = new UserConnectedEventArgs(player);
      OnUserConnected?.Invoke(instance, args);

    } catch (Exception ex) {
      Log.Error($"Error invoking OnUserConnected event: {ex}");
    }
  }
  /// <summary>
  /// Invokes the OnUserDisconnected event safely
  /// </summary>
  /// <param name="player">The PlayerData of the disconnected user</param>
  /// <param name="reason">The disconnection reason</param>
  internal static void InvokeUserDisconnected(PlayerData player, ConnectionStatusChangeReason reason, ServerBootstrapSystem instance = null) {
    try {
      var args = new UserDisconnectedEventArgs(player, reason);
      OnUserDisconnected?.Invoke(instance, args);

    } catch (Exception ex) {
      Log.Error($"Error invoking OnUserDisconnected event: {ex}");
    }
  }
  /// <summary>
  /// Invokes the OnUserKicked event safely
  /// </summary>
  /// <param name="player">The PlayerData of the kicked user</param>
  /// <param name="instance">The ServerBootstrapSystem instance</param>
  internal static void InvokeUserKicked(PlayerData player, ProjectM.ServerBootstrapSystem instance = null) {
    try {
      var args = new UserKickedEventArgs(player, instance);
      OnUserKicked?.Invoke(instance, args);

    } catch (Exception ex) {
      Log.Error($"Error invoking OnUserKicked event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnUserBanned event safely
  /// </summary>
  /// <param name="player">The PlayerData of the banned user</param>
  /// <param name="instance">The ServerBootstrapSystem instance</param>
  internal static void InvokeUserBanned(PlayerData player, ProjectM.ServerBootstrapSystem instance = null) {
    try {
      var args = new UserBannedEventArgs(player, instance);
      OnUserBanned?.Invoke(instance, args);

    } catch (Exception ex) {
      Log.Error($"Error invoking OnUserBanned event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnAnyDeath event safely with a batch of deaths
  /// </summary>
  /// <param name="deaths">List of death information</param>
  internal static void InvokeAnyDeath(List<DeathInfo> deaths, DeathEventListenerSystem instance = null) {
    try {
      if (deaths == null || deaths.Count == 0) return;

      var args = new DeathEventArgs(deaths);
      OnAnyDeath?.Invoke(instance, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnAnyDeath event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnOtherDeath event safely with a batch of Other deaths
  /// </summary>
  /// <param name="deaths">List of Other death information</param>
  internal static void InvokeOtherDeath(List<DeathInfo> deaths, DeathEventListenerSystem instance = null) {
    try {
      if (deaths == null || deaths.Count == 0) return;

      var args = new DeathEventArgs(deaths);
      OnOtherDeath?.Invoke(instance, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnOtherDeath event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnPlayerDeath event safely with a batch of player deaths
  /// </summary>
  /// <param name="deaths">List of player death information</param>
  internal static void InvokePlayerDeath(List<DeathInfo> deaths, DeathEventListenerSystem instance = null) {
    try {
      if (deaths == null || deaths.Count == 0) return;

      var args = new DeathEventArgs(deaths);
      OnPlayerDeath?.Invoke(instance, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnPlayerDeath event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnVBloodDeath event safely with a batch of V Blood deaths
  /// </summary>
  /// <param name="deaths">List of V Blood death information</param>
  internal static void InvokeVBloodDeath(List<DeathInfo> deaths, DeathEventListenerSystem instance = null) {
    try {
      if (deaths == null || deaths.Count == 0) return;

      var args = new DeathEventArgs(deaths);
      OnVBloodDeath?.Invoke(instance, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnVBloodDeath event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnServantDeath event safely with a batch of servant deaths
  /// </summary>
  /// <param name="deaths">List of servant death information</param>
  internal static void InvokeServantDeath(List<DeathInfo> deaths, DeathEventListenerSystem instance = null) {
    try {
      if (deaths == null || deaths.Count == 0) return;

      var args = new DeathEventArgs(deaths);
      OnServantDeath?.Invoke(instance, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnServantDeath event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnDealDamage event safely with a batch of damage instances
  /// </summary>
  /// <param name="damageInstances">List of damage information</param>
  internal static void InvokeDealDamage(List<DamageInfo> damageInstances, StatChangeSystem instance = null) {
    try {
      if (damageInstances == null || damageInstances.Count == 0) return;

      var args = new DamageEventArgs(damageInstances);
      OnDealDamage?.Invoke(instance, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnDealDamage event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnUnitSpawn event safely with a batch of unit spawns
  /// </summary>
  /// <param name="unitSpawns">List of unit spawn information</param>
  internal static void InvokeUnitSpawn(List<Entity> unitSpawns, UnitSpawnerReactSystem instance = null) {
    try {
      if (unitSpawns == null || unitSpawns.Count == 0) return;

      var args = new UnitSpawnEventArgs(unitSpawns);
      OnUnitSpawn?.Invoke(instance, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnUnitSpawn event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnWarEventsStarted event safely with a batch of war events
  /// </summary>
  /// <param name="warEvents">List of war events that started</param>
  internal static void InvokeWarEventsStarted(List<WarEvent> warEvents, WarEventSystem instance = null) {
    try {
      if (warEvents == null || warEvents.Count == 0) return;

      var args = new WarEventStartedEventArgs(warEvents);
      OnWarEventsStarted?.Invoke(instance, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnWarEventsStarted event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnWarEventsEnded event safely
  /// </summary>
  internal static void InvokeWarEventsEnded(WarEventSystem instance = null) {
    try {
      OnWarEventsEnded?.Invoke(instance, null);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnWarEventsEnded event: {ex}");
    }
  }

  /// <summary>
  /// Invokes the OnPlayerDowned event safely with a batch of downed entities
  /// </summary>
  /// <param name="downedEntities">List of entities that were downed</param>
  internal static void InvokeVampireDowned(NativeArray<Entity> downedEntities, VampireDownedServerEventSystem instance = null) {
    try {
      if (downedEntities.Length == 0) return;

      var entityList = new List<Entity>();
      foreach (var entity in downedEntities) {
        entityList.Add(entity);
      }

      var args = new PlayerDownedEventArgs(entityList);
      OnPlayerDowned?.Invoke(instance, args);
    } catch (Exception ex) {
      Log.Error($"Error invoking OnPlayerDowned event: {ex}");
    }
  }
  #endregion
  #region Event Management

  /// <summary>
  /// Gets the number of subscribers for the OnInitialize event
  /// </summary>
  public static int InitializeSubscriberCount => OnInitialize?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnChatMessage event
  /// </summary>
  public static int ChatMessageSubscriberCount => OnChatMessage?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnUserConnected event
  /// </summary>
  public static int UserConnectedSubscriberCount => OnUserConnected?.GetInvocationList()?.Length ?? 0;
  /// <summary>
  /// Gets the number of subscribers for the OnUserDisconnected event
  /// </summary>
  public static int UserDisconnectedSubscriberCount => OnUserDisconnected?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnUserKicked event
  /// </summary>
  public static int UserKickedSubscriberCount => OnUserKicked?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnUserBanned event
  /// </summary>
  public static int UserBannedSubscriberCount => OnUserBanned?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnAnyDeath event
  /// </summary>
  public static int AnyDeathSubscriberCount => OnAnyDeath?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnOtherDeath event
  /// </summary>
  public static int OtherDeathSubscriberCount => OnOtherDeath?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnPlayerDeath event
  /// </summary>
  public static int PlayerDeathSubscriberCount => OnPlayerDeath?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnVBloodDeath event
  /// </summary>
  public static int VBloodDeathSubscriberCount => OnVBloodDeath?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnServantDeath event
  /// </summary>
  public static int ServantDeathSubscriberCount => OnServantDeath?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnDealDamage event
  /// </summary>
  public static int DealDamageSubscriberCount => OnDealDamage?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnUnitSpawn event
  /// </summary>
  public static int UnitSpawnSubscriberCount => OnUnitSpawn?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnWarEventsStarted event
  /// </summary>
  public static int WarEventsStartedSubscriberCount => OnWarEventsStarted?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Gets the number of subscribers for the OnWarEventsEnded event
  /// </summary>
  public static int WarEventsEndedSubscriberCount => OnWarEventsEnded?.GetInvocationList()?.Length ?? 0;
  /// <summary>
  /// Gets the number of subscribers for the OnPlayerDowned event
  /// </summary>
  public static int PlayerDownedSubscriberCount => OnPlayerDowned?.GetInvocationList()?.Length ?? 0;

  /// <summary>
  /// Clears all event subscribers (use with caution)
  /// </summary>
  public static void ClearAllSubscribers() {
    OnInitialize = null;
    OnChatMessage = null;
    OnUserConnected = null;
    OnUserDisconnected = null;
    OnUserKicked = null;
    OnUserBanned = null;
    OnAnyDeath = null;
    OnOtherDeath = null;
    OnPlayerDeath = null;
    OnVBloodDeath = null;
    OnServantDeath = null;
    OnDealDamage = null;
    OnUnitSpawn = null;
    OnWarEventsStarted = null;
    OnWarEventsEnded = null;
    OnPlayerDowned = null;
    Log.Warning("All ScarletEvents subscribers have been cleared");
  }

  #endregion
}
