namespace ScarletCore.Interface.Builders;

/// <summary>
/// Controls the main-axis flow direction of children in a container element.
/// </summary>
public enum FlowDirection {
  /// <summary>
  /// Children are laid out left-to-right.
  /// Default for <see cref="ScarletCore.Interface.Elements.Row"/>.
  /// </summary>
  Horizontal,
  /// <summary>
  /// Children are laid out top-to-bottom.
  /// Default for <see cref="ScarletCore.Interface.Window"/>.
  /// </summary>
  Vertical,
}
