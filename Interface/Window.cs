using System.Collections;
using System.Collections.Generic;
using ScarletCore.Services;
using ScarletCore.Interface.Builders;
using ScarletCore.Interface.Elements;
using ScarletCore.Interface.Models;

namespace ScarletCore.Interface;

/// <summary>
/// A top-level UI window. Construct with a player/plugin/id, populate via object
/// initializer or <see cref="Add"/>, then call <see cref="Send"/> to deliver.
/// <code>
/// new Window(player, "myplugin", "shop") {
///   Width = 400, Height = 300,
///   Background = UIBackground.FromColor(UIColor.Hex("#1a1a2e")),
///   Border = new Border(UIColor.Hex("#333"), 1, 8),
///   Padding = Spacing.All(10),
///   Gap = 8,
///   Anchor = Anchor.MiddleCenter,
///   Draggable = true,
///   Children = {
///     new Row {
///       Height = 40,
///       Children = {
///         new Text { Content = "Shop", FontSize = 18 },
///         new CloseButton()
///       }
///     }
///   }
/// }.Send();
/// </code>
/// </summary>
public class Window : IEnumerable<UIElement> {
  readonly string _plugin;
  readonly PlayerData _player; // null = broadcast to all
  readonly string _id;

  /// <summary>Creates a window targeting a specific player.</summary>
  public Window(PlayerData player, string plugin, string id) {
    _player = player;
    _plugin = plugin;
    _id = id;
  }

  /// <summary>Creates a window that broadcasts to all connected players.</summary>
  public Window(string plugin, string id) {
    _player = null;
    _plugin = plugin;
    _id = id;
  }

  // ─── Dimensions ──────────────────────────────────────────────────────────

  /// <summary>Window width (pixels or percentage string). Default: auto.</summary>
  public Dimension Width { get; set; }
  /// <summary>Window height (pixels or percentage string). Default: auto.</summary>
  public Dimension Height { get; set; }

  // ─── Background ──────────────────────────────────────────────────────────

  /// <summary>Window background (color, gradient, image, sprite, or combinations).</summary>
  public UIBackground? Background { get; set; }

  // ─── Border ──────────────────────────────────────────────────────────────

  /// <summary>Optional border (color, thickness, radius).</summary>
  public Border? Border { get; set; }

  // ─── Spacing ─────────────────────────────────────────────────────────────

  /// <summary>Inner spacing between the border and child content.</summary>
  public Spacing? Padding { get; set; }

  // ─── Layout ──────────────────────────────────────────────────────────────

  /// <summary>Flow direction: Vertical (default, top-to-bottom) or Horizontal (left-to-right).</summary>
  public FlowDirection Direction { get; set; } = FlowDirection.Vertical;
  /// <summary>Gap in pixels between children inside the window.</summary>
  public float Gap { get; set; }
  /// <summary>How overflowing child content is handled.</summary>
  public OverflowMode Overflow { get; set; }

  // ─── Scrollbar ───────────────────────────────────────────────────────────

  /// <summary>Scrollbar thumb color.</summary>
  public UIColor? ScrollbarColor { get; set; }
  /// <summary>Scrollbar track color.</summary>
  public UIColor? ScrollbarBackgroundColor { get; set; }
  /// <summary>Scrollbar width in pixels. Default: 8.</summary>
  public float ScrollbarWidth { get; set; } = 8f;

  // ─── Positioning ─────────────────────────────────────────────────────────

  /// <summary>
  /// Anchor point on the screen used to position the window.
  /// Default: MiddleCenter.
  /// </summary>
  public Anchor Anchor { get; set; } = Anchor.MiddleCenter;

  /// <summary>Position offset (X, Y) and optional ZIndex for canvas sorting.</summary>
  public Position? Position { get; set; }

  /// <summary>Internal pivot of the window.</summary>
  public Pivot? Pivot { get; set; }

  /// <summary>Rotation in degrees applied to the window. 0 = no rotation.</summary>
  public float Rotation { get; set; }

  // ─── Shadow ──────────────────────────────────────────────────────────────

  /// <summary>Optional box shadow rendered behind the window.</summary>
  public BoxShadow? BoxShadow { get; set; }

