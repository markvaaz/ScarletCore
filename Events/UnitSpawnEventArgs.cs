using System;
using System.Collections.Generic;
using Unity.Entities;
using ProjectM;
using Stunlock.Core;
using ScarletCore.Patches;

namespace ScarletCore.Events;

/// <summary>
/// Event arguments for unit spawn events containing multiple spawn instances that occurred in a single frame
/// </summary>
/// <param name="entities">List of entities that were spawned</param>
public class UnitSpawnEventArgs(List<Entity> entities) : EventArgs {
  /// <summary>
  /// List of entities that were spawned in this batch
  /// </summary>
  public List<Entity> Entities { get; } = entities ?? [];

  /// <summary>
  /// Gets the count of entities spawned in this event
  /// </summary>
  public int EntityCount => Entities.Count;
}