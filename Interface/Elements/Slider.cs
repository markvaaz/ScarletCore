using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A draggable value between <see cref="Min"/> and <see cref="Max"/>: a track, the filled part up to
/// the value, and a handle. Drag it, click anywhere on it, or turn the mouse wheel over it.
/// <para>Like an <see cref="Input"/>, the value is client-side state read by other elements through
/// <c>{Id}</c> token replacement — e.g. a Button with <c>Command = ".volume {vol}"</c>. It resolves
/// to the number in invariant culture ("42", "0.5").</para>
/// <para><see cref="Command"/> runs once when the user lets go (or per wheel notch) — send server
/// commands there. <see cref="ChangeCommand"/> runs on every change while dragging and only runs
/// client-side actions (<c>close-window</c> and registered client commands such as <c>paint:</c>);
/// chat commands in it are ignored so a drag can never flood the server.</para>
/// <para>The user's value survives window resends: the client only re-applies <see cref="Value"/>
/// when the server actually changes it.</para>
/// </summary>
public class Slider : UIElement {
  /// <summary>Unique identifier used to reference the value in other elements' commands ({Id}).</summary>
  public string Id { get; set; }
  /// <summary>Lowest value. Default: 0.</summary>
  public float Min { get; set; }
  /// <summary>Highest value. Default: 100.</summary>
  public float Max { get; set; } = 100f;
  /// <summary>Values snap to multiples of this (from Min). 0 = continuous. Default: 1.</summary>
  public float Step { get; set; } = 1f;
  /// <summary>Initial value.</summary>
  public float Value { get; set; }
  /// <summary>Bottom-to-top instead of left-to-right.</summary>
  public bool Vertical { get; set; }
  /// <summary>Colour of the empty track. Default: dark grey.</summary>
  public UIColor? TrackColor { get; set; }
  /// <summary>Colour of the filled part. Default: light grey.</summary>
  public UIColor? FillColor { get; set; }
  /// <summary>Colour of the handle. Default: white.</summary>
  public UIColor? HandleColor { get; set; }
  /// <summary>Handle diameter in pixels. 0 = no handle. Default: 14.</summary>
  public float HandleSize { get; set; } = 14f;
  /// <summary>Track thickness in pixels (across the slider). Default: 6.</summary>
  public float TrackThickness { get; set; } = 6f;
  /// <summary>Command run when the user releases the slider (or turns the wheel over it). Tokens resolve.</summary>
  public string Command { get; set; }
  /// <summary>Client-side command run on every change while dragging (chat commands are ignored). Tokens resolve.</summary>
  public string ChangeCommand { get; set; }
}
