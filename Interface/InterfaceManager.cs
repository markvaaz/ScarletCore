using System;
using System.Collections.Generic;
using System.Linq;
using ScarletCore.Services;
using ScarletCore.Interface.Builders;
using ScarletCore.Interface.Models;

namespace ScarletCore.Interface;

/// <summary>
/// Main entry point for the ScarletInterface server-side API.
/// Build windows directly with <c>new Window(player, plugin, id) { ... }.Send();</c>.
/// </summary>
public static class InterfaceManager {
  /// <summary>
  /// Closes the specified window for a player.
  /// </summary>
  public static void CloseWindow(PlayerData player, string plugin, string windowId) =>
    new Window(player, plugin, windowId).Send(WindowAction.Close);

  /// <summary>
  /// Creates a <see cref="NativeElementBuilder"/> targeting an existing game GameObject
  /// for a specific player. Use the normalized path (without "(Clone)" suffixes).
  /// </summary>
  /// <example>
  /// InterfaceManager.Native(player, "myplugin",
  ///     "HUDMenuParent/CharacterMenu/SubMenu/InventoryMenu/MenuParent/" +
  ///     "CharacterInventorySubMenu/MotionRoot/EquipmentTab/ParentContainerInventory/EquipmentContainer/Slot_45")
  ///   .SetPosition(100f, -200f)
  ///   .Send();
  /// </example>
  public static NativeElementBuilder Native(PlayerData player, string plugin, string path) =>
    new(plugin, player, path);

  /// <summary>
  /// Creates a <see cref="NativeElementBuilder"/> broadcasting to all connected players.
  /// </summary>
  public static NativeElementBuilder NativeAll(string plugin, string path) =>
    new(plugin, null, path);

  /// <summary>
  /// Removes the persistent listener for <paramref name="path"/> on a specific player,
  /// so the server no longer re-applies modifications when that UI reloads.
  /// Use <see cref="InterfaceManager.Native"/> and call <c>.Clear()</c> for the same effect.
  /// </summary>
  public static void NativeClear(PlayerData player, string plugin, string path) =>
    Native(player, plugin, path).Clear();

  /// <summary>Removes the persistent listener for all connected players.</summary>
  public static void NativeClearAll(string plugin, string path) =>
    NativeAll(plugin, path).Clear();

  /// <summary>
  /// Creates a <see cref="SpriteReplaceBuilder"/> that replaces every <c>Image</c> component
  /// whose <c>sprite.name</c> equals <paramref name="spriteName"/> with a texture from a URL,
  /// for a specific player.
  /// </summary>
  /// <example>
  /// InterfaceManager.ReplaceSprite(player, "myplugin", "StatBG")
  ///   .WithUrl("https://example.com/my-bg.png")
  ///   .Send();
  /// </example>
  public static SpriteReplaceBuilder ReplaceSprite(PlayerData player, string plugin, string spriteName) =>
    new(plugin, player, spriteName);

  /// <summary>
  /// Creates a <see cref="SpriteReplaceBuilder"/> that broadcasts the sprite replacement
  /// to all connected players.
  /// </summary>
  public static SpriteReplaceBuilder ReplaceSpriteAll(string plugin, string spriteName) =>
    new(plugin, null, spriteName);

  /// <summary>
  /// Sends a font bundle URL to all connected players with ScarletInterface installed.
  /// The client downloads the <c>fonts.bin</c> file, creates TMP font assets from the
  /// embedded TTFs, and makes them available by name via <c>font=</c> in AddText.
  /// Call once at plugin load time; results are cached on disk per server.
  /// </summary>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  /// <param name="url">The URL of the <c>fonts.bin</c> file to load.</param>
  public static void LoadFontBundleAll(string plugin, string url) =>
    PacketManager.SendPacketToAll(new ScarletPacket {
      Type = "LF",
      Plugin = plugin,
      Window = "$fonts",
      Data = new() { ["ur"] = url }
    });

  /// <summary>
  /// Sends a font bundle URL to a specific player.
  /// The client downloads the <c>fonts.bin</c> file, creates TMP font assets from the
  /// embedded TTFs, and makes them available by name via <c>font=</c> in AddText.
  /// </summary>
  /// <param name="player">The target player.</param>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  /// <param name="url">The URL of the <c>fonts.bin</c> file to load.</param>
  public static void LoadFontBundle(PlayerData player, string plugin, string url) =>
    PacketManager.SendPacket(player, new ScarletPacket {
      Type = "LF",
      Plugin = plugin,
      Window = "$fonts",
      Data = new() { ["ur"] = url }
    });

  /// <summary>
  /// Sends a list of image URLs to be pre-cached on disk on every connected player's client.
  /// Images are stored per-server and reused across sessions; outdated images (size changed) are re-downloaded automatically.
  /// Call this once at load time, before sending any windows that reference these URLs.
  /// </summary>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  /// <param name="urls">The URLs to pre-cache.</param>
  public static void PreCacheImages(string plugin, string[] urls) =>
    PacketManager.SendPacketToAll(new ScarletPacket {
      Type = "PI",
      Plugin = plugin,
      Window = "$precache",
      Data = new() { ["ul"] = string.Join("\n", urls) }
    });

