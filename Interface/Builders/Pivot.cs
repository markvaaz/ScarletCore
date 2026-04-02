namespace ScarletCore.Interface.Builders;

/// <summary>
/// 9-point internal origin (pivot) of an element.
/// Determines which point of the element itself is placed at the anchor position.
/// For example, <see cref="MiddleCenter"/> centers the element on its anchor point,
/// while <see cref="TopLeft"/> (the default) places the element's top-left corner there.
/// </summary>
public enum Pivot {
  /// <summary>Pivot at the element's top-left corner (default).</summary>
  TopLeft,
  /// <summary>Pivot at the top edge, horizontally centered.</summary>
  TopCenter,
  /// <summary>Pivot at the element's top-right corner.</summary>
  TopRight,
  /// <summary>Pivot at the left edge, vertically centered.</summary>
  MiddleLeft,
  /// <summary>Pivot at the element's center.</summary>
  MiddleCenter,
  /// <summary>Pivot at the right edge, vertically centered.</summary>
  MiddleRight,
  /// <summary>Pivot at the element's bottom-left corner.</summary>
  BottomLeft,
  /// <summary>Pivot at the bottom edge, horizontally centered.</summary>
  BottomCenter,
  /// <summary>Pivot at the element's bottom-right corner.</summary>
  BottomRight,
}
