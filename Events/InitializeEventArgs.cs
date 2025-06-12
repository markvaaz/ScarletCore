using System;
using ProjectM;
using ProjectM.Network;

namespace ScarletCore.Events;

/// <summary>
/// Event arguments for when ScarletCore initializes
/// </summary>
public class InitializeEventArgs : EventArgs {
  /// <summary>
  /// The timestamp when initialization occurred
  /// </summary>
  public DateTime InitializationTime { get; }

  /// <summary>
  /// Initializes a new instance of InitializeEventArgs
  /// </summary>
  public InitializeEventArgs() {
    InitializationTime = DateTime.Now;
  }
}
