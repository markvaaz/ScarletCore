namespace ScarletCore.Interface.Builders;

/// <summary>Defines how the animation cycles through its frames.</summary>
public enum AnimationLoopType {
  /// <summary>Cycles frames 0 → N → 0 → N… (wraps back to start).</summary>
  Loop,
  /// <summary>Ping-pongs frames 0 → N → 0 → N… (reverses direction).</summary>
  Bounce,
}
