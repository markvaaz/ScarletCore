using System;
using System.Collections.Generic;
using ProjectM;
using Unity.Entities;

namespace ScarletCore.Events;

/// <remarks>
/// Initializes a new instance of DeathEventArgs
/// </remarks>
/// <param name="deaths">List of death information</param>
public class DeathEventArgs(List<DeathInfo> deaths) : EventArgs {
  /// <summary>
  /// List of all deaths that occurred
  /// </summary>
  public List<DeathInfo> Deaths { get; } = deaths ?? [];

  /// <summary>
  /// Number of deaths in this batch
  /// </summary>
  public int DeathCount => Deaths.Count;
}

/// <remarks>
/// Initializes a new instance of DeathInfo
/// </remarks>
/// <param name="died">The entity that died</param>
/// <param name="killer">The entity that caused the death</param>
/// <param name="source">The source of the damage/death</param>
/// <param name="deathTime">When this death occurred</param>
public class DeathInfo(Entity died, Entity killer, Entity source) {
  /// <summary>
  /// The entity that died
  /// </summary>
  public Entity Died { get; } = died;

  /// <summary>
  /// The entity that caused the death (killer)
  /// </summary>
  public Entity Killer { get; } = killer;

  /// <summary>
  /// The source of the damage/death
  /// </summary>
  public Entity Source { get; } = source;
}
