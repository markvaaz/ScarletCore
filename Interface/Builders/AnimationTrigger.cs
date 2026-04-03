namespace ScarletCore.Interface.Builders;

/// <summary>
/// Defines what interaction or event starts the animation. Multiple values can be
/// combined with the bitwise OR operator (e.g. <c>Always | Manual</c>).
/// </summary>
[System.Flags]
public enum AnimationTrigger {
  None = 0,
  /// <summary>Starts immediately when the element is created and runs indefinitely.</summary>
  Always = 1,
  /// <summary>Starts each time the containing window is opened.</summary>
  WindowOpen = 2,
  /// <summary>Plays while the pointer is hovering over the element.</summary>
  Hover = 4,
  /// <summary>Plays while the element is being pressed.</summary>
  Pressed = 8,
  /// <summary>Starts one playthrough on each click.</summary>
  Click = 16,
  /// <summary>Controlled manually — start and stop via server-side <c>Playing</c> property.</summary>
  Manual = 32,
}
