namespace ScarletCore.Interface.Builders;

/// <summary>
/// Style of connection lines drawn between branch nodes in a ScrollCanvas.
/// </summary>
public enum LineStyle
{
  /// <summary>Straight diagonal line from parent to child.</summary>
  Direct,
  /// <summary>L-shaped staircase: vertical down to midpoint, horizontal, then vertical to child.</summary>
  Staircase,
}
