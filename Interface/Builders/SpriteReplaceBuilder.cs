using System.Collections.Generic;
using ScarletCore.Services;
using ScarletCore.Interface.Models;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Fluent builder for replacing a game sprite by name with an image loaded from a URL.
/// All <c>Image</c> components whose <c>sprite.name</c> matches the given name will be updated,
/// including future instances after UI reloads.
/// </summary>
public class SpriteReplaceBuilder {
  readonly string _plugin;
  readonly PlayerData _player; // null = broadcast
  readonly string _spriteName;
  string _url;

  internal SpriteReplaceBuilder(string plugin, PlayerData player, string spriteName) {
    _plugin = plugin;
    _player = player;
    _spriteName = spriteName;
  }

  /// <summary>The URL of the image that will replace the sprite.</summary>
  public SpriteReplaceBuilder WithUrl(string url) {
    _url = url; return this;
  }

  /// <summary>
  /// Sends the replacement to the target player (or all players if created via
  /// <see cref="InterfaceManager.ReplaceSpriteAll"/>).
  /// The client will download the texture, find every <c>Image</c> whose sprite name
  /// matches and swap it — and persist the replacement so it is re-applied after UI reloads.
  /// </summary>
  public void Send() {
    var packet = new ScarletPacket {
      Type = "SR",
      Plugin = _plugin,
      Window = _spriteName,
      Data = new Dictionary<string, string> { ["su"] = _url ?? "" },
    };

    if (_player != null)
      PacketManager.SendPacket(_player, packet);
    else
      PacketManager.SendPacketToAll(packet);
  }

  /// <summary>
  /// Removes the persistent sprite replacement so the original sprite is restored on
  /// the next UI reload (existing instances are not reverted).
  /// </summary>
  public void Clear() {
    var packet = new ScarletPacket {
      Type = "XC",
      Plugin = _plugin,
      Window = _spriteName,
      Data = new Dictionary<string, string>(),
    };

    if (_player != null)
      PacketManager.SendPacket(_player, packet);
    else
      PacketManager.SendPacketToAll(packet);
  }
}