  // ─── Behaviour ───────────────────────────────────────────────────────────

  /// <summary>Whether the player can drag the window. Default: true.</summary>
  public bool Draggable { get; set; } = true;
  /// <summary>If true, renders the window background transparent.</summary>
  public bool Transparent { get; set; }
  /// <summary>Optional native UI parent identifier to attach to.</summary>
  public string NativeParent { get; set; }
  /// <summary>If true, window is hidden when any in-game menu opens. Default: true.</summary>
  public bool HideOnMenuOpen { get; set; } = true;

  // ─── Animations ──────────────────────────────────────────────────────────

  /// <summary>Animation played when the window opens. Default: None.</summary>
  public WindowAnimation OpenAnimation { get; set; }
  /// <summary>Animation played when the window closes. Default: None.</summary>
  public WindowAnimation CloseAnimation { get; set; }
  /// <summary>Duration in seconds for open/close animations. Default: 0.2s.</summary>
  public float AnimationDuration { get; set; } = 0.2f;

  // ─── Auto-Close ──────────────────────────────────────────────────────────

  /// <summary>
  /// Seconds of inactivity before the window auto-closes. 0 = disabled.
  /// Any player interaction (button click, input change, dropdown select, accordion toggle)
  /// resets the timer back to zero.
  /// </summary>
  public float AutoClose { get; set; }

  // ─── Close Keybind ───────────────────────────────────────────────────────

  /// <summary>
  /// Key that closes this window when pressed on the client.
  /// <c>null</c> = no close keybind.
  /// The close keybind takes priority over any global command keybind on the same key.
  /// Reset when the window is closed.
  /// </summary>
  public InputKey? CloseKey { get; set; }

  // ─── Children ────────────────────────────────────────────────────────────

  /// <summary>Child elements (Rows, standalone elements, Accordions).</summary>
  public List<UIElement> Children { get; set; } = [];

  /// <summary>Adds a child element (enables collection initializer syntax).</summary>
  public void Add(UIElement child) => Children.Add(child);

  // ─── Custom Texture (9-slice frame) ──────────────────────────────────────

  /// <summary>9-slice frame configuration. Set via <see cref="SetCustomTexture"/>.</summary>
  internal CustomTextureData CustomTexture { get; private set; }

  /// <summary>
  /// Sets a 9-piece tiled image frame on the window (4 corners, 4 borders, background).
  /// </summary>
  public Window SetCustomTexture(
      string topLeftCorner = null, string topRightCorner = null,
      string bottomLeftCorner = null, string bottomRightCorner = null,
      string topBorder = null, string bottomBorder = null,
      string leftBorder = null, string rightBorder = null,
      string background = null, bool backgroundRepeat = false,
      int cornerSize = 32, int frameExpand = 0) {
    CustomTexture = new CustomTextureData {
      TopLeftCorner = topLeftCorner, TopRightCorner = topRightCorner,
      BottomLeftCorner = bottomLeftCorner, BottomRightCorner = bottomRightCorner,
      TopBorder = topBorder, BottomBorder = bottomBorder,
      LeftBorder = leftBorder, RightBorder = rightBorder,
      Background = background, BackgroundRepeat = backgroundRepeat,
      CornerSize = cornerSize, FrameExpand = frameExpand,
    };
    return this;
  }

  // ─── Send ────────────────────────────────────────────────────────────────

  /// <summary>
  /// Serializes the window and all children into packets and sends them
  /// to the target player(s). The <paramref name="action"/> is sent last.
  /// </summary>
  public void Send(WindowAction action = WindowAction.Open) {
    var packets = ElementSerializer.Serialize(this, _plugin, _id);

    // Action packet last (short type tokens)
    if (action != WindowAction.None) {
      string typeToken = action switch {
        WindowAction.Open => "OP",
        WindowAction.Close => "CL",
        WindowAction.Clear => "CR",
        WindowAction.Reset => "RS",
        _ => action.ToString(),
      };
      packets.Add(new ScarletPacket {
        Type = typeToken,
        Plugin = _plugin,
        Window = _id,
        Data = [],
      });
    }

    foreach (var packet in packets) {
      if (_player != null)
        PacketManager.SendPacket(_player, packet);
      else
        PacketManager.SendPacketToAll(packet);
    }
  }

