using System.Globalization;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// A CSS-like dimension value: pixels (<c>10</c>, <c>10f</c>) or percentage (<c>"50%"</c>).
/// Used for widths, heights, and positional offsets.
/// </summary>
public readonly struct Dimension {
  readonly string _raw;
  Dimension(string v) => _raw = v;
  internal bool HasValue => !string.IsNullOrEmpty(_raw);
  internal string Raw => _raw;
  /// <summary>Converts an integer to a pixel-based <see cref="Dimension"/>.</summary>
  public static implicit operator Dimension(int px) => new(px.ToString(CultureInfo.InvariantCulture));
  /// <summary>Converts a float to a pixel-based <see cref="Dimension"/>.</summary>
  public static implicit operator Dimension(float px) => new(px.ToString(CultureInfo.InvariantCulture));
  /// <summary>Converts a string (e.g. <c>"50%"</c>) to a <see cref="Dimension"/>.</summary>
  public static implicit operator Dimension(string s) => new(s);
  /// <inheritdoc/>
  public override string ToString() => _raw ?? string.Empty;
}
