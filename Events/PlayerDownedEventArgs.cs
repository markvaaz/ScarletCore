using System;
using System.Collections.Generic;
using Unity.Entities;
using ProjectM;

namespace ScarletCore.Events;

/// <summary>
/// Event arguments for vampire/player downed events
/// </summary>
/// <remarks>
/// Initializes a new instance of PlayerDownedEventArgs
/// </remarks>
/// <param name="downedEntities">List of entities that were downed</param>
/// <param name="instance">The VampireDownedServerEventSystem instance that triggered this event</param>
public class PlayerDownedEventArgs(List<Entity> downedEntities) : EventArgs {
  /// <summary>
  /// List of all entities that were downed
  /// </summary>
  public List<Entity> DownedEntities { get; } = downedEntities ?? [];

  /// <summary>
  /// Number of entities that were downed
  /// </summary>
  public int DownedCount => DownedEntities.Count;
}
