using System.Globalization;

namespace ScarletCore.Interface.Builders;

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
