using System.Collections;
using System.Collections.Generic;
using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// An omnidirectional free-scroll canvas that renders a tree of <see cref="Branch"/> nodes.
/// The canvas can be panned in any direction by dragging with the mouse or using the scroll wheel.
/// No scrollbars are rendered. The layout is computed bottom-up: the deepest child determines the
/// space required by each parent, preventing overlap. Ideal for skill trees and node graphs.
/// </summary>
/// <example>
/// <code>
/// new ScrollCanvas {
///   Width = 600, Height = 400,
///   NodeWidth = 120, NodeHeight = 60,
///   ColumnGap = 24, RowGap = 48,
///   LineColor = UIColor.White,
///   Branches = {
///     new Branch("br_root") {
///       new Branch("br_child_a"),
///       new Branch("br_child_b"),
///     }
///   }
/// }
/// </code>
/// </example>
public class ScrollCanvas : UIElement, IEnumerable<Branch>
{
  /// <summary>Root branches of the tree. Each root may have its own child subtree.</summary>
  public List<Branch> Branches { get; set; } = [];

  /// <summary>
  /// The point in the canvas content that is visible when it first opens.
  /// <c>TopLeft</c> = top-left corner (default). <c>MiddleCenter</c> = center the view.
  /// </summary>
  public Builders.Anchor ViewOrigin { get; set; } = Builders.Anchor.TopLeft;

  /// <summary>Default node width in pixels. Individual branches may override via <see cref="UIElement.Width"/>.</summary>
  public float NodeWidth { get; set; } = 120f;

  /// <summary>Default node height in pixels. Individual branches may override via <see cref="UIElement.Height"/>.</summary>
  public float NodeHeight { get; set; } = 60f;

  /// <summary>Horizontal gap in pixels between sibling subtrees.</summary>
  public float ColumnGap { get; set; } = 20f;

  /// <summary>Vertical gap in pixels between parent and child branches.</summary>
  public float RowGap { get; set; } = 40f;

  /// <summary>Color of the lines connecting parent and child nodes. No lines are drawn when null.</summary>
  public UIColor? LineColor { get; set; }

  /// <summary>
  /// Color of the extra shared-node edges declared via <see cref="Branch.LinkFrom"/> (cross-branch /
  /// convergence links). Lets those stand out from the normal tree edges. Falls back to
  /// <see cref="LineColor"/> when null.
  /// </summary>
  public UIColor? LinkColor { get; set; }

  /// <summary>Thickness of connection lines in pixels.</summary>
  public float LineWidth { get; set; } = 2f;

  /// <summary>
  /// Glow spread around connection lines, in pixels beyond the line edge. 0 = no glow (default).
  /// Overlapping/joining glows resolve as max intensity client-side — they never sum brighter.
  /// </summary>
  public float GlowWidth { get; set; }

  /// <summary>
  /// Glow color, fully independent of <see cref="LineColor"/>; the alpha channel is the peak
  /// glow strength. Null = each line glows in its own color (including <see cref="LinkColor"/>
  /// and per-node <see cref="Branch.EdgeColor"/>) at 60% of its alpha.
  /// </summary>
  public UIColor? GlowColor { get; set; }

  /// <summary>Glow fade exponent: 1 = linear, 2 = soft (default), higher = tighter core.</summary>
  public float GlowFalloff { get; set; } = 2f;

  /// <summary>
  /// Opt-in x-assignment mode for the whole canvas: every node is centred on the connections that
  /// enter it — its tree parent plus its <see cref="Branch.LinkFrom"/> sources — level by level from
  /// the top, with sibling overlaps resolved so <see cref="ColumnGap"/> is always respected. Use for
  /// DAG-style trees where convergence nodes should sit at the common centre of their links instead
  /// of directly under their single tree parent. Off by default (classic compact tree layout).
  /// </summary>
  public bool CenterOnParents { get; set; }

  /// <summary>Style of the connection lines between branch nodes. Defaults to Direct (straight line).</summary>
  public Builders.LineStyle LineStyle { get; set; } = Builders.LineStyle.Direct;

  /// <summary>
  /// Inner padding that pushes tree content away from the canvas border.
  /// Accepts a single float (uniform) or <see cref="Spacing"/> per-side.
  /// <code>
  /// Padding = 20              // 20px all sides
  /// Padding = new Spacing(20, 10)   // 20px vertical, 10px horizontal
  /// </code>
  /// </summary>
  public new Spacing? Padding { get => base.Padding; set => base.Padding = value; }

  /// <summary>Adds a root branch (enables collection initializer syntax).</summary>
  public void Add(Branch branch) => Branches.Add(branch);
  /// <summary>Enumerates the root branches.</summary>
  public IEnumerator<Branch> GetEnumerator() => Branches.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
