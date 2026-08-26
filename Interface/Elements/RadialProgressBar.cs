namespace ScarletCore.Interface.Elements;

/// <summary>
/// A circular (radial) progress bar. Inherits every <see cref="ProgressBar"/> setting —
/// value range, colors/gradients, value animation, health-bar mode and overlay label.
/// Image/sprite fills, BoxShadow and BorderRadius are not supported on the radial variant;
/// Border renders as a thin ring on the outer edge.
/// </summary>
public class RadialProgressBar : ProgressBar {
  /// <summary>Circle radius in pixels — the element is a (2 × Radius) square. Default: 40.</summary>
  public float Radius { get; set; } = 40f;
  /// <summary>Ring thickness in pixels. 0 (or ≥ Radius) renders a full disc (pie style). Default: 8.</summary>
  public float Thickness { get; set; } = 8f;
  /// <summary>Angle in degrees where the fill starts, clockwise from the top. Default: 0.</summary>
  public float StartAngle { get; set; }
  /// <summary>Fill direction around the circle. Default: true (clockwise).</summary>
  public bool Clockwise { get; set; } = true;
}
