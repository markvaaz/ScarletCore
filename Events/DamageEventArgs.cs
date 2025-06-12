using System;
using System.Collections.Generic;
using Unity.Entities;
using ProjectM;
using ProjectM.Gameplay.Systems;

namespace ScarletCore.Events;

/// <summary>
/// Event arguments for damage events containing multiple damage instances that occurred in a single frame
/// </summary>
/// <remarks>
/// Initializes a new instance of DamageEventArgs
/// </remarks>
/// <param name="damageInstances">List of damage information</param>
public class DamageEventArgs(List<DamageInfo> damageInstances) : EventArgs {
  /// <summary>
  /// List of all damage instances that occurred
  /// </summary>
  public List<DamageInfo> DamageInstances { get; } = damageInstances ?? [];

  /// <summary>
  /// Number of damage instances in this batch
  /// </summary>
  public int DamageCount => DamageInstances.Count;
}

/// <summary>
/// Information about a single damage instance
/// </summary>
/// <remarks>
/// Initializes a new instance of DamageInfo
/// </remarks>
/// <param name="attacker">The entity that dealt the damage</param>
/// <param name="target">The entity that received the damage</param>
public class DamageInfo(Entity attacker, Entity target) {
  /// <summary>
  /// The entity that dealt the damage (attacker)
  /// </summary>
  public Entity Attacker { get; } = attacker;

  /// <summary>
  /// The entity that received the damage (target)
  /// </summary>
  public Entity Target { get; } = target;
}
