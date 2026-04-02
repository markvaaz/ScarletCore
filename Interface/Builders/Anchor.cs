namespace ScarletCore.Interface.Builders;

/// <summary>
/// 9-point origin used to calculate element position.
/// Defines which point of the parent the element is anchored to.
/// </summary>
public enum Anchor {
  /// <summary>Top-left corner of the parent.</summary>
  TopLeft,
  /// <summary>Top edge, horizontally centered.</summary>
  TopCenter,
  /// <summary>Top-right corner of the parent.</summary>
  TopRight,
  /// <summary>Left edge, vertically centered.</summary>
  MiddleLeft,
  /// <summary>Center of the parent.</summary>
  MiddleCenter,
  /// <summary>Right edge, vertically centered.</summary>
  MiddleRight,
  /// <summary>Bottom-left corner of the parent.</summary>
  BottomLeft,
  /// <summary>Bottom edge, horizontally centered.</summary>
  BottomCenter,
  /// <summary>Bottom-right corner of the parent.</summary>
  BottomRight,
}
