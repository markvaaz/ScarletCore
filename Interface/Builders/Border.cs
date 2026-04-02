namespace ScarletCore.Interface.Builders;

/// <summary>Border definition: color, thickness (pixels), and corner radius (pixels).</summary>
public readonly struct Border {
  /// <summary>Border color.</summary>
  public readonly UIColor Color;
  /// <summary>Border thickness in pixels.</summary>
  public readonly float Width;
  /// <summary>Corner radius in pixels (default 6).</summary>
  public readonly float Radius;
  /// <summary>Creates a border with the given color, thickness, and optional corner radius.</summary>
  public Border(UIColor color, float width, float radius = 0f) { Color = color; Width = width; Radius = radius; }
}
