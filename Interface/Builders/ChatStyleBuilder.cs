using System.Collections.Generic;
using System.Globalization;
using ScarletCore.Interface.Models;
using ScarletCore.Services;

namespace ScarletCore.Interface.Builders;

/// <summary>Which part of the native chat window a <see cref="ChatStyleBuilder"/> restyles.</summary>
public enum ChatTarget {
  /// <summary>The message area (the rectangle behind the chat lines).</summary>
  Container,
  /// <summary>The text-input box.</summary>
  Input,
  /// <summary>The ScarletInterface filter-tab bar (Global / Local / custom channels …).</summary>
  Tabs,
}

/// <summary>
/// Sends a visual skin for the ScarletInterface client's native chat — a background and border drawn
/// behind one of the chat areas, using the same <see cref="UIBackground"/> / <see cref="Border"/>
/// vocabulary as any ScarletUI element (solid colour, gradient, image, sprite, animated material).
/// For <see cref="ChatTarget.Tabs"/> you can additionally set the filter-button colours.
///
/// A no-op on clients without ScarletInterface. The skin persists on the client until changed or
/// <see cref="Clear"/>ed; resend it on <c>PlayerEvents.InterfaceAuth</c> so it survives a relog.
/// </summary>
/// <example>
/// InterfaceManager.ChatStyle(player, "MyMod", ChatTarget.Container)
///   .Background(UIBackground.FromColor(UIColor.Hex("#0d1420")).WithMaterial("Stunlock/UI/UIFlowmap"))
///   .Border(new Border(UIColor.Hex("#c04040"), 2f, 8f))
///   .Send();
///
/// InterfaceManager.ChatStyle(player, "MyMod", ChatTarget.Tabs)
///   .Background(UIColor.Hex("#101018"))
///   .TabColors(idle: UIColor.Hex("#00000070"), selected: UIColor.Hex("#c04040"))
///   .Send();
/// </example>
public sealed class ChatStyleBuilder {
  readonly string _plugin;
  readonly PlayerData _player;   // null = broadcast to everyone
  readonly ChatTarget _target;

  UIBackground? _background;
  Border? _border;
  UIColor? _tabIdle, _tabSelected, _tabText, _tabSelectedText;
  bool _clear;

  internal ChatStyleBuilder(string plugin, PlayerData player, ChatTarget target) {
    _plugin = plugin;
    _player = player;
    _target = target;
  }

  /// <summary>The background drawn behind the target (colour / gradient / image / sprite / material).</summary>
  public ChatStyleBuilder Background(UIBackground background) { _background = background; return this; }

  /// <summary>A border frame around the target.</summary>
  public ChatStyleBuilder Border(Border border) { _border = border; return this; }

  /// <summary>Filter-tab button colours (Tabs target only). Any argument left null keeps the default.</summary>
  public ChatStyleBuilder TabColors(UIColor? idle = null, UIColor? selected = null,
      UIColor? text = null, UIColor? selectedText = null) {
    if (idle.HasValue) _tabIdle = idle;
    if (selected.HasValue) _tabSelected = selected;
    if (text.HasValue) _tabText = text;
    if (selectedText.HasValue) _tabSelectedText = selectedText;
    return this;
  }

  /// <summary>Removes the skin for this target, restoring the vanilla look.</summary>
  public ChatStyleBuilder Clear() { _clear = true; return this; }

  /// <summary>Sends the style to the target player (or everyone, for the *All entry point).</summary>
  public void Send() {
    var packet = BuildPacket();
    if (_player == null) PacketManager.SendPacketToAll(packet);
    else PacketManager.SendPacket(_player, packet);
  }

  ScarletPacket BuildPacket() {
    var d = new Dictionary<string, string> { ["cst"] = Wire(_target) };

    if (_clear) {
      d["Clear"] = "1";
    } else {
      if (_background.HasValue && _background.Value.HasValue) _background.Value.Apply(d, "b");
      if (_border.HasValue) {
        d["dc"] = _border.Value.Color;
        d["dw"] = F(_border.Value.Width);
        d["dr"] = F(_border.Value.Radius);
      }
      if (_tabIdle.HasValue) d["tbg"] = _tabIdle.Value;
      if (_tabSelected.HasValue) d["tsb"] = _tabSelected.Value;
      if (_tabText.HasValue) d["ttx"] = _tabText.Value;
      if (_tabSelectedText.HasValue) d["tst"] = _tabSelectedText.Value;
    }

    return new ScarletPacket { Type = "SetChatStyle", Plugin = _plugin, Window = "", Data = d };
  }

  static string Wire(ChatTarget t) => t switch {
    ChatTarget.Input => "input",
    ChatTarget.Tabs => "tabs",
    _ => "container",
  };

  static string F(float v) => v.ToString(CultureInfo.InvariantCulture);
}
