using System.Collections;
using System.Collections.Generic;
using ScarletCore.Interface.Builders;

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

  /// <summary>
  /// Shared-node support: ElemIds of OTHER branch nodes in the same <see cref="ScrollCanvas"/> that
  /// also connect INTO this node — extra incoming edges beyond the single tree parent. Use for a DAG /
  /// skill-web where a node converges from several branches (e.g. an elite node gated behind two
  /// different sub-branches). The node is still POSITIONED by its tree parent (its nesting); each id
  /// here only draws an additional connector line from that source node down to this one, styled with
  /// <see cref="ScrollCanvas.LinkColor"/> (falls back to <see cref="ScrollCanvas.LineColor"/>).
  /// The referenced ids must exist in the same canvas; unknown/hub ids are skipped.
  /// </summary>
  public List<string> LinkFrom { get; set; } = [];

  /// <summary>
  /// Per-link styled version of <see cref="LinkFrom"/>: each <see cref="BranchLink"/> is an extra
  /// incoming edge whose color / glow / draw-order can be set INDIVIDUALLY, instead of every incoming
  /// edge sharing the node-level <see cref="EdgeColor"/>/<see cref="GlowColor"/>. Use when different
  /// convergence edges must look different (e.g. a skill web where only the edges from already-unlocked
  /// prerequisites glow, while the ones from still-locked prerequisites stay dim). Coexists with
  /// <see cref="LinkFrom"/> — both feed the same set of edges; a link listed here with all overrides
  /// null renders exactly like a plain <see cref="LinkFrom"/> entry. Any field left null inherits the
  /// node-level value (which in turn inherits the canvas default). See <see cref="BranchLink"/>.
  /// </summary>
  public List<BranchLink> Links { get; set; } = [];

  /// <summary>
  /// Cor das arestas que ENTRAM neste nó — a linha vinda do parent de árvore e as arestas
  /// <see cref="LinkFrom"/> apontando pra ele — sobrescrevendo <see cref="ScrollCanvas.LineColor"/>/
  /// <see cref="ScrollCanvas.LinkColor"/>. Use pra colorir por estado (ex.: destacar o caminho já
  /// desbloqueado numa skill tree). Null = cor padrão do canvas.
  /// </summary>
  public UIColor? EdgeColor { get; set; }

  /// <summary>
  /// Draw order da aresta do tree-parent que entra neste nó — contraparte de <see cref="BranchLink.ZIndex"/>
  /// pras cruzadas. Maior = desenhada por cima. Serve pra garantir que uma aresta destacada (ex.: caminho
  /// desbloqueado) fique acima de uma apagada que compartilha o mesmo segmento. Conectores internos e
  /// arestas sem override ficam em 0. Default 0.
  /// </summary>
  public int EdgeZIndex { get; set; }

  /// <summary>
  /// Glow das arestas que ENTRAM neste nó (mesma convenção de <see cref="EdgeColor"/>: linha do
  /// parent de árvore + arestas <see cref="LinkFrom"/> + conectores internos do nó). Spread em px
  /// além da borda da linha. Use pra destacar por estado (ex.: caminho desbloqueado numa skill
  /// tree). Glows sobrepostos resolvem como intensidade máxima no client — nunca somam mais
  /// claro. Null = herda <see cref="ScrollCanvas.GlowWidth"/>.
  /// </summary>
  public float? GlowWidth { get; set; }

  /// <summary>
  /// Cor do glow das arestas que entram neste nó, independente da cor da linha; o alpha é a
  /// intensidade de pico. Null = herda <see cref="ScrollCanvas.GlowColor"/> (que por sua vez
  /// cai na cor da própria linha quando null).
  /// </summary>
  public UIColor? GlowColor { get; set; }

  /// <summary>
  /// Expoente do fade do glow deste nó: 1 linear, 2 suave, maior = núcleo mais concentrado.
  /// Null = herda <see cref="ScrollCanvas.GlowFalloff"/>.
  /// </summary>
  public float? GlowFalloff { get; set; }

  /// <summary>Vertical placement of the node's content (Start = top, Center, End = bottom).</summary>
  public JustifyContent JustifyContent { get; set; }

  /// <summary>Horizontal placement of the node's content (Start = left, Center, End = right).</summary>
  public AlignItems AlignItems { get; set; }

  /// <summary>Creates an empty branch.</summary>
  public Branch() { }

  /// <summary>Creates a branch with an explicit element ID.</summary>
  public Branch(string elemId) { ElemId = elemId; }

  /// <summary>Adds any child element (enables collection initializer syntax).</summary>
  public void Add(UIElement child) => Children.Add(child);
  /// <summary>Enumerates the child elements.</summary>
  public IEnumerator<UIElement> GetEnumerator() => Children.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// One styled incoming edge for <see cref="Branch.Links"/>: a source node id plus optional per-edge
/// overrides. Every override left null inherits the target node's edge value (which inherits the
/// canvas default), so a bare <c>new BranchLink(sourceId)</c> is identical to a plain
/// <see cref="Branch.LinkFrom"/> entry.
/// </summary>
public struct BranchLink {
  /// <summary>ElemId of the source branch node this edge comes FROM (same meaning as a <see cref="Branch.LinkFrom"/> entry).</summary>
  public string SourceId { get; set; }
  /// <summary>Edge line color. Null = inherit the target node's <see cref="Branch.EdgeColor"/> / canvas <see cref="ScrollCanvas.LinkColor"/>.</summary>
  public UIColor? Color { get; set; }
  /// <summary>Glow spread in px for this edge (0 = no glow). Null = inherit the node/canvas glow width.</summary>
  public float? GlowWidth { get; set; }
  /// <summary>Glow color for this edge. Null = inherit (falls back to the edge line color at 60% alpha).</summary>
  public UIColor? GlowColor { get; set; }
  /// <summary>Glow fade exponent for this edge. Null = inherit the node/canvas falloff.</summary>
  public float? GlowFalloff { get; set; }
  /// <summary>
  /// Draw order of this edge's core line relative to other edges: higher = drawn on top. Lets a link
  /// created later be forced above or below one drawn earlier when their lines overlap. Tree edges and
  /// intra-branch connectors sit at 0. Default 0.
  /// </summary>
  public int ZIndex { get; set; }

  /// <summary>Creates a link from <paramref name="sourceId"/> with no overrides (renders like a plain LinkFrom entry).</summary>
  public BranchLink(string sourceId) { SourceId = sourceId; }
}