  // ─── IEnumerable ─────────────────────────────────────────────────────────

  /// <summary>
  /// Sends a targeted update for a single element already present in this window on the client.
  /// Only a player-targeted window (<see cref="Window(PlayerData,string,string)"/>) can send
  /// element updates — broadcast windows are silently ignored.
  /// <para>
  /// The client will locate the element by <paramref name="elemId"/> and DiffPatch only the
  /// changed properties. If the window is not open or the element ID is not found, the packet
  /// is silently discarded by the client with no side effects.
  /// </para>
  /// </summary>
  /// <param name="elemId">The <see cref="UIElement.ElemId"/> used when the window was first sent.</param>
  /// <param name="elem">A new element instance carrying the updated property values.</param>
  public void SendUpdate(string elemId, UIElement elem) {
    if (_player == null) return;
    foreach (var packet in ElementSerializer.SerializeElement(_plugin, _id, elem, elemId))
      PacketManager.SendPacket(_player, packet);
  }

  /// <summary>
  /// Removes a single element by ID from this window on the client.
  /// Only works on player-targeted windows — broadcast windows are silently ignored.
  /// <para>
  /// The client destroys the element's GameObject and clears all associated tracking state.
  /// If the window is not open or the element ID is not found, the packet is discarded silently.
  /// </para>
  /// </summary>
  /// <param name="elemId">The <see cref="UIElement.ElemId"/> of the element to remove.</param>
  public void SendDelete(string elemId) {
    if (_player == null) return;
    PacketManager.SendPacket(_player, ElementSerializer.SerializeDeleteElement(_plugin, _id, elemId));
  }

  /// <summary>
  /// Static shorthand: sends a targeted update for a single element without constructing a
  /// <see cref="Window"/> instance. Useful when only the player, plugin, window ID, and element
  /// are known at the call site.
  /// <para>
  /// The client will locate the element by <paramref name="elemId"/> and DiffPatch only the
  /// changed properties. If the element type differs from the original, the existing element is
  /// destroyed and recreated in-place with the new type.
  /// If the window is not open or the element ID is not found, the packet is silently discarded.
  /// </para>
  /// </summary>
  /// <param name="player">Target player. Null is ignored silently.</param>
  /// <param name="plugin">Plugin identifier used when the window was created.</param>
  /// <param name="windowId">ID of the window that contains the element.</param>
  /// <param name="elemId">The <see cref="UIElement.ElemId"/> used when the window was first sent.</param>
  /// <param name="elem">A new element instance carrying the updated property values.</param>
  public static void SendUpdate(PlayerData player, string plugin, string windowId, string elemId, UIElement elem) {
    if (player == null) return;
    foreach (var packet in ElementSerializer.SerializeElement(plugin, windowId, elem, elemId))
      PacketManager.SendPacket(player, packet);
  }

  /// <summary>
  /// Static shorthand: removes a single element by ID from a window without constructing a
  /// <see cref="Window"/> instance.
  /// <para>
  /// The client destroys the element's GameObject and clears all associated tracking state.
  /// If the window is not open or the element ID is not found, the packet is silently discarded.
  /// </para>
  /// </summary>
  /// <param name="player">Target player. Null is ignored silently.</param>
  /// <param name="plugin">Plugin identifier used when the window was created.</param>
  /// <param name="windowId">ID of the window that contains the element.</param>
  /// <param name="elemId">The <see cref="UIElement.ElemId"/> of the element to remove.</param>
  public static void SendDelete(PlayerData player, string plugin, string windowId, string elemId) {
    if (player == null) return;
    PacketManager.SendPacket(player, ElementSerializer.SerializeDeleteElement(plugin, windowId, elemId));
  }

  public IEnumerator<UIElement> GetEnumerator() => Children.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>Data holder for 9-slice custom texture frame.</summary>
internal class CustomTextureData {
  public string TopLeftCorner, TopRightCorner, BottomLeftCorner, BottomRightCorner;
  public string TopBorder, BottomBorder, LeftBorder, RightBorder;
  public string Background;
  public bool BackgroundRepeat;
  public int CornerSize = 32;
  public int FrameExpand;
}
