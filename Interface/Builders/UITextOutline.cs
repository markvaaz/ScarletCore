using System.Globalization;

namespace ScarletCore.Interface.Builders;

/// <summary>An outline drawn around each text glyph using TMP's SDF rendering.</summary>
public readonly struct UITextOutline {
  internal readonly string Raw;

  /// <summary>Creates a text outline.</summary>
  /// <param name="color">Outline color.</param>
  /// <param name="width">Outline thickness in pixels (typically 1–4).</param>
  public UITextOutline(UIColor color, float width = 1f) =>
    Raw = string.Format(CultureInfo.InvariantCulture, "{0}|{1}", (string)color, width);
}
