using System;
using System.Collections.Generic;
using ProjectM.Gameplay.WarEvents;
using ProjectM.Shared.WarEvents;

namespace ScarletCore.Events;

/// <summary>
/// Event arguments for war events that started
/// </summary>
/// <remarks>
/// Initializes a new instance of WarEventStartedEventArgs
/// </remarks>
/// <param name="warEvents">List of war events that started</param>
/// <param name="instance">The WarEventSystem instance that triggered this event</param>
public class WarEventStartedEventArgs(List<WarEvent> warEvents, WarEventSystem instance = null) : EventArgs {
  /// <summary>
  /// List of all war events that started
  /// </summary>
  public List<WarEvent> WarEvents { get; } = warEvents ?? [];

  /// <summary>
  /// Number of war events in this batch
  /// </summary>
  public int WarEventCount => WarEvents.Count;

  /// <summary>
  /// The instance of the WarEventSystem that triggered this event
  /// </summary>
  public WarEventSystem Instance { get; set; } = instance;
}