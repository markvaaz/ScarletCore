using System.Collections;
using System.Collections.Generic;
using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A horizontal row container. Children are laid out left-to-right.
/// Supports collection initializer: <c>new Row { new Text {...}, new Button {...} }</c>.
/// </summary>
public class Row : UIElement, IEnumerable<UIElement> {
  /// <summary>Child elements inside this row.</summary>
  public List<UIElement> Children { get; set; } = [];

  /// <summary>Flow direction: Horizontal (default, left-to-right) or Vertical (top-to-bottom).</summary>
  public FlowDirection Direction { get; set; } = FlowDirection.Horizontal;
  /// <summary>Gap between child elements in pixels.</summary>
  public float Gap { get; set; }
  /// <summary>Horizontal distribution of children.</summary>
  public JustifyContent JustifyContent { get; set; }
  /// <summary>Vertical alignment of children.</summary>
  public AlignItems AlignItems { get; set; }
  /// <summary>How overflowing content is handled.</summary>
  public OverflowMode Overflow { get; set; }
  /// <summary>Scrollbar thumb color for overflowed rows.</summary>
  public UIColor? ScrollbarColor { get; set; }
  /// <summary>Scrollbar track color for overflowed rows.</summary>
  public UIColor? ScrollbarBackgroundColor { get; set; }
  /// <summary>Scrollbar width in pixels. Default: 8.</summary>
  public float ScrollbarWidth { get; set; } = 8f;

  /// <summary>Adds a child element (enables collection initializer syntax).</summary>
  public void Add(UIElement child) => Children.Add(child);
  public IEnumerator<UIElement> GetEnumerator() => Children.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
