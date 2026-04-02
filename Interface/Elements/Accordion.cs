using System.Collections;
using System.Collections.Generic;
using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A collapsible accordion section. The header is always visible; clicking it
/// toggles the content area client-side with no server round-trip.
/// Supports collection initializer: <c>new Accordion { new Text {...} }</c>.
/// </summary>
public class Accordion : UIElement, IEnumerable<UIElement> {
  /// <summary>Header title text.</summary>
  public string Title { get; set; }
  /// <summary>Whether the accordion starts expanded.</summary>
  public bool Expanded { get; set; }
  /// <summary>Header height in pixels. Default: 32.</summary>
  public float HeaderHeight { get; set; } = 32f;
  /// <summary>Header background.</summary>
  public UIBackground? HeaderBackground { get; set; }
  /// <summary>Header title text color.</summary>
  public UIColor? HeaderTextColor { get; set; }
  /// <summary>Chevron icon color.</summary>
  public UIColor? ChevronColor { get; set; }
  /// <summary>Custom chevron icon string (SVG token or inline icon). null = default.</summary>
  public string ChevronIcon { get; set; }
  /// <summary>Whether to show the expand/collapse chevron. Default: true.</summary>
  public bool ShowChevron { get; set; } = true;
  /// <summary>Content area background.</summary>
  public UIBackground? ContentBackground { get; set; }
  /// <summary>Gap between child elements inside the content area.</summary>
  public float Gap { get; set; }
  /// <summary>Font size for the header title. 0 = inherit.</summary>
  public float FontSize { get; set; }

  /// <summary>Child elements inside the accordion content area.</summary>
  public List<UIElement> Children { get; set; } = [];

  /// <summary>Adds a child element (enables collection initializer syntax).</summary>
  public void Add(UIElement child) => Children.Add(child);
  public IEnumerator<UIElement> GetEnumerator() => Children.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
