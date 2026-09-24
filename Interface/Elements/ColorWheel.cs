using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A color picker: a saturation/brightness square with a vertical hue bar beside it, plus an
/// optional alpha bar, preview swatch and editable hex field — all kept in sync client-side.
/// <para>The selected color never travels on its own. Like an <see cref="Input"/>, it is read by
/// other elements through <c>{Id}</c> token replacement — e.g. a Button with
/// <c>Command = ".setcolor {myColor}"</c>. The token resolves to <c>#RRGGBB</c>, or
/// <c>#RRGGBBAA</c> when alpha is below 255; both forms parse with <see cref="UIColor.Hex"/>.</para>
/// <para>The user's pick survives window resends: the client only re-applies <see cref="Value"/>
/// when the server actually changes it.</para>
/// </summary>
public class ColorWheel : UIElement {
  /// <summary>Unique identifier used to reference the selected color in other elements' commands.</summary>
  public string Id { get; set; }
  /// <summary>Initial color. Default: opaque red.</summary>
  public UIColor? Value { get; set; }
  /// <summary>Show the alpha (transparency) bar. When false the color is always opaque. Default: true.</summary>
  public bool ShowAlpha { get; set; } = true;
  /// <summary>Show the editable hex field. Typing a valid hex moves the picker, and vice versa. Default: true.</summary>
  public bool ShowHex { get; set; } = true;
  /// <summary>Show the preview swatch of the current color. Default: true.</summary>
  public bool ShowPreview { get; set; } = true;
  /// <summary>Width of the vertical hue bar in pixels. Default: 18.</summary>
  public float HueBarWidth { get; set; } = 18f;
  /// <summary>Spacing between the picker's inner parts in pixels. Default: 8.</summary>
  public float Gap { get; set; } = 8f;
  /// <summary>Background color of the hex field.</summary>
  public UIColor? InputBackground { get; set; }
  /// <summary>Text color of the hex field. Default: white.</summary>
  public UIColor? TextColor { get; set; }
  /// <summary>Font size of the hex field in pixels. 0 = client default (13).</summary>
  public float FontSize { get; set; }
}
