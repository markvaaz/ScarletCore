using System.Globalization;

namespace ScarletCore.Interface.Builders;

// ─── Color ────────────────────────────────────────────────────────────────────

/// <summary>An RGBA color with components in the 0–1 range.</summary>
public readonly struct UIColor {
  readonly float _r, _g, _b, _a;
  UIColor(float r, float g, float b, float a) { _r = r; _g = g; _b = b; _a = a; }

  /// <summary>Creates a fully opaque color from RGB components (0–1).</summary>
  public static UIColor RGB(float r, float g, float b) => new(r, g, b, 1f);
  /// <summary>Creates a color from RGBA components (0–1).</summary>
  public static UIColor RGBA(float r, float g, float b, float a) => new(r, g, b, a);
  /// <summary>Opaque white (1, 1, 1, 1).</summary>
  public static UIColor White => new(1f, 1f, 1f, 1f);
  /// <summary>Opaque black (0, 0, 0, 1).</summary>
  public static UIColor Black => new(0f, 0f, 0f, 1f);
  /// <summary>Opaque red (1, 0, 0, 1).</summary>
  public static UIColor Red => new(1f, 0f, 0f, 1f);
  /// <summary>Opaque green (0, 1, 0, 1).</summary>
  public static UIColor Green => new(0f, 1f, 0f, 1f);
  /// <summary>Opaque blue (0, 0, 1, 1).</summary>
  public static UIColor Blue => new(0f, 0f, 1f, 1f);
  /// <summary>Fully transparent (0, 0, 0, 0).</summary>
  public static UIColor Transparent => new(0f, 0f, 0f, 0f);

  /// <summary>Creates a color from a hex string: <c>#RGB</c>, <c>#RRGGBB</c>, or <c>#RRGGBBAA</c>.</summary>
  public static UIColor Hex(string hex) {
    if (string.IsNullOrEmpty(hex)) return new(1f, 1f, 1f, 1f);
    hex = hex.TrimStart('#');
    if (hex.Length == 3) hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
    if (!uint.TryParse(hex, NumberStyles.HexNumber, null, out uint v)) return new(1f, 1f, 1f, 1f);
    return hex.Length >= 8
      ? new(((v >> 24) & 0xFF) / 255f, ((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f)
      : new(((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f, 1f);
  }

  internal string Serialize() => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", _r, _g, _b, _a);
  /// <summary>Implicitly converts a <see cref="UIColor"/> to its serialized <c>"r,g,b,a"</c> string form.</summary>
  public static implicit operator string(UIColor c) => c.Serialize();
}

// ─── Position ──────────────────────────────────────────────────────────────────────

/// <summary>A CSS-like dimension value: pixels (<c>10</c>, <c>"10px"</c>) or percentage (<c>"50%"</c>).</summary>
public readonly struct Position {
  readonly string _raw;
  Position(string v) => _raw = v;
  internal bool HasValue => !string.IsNullOrEmpty(_raw);
  internal string Raw => _raw;
  /// <summary>Converts an integer to a pixel-based <see cref="Position"/>.</summary>
  public static implicit operator Position(int px) => new(px.ToString(CultureInfo.InvariantCulture));
  /// <summary>Converts a float to a pixel-based <see cref="Position"/>.</summary>
  public static implicit operator Position(float px) => new(px.ToString(CultureInfo.InvariantCulture));
  /// <summary>Converts a string (e.g. <c>"50%"</c>) to a <see cref="Position"/>.</summary>
  public static implicit operator Position(string s) => new(s);
  /// <inheritdoc/>
  public override string ToString() => _raw ?? string.Empty;
}

// ─── Gradient ─────────────────────────────────────────────────────────────────

/// <summary>A gradient descriptor for background fields.</summary>
public readonly struct UIGradient {
  readonly string _raw;
  UIGradient(string v) => _raw = v;
  internal bool HasValue => !string.IsNullOrEmpty(_raw);
  internal string Raw => _raw;

  /// <summary>Linear gradient. <paramref name="angle"/> in degrees: 0=left→right, 90=bottom→top.</summary>
  public static UIGradient Linear(float angle, UIColor c1, UIColor c2, UIColor c3) =>
    new($"linear|{angle.ToString(CultureInfo.InvariantCulture)}|{(string)c1}|{(string)c2}|{(string)c3}");

  /// <summary>Radial gradient with center at (<paramref name="cx"/>, <paramref name="cy"/>) in 0–1 coords.</summary>
  public static UIGradient Radial(float cx, float cy, UIColor c1, UIColor c2, UIColor c3) =>
    new($"radial|{cx.ToString(CultureInfo.InvariantCulture)}|{cy.ToString(CultureInfo.InvariantCulture)}|{(string)c1}|{(string)c2}|{(string)c3}");
}

// ─── Icon helper ──────────────────────────────────────────────────────────────

/// <summary>Utility for embedding item icons inside text strings sent to ScarletInterface.</summary>
public static class UIIcons {
  /// <summary>
  /// Returns an inline icon token for the item identified by <paramref name="guidHash"/>.
  /// Embed this inside any <c>text</c> parameter:
  /// <code>$"Craft: 5x {UIIcons.Icon(-1234567890)}"</code>
  /// The client resolves the GUID to the item's sprite via <c>ManagedItemData.Icon</c>
  /// and renders it as an inline <c>Image</c> component.
  /// </summary>
  /// <param name="guidHash">The integer hash of the <c>PrefabGUID</c> (i.e. the raw int32 GUID value).</param>
  public static string Icon(int guidHash) => $"{{icon:{guidHash}}}";

  static readonly System.Globalization.CultureInfo _ic = System.Globalization.CultureInfo.InvariantCulture;

  /// <summary>
  /// Returns an inline icon token with an explicit pixel size.
  /// </summary>
  /// <param name="guidHash">PrefabGUID hash.</param>
  /// <param name="size">Icon size in pixels (width = height). 0 = inherit from font size.</param>
  public static string Icon(int guidHash, float size) =>
    size <= 0f
      ? $"{{icon:{guidHash}}}"
      : $"{{icon:{guidHash}:{size.ToString(_ic)}}}";

  /// <summary>
  /// Returns an inline icon token with explicit pixel size and horizontal spacing.
  /// </summary>
  /// <param name="guidHash">PrefabGUID hash.</param>
  /// <param name="size">Icon size in pixels. 0 = inherit from font size.</param>
  /// <param name="spacing">Gap in pixels added on each side of the icon. 0 = default (3 px).</param>
  public static string Icon(int guidHash, float size, float spacing) =>
    $"{{icon:{guidHash}:{size.ToString(_ic)}:{spacing.ToString(_ic)}}}";
}

// ─── Layout types ─────────────────────────────────────────────────────────────

/// <summary>
/// 9-point origin used to calculate element position.
/// Defines which point of the parent the element is anchored to.
/// </summary>
public enum Anchor {
  /// <summary>Top-left corner of the parent.</summary>
  TopLeft,
  /// <summary>Top edge, horizontally centered.</summary>
  TopCenter,
  /// <summary>Top-right corner of the parent.</summary>
  TopRight,
  /// <summary>Left edge, vertically centered.</summary>
  MiddleLeft,
  /// <summary>Center of the parent.</summary>
  MiddleCenter,
  /// <summary>Right edge, vertically centered.</summary>
  MiddleRight,
  /// <summary>Bottom-left corner of the parent.</summary>
  BottomLeft,
  /// <summary>Bottom edge, horizontally centered.</summary>
  BottomCenter,
  /// <summary>Bottom-right corner of the parent.</summary>
  BottomRight,
}

/// <summary>
/// 9-point internal origin (pivot) of an element.
/// Determines which point of the element itself is placed at the anchor position.
/// For example, <see cref="MiddleCenter"/> centers the element on its anchor point,
/// while <see cref="TopLeft"/> (the default) places the element's top-left corner there.
/// </summary>
public enum Pivot {
  /// <summary>Pivot at the element's top-left corner (default).</summary>
  TopLeft,
  /// <summary>Pivot at the top edge, horizontally centered.</summary>
  TopCenter,
  /// <summary>Pivot at the element's top-right corner.</summary>
  TopRight,
  /// <summary>Pivot at the left edge, vertically centered.</summary>
  MiddleLeft,
  /// <summary>Pivot at the element's center.</summary>
  MiddleCenter,
  /// <summary>Pivot at the right edge, vertically centered.</summary>
  MiddleRight,
  /// <summary>Pivot at the element's bottom-left corner.</summary>
  BottomLeft,
  /// <summary>Pivot at the bottom edge, horizontally centered.</summary>
  BottomCenter,
  /// <summary>Pivot at the element's bottom-right corner.</summary>
  BottomRight,
}

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

/// <summary>
/// Controls whether padding is included within the specified width/height (<see cref="BorderBox"/>)
/// or added on top of it, growing the element (<see cref="ContentBox"/>).
/// </summary>
public enum BoxSizing {
  /// <summary>Width/height include padding and border — element stays at the declared size.</summary>
  BorderBox,
  /// <summary>Width/height describe the content area only — padding and border grow the element.</summary>
  ContentBox
}

/// <summary>Controls how children that overflow the container bounds are handled.</summary>
public enum OverflowMode {
  /// <summary>Content is not clipped and overflows the bounds.</summary>
  Visible,
  /// <summary>Content is clipped at the container bounds.</summary>
  Hidden,
  /// <summary>Scrollable on both axes.</summary>
  Scroll,
  /// <summary>Scrollable horizontally only.</summary>
  ScrollX,
  /// <summary>Scrollable vertically only (VLG-based list).</summary>
  ScrollY,
  /// <summary>Scroll axes are activated automatically when content overflows.</summary>
  Auto,
}

/// <summary>Horizontal distribution of children inside a Row.</summary>
public enum JustifyContent {
  /// <summary>Pack children toward the start of the row.</summary>
  Start,
  /// <summary>Center children within the row.</summary>
  Center,
  /// <summary>Pack children toward the end of the row.</summary>
  End,
  /// <summary>Distribute children with equal space between them.</summary>
  SpaceBetween,
  /// <summary>Distribute children with equal space around each child.</summary>
  SpaceAround,
  /// <summary>Distribute children with equal space between and at the edges.</summary>
  SpaceEvenly
}

/// <summary>Vertical alignment of children inside a Row.</summary>
public enum AlignItems {
  /// <summary>Align children to the top of the row.</summary>
  Start,
  /// <summary>Vertically center children within the row.</summary>
  Center,
  /// <summary>Align children to the bottom of the row.</summary>
  End
}

/// <summary>Horizontal alignment of text inside a text element.</summary>
public enum TextAlignment {
  /// <summary>Align text to the left edge.</summary>
  Left,
  /// <summary>Center text horizontally.</summary>
  Center,
  /// <summary>Align text to the right edge.</summary>
  Right
}

/// <summary>Controls how an image fills the area defined by its width and height.</summary>
public enum ImageFit {
  /// <summary>Stretches to fill the entire area. Aspect ratio is not preserved.</summary>
  Stretch,
  /// <summary>Scales uniformly to fit entirely within the area (letterbox). Aspect ratio preserved.</summary>
  Fit,
  /// <summary>Scales uniformly to cover the entire area, cropping the excess (cover). Aspect ratio preserved.</summary>
  Fill,
}

/// <summary>
/// Action to execute on the window when <see cref="WindowBuilder.Send"/> is called.
/// The action is always applied <em>after</em> all element packets are sent,
/// regardless of where in the fluent chain the method was called.
/// </summary>
public enum WindowAction {
  /// <summary>No lifecycle action — only element packets are sent.</summary>
  None,
  /// <summary>Opens the window and makes it visible to the player.</summary>
  Open,
  /// <summary>Hides the window without destroying its content.</summary>
  Close,
  /// <summary>Removes all elements from the window (keeps the window alive).</summary>
  Clear,
  /// <summary>Destroys and fully recreates the window on the next Open.</summary>
  Reset,
}
