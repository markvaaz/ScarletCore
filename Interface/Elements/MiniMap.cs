using System.Collections;
using System.Collections.Generic;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A minimap: one zoomed rectangle of the game's world map, drawn from the native
/// <c>WorldMapRevealMaterial</c>. It is also a positioning parent — any child carrying
/// <see cref="UIElement.WorldX"/>/<see cref="UIElement.WorldZ"/> is placed at the matching world
/// spot on the map after layout, so a <see cref="Button"/> becomes a clickable map marker with its
/// full toolkit (command, hover art, tooltip), an <see cref="Image"/> becomes a static icon, etc.
/// <para>
/// The visible area is the world rectangle
/// [<see cref="MinWorldX"/>,<see cref="MinWorldZ"/>]..[<see cref="MaxWorldX"/>,<see cref="MaxWorldZ"/>]
/// in world units (+Z = north). Leave the corners unset to show the whole map. Markers that fall
/// outside the rectangle are clipped away (see <see cref="Clip"/>).
/// </para>
/// A solid <see cref="UIElement.Background"/> color shows through unrevealed (fogged) areas.
/// Supports collection initializer: <c>new MiniMap { new Button {...} }</c>.
/// </summary>
public class MiniMap : UIElement, IEnumerable<UIElement> {
  /// <summary>Marker elements, each positioned by its <see cref="UIElement.WorldX"/>/<see cref="UIElement.WorldZ"/>.</summary>
  public List<UIElement> Children { get; set; } = [];

  /// <summary>West edge of the visible world rectangle (world X). Unset = full map.</summary>
  public float? MinWorldX { get; set; }
  /// <summary>South edge of the visible world rectangle (world Z). Unset = full map.</summary>
  public float? MinWorldZ { get; set; }
  /// <summary>East edge of the visible world rectangle (world X). Unset = full map.</summary>
  public float? MaxWorldX { get; set; }
  /// <summary>North edge of the visible world rectangle (world Z). Unset = full map.</summary>
  public float? MaxWorldZ { get; set; }

  /// <summary>Clip markers to the map rectangle. Default: true.</summary>
  public bool Clip { get; set; } = true;

  /// <summary>Adds a marker element (enables collection initializer syntax).</summary>
  public void Add(UIElement child) => Children.Add(child);
  public IEnumerator<UIElement> GetEnumerator() => Children.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