  /// <summary>
  /// Sends a list of image URLs to be pre-cached on disk for a specific player's client.
  /// Images are stored per-server and reused across sessions; outdated images (size changed) are re-downloaded automatically.
  /// Call this once at load time, before sending any windows that reference these URLs.
  /// </summary>
  /// <param name="player">The player to send the pre-cache request to.</param>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  /// <param name="urls">The URLs to pre-cache.</param>
  public static void PreCacheImages(PlayerData player, string plugin, string[] urls) =>
    PacketManager.SendPacket(player, new ScarletPacket {
      Type = "PI",
      Plugin = plugin,
      Window = "$precache",
      Data = new() { ["ul"] = string.Join("\n", urls) }
    });

  /// <summary>
  /// Pre-builds the sprite name index on every connected player's client so that
  /// subsequent windows that reference game sprites by name open without a freeze.
  /// Call this once at load time (e.g. on InterfaceAuth), before sending any windows
  /// that use <c>UIBackground.FromSprite</c>.
  /// </summary>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  /// <param name="names">The sprite names used by your UI (for diagnostic logging).</param>
  public static void PreCacheSprites(string plugin, string[] names) =>
    PacketManager.SendPacketToAll(new ScarletPacket {
      Type = "PS",
      Plugin = plugin,
      Window = "$precache",
      Data = new() { ["sl"] = string.Join("\n", names) }
    });

  /// <summary>
  /// Pre-builds the sprite name index on a specific player's client so that
  /// subsequent windows that reference game sprites by name open without a freeze.
  /// Call this once at load time (e.g. on InterfaceAuth), before sending any windows
  /// that use <c>UIBackground.FromSprite</c>.
  /// </summary>
  /// <param name="player">The target player.</param>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  /// <param name="names">The sprite names used by your UI (for diagnostic logging).</param>
  public static void PreCacheSprites(PlayerData player, string plugin, string[] names) =>
    PacketManager.SendPacket(player, new ScarletPacket {
      Type = "PS",
      Plugin = plugin,
      Window = "$precache",
      Data = new() { ["sl"] = string.Join("\n", names) }
    });

  /// <summary>
  /// Registers a callback invoked when a player sends a raw chat message starting with <paramref name="prefix"/>.
  /// Useful for handling button commands that don't use the ScarletCore command system.
  /// </summary>
  /// <example>
  /// ScarletInterface.OnMessage("mymod_confirm", (player, args) => { ... });
  /// </example>
  public static void OnMessage(string prefix, Action<PlayerData, string[]> handler) =>
    PacketManager.OnMessage(prefix, handler);

  /// <summary>
  /// Registers a callback invoked when a player runs a ScarletCore command with the given name.
  /// Equivalent to listening on <c>CommandEvents.OnBeforeExecute</c> filtered by command name.
  /// </summary>
  /// <example>
  /// ScarletInterface.OnCommand("mymod.shop", (player, args) => { ... });
  /// </example>
  public static void OnCommand(string commandName, Action<PlayerData, string[]> handler) =>
    PacketManager.OnCommand(commandName, handler);

  /// <summary>
  /// Sends a keybind map to a specific player. Each entry maps a Unity <c>KeyCode</c> name
  /// (e.g. <c>"G"</c>, <c>"F1"</c>) to a command string that is executed on the client when
  /// that key is pressed. The command is fired once per press with a 1-second cooldown.
  /// <para>
  /// Pass an empty dictionary to clear all keybinds for this plugin on the client.
  /// </para>
  /// </summary>
  /// <param name="player">The target player.</param>
  /// <param name="plugin">A unique identifier for the calling plugin.</param>
  /// <param name="binds">Key → command pairs.</param>
  public static void SetKeybinds(PlayerData player, string plugin, Dictionary<InputKey, string> binds) {
    var data = new Dictionary<string, string>();
    if (binds != null && binds.Count > 0)
      data["kb"] = string.Join("\n", binds.Select(kv => $"{kv.Key}={kv.Value}"));
    PacketManager.SendPacket(player, new ScarletPacket {
      Type = "SK",
      Plugin = plugin,
      Window = "",
      Data = data,
    });
  }

  /// <summary>
  /// Broadcasts a keybind map to all connected players. See <see cref="SetKeybinds(PlayerData, string, Dictionary{InputKey,string})"/> for details.
  /// </summary>
  /// <param name="plugin">A unique identifier for the calling plugin.</param>
  /// <param name="binds">Key → command pairs.</param>
  public static void SetKeybindsAll(string plugin, Dictionary<InputKey, string> binds) {
    var data = new Dictionary<string, string>();
    if (binds != null && binds.Count > 0)
      data["kb"] = string.Join("\n", binds.Select(kv => $"{kv.Key}={kv.Value}"));
    PacketManager.SendPacketToAll(new ScarletPacket {
      Type = "SK",
      Plugin = plugin,
      Window = "",
      Data = data,
    });
  }
}
