namespace ScarletCore.Interface.Builders;

/// <summary>
/// Per-side spacing (padding or margin).
/// Use <c>Spacing.All(v)</c> or cast a <c>float</c> to apply the same value to all sides.
/// </summary>
public readonly struct Spacing {
  /// <summary>Top spacing.</summary>
  public readonly float Top;
  /// <summary>Right spacing.</summary>
  public readonly float Right;
  /// <summary>Bottom spacing.</summary>
  public readonly float Bottom;
  /// <summary>Left spacing.</summary>
  public readonly float Left;
  /// <summary>Creates spacing with explicit per-side values.</summary>
  public Spacing(float top, float right, float bottom, float left) { Top = top; Right = right; Bottom = bottom; Left = left; }
  /// <summary>Creates spacing with symmetric vertical and horizontal values.</summary>
  public Spacing(float vertical, float horizontal) : this(vertical, horizontal, vertical, horizontal) { }
  /// <summary>Creates equal spacing on all four sides.</summary>
  public static Spacing All(float v) => new(v, v, v, v);
  /// <summary>Left and right equal <paramref name="h"/>; top and bottom are 0.</summary>
  public static Spacing Horizontal(float h) => new(0f, h, 0f, h);
  /// <summary>Top and bottom equal <paramref name="v"/>; left and right are 0.</summary>
  public static Spacing Vertical(float v) => new(v, 0f, v, 0f);
  /// <summary>Left/right equal <paramref name="h"/>, top/bottom equal <paramref name="v"/>.</summary>
  public static Spacing XY(float h, float v) => new(v, h, v, h);
  /// <summary>Implicitly converts a float to uniform spacing on all sides.</summary>
  public static implicit operator Spacing(float v) => All(v);
}
