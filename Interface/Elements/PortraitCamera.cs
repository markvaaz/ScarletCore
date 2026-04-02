using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// Displays the local character's camera feed.
/// Background properties here are specific to the 3D camera background quad
/// and intentionally separate from the base <see cref="UIElement.Background"/>.
/// </summary>
public class PortraitCamera : UIElement {
  /// <summary>Camera field of view in degrees. Default: 60.</summary>
  public float FieldOfView { get; set; } = 60f;
  /// <summary>Orbit angle in degrees around the anchor bone. Default: 0.</summary>
  public float OrbitAngle { get; set; }
  /// <summary>Distance multiplier from the anchor bone. 1.0 = default. Default: 1.</summary>
  public float Distance { get; set; } = 1f;
  /// <summary>Character bone the camera orbits. Default: Head_JNT.</summary>
  public string AnchorBone { get; set; }

  // ─── 3D background quad ─────────────────────────────────────────────────
  /// <summary>URL of the background texture rendered behind the character.</summary>
  public string BackgroundUrl { get; set; }
  /// <summary>Solid tint applied to the background quad.</summary>
  public UIColor? BackgroundColor { get; set; }
  /// <summary>World-space size (metres) of the background quad. Default: 1.6.</summary>
  public float BackgroundSize { get; set; } = 1.6f;
  /// <summary>UV offset X for panning within the background texture.</summary>
  public float BackgroundOffsetX { get; set; }
  /// <summary>UV offset Y for panning within the background texture.</summary>
  public float BackgroundOffsetY { get; set; }
  /// <summary>UV scale X for zooming within the background texture. Default: 1.</summary>
  public float BackgroundScaleX { get; set; } = 1f;
  /// <summary>UV scale Y for zooming within the background texture. Default: 1.</summary>
  public float BackgroundScaleY { get; set; } = 1f;
}
