using System.Collections;
using System.Collections.Generic;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A node in a <see cref="ScrollCanvas"/> branch tree.
/// <para>
/// <c>Children</c> is a mixed list: <see cref="Branch"/> items form the visual tree
/// (connected by lines, placed on the next level); all other <see cref="UIElement"/> items
/// are rendered as content inside this node's bounds, laid out vertically.
/// </para>
/// Layout is computed bottom-up — the deepest subtree determines each parent's required width.
/// </summary>
/// <example>
/// <code>
/// new Branch("skill_1") {
///   Background = UIBackground.SolidColor(UIColor.DarkGray),
///   Children = {
///     new Text { Value = "Skill Name" },
///     new Button { Label = "Unlock" },
///     new Branch("skill_1a") {
///       Children = { new Text { Value = "Sub-skill" } }
///     },
///   }
/// }
/// </code>
/// </example>
public class Branch : UIElement, IEnumerable<UIElement>
{
  /// <summary>
  /// Mixed children list. <see cref="Branch"/> items are tree children (placed below, connected
  /// by lines). Any other <see cref="UIElement"/> (Button, Text, Container, Image…) is content
  /// rendered vertically inside this node.
  /// Collection initializer syntax: <c>new Branch { elem1, branch1, elem2 }</c>.
  /// </summary>
  public List<UIElement> Children { get; set; } = [];

  public Branch() { }

  /// <summary>Creates a branch with an explicit element ID.</summary>
  public Branch(string elemId) { ElemId = elemId; }

  /// <summary>Adds any child element (enables collection initializer syntax).</summary>
  public void Add(UIElement child) => Children.Add(child);
  public IEnumerator<UIElement> GetEnumerator() => Children.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
