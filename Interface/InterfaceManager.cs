using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
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
  public static void SetKeybinds(PlayerData player, string plugin, Dictionary<InputKey, string> binds) =>
    PacketManager.SendPacket(player, KeybindPacket(plugin, SerializeKeybinds(binds)));

  /// <summary>
  /// Broadcasts a keybind map to all connected players. See <see cref="SetKeybinds(PlayerData, string, Dictionary{InputKey,string})"/> for details.
  /// </summary>
  /// <param name="plugin">A unique identifier for the calling plugin.</param>
  /// <param name="binds">Key → command pairs.</param>
  public static void SetKeybindsAll(string plugin, Dictionary<InputKey, string> binds) =>
    PacketManager.SendPacketToAll(KeybindPacket(plugin, SerializeKeybinds(binds)));

  /// <summary>
  /// Sends keybinds that carry a friendly <see cref="Keybind.Label"/> shown in the player's
  /// in-game rebinding menu (Controls tab). Players can re-bind each to a different key;
  /// the chosen key is stored client-side and overrides the default sent here.
  /// Pass an empty array to clear this plugin's binds.
  /// </summary>
  public static void SetKeybinds(PlayerData player, string plugin, params Keybind[] binds) =>
    PacketManager.SendPacket(player, KeybindPacket(plugin, SerializeKeybinds(binds)));

  /// <summary>Broadcasts labelled keybinds to all connected players. See <see cref="SetKeybinds(PlayerData, string, Keybind[])"/>.</summary>
  public static void SetKeybindsAll(string plugin, params Keybind[] binds) =>
    PacketManager.SendPacketToAll(KeybindPacket(plugin, SerializeKeybinds(binds)));

  static ScarletPacket KeybindPacket(string plugin, string kb) {
    var data = new Dictionary<string, string>();
    if (!string.IsNullOrEmpty(kb)) data["kb"] = kb;
    return new ScarletPacket { Type = "SK", Plugin = plugin, Window = "", Data = data };
  }

  static string SerializeKeybinds(Dictionary<InputKey, string> binds) =>
    binds == null || binds.Count == 0 ? null
      : string.Join("\n", binds.Select(kv => $"{kv.Key}={kv.Value}"));

  // Line format: "Key=Command" then optional tab-separated "Label" and "ToggleWindow"
  // fields (an empty label placeholder is emitted when only a toggle window is present).
  // Tabs/newlines are stripped from the trailing fields so they can't corrupt the framing.
  static string SerializeKeybinds(Keybind[] binds) =>
    binds == null || binds.Length == 0 ? null
      : string.Join("\n", binds.Where(b => !string.IsNullOrEmpty(b.Command)).Select(SerializeKeybind));

  static string SerializeKeybind(Keybind b) {
    var line = $"{b.Key}={b.Command}";
    bool hasLabel = !string.IsNullOrEmpty(b.Label);
    bool hasToggle = !string.IsNullOrEmpty(b.ToggleWindow);
    if (hasLabel || hasToggle) line += "\t" + (hasLabel ? Clean(b.Label) : "");
    if (hasToggle) line += "\t" + Clean(b.ToggleWindow);
    return line;
  }

  static string Clean(string s) => s.Replace('\t', ' ').Replace('\n', ' ');

  // ── Options-menu branding ───────────────────────────────────────────────────────

  /// <summary>
  /// Sets the title shown for this mod's sections in the player's native Options menu
  /// (the Sound volume section and the Controls keybinds section) — typically your server's
  /// name. Persists client-side, so it still shows at the main menu before reconnecting.
  /// Call once on <c>InterfaceAuth</c> (and/or at load for already-connected players).
  /// </summary>
  public static void SetOptionsTitle(PlayerData player, string plugin, string title) =>
    PacketManager.SendPacket(player, OptionsBrandingPacket(plugin, title));

  /// <summary>Sets the Options-menu section title on every connected player. See <see cref="SetOptionsTitle"/>.</summary>
  public static void SetOptionsTitleAll(string plugin, string title) =>
    PacketManager.SendPacketToAll(OptionsBrandingPacket(plugin, title));

  static ScarletPacket OptionsBrandingPacket(string plugin, string title) =>
    new() { Type = "OB", Plugin = plugin, Window = "", Data = new() { ["tx"] = title ?? "" } };

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
  //
  // GLOBAL SYNC (<c>syncAnchorUtc</c>): pass the UTC moment the track's timeline started
  // (e.g. a fixed anchor stored when your zone music began looping). The elapsed time is
  // measured at send time and the client seeks to <c>elapsed % length</c>, then keeps the
  // channel drift-corrected — every player hears the same part of the track no matter when
  // they arrived. Reuse the SAME anchor for every send of that track. Assumes pitch 1.
  //
  // Other playback options: startAtMs (fixed start offset), fadeInMs / fadeOutMs
  // (sample-accurate fades), pitch (playback rate), pan (2D stereo, -1..1), and
  // pause/resume + seek via UpdateSound.

  static string F(float v) => NativeElementBuilder.F(v);

  static ScarletPacket AudioPacket(string type, string plugin, Dictionary<string, string> data) =>
    new() { Type = type, Plugin = plugin, Window = "$audio", Data = data };

  /// <summary>
  /// Pre-registers audio categories on the player's client so a volume slider appears for
  /// each in the Sound options menu even before any sound of that category has played.
  /// Categories are also discovered automatically when a sound carrying one is played, so
  /// this is only needed to surface the slider up-front. Call on <c>InterfaceAuth</c>.
  /// </summary>
  public static void RegisterAudioCategories(PlayerData player, string plugin, params string[] categories) =>
    PacketManager.SendPacket(player, AudioCategoryPacket(plugin, categories));

  /// <summary>Pre-registers audio categories on every connected player. See <see cref="RegisterAudioCategories"/>.</summary>
  public static void RegisterAudioCategoriesAll(string plugin, params string[] categories) =>
    PacketManager.SendPacketToAll(AudioCategoryPacket(plugin, categories));

  static ScarletPacket AudioCategoryPacket(string plugin, string[] categories) {
    var data = new Dictionary<string, string>();
    var joined = string.Join("\n", (categories ?? []).Where(c => !string.IsNullOrWhiteSpace(c)));
    if (joined.Length > 0) data["cats"] = joined;
    return new ScarletPacket { Type = "RAC", Plugin = plugin, Window = "$audio", Data = data };
  }

  // ── Ability bar visuals ─────────────────────────────────────────────────────────
  //
  // Override the icon and/or the hover-tooltip text of an ability in the player's ability
  // bar (BottomBar), keyed by the ability's PrefabGUID. The change is applied client-side on
  // the game's own ability-bar and tooltip UI, so it follows the ability wherever it is shown
  // and survives cooldown repaints. Overrides persist until cleared or the client disconnects.

  /// <summary>
  /// Replaces the icon of an ability in a player's ability bar. <paramref name="icon"/> is an
  /// http(s)/file URL (downloaded and disk-cached on the client) or the name of a native game
  /// sprite. Pass an empty string to remove just the icon override.
  /// <paramref name="abilityGuid"/> is the ability's PrefabGUID hash (e.g. <c>prefabGuid.GuidHash</c>).
  /// </summary>
  public static void SetAbilityIcon(PlayerData player, string plugin, int abilityGuid, string icon) =>
    PacketManager.SendPacket(player, AbilityIconPacket(plugin, abilityGuid, icon));

  /// <summary>Replaces an ability's icon on every connected player. See <see cref="SetAbilityIcon"/>.</summary>
  public static void SetAbilityIconAll(string plugin, int abilityGuid, string icon) =>
    PacketManager.SendPacketToAll(AbilityIconPacket(plugin, abilityGuid, icon));

  /// <summary>
  /// Overrides an ability's hover-tooltip text in a player's ability bar. Pass null/empty for a
  /// field to keep the game's own value for that field (e.g. change only the description).
  /// <paramref name="abilityGuid"/> is the ability's PrefabGUID hash (e.g. <c>prefabGuid.GuidHash</c>).
  /// </summary>
  public static void SetAbilityTooltip(PlayerData player, string plugin, int abilityGuid, string title, string description) =>
    PacketManager.SendPacket(player, AbilityTooltipPacket(plugin, abilityGuid, title, description));

  /// <summary>Overrides an ability's tooltip text on every connected player. See <see cref="SetAbilityTooltip"/>.</summary>
  public static void SetAbilityTooltipAll(string plugin, int abilityGuid, string title, string description) =>
    PacketManager.SendPacketToAll(AbilityTooltipPacket(plugin, abilityGuid, title, description));

  /// <summary>Removes all overrides (icon + tooltip) for an ability on a player's client.</summary>
  public static void ClearAbilityVisual(PlayerData player, string plugin, int abilityGuid) =>
    PacketManager.SendPacket(player, AbilityClearPacket(plugin, abilityGuid));

  /// <summary>Removes all overrides for an ability on every connected player.</summary>
  public static void ClearAbilityVisualAll(string plugin, int abilityGuid) =>
    PacketManager.SendPacketToAll(AbilityClearPacket(plugin, abilityGuid));

  static ScarletPacket AbilityIconPacket(string plugin, int abilityGuid, string icon) =>
    new() {
      Type = "SAI", Plugin = plugin, Window = "$ability",
      Data = new() { ["agid"] = abilityGuid.ToString(), ["aic"] = icon ?? "" }
    };

  static ScarletPacket AbilityTooltipPacket(string plugin, int abilityGuid, string title, string description) {
    var data = new Dictionary<string, string> { ["agid"] = abilityGuid.ToString() };
    if (!string.IsNullOrEmpty(title)) data["atl"] = title;
    if (!string.IsNullOrEmpty(description)) data["ade"] = description;
    return new ScarletPacket { Type = "SAT", Plugin = plugin, Window = "$ability", Data = data };
  }

  static ScarletPacket AbilityClearPacket(string plugin, int abilityGuid) =>
    new() {
      Type = "CAV", Plugin = plugin, Window = "$ability",
      Data = new() { ["agid"] = abilityGuid.ToString() }
    };

  // ── Item visuals ────────────────────────────────────────────────────────────────
  //
  // Change how an item TYPE looks on a client, keyed by its PrefabGUID. The icon, name and
  // description ride on the game's own ManagedItemData, so they follow the item everywhere it is
  // drawn — the player inventory, the inventory with a container open, external containers, the
  // quick-access bar and the tooltip header.
  //
  // Two consequences worth knowing before you use this:
  //   * It is keyed by item TYPE and is global on the client. Reskinning a prefab reskins EVERY
  //     copy of it, including ones your mod never created. If your item is a repurposed game item,
  //     that vanilla item changes too.
  //   * Numbers can still vary per instance: the tooltip's field text and stat rows are resolved
  //     against the hovered item, driven by curves you sample from your own code. See
  //     ItemVisualBuilder.
  //
  // Overrides persist on the client until changed or cleared.

  /// <summary>
  /// Builds an item type's appearance for one player. Every string must already be localized for
  /// them. See <see cref="ItemVisualBuilder"/> for the full shape.
  /// <paramref name="itemGuid"/> is the item's PrefabGUID hash (e.g. <c>prefabGuid.GuidHash</c>).
  /// </summary>
  /// <example>
  /// InterfaceManager.ItemVisual(player, "myplugin", itemGuid)
  ///   .Icon("https://example.com/icon.png")
  ///   .Name(Localizer.Get(player, "my_item_name"))
  ///   .Send();
  /// </example>
  public static ItemVisualBuilder ItemVisual(PlayerData player, string plugin, int itemGuid) =>
    new(plugin, player, itemGuid);

  /// <summary>Builds an item type's appearance for every connected player. See <see cref="ItemVisual"/>.</summary>
  public static ItemVisualBuilder ItemVisualAll(string plugin, int itemGuid) =>
    new(plugin, null, itemGuid);

  /// <summary>Removes every override for an item type on a player's client, restoring the game's own
  /// icon, name and description.</summary>
  public static void ClearItemVisual(PlayerData player, string plugin, int itemGuid) =>
    PacketManager.SendPacket(player, ItemClearPacket(plugin, itemGuid));

  /// <summary>Removes every override for an item type on all connected players.</summary>
  public static void ClearItemVisualAll(string plugin, int itemGuid) =>
    PacketManager.SendPacketToAll(ItemClearPacket(plugin, itemGuid));

  static ScarletPacket ItemClearPacket(string plugin, int itemGuid) =>
    new() {
      Type = "CIV", Plugin = plugin, Window = "$item",
      Data = new() { ["igid"] = itemGuid.ToString() }
    };

  // ── Buff visuals ────────────────────────────────────────────────────────────────
  //
  // Change how a character LOOKS while it carries a buff, keyed by the buff's PrefabGUID. You
  // register the buff's visual once (per client, or on InterfaceAuth), and from then on simply
  // applying or removing the buff server-side drives the look — no further packets.
  //
  // Why it works this way: a model's size cannot be replicated. Rendered units go through the game's
  // Hybrid model path, where the visible mesh is a Unity GameObject whose transform the client rebuilds
  // every frame — the ECS scale is never read. And nothing could carry it anyway: LocalTransform is not
  // in the codegen'd replication whitelist, so no scale field exists on the wire. Buffs, on the other
  // hand, DO replicate, which makes them the channel: the client watches for the buff and applies the
  // registered scale itself.
  //
  // Applies to every entity carrying the buff, players and NPCs alike. Registrations persist on the
  // client until cleared or the client disconnects.

  /// <summary>
  /// Scale meaning "leave the model at its own size". Note this is NOT <c>1f</c>: scales are absolute,
  /// and many characters are natively 1.2, 1.5 and so on, so <c>1f</c> would actively resize them.
  /// Register a buff at this value to park it on characters harmlessly, then change the registration
  /// when you want them to grow — no need to re-apply the buff.
  /// </summary>
  public const float OriginalScale = -1f;

  /// <summary>
  /// Registers the absolute model scale applied to any entity carrying <paramref name="buffGuid"/> on a
  /// player's client. <c>2f</c> is double the size; <see cref="OriginalScale"/> leaves the model at
  /// whatever size it natively is. Takes effect for entities already carrying the buff, and entities go
  /// back to their native size when the buff is removed.
  /// <paramref name="buffGuid"/> is the buff's PrefabGUID hash (e.g. <c>prefabGuid.GuidHash</c>).
  /// </summary>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="scale"/> is not a finite number greater than 0, nor <see cref="OriginalScale"/>.
  /// </exception>
  public static void SetBuffScale(PlayerData player, string plugin, int buffGuid, float scale) =>
    PacketManager.SendPacket(player, BuffScalePacket(plugin, buffGuid, scale));

  /// <summary>Registers a buff's model scale on every connected player. See <see cref="SetBuffScale"/>.</summary>
  public static void SetBuffScaleAll(string plugin, int buffGuid, float scale) =>
    PacketManager.SendPacketToAll(BuffScalePacket(plugin, buffGuid, scale));

  /// <summary>Removes a buff's visual registration on a player's client. Entities carrying the buff
  /// return to their normal look.</summary>
  public static void ClearBuffVisual(PlayerData player, string plugin, int buffGuid) =>
    PacketManager.SendPacket(player, BuffClearPacket(plugin, buffGuid));

  /// <summary>Removes a buff's visual registration on every connected player.</summary>
  public static void ClearBuffVisualAll(string plugin, int buffGuid) =>
    PacketManager.SendPacketToAll(BuffClearPacket(plugin, buffGuid));

  static ScarletPacket BuffScalePacket(string plugin, int buffGuid, float scale) {
    // Negative flips the mesh inside out, zero makes it vanish, and NaN/Infinity would poison the
    // client's transition permanently. None of those are things a caller means; fail loudly here rather
    // than let the client quietly drop the packet. (buffGuid is NOT checked — PrefabGUIDs are routinely
    // negative.)
    if (scale != OriginalScale && (!float.IsFinite(scale) || scale <= 0f))
      throw new ArgumentOutOfRangeException(nameof(scale), scale,
        $"Buff scale must be a finite number greater than 0, or InterfaceManager.OriginalScale ({OriginalScale}) to leave the model at its own size.");

    return new ScarletPacket {
      Type = "SBS", Plugin = plugin, Window = "$buff",
      Data = new() {
        ["bgid"] = buffGuid.ToString(),
        // Invariant: the client parses invariant, and a comma decimal separator would not survive.
        ["bsc"] = scale.ToString(CultureInfo.InvariantCulture)
      }
    };
  }

  static ScarletPacket BuffClearPacket(string plugin, int buffGuid) =>
    new() {
      Type = "CBV", Plugin = plugin, Window = "$buff",
      Data = new() { ["bgid"] = buffGuid.ToString() }
    };

  // Milliseconds elapsed since the track's global start, measured now (send time).
  static string SyncElapsed(DateTime anchorUtc) =>
    Math.Max(0L, (long)(DateTime.UtcNow - anchorUtc.ToUniversalTime()).TotalMilliseconds).ToString();

  static void AddPlayOptions(Dictionary<string, string> d, float startAtMs,
      DateTime? syncAnchorUtc, float fadeInMs, float pitch, float pan,
      string[] duckCategories, float duckLevel) {
    if (startAtMs > 0f) d["sat"] = F(startAtMs);
    if (syncAnchorUtc.HasValue) d["sye"] = SyncElapsed(syncAnchorUtc.Value);
    if (fadeInMs > 0f) d["fdi"] = F(fadeInMs);
    if (pitch != 1f) d["pit"] = F(pitch);
    if (pan != 0f) d["pn"] = F(pan);
    if (duckCategories != null && duckCategories.Length > 0) {
      d["dkc"] = string.Join(",", duckCategories);
      d["dkl"] = F(duckLevel);
    }
  }

  static Dictionary<string, string> Build2D(string soundId, string url, float volume, bool loop,
      string category, float startAtMs, DateTime? syncAnchorUtc, float fadeInMs, float pitch, float pan,
      string[] duckCategories, float duckLevel) {
    var d = new Dictionary<string, string> { ["aid"] = soundId, ["ur"] = url, ["am"] = "2d" };
    if (volume != 1f) d["vol"] = F(volume);
    if (loop) d["lp"] = "true";
    if (!string.IsNullOrEmpty(category)) d["aca"] = category;
    AddPlayOptions(d, startAtMs, syncAnchorUtc, fadeInMs, pitch, pan, duckCategories, duckLevel);
    return d;
  }

  static Dictionary<string, string> Build3D(string soundId, string url, float x, float y, float z,
      float minDistance, float maxDistance, float volume, bool loop, string resumeMode, string category,
      float startAtMs, DateTime? syncAnchorUtc, float fadeInMs, float pitch,
      string[] duckCategories, float duckLevel) {
    var d = new Dictionary<string, string> {
      ["aid"] = soundId, ["ur"] = url, ["am"] = "3d",
      ["wx"] = F(x), ["wy"] = F(y), ["wz"] = F(z),
      ["mnd"] = F(minDistance), ["mxd"] = F(maxDistance),
    };
    if (volume != 1f) d["vol"] = F(volume);
    if (loop) d["lp"] = "true";
    if (!string.IsNullOrEmpty(resumeMode) && resumeMode != "pause") d["rz"] = resumeMode;
    if (!string.IsNullOrEmpty(category)) d["aca"] = category;
    AddPlayOptions(d, startAtMs, syncAnchorUtc, fadeInMs, pitch, 0f, duckCategories, duckLevel);   // pan is 2D-only
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
  /// <param name="startAtMs">Start playback at this offset into the file (ms). Default 0.</param>
  /// <param name="syncAnchorUtc">
  /// Global sync: the UTC moment the track's timeline started. The client seeks to
  /// <c>elapsed % length</c> and stays drift-corrected, so every player hears the same
  /// part of the track. Reuse the same anchor on every send of that track.
  /// </param>
  /// <param name="fadeInMs">Fade the volume in over this many ms. Default 0 (no fade).</param>
  /// <param name="pitch">Playback rate multiplier (1 = normal). Default 1.</param>
  /// <param name="pan">Stereo pan, -1 (left) … 0 (center) … 1 (right). Default 0.</param>
  /// <param name="duckCategories">
  /// While this sound plays, lower every sound in these categories to
  /// <paramref name="duckLevel"/> (e.g. an announcement ducking "music"). Restored on stop.
  /// </param>
  /// <param name="duckLevel">Volume multiplier applied to the ducked categories. Default 0.25.</param>
  public static void PlaySound2D(PlayerData player, string plugin, string soundId, string url,
      float volume = 1f, bool loop = false, string category = null,
      float startAtMs = 0f, DateTime? syncAnchorUtc = null, float fadeInMs = 0f,
      float pitch = 1f, float pan = 0f, string[] duckCategories = null, float duckLevel = 0.25f) =>
    PacketManager.SendPacket(player, AudioPacket("PA", plugin,
      Build2D(soundId, url, volume, loop, category, startAtMs, syncAnchorUtc, fadeInMs, pitch, pan, duckCategories, duckLevel)));

  /// <summary>Plays a 2D sound on every connected interface player. See <see cref="PlaySound2D"/>.</summary>
  public static void PlaySound2DAll(string plugin, string soundId, string url,
      float volume = 1f, bool loop = false, string category = null,
      float startAtMs = 0f, DateTime? syncAnchorUtc = null, float fadeInMs = 0f,
      float pitch = 1f, float pan = 0f, string[] duckCategories = null, float duckLevel = 0.25f) =>
    PacketManager.SendPacketToAll(AudioPacket("PA", plugin,
      Build2D(soundId, url, volume, loop, category, startAtMs, syncAnchorUtc, fadeInMs, pitch, pan, duckCategories, duckLevel)));

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
  /// <param name="startAtMs">Start playback at this offset into the file (ms). Default 0.</param>
  /// <param name="syncAnchorUtc">
  /// Global sync: the UTC moment the track's timeline started. The client seeks to
  /// <c>elapsed % length</c> and stays drift-corrected — ideal for looping city/zone
  /// music that must sound identical to everyone. Reuse the same anchor on every send.
  /// With ResumeMode "pause", a synced sound re-seeks to the global position on resume.
  /// </param>
  /// <param name="fadeInMs">Fade the volume in over this many ms. Default 0 (no fade).</param>
  /// <param name="pitch">Playback rate multiplier (1 = normal). Default 1.</param>
  /// <param name="duckCategories">While audible, lower these categories to <paramref name="duckLevel"/>.</param>
  /// <param name="duckLevel">Volume multiplier applied to the ducked categories. Default 0.25.</param>
  public static void PlaySound3D(PlayerData player, string plugin, string soundId, string url,
      float x, float y, float z, float minDistance, float maxDistance,
      float volume = 1f, bool loop = true, string resumeMode = "pause", string category = null,
      float startAtMs = 0f, DateTime? syncAnchorUtc = null, float fadeInMs = 0f, float pitch = 1f,
      string[] duckCategories = null, float duckLevel = 0.25f) =>
    PacketManager.SendPacket(player, AudioPacket("PA", plugin,
      Build3D(soundId, url, x, y, z, minDistance, maxDistance, volume, loop, resumeMode, category,
        startAtMs, syncAnchorUtc, fadeInMs, pitch, duckCategories, duckLevel)));

  /// <summary>Plays a 3D positional sound on every connected interface player. See <see cref="PlaySound3D"/>.</summary>
  public static void PlaySound3DAll(string plugin, string soundId, string url,
      float x, float y, float z, float minDistance, float maxDistance,
      float volume = 1f, bool loop = true, string resumeMode = "pause", string category = null,
      float startAtMs = 0f, DateTime? syncAnchorUtc = null, float fadeInMs = 0f, float pitch = 1f,
      string[] duckCategories = null, float duckLevel = 0.25f) =>
    PacketManager.SendPacketToAll(AudioPacket("PA", plugin,
      Build3D(soundId, url, x, y, z, minDistance, maxDistance, volume, loop, resumeMode, category,
        startAtMs, syncAnchorUtc, fadeInMs, pitch, duckCategories, duckLevel)));

  /// <summary>
  /// Live-updates an active sound by id. Only the non-null values are changed; everything
  /// else keeps its current value. Useful for moving a 3D emitter, fading volume,
  /// pausing/resuming (<paramref name="paused"/>) or jumping to a position
  /// (<paramref name="seekMs"/> — re-anchors global sync to the new position).
  /// </summary>
  public static void UpdateSound(PlayerData player, string plugin, string soundId,
      float? volume = null, float? x = null, float? y = null, float? z = null,
      float? minDistance = null, float? maxDistance = null,
      float? pitch = null, float? pan = null, bool? paused = null, float? seekMs = null) =>
    PacketManager.SendPacket(player, AudioPacket("UA", plugin,
      BuildUpdate(soundId, volume, x, y, z, minDistance, maxDistance, pitch, pan, paused, seekMs)));

  /// <summary>Live-updates an active sound on every connected interface player. See <see cref="UpdateSound"/>.</summary>
  public static void UpdateSoundAll(string plugin, string soundId,
      float? volume = null, float? x = null, float? y = null, float? z = null,
      float? minDistance = null, float? maxDistance = null,
      float? pitch = null, float? pan = null, bool? paused = null, float? seekMs = null) =>
    PacketManager.SendPacketToAll(AudioPacket("UA", plugin,
      BuildUpdate(soundId, volume, x, y, z, minDistance, maxDistance, pitch, pan, paused, seekMs)));

  /// <summary>
  /// Live-updates every active sound tagged with <paramref name="category"/> on a specific
  /// player's client — e.g. lower or pause all "music" at once.
  /// </summary>
  public static void UpdateCategory(PlayerData player, string plugin, string category,
      float? volume = null, bool? paused = null) =>
    PacketManager.SendPacket(player, AudioPacket("UA", plugin,
      BuildCategoryUpdate(category, volume, paused)));

  /// <summary>Live-updates a sound category on every connected interface player. See <see cref="UpdateCategory"/>.</summary>
  public static void UpdateCategoryAll(string plugin, string category,
      float? volume = null, bool? paused = null) =>
    PacketManager.SendPacketToAll(AudioPacket("UA", plugin,
      BuildCategoryUpdate(category, volume, paused)));

  static Dictionary<string, string> BuildUpdate(string soundId, float? volume,
      float? x, float? y, float? z, float? minDistance, float? maxDistance,
      float? pitch, float? pan, bool? paused, float? seekMs) {
    var d = new Dictionary<string, string> { ["aid"] = soundId };
    if (volume.HasValue) d["vol"] = F(volume.Value);
    if (x.HasValue) d["wx"] = F(x.Value);
    if (y.HasValue) d["wy"] = F(y.Value);
    if (z.HasValue) d["wz"] = F(z.Value);
    if (minDistance.HasValue) d["mnd"] = F(minDistance.Value);
    if (maxDistance.HasValue) d["mxd"] = F(maxDistance.Value);
    if (pitch.HasValue) d["pit"] = F(pitch.Value);
    if (pan.HasValue) d["pn"] = F(pan.Value);
    if (paused.HasValue) d["pd"] = paused.Value ? "true" : "false";
    if (seekMs.HasValue) d["skm"] = F(seekMs.Value);
    return d;
  }

  static Dictionary<string, string> BuildCategoryUpdate(string category, float? volume, bool? paused) {
    var d = new Dictionary<string, string> { ["aca"] = category };
    if (volume.HasValue) d["vol"] = F(volume.Value);
    if (paused.HasValue) d["pd"] = paused.Value ? "true" : "false";
    return d;
  }

  static Dictionary<string, string> BuildStop(string soundId, string category, float fadeOutMs) {
    var d = new Dictionary<string, string>();
    if (!string.IsNullOrEmpty(soundId)) d["aid"] = soundId;
    if (!string.IsNullOrEmpty(category)) d["aca"] = category;
    if (fadeOutMs > 0f) d["fdo"] = F(fadeOutMs);
    return d;
  }

  /// <summary>
  /// Stops a single sound by id on a specific player's client.
  /// <paramref name="fadeOutMs"/> &gt; 0 fades to silence over that many ms before stopping.
  /// </summary>
  public static void StopSound(PlayerData player, string plugin, string soundId, float fadeOutMs = 0f) =>
    PacketManager.SendPacket(player, AudioPacket("XA", plugin, BuildStop(soundId, null, fadeOutMs)));

  /// <summary>Stops a single sound by id on every connected interface player.</summary>
  public static void StopSoundAll(string plugin, string soundId, float fadeOutMs = 0f) =>
    PacketManager.SendPacketToAll(AudioPacket("XA", plugin, BuildStop(soundId, null, fadeOutMs)));

  /// <summary>Stops every sound tagged with <paramref name="category"/> on a specific player's client.</summary>
  public static void StopCategory(PlayerData player, string plugin, string category, float fadeOutMs = 0f) =>
    PacketManager.SendPacket(player, AudioPacket("XA", plugin, BuildStop(null, category, fadeOutMs)));

  /// <summary>Stops every sound tagged with <paramref name="category"/> on every connected interface player.</summary>
  public static void StopCategoryAll(string plugin, string category, float fadeOutMs = 0f) =>
    PacketManager.SendPacketToAll(AudioPacket("XA", plugin, BuildStop(null, category, fadeOutMs)));

  /// <summary>Stops all sounds on a specific player's client.</summary>
  public static void StopAllSounds(PlayerData player, string plugin, float fadeOutMs = 0f) =>
    PacketManager.SendPacket(player, AudioPacket("XA", plugin, BuildStop(null, null, fadeOutMs)));

  /// <summary>Stops all sounds on every connected interface player.</summary>
  public static void StopAllSoundsForAll(string plugin, float fadeOutMs = 0f) =>
    PacketManager.SendPacketToAll(AudioPacket("XA", plugin, BuildStop(null, null, fadeOutMs)));

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
