namespace ScarletCore.Interface.Builders;

/// <summary>A vertex-color gradient applied directly to text characters (not the background).</summary>
public readonly struct UITextGradient {
  readonly string _raw;
  UITextGradient(string v) => _raw = v;
  internal bool HasValue => !string.IsNullOrEmpty(_raw);
  internal string Raw => _raw;

  /// <summary>Gradient from <paramref name="top"/> to <paramref name="bottom"/> across the glyph height.</summary>
  public static UITextGradient Vertical(UIColor top, UIColor bottom) =>
    new($"{(string)top}|{(string)top}|{(string)bottom}|{(string)bottom}");

  /// <summary>Gradient from <paramref name="left"/> to <paramref name="right"/> across the glyph width.</summary>
  public static UITextGradient Horizontal(UIColor left, UIColor right) =>
    new($"{(string)left}|{(string)right}|{(string)left}|{(string)right}");

  /// <summary>Full four-corner gradient: <paramref name="topLeft"/>, <paramref name="topRight"/>, <paramref name="bottomLeft"/>, <paramref name="bottomRight"/>.</summary>
  public static UITextGradient FourCorner(UIColor topLeft, UIColor topRight, UIColor bottomLeft, UIColor bottomRight) =>
    new($"{(string)topLeft}|{(string)topRight}|{(string)bottomLeft}|{(string)bottomRight}");

  /// <summary>Linear gradient across text glyphs. <paramref name="angle"/> in degrees: 0=left→right, 90=bottom→top. Requires 2 or more colors.</summary>
  public static UITextGradient Linear(float angle, params UIColor[] colors) {
    if (colors == null || colors.Length < 2) throw new System.ArgumentException("UITextGradient.Linear requires at least 2 colors.", nameof(colors));
    var stops = string.Join("|", System.Array.ConvertAll(colors, c => (string)c));
    return new($"linear|{angle.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{stops}");
  }

  /// <summary>Radial gradient across text glyphs with center at (<paramref name="cx"/>, <paramref name="cy"/>) in 0–1 coords. Requires 2 or more colors.</summary>
  public static UITextGradient Radial(float cx, float cy, params UIColor[] colors) {
    if (colors == null || colors.Length < 2) throw new System.ArgumentException("UITextGradient.Radial requires at least 2 colors.", nameof(colors));
    var stops = string.Join("|", System.Array.ConvertAll(colors, c => (string)c));
    return new($"radial|{cx.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{cy.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{stops}");
  }
}
