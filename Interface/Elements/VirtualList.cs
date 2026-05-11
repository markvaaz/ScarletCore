using System.Collections.Generic;
using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A virtualized scrollable list. Only items currently visible in the scroll viewport
/// have GameObjects — the client creates and destroys them on demand as the user scrolls,
/// keeping the live GO count bounded regardless of total item count.
///
/// Each entry in <see cref="Items"/> is an array of <see cref="UIElement"/> that make
/// up a single row in the list. All rows must have the same height (<see cref="ItemHeight"/>).
/// </summary>
public class VirtualList : UIElement {
  /// <summary>Fixed pixel height for every item row. Default: 60.</summary>
  public float ItemHeight { get; set; } = 60f;

  /// <summary>
  /// When <c>true</c> (default), the list uses sticky auto-scroll: newly appended items
  /// automatically scroll to the bottom only when the user is already near the bottom
  /// (within one item's height). If the user scrolled up to read older content the
  /// position is preserved. Set to <c>false</c> to disable auto-scroll entirely.
  /// </summary>
  public bool AutoScrollToBottom { get; set; } = true;

  /// <summary>
  /// All items. Each entry is an array of UIElements that are laid out inside a single
  /// item slot. Use <see cref="Add"/> to append items using collection-initializer syntax.
  /// </summary>
  public List<UIElement[]> Items { get; } = new();

  /// <summary>Appends one item (a row of elements) to the list.</summary>
  public void Add(params UIElement[] children) => Items.Add(children);
}
