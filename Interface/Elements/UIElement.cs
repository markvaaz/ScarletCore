using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// Base class for all UI elements. Carries shared visual and positioning
/// properties inherited by every concrete element type.
/// </summary>
public abstract class UIElement {
  // ─── Dimensions ──────────────────────────────────────────────────────────

  /// <summary>Element width (pixels or percentage string). Default: auto.</summary>
  public Dimension Width { get; set; }
  /// <summary>Element height (pixels or percentage string). Default: auto.</summary>
  public Dimension Height { get; set; }

  // ─── Background ──────────────────────────────────────────────────────────

  /// <summary>
  /// Unified background: solid color, gradient, image, sprite, or combinations.
  /// <code>Background = UIBackground.FromColor(UIColor.Hex("#1a1a2e"))</code>
  /// <code>Background = UIColor.Red  // implicit conversion</code>
  /// </summary>
  public UIBackground? Background { get; set; }

  // ─── Border ──────────────────────────────────────────────────────────────

  /// <summary>Optional border (color, thickness, radius).</summary>
  public Border? Border { get; set; }

  // ─── Spacing ─────────────────────────────────────────────────────────────

  /// <summary>Inner spacing between border and content.</summary>
  public Spacing? Padding { get; set; }
  /// <summary>Outer spacing around the element.</summary>
  public Spacing? Margin { get; set; }

  // ─── Transform ───────────────────────────────────────────────────────────

  /// <summary>Rotation in degrees. 0 = no rotation.</summary>
  public float Rotation { get; set; }

  // ─── Shadow ──────────────────────────────────────────────────────────────

  /// <summary>Optional box shadow rendered behind the element.</summary>
  public BoxShadow? BoxShadow { get; set; }

  // ─── Standalone positioning ──────────────────────────────────────────────

  /// <summary>
  /// Anchor point on the parent. When set, the element is positioned
  /// absolutely within its container instead of following flow layout.
  /// </summary>
  public Anchor? Anchor { get; set; }

  /// <summary>
  /// Position offset (X, Y) and optional ZIndex.
  /// Used together with <see cref="Anchor"/> for standalone positioning,
  /// or alone for ZIndex-only control.
  /// </summary>
  public Position? Position { get; set; }

  /// <summary>
  /// Pivot point of the element itself (determines which point is placed at
  /// the anchor position). Defaults to TopLeft when omitted.
  /// </summary>
  public Pivot? Pivot { get; set; }

  // ─── Interaction ─────────────────────────────────────────────────────────

  /// <summary>
  /// Optional stable identifier for this element within its window.
  /// When set, allows targeted partial updates via <see cref="Window.SendUpdate"/>
  /// without re-sending the full window. Must be unique within a window.
  /// IDs are scoped per window — the same ID can be reused across different windows.
  /// </summary>
  public string ElemId { get; set; }

  /// <summary>
  /// ID of a window to show as a tooltip when the mouse hovers this element.
  /// </summary>
  public string Tooltip { get; set; }
}

/// <summary>
/// Interface for elements that display text and support shared text styling.
/// </summary>
public interface ITextElement {
  /// <summary>Text color. Default: white.</summary>
  UIColor? TextColor { get; set; }
  /// <summary>Font size in pixels. 0 = inherit from parent/window default.</summary>
  float FontSize { get; set; }
  /// <summary>TMP font asset name. null = game default.</summary>
  string Font { get; set; }
  /// <summary>Vertex-color gradient applied to text characters.</summary>
  UITextGradient? TextGradient { get; set; }
  /// <summary>Drop shadow rendered behind text characters.</summary>
  UITextShadow? TextShadow { get; set; }
  /// <summary>Outline around each text glyph.</summary>
  UITextOutline? TextOutline { get; set; }
}
