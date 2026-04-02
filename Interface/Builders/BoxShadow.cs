using System.Globalization;

namespace ScarletCore.Interface.Builders;

/// <summary>A box shadow drawn behind a UI element, respecting its corner radius.</summary>
public readonly struct BoxShadow {
  internal readonly string Raw;
  /// <summary>Creates a box shadow.</summary>
  /// <param name="color">Shadow color (usually dark, semi-transparent).</param>
  /// <param name="offsetX">Horizontal offset in pixels. Positive = right.</param>
  /// <param name="offsetY">Vertical offset in pixels. Positive = down.</param>
  /// <param name="blur">Feather radius in pixels. 0 = hard edge.</param>
  /// <param name="spread">Expands (+) or contracts (-) the shadow before blurring. 0 = same size as element.</param>
  public BoxShadow(UIColor color, float offsetX = 0f, float offsetY = 4f, float blur = 8f, float spread = 0f) =>
    Raw = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}", (string)color, offsetX, offsetY, blur, spread);
}
