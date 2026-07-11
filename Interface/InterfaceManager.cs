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

  // ── Audio ─────────────────────────────────────────────────────────────────────
  //
  // The server tells clients to play/stop sounds; each sound carries a caller-chosen
  // <c>soundId</c> (a handle) so it can be stopped or live-updated individually. Audio
  // files are fetched from a URL and cached on disk per client (like images).
  //
  // Supported formats: WAV, OGG, MP3 and FLAC — playback goes through the game's own
  // FMOD core system, which decodes these natively (Unity's audio pipeline is disabled
  // in V Rising, so AudioSource/AudioClip cannot be used).
  //
  // 2D sounds play at a constant volume (UI / music). 3D sounds are anchored at an
  // in-game world coordinate; each client attenuates the volume by the distance between
  // the sound and its local player, computed entirely client-side.

  static string F(float v) => NativeElementBuilder.F(v);

  static ScarletPacket AudioPacket(string type, string plugin, Dictionary<string, string> data) =>
    new() { Type = type, Plugin = plugin, Window = "$audio", Data = data };

  static Dictionary<string, string> Build2D(string soundId, string url, float volume, bool loop, string category) {
    var d = new Dictionary<string, string> { ["aid"] = soundId, ["ur"] = url, ["am"] = "2d" };
    if (volume != 1f) d["vol"] = F(volume);
    if (loop) d["lp"] = "true";
    if (!string.IsNullOrEmpty(category)) d["aca"] = category;
    return d;
  }

  static Dictionary<string, string> Build3D(string soundId, string url, float x, float y, float z,
      float minDistance, float maxDistance, float volume, bool loop, string resumeMode, string category) {
    var d = new Dictionary<string, string> {
      ["aid"] = soundId, ["ur"] = url, ["am"] = "3d",
      ["wx"] = F(x), ["wy"] = F(y), ["wz"] = F(z),
      ["mnd"] = F(minDistance), ["mxd"] = F(maxDistance),
    };
    if (volume != 1f) d["vol"] = F(volume);
    if (loop) d["lp"] = "true";
    if (!string.IsNullOrEmpty(resumeMode) && resumeMode != "pause") d["rz"] = resumeMode;
    if (!string.IsNullOrEmpty(category)) d["aca"] = category;
    return d;
  }

  /// <summary>
  /// Plays a 2D sound (constant volume) on a specific player's client.
  /// </summary>
  /// <param name="player">Target player.</param>
  /// <param name="plugin">A unique identifier for the calling plugin.</param>
  /// <param name="soundId">Caller-chosen handle; reusing an id replaces the previous sound.</param>
  /// <param name="url">HTTP(S) URL of the audio file (WAV, OGG, MP3 or FLAC).</param>
  /// <param name="volume">0..1 playback volume. Default 1.</param>
  /// <param name="loop">Whether the clip loops. Default false.</param>
  /// <param name="category">Optional group tag for <see cref="StopCategory"/> (e.g. "music").</param>
  public static void PlaySound2D(PlayerData player, string plugin, string soundId, string url,
      float volume = 1f, bool loop = false, string category = null) =>
    PacketManager.SendPacket(player, AudioPacket("PA", plugin, Build2D(soundId, url, volume, loop, category)));

  /// <summary>Plays a 2D sound on every connected interface player. See <see cref="PlaySound2D"/>.</summary>
  public static void PlaySound2DAll(string plugin, string soundId, string url,
      float volume = 1f, bool loop = false, string category = null) =>
    PacketManager.SendPacketToAll(AudioPacket("PA", plugin, Build2D(soundId, url, volume, loop, category)));

  /// <summary>
  /// Plays a 3D positional sound anchored at an in-game world coordinate on a specific
  /// player's client. The client attenuates the volume by the distance between the sound
  /// and the local player: full volume within <paramref name="minDistance"/>, silent beyond
  /// <paramref name="maxDistance"/>.
  /// </summary>
  /// <param name="player">Target player.</param>
  /// <param name="plugin">A unique identifier for the calling plugin.</param>
  /// <param name="soundId">Caller-chosen handle; reusing an id replaces the previous sound.</param>
  /// <param name="url">HTTP(S) URL of the audio file (WAV, OGG, MP3 or FLAC).</param>
  /// <param name="x">World X of the emitter.</param>
  /// <param name="y">World Y of the emitter.</param>
  /// <param name="z">World Z of the emitter.</param>
  /// <param name="minDistance">Distance (world units) within which the sound plays at full volume.</param>
  /// <param name="maxDistance">Distance beyond which the sound is inaudible.</param>
  /// <param name="volume">0..1 base volume before distance attenuation. Default 1.</param>
  /// <param name="loop">Whether the clip loops. Default true (typical for ambient emitters).</param>
  /// <param name="resumeMode">
  /// Out-of-range behaviour: <c>"pause"</c> (freeze the timeline and resume where it left off)
  /// or <c>"virtual"</c> (keep the timeline advancing while muted, resuming in sync). Default "pause".
  /// </param>
  /// <param name="category">Optional group tag for <see cref="StopCategory"/>.</param>
  public static void PlaySound3D(PlayerData player, string plugin, string soundId, string url,
      float x, float y, float z, float minDistance, float maxDistance,
      float volume = 1f, bool loop = true, string resumeMode = "pause", string category = null) =>
    PacketManager.SendPacket(player, AudioPacket("PA", plugin,
      Build3D(soundId, url, x, y, z, minDistance, maxDistance, volume, loop, resumeMode, category)));

  /// <summary>Plays a 3D positional sound on every connected interface player. See <see cref="PlaySound3D"/>.</summary>
  public static void PlaySound3DAll(string plugin, string soundId, string url,
      float x, float y, float z, float minDistance, float maxDistance,
      float volume = 1f, bool loop = true, string resumeMode = "pause", string category = null) =>
    PacketManager.SendPacketToAll(AudioPacket("PA", plugin,
      Build3D(soundId, url, x, y, z, minDistance, maxDistance, volume, loop, resumeMode, category)));

  /// <summary>
  /// Live-updates an active sound by id. Only the non-null values are changed; everything
  /// else keeps its current value. Useful for moving a 3D emitter or fading volume.
  /// </summary>
  public static void UpdateSound(PlayerData player, string plugin, string soundId,
      float? volume = null, float? x = null, float? y = null, float? z = null,
      float? minDistance = null, float? maxDistance = null) =>
    PacketManager.SendPacket(player, AudioPacket("UA", plugin,
      BuildUpdate(soundId, volume, x, y, z, minDistance, maxDistance)));

  /// <summary>Live-updates an active sound on every connected interface player. See <see cref="UpdateSound"/>.</summary>
  public static void UpdateSoundAll(string plugin, string soundId,
      float? volume = null, float? x = null, float? y = null, float? z = null,
      float? minDistance = null, float? maxDistance = null) =>
    PacketManager.SendPacketToAll(AudioPacket("UA", plugin,
      BuildUpdate(soundId, volume, x, y, z, minDistance, maxDistance)));

  static Dictionary<string, string> BuildUpdate(string soundId, float? volume,
      float? x, float? y, float? z, float? minDistance, float? maxDistance) {
    var d = new Dictionary<string, string> { ["aid"] = soundId };
    if (volume.HasValue) d["vol"] = F(volume.Value);
    if (x.HasValue) d["wx"] = F(x.Value);
    if (y.HasValue) d["wy"] = F(y.Value);
    if (z.HasValue) d["wz"] = F(z.Value);
    if (minDistance.HasValue) d["mnd"] = F(minDistance.Value);
    if (maxDistance.HasValue) d["mxd"] = F(maxDistance.Value);
    return d;
  }

  /// <summary>Stops a single sound by id on a specific player's client.</summary>
  public static void StopSound(PlayerData player, string plugin, string soundId) =>
    PacketManager.SendPacket(player, AudioPacket("XA", plugin, new() { ["aid"] = soundId }));

  /// <summary>Stops a single sound by id on every connected interface player.</summary>
  public static void StopSoundAll(string plugin, string soundId) =>
    PacketManager.SendPacketToAll(AudioPacket("XA", plugin, new() { ["aid"] = soundId }));

  /// <summary>Stops every sound tagged with <paramref name="category"/> on a specific player's client.</summary>
  public static void StopCategory(PlayerData player, string plugin, string category) =>
    PacketManager.SendPacket(player, AudioPacket("XA", plugin, new() { ["aca"] = category }));

  /// <summary>Stops every sound tagged with <paramref name="category"/> on every connected interface player.</summary>
  public static void StopCategoryAll(string plugin, string category) =>
    PacketManager.SendPacketToAll(AudioPacket("XA", plugin, new() { ["aca"] = category }));

  /// <summary>Stops all sounds on a specific player's client.</summary>
  public static void StopAllSounds(PlayerData player, string plugin) =>
    PacketManager.SendPacket(player, AudioPacket("XA", plugin, new()));

  /// <summary>Stops all sounds on every connected interface player.</summary>
  public static void StopAllSoundsForAll(string plugin) =>
    PacketManager.SendPacketToAll(AudioPacket("XA", plugin, new()));

  /// <summary>
  /// Pre-caches audio files on disk on every connected player's client so later
  /// <see cref="PlaySound2D"/>/<see cref="PlaySound3D"/> calls start without a download stall.
  /// Call once at load time. Files are stored per-server and re-downloaded only when they change.
  /// </summary>
  public static void PreCacheAudio(string plugin, string[] urls) =>
    PacketManager.SendPacketToAll(AudioPacket("PCA", plugin, new() { ["ul"] = string.Join("\n", urls) }));

  /// <summary>Pre-caches audio files on disk for a specific player's client. See <see cref="PreCacheAudio(string, string[])"/>.</summary>
  public static void PreCacheAudio(PlayerData player, string plugin, string[] urls) =>
    PacketManager.SendPacket(player, AudioPacket("PCA", plugin, new() { ["ul"] = string.Join("\n", urls) }));
}
