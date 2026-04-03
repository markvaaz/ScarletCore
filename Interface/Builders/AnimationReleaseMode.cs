namespace ScarletCore.Interface.Builders;

/// <summary>
/// Defines what happens to the animation when its interaction trigger is released
/// (e.g. pointer leaves on Hover, button released on Pressed).
/// </summary>
public enum AnimationReleaseMode {
  /// <summary>Element becomes invisible (alpha 0) but remains interactive — hover and click still work.</summary>
  Hide,
  /// <summary>Freezes on the current frame.</summary>
  Pause,
  /// <summary>Jumps back to frame 0 and freezes.</summary>
  Reset,
  /// <summary>Element becomes invisible (alpha 0) and non-interactive.</summary>
  Disable,
}
