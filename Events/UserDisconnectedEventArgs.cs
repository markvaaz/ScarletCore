using System;
using Stunlock.Network;
using ScarletCore.Data;
using ProjectM;

namespace ScarletCore.Events;

/// <summary>
/// Event arguments for when a user disconnects from the server
/// </summary>
/// <remarks>
/// Initializes a new instance of UserDisconnectedEventArgs
/// </remarks>
/// <param name="player">The PlayerData of the disconnected user</param>
/// <param name="disconnectionReason">The reason for disconnection</param>
public class UserDisconnectedEventArgs(PlayerData player, ConnectionStatusChangeReason disconnectionReason) : EventArgs {
  /// <summary>
  /// The PlayerData of the disconnected user
  /// </summary>
  public PlayerData Player { get; } = player;

  /// <summary>
  /// The reason for the disconnection
  /// </summary>
  public ConnectionStatusChangeReason DisconnectionReason { get; } = disconnectionReason;
}
