using System;
using ScarletCore.Data;
using ProjectM.Network;

namespace ScarletCore.Events;

/// <summary>
/// Event arguments for when a user is banned from the server
/// </summary>
/// <remarks>
/// Initializes a new instance of UserBannedEventArgs
/// </remarks>
/// <param name="player">The PlayerData of the banned user</param>
/// <param name="serverBootstrapSystem">The ServerBootstrapSystem instance</param>
public class UserBannedEventArgs(PlayerData player, ProjectM.ServerBootstrapSystem serverBootstrapSystem) : EventArgs {
  /// <summary>
  /// The PlayerData of the banned user
  /// </summary>
  public PlayerData Player { get; } = player;

  /// <summary>
  /// The ServerBootstrapSystem instance
  /// </summary>
  public ProjectM.ServerBootstrapSystem ServerBootstrapSystem { get; } = serverBootstrapSystem;
}
