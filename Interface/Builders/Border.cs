namespace ScarletCore.Interface.Builders;

/// <summary>Border definition: color, thickness (pixels), and corner radius (pixels).</summary>
public readonly struct Border {
  /// <summary>Border color.</summary>
  public readonly UIColor Color;
  /// <summary>Border thickness in pixels.</summary>
  public readonly float Width;
  /// <summary>Corner radius in pixels (default 6).</summary>
  public readonly float Radius;

  /// <summary>
  /// Glow color of the border halo, independent of <see cref="Color"/>; the alpha channel is the
  /// peak intensity. Null = no glow.
  /// </summary>
  public readonly UIColor? GlowColor;
  /// <summary>Glow spread in pixels, fading outward AND inward (over the element content) from
  /// the border stroke, following the corner radius. 0 = no glow.</summary>
  public readonly float GlowWidth;
  /// <summary>Glow fade exponent: 1 linear, 2 soft (default), higher = tighter core.</summary>
  public readonly float GlowFalloff;

  /// <summary>Creates a border with the given color, thickness, and optional corner radius.</summary>
  public Border(UIColor color, float width, float radius = 0f)
    : this(color, width, radius, null, 0f) { }

  /// <summary>Creates a border with a glow halo hugging the border shape (radius included).</summary>
  public Border(UIColor color, float width, float radius, UIColor? glowColor, float glowWidth,
      float glowFalloff = 2f) {
    Color = color; Width = width; Radius = radius;
    GlowColor = glowColor; GlowWidth = glowWidth; GlowFalloff = glowFalloff;
  }

  /// <summary>Copy of this border with a glow halo (fluent: <c>new Border(c, 2, 12).WithGlow(g, 8)</c>).</summary>
  public Border WithGlow(UIColor glowColor, float glowWidth = 8f, float glowFalloff = 2f)
    => new(Color, Width, Radius, glowColor, glowWidth, glowFalloff);
}
