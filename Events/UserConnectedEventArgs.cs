using System;
using ProjectM;
using ScarletCore.Data;

namespace ScarletCore.Events;

/// <summary>
/// Event arguments for when a user connects to the server
/// </summary>
/// <remarks>
/// Initializes a new instance of UserConnectedEventArgs
/// </remarks>
/// <param name="player">The PlayerData of the connected user</param>
public class UserConnectedEventArgs(PlayerData player) : EventArgs {
  /// <summary>
  /// The PlayerData of the connected user
  /// </summary>
  public PlayerData Player { get; } = player;
}
