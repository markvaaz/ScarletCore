using System.Globalization;

namespace ScarletCore.Interface.Builders;

/// <summary>A drop shadow rendered behind text characters.</summary>
public readonly struct UITextShadow {
  internal readonly string Raw;

  /// <summary>Creates a drop shadow.</summary>
  /// <param name="color">Shadow color (usually dark, semi-transparent).</param>
  /// <param name="offsetX">Horizontal offset in pixels. Positive = right.</param>
  /// <param name="offsetY">Vertical offset in pixels. Positive = down.</param>
  public UITextShadow(UIColor color, float offsetX = 2f, float offsetY = 2f) =>
    Raw = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}", (string)color, offsetX, offsetY);
}
