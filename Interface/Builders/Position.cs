namespace ScarletCore.Interface.Builders;

/// <summary>
/// Spatial position with X/Y offsets and an optional Z-order (canvas sorting).
/// X and Y support pixels or percentage strings via <see cref="Dimension"/>.
/// </summary>
public readonly struct Position {
  /// <summary>Horizontal offset (pixels or percentage).</summary>
  public readonly Dimension X;
  /// <summary>Vertical offset (pixels or percentage).</summary>
  public readonly Dimension Y;
  /// <summary>Canvas sorting order. Higher values render on top. 0 = default.</summary>
  public readonly int ZIndex;

  /// <summary>Creates a position with X and Y offsets.</summary>
  public Position(Dimension x, Dimension y) { X = x; Y = y; ZIndex = 0; }
  /// <summary>Creates a position with X, Y offsets and a Z-order.</summary>
  public Position(Dimension x, Dimension y, int zIndex) { X = x; Y = y; ZIndex = zIndex; }
  /// <summary>Creates a position with only a Z-order (X and Y default to unset).</summary>
  public Position(int zIndex) { X = default; Y = default; ZIndex = zIndex; }

  internal bool HasValue => X.HasValue || Y.HasValue || ZIndex != 0;
}
