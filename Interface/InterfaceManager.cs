using System;
using ScarletCore.Services;
using ScarletCore.Interface.Builders;
using ScarletCore.Interface.Models;

namespace ScarletCore.Interface;

/// <summary>
/// Main entry point for the ScarletInterface server-side API.
/// Use <see cref="For"/> or <see cref="ForAll"/> to start building a UI window.
/// </summary>
public static class InterfaceManager {
  /// <summary>
  /// Creates a <see cref="WindowBuilder"/> targeting a specific player.
  /// </summary>
  /// <param name="player">The player to send the UI to.</param>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  public static WindowBuilder For(PlayerData player, string plugin) => new(plugin, player);

  /// <summary>
  /// Creates a <see cref="WindowBuilder"/> that broadcasts the UI to all connected players.
  /// </summary>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  public static WindowBuilder ForAll(string plugin) => new(plugin);

  /// <summary>
  /// Closes the specified window for a player.
  /// </summary>
  /// <param name="player">The player whose window should be closed.</param>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  /// <param name="windowId">The ID of the window to close.</param>
  public static void CloseWindow(PlayerData player, string plugin, string windowId) =>
    new WindowBuilder(plugin, player).Window(windowId).Send(WindowAction.Close);

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
  /// Sends a list of image URLs to be pre-cached on disk on every connected player's client.
  /// Images are stored per-server and reused across sessions; outdated images (size changed) are re-downloaded automatically.
  /// Call this once at load time, before sending any windows that reference these URLs.
  /// </summary>
  /// <param name="plugin">A unique identifier for the calling plugin (e.g. "myplugin").</param>
  /// <param name="urls">The URLs to pre-cache.</param>
  public static void PreCacheImages(string plugin, string[] urls) =>
    PacketManager.SendPacketToAll(new ScarletPacket {
      Type = "PreCacheImages",
      Plugin = plugin,
      Window = "$precache",
      Data = new() { ["Urls"] = string.Join("\n", urls) }
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
      Type = "PreCacheImages",
      Plugin = plugin,
      Window = "$precache",
      Data = new() { ["Urls"] = string.Join("\n", urls) }
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
}
