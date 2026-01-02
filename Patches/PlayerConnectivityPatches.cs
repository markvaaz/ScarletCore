using HarmonyLib;
using ProjectM;
using ScarletCore.Systems;
using ScarletCore.Events;
using ScarletCore.Services;
using ScarletCore.Utils;
using Stunlock.Network;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace ScarletCore.Patches;

[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
internal static class OnUserConnectedPatch {
  public static void Postfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId) {
    // Ensure game systems are initialized before processing connection
    if (!GameSystems.Initialized) GameSystems.Initialize();

    try {
      // Validate that the server bootstrap system is in a valid state
      // This prevents crashes when accessing internal server state
      if (__instance == null || __instance._NetEndPointToApprovedUserIndex == null || __instance._ApprovedUsersLookup == null) {
        Log.Warning("ServerBootstrapSystem instance or lookups are null.");
        return;
      }

      // Get the user index from the connection ID mapping
      // This maps network connections to approved user entries
      if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out var index)) {
        Log.Warning("Failed to get user index for connection.");
        return;
      }

      // Validate the user index is within valid bounds
      // Prevents array out of bounds exceptions
      if (index < 0 || index >= __instance._ApprovedUsersLookup.Length) {
        Log.Warning("User index is out of bounds.");
        return;
      }

      // Get the client data from the approved users lookup
      var client = __instance._ApprovedUsersLookup[index];
      if (client == null || client.UserEntity.Equals(Entity.Null)) {
        Log.Warning("Failed to get user entity.");
        return;
      }

      // Cache the player data for quick access by other systems
      // This creates or updates the player cache entry
      var playerData = PlayerService.SetPlayerCache(client.UserEntity);

      if (playerData == null) {
        Log.Warning("Failed to set player cache for connected user.");
        return;
      }

      // Fire the user connected event for subscribers (only if there are listeners)
      if (EventManager.GetSubscriberCount(PlayerEvents.PlayerJoined) > 0)
        EventManager.Emit(PlayerEvents.PlayerJoined, playerData);
    } catch (Exception e) {
      // Log connection errors without crashing the server
      Log.Error($"An error occurred while connecting player: {e.Message}");
    }
  }
}

// Harmony patch that intercepts when a user disconnects from the server
[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
internal static class OnUserDisconnectedPatch {
  // Connection reasons that should be ignored (user never fully connected)
  // These represent failed connection attempts rather than actual disconnections
  private static readonly HashSet<ConnectionStatusChangeReason> IgnoreReasons = [
    ConnectionStatusChangeReason.IncorrectPassword,
    ConnectionStatusChangeReason.ServerFull,
    ConnectionStatusChangeReason.Unknown,
    ConnectionStatusChangeReason.AuthenticationError,
    ConnectionStatusChangeReason.AuthSessionCancelled
  ];

  private static void Prefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId, ConnectionStatusChangeReason connectionStatusReason) {
    // Ensure game systems are initialized before processing disconnection
    if (!GameSystems.Initialized) GameSystems.Initialize();

    // Skip processing for ignored connection reasons
    // These aren't actual disconnections from gameplay
    if (IgnoreReasons.Contains(connectionStatusReason)) return;

    try {
      // Get the user index from the connection mapping
      if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out var index)) {
        Log.Warning("Failed to get user index for disconnection.");
        return;
      }

      // Get the client data for the disconnecting user
      var client = __instance._ApprovedUsersLookup[index];
      if (client == null || client.UserEntity.Equals(Entity.Null)) {
        Log.Warning("Failed to get user entity during disconnect.");
        return;
      }

      PlayerData playerData = null;

      // Update player cache with disconnection status
      // The 'true' parameter indicates this is a disconnection update
      playerData = PlayerService.SetPlayerCache(client.UserEntity, true);
      if (playerData == null) {
        Log.Warning("Failed to set player cache for disconnected user.");
        return;
      }

      // Fire the general user disconnected event (only if there are listeners)
      if (EventManager.GetSubscriberCount(PlayerEvents.PlayerLeft) > 0)
        EventManager.Emit(PlayerEvents.PlayerLeft, playerData);

      // Fire specific events based on disconnection reason
      // This allows mods to handle kicks and bans differently
      if (connectionStatusReason == ConnectionStatusChangeReason.Kicked) {
        if (EventManager.GetSubscriberCount(PlayerEvents.PlayerKicked) > 0)
          EventManager.Emit(PlayerEvents.PlayerKicked, playerData);
      }

      if (connectionStatusReason == ConnectionStatusChangeReason.Banned) {
        if (EventManager.GetSubscriberCount(PlayerEvents.PlayerBanned) > 0)
          EventManager.Emit(PlayerEvents.PlayerBanned, playerData);
      }
    } catch (Exception e) {
      // Log disconnection processing errors without crashing
      Log.Error($"An error occurred while disconnecting player: {e.Message}");
    }
  }
}