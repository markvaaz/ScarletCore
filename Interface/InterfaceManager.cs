using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using ScarletCore.Services;
using ScarletCore.Interface.Builders;
using ScarletCore.Interface.Models;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM.Network;

namespace ScarletCore.Interface;

/// <summary>
/// How many instances of a <see cref="InterfaceManager.UnitHud"/> bind are visible, and how they are
/// triggered. See the <c>ush</c> clause in the Unit Proximity HUD spec.
/// </summary>
public enum UnitHudShow {
  /// <summary>Every matched entity in range shows an instance (default).</summary>
  All,
  /// <summary>Only the nearest matched entity — or the nearest <c>showCount</c> — is shown.</summary>
  Closest,
  /// <summary>An instance exists only while the pointer is over the entity (or the window), with a linger grace period.</summary>
  Hover,
  /// <summary>An instance toggles open on right-click on the entity.</summary>
  Click,
}

/// <summary>
/// Whether the ability bar's extra (shift) slot is shown on the client. See
/// <see cref="InterfaceManager.SetExtraSlot"/>.
/// </summary>
public enum ExtraSlotMode {
  /// <summary>Pinned visible even while the slot is empty.</summary>
  Always,
  /// <summary>Visible only while an ability is bound to it — the game's own default behaviour.</summary>
  WhenBound,
  /// <summary>Kept hidden even while an ability is bound to it.</summary>
  Hidden,
}

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
  /// Sets the name of this mod's own tab in the player's native Options menu (the fifth tab,
  /// after General / Controls / Graphics / Sound, holding the audio settings and the
  /// keybinds) — typically your server's name. Persists client-side, so it still shows at
  /// the main menu before reconnecting.
  /// Call once on <c>InterfaceAuth</c> (and/or at load for already-connected players).
  /// </summary>
  public static void SetOptionsTitle(PlayerData player, string plugin, string title) =>
    PacketManager.SendPacket(player, OptionsBrandingPacket(plugin, title));

  /// <summary>Sets the Options-menu section title on every connected player. See <see cref="SetOptionsTitle"/>.</summary>
  public static void SetOptionsTitleAll(string plugin, string title) =>
    PacketManager.SendPacketToAll(OptionsBrandingPacket(plugin, title));

  static ScarletPacket OptionsBrandingPacket(string plugin, string title) =>
    new() { Type = "OB", Plugin = plugin, Window = "", Data = new() { ["tx"] = title ?? "" } };

  /// <summary>
  /// Replaces the V Rising logo at the top of the player's ESC menu with an image loaded
  /// from <paramref name="url"/> — typically your server's logo. Pass null or an empty url
  /// to restore the game's own logo. The image keeps its aspect ratio inside the logo slot,
  /// which is shaped for the tall "V", so a wide banner will be letterboxed rather than
  /// stretched. Call once on <c>InterfaceAuth</c>.
  /// </summary>
  public static void SetServerLogo(PlayerData player, string plugin, string url) =>
    PacketManager.SendPacket(player, ServerLogoPacket(plugin, url));

  /// <summary>Sets the ESC-menu logo on every connected player. See <see cref="SetServerLogo"/>.</summary>
  public static void SetServerLogoAll(string plugin, string url) =>
    PacketManager.SendPacketToAll(ServerLogoPacket(plugin, url));

  static ScarletPacket ServerLogoPacket(string plugin, string url) =>
    new() { Type = "SL", Plugin = plugin, Window = "", Data = new() { ["ur"] = url ?? "" } };

  // ── Floating character HUD ────────────────────────────────────────────────────

  /// <summary>
  /// Pins the nameplate floating above characters so it is always visible instead of only on
  /// hover / when damaged. It is the whole nameplate — name, health bar and level together — for
  /// every character type: players, regular units and V Bloods alike. False is vanilla behaviour.
  ///
  /// The parts are not separable, because the game draws the nameplate as one thing and offers no
  /// lever to split it: pinning works by convincing the client that every character on screen is
  /// under the cursor, and a hover shows all three.
  ///
  /// <b>This is a suggestion, not a setting.</b> The player owns this toggle in the mod's Options
  /// tab: what you send only takes effect while they have never touched it, and the moment they do
  /// it is theirs on your server and every other. Call once on <c>InterfaceAuth</c>.
  /// </summary>
  public static void SetCharacterHud(PlayerData player, string plugin, bool alwaysShow) =>
    PacketManager.SendPacket(player, CharacterHudPacket(plugin, alwaysShow));

  /// <summary>Suggests the nameplate pinning to every connected player. See <see cref="SetCharacterHud"/>.</summary>
  public static void SetCharacterHudAll(string plugin, bool alwaysShow) =>
    PacketManager.SendPacketToAll(CharacterHudPacket(plugin, alwaysShow));

  static ScarletPacket CharacterHudPacket(string plugin, bool alwaysShow) =>
    new() {
      Type = "CHV",
      Plugin = plugin,
      Window = "",
      Data = new() { ["chn"] = B(alwaysShow) },
    };

  static string B(bool v) => v ? "1" : "0";

  // ── Ability bar extra (shift) slot ─────────────────────────────────────────────

  /// <summary>
  /// Suggests whether the ability bar's <b>extra (shift) slot</b> is shown: the fourth slot the
  /// game normally reveals only once a shift-cast ability is bound to it. <see cref="ExtraSlotMode"/>
  /// picks between always shown, shown only when bound (the game's own default), and always hidden.
  ///
  /// <b>This is a suggestion, not a setting.</b> The player owns this toggle in the mod's Options
  /// tab: what you send only takes effect while they have never touched it, and the moment they do
  /// it is theirs on your server and every other. Call once on <c>InterfaceAuth</c>.
  /// </summary>
  public static void SetExtraSlot(PlayerData player, string plugin, ExtraSlotMode mode) =>
    PacketManager.SendPacket(player, ExtraSlotPacket(plugin, mode));

  /// <summary>Suggests the extra-slot visibility to every connected player. See <see cref="SetExtraSlot"/>.</summary>
  public static void SetExtraSlotAll(string plugin, ExtraSlotMode mode) =>
    PacketManager.SendPacketToAll(ExtraSlotPacket(plugin, mode));

  static ScarletPacket ExtraSlotPacket(string plugin, ExtraSlotMode mode) =>
    new() {
      Type = "ESV",
      Plugin = plugin,
      Window = "",
      Data = new() { ["esm"] = ((int)mode).ToString(CultureInfo.InvariantCulture) },
    };

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

  // ── World-position proximity triggers ─────────────────────────────────────────────
  //
  // Registers a world point + radius on the player's client. When the local player moves within
  // <c>radius</c> world units of the point the ENTER actions fire (open <paramref name="enterWindow"/>
  // and/or run <paramref name="enterCommand"/>); when they move back out, the EXIT actions fire
  // (open <paramref name="exitWindow"/> and/or run <paramref name="exitCommand"/>). The whole
  // "got close → show" mechanic runs client-side; the server only registers the trigger.
  //
  // Entering also closes any exit window still up, and leaving closes the enter window — so a
  // single window passed as both enterWindow and exitWindow behaves like the classic
  // "approach → open, leave → close". A small fixed hysteresis on the client keeps the boundary
  // from flickering when the player stands right on the edge.
  //
  // Windows referenced here must already exist on the client. Build each once with
  // <c>new Window(player, plugin, id){...}.Send(WindowAction.None)</c> so it is created closed;
  // this trigger then only decides when to show or hide it.
  //
  // <paramref name="mandatory"/>: if the player closes the shown window by hand while still in
  //   range, reopen it (an obligatory popup). Default false = dismissible until they leave and return.
  // <paramref name="oneShot"/>: fire only once, ever — persisted on the client across reconnects
  //   and game restarts. The persistence key is the (plugin, id) pair; bake a per-server or
  //   per-anything token into <paramref name="id"/> if you need finer scoping.
  //
  // Two flavours of trigger, chosen by the position overload:
  //   * 3D — <c>float x,y,z</c> or <c>float3</c>: distance uses all three axes.
  //   * 2D — <c>float2</c>: height-independent, only X and Z count (the float2 is (X, Z)).

  /// <summary>Registers a 3D proximity trigger (x/y/z) for one player. See remarks above.</summary>
  public static void ProximityTrigger(PlayerData player, string plugin, string id,
      float x, float y, float z, float radius,
      string enterWindow = null, string exitWindow = null,
      string enterCommand = null, string exitCommand = null,
      bool mandatory = false, bool oneShot = false) =>
    PacketManager.SendPacket(player, ProximityPacket(plugin, id, x, y, z, radius,
      enterWindow, exitWindow, enterCommand, exitCommand, mandatory, oneShot, flat: false));

  /// <summary>Registers a 3D proximity trigger for every connected player. See <see cref="ProximityTrigger(PlayerData,string,string,float,float,float,float,string,string,string,string,bool,bool)"/>.</summary>
  public static void ProximityTriggerAll(string plugin, string id,
      float x, float y, float z, float radius,
      string enterWindow = null, string exitWindow = null,
      string enterCommand = null, string exitCommand = null,
      bool mandatory = false, bool oneShot = false) =>
    PacketManager.SendPacketToAll(ProximityPacket(plugin, id, x, y, z, radius,
      enterWindow, exitWindow, enterCommand, exitCommand, mandatory, oneShot, flat: false));

  /// <summary>Registers a 3D proximity trigger from a <see cref="float3"/> for one player.</summary>
  public static void ProximityTrigger(PlayerData player, string plugin, string id,
      float3 position, float radius,
      string enterWindow = null, string exitWindow = null,
      string enterCommand = null, string exitCommand = null,
      bool mandatory = false, bool oneShot = false) =>
    ProximityTrigger(player, plugin, id, position.x, position.y, position.z, radius,
      enterWindow, exitWindow, enterCommand, exitCommand, mandatory, oneShot);

  /// <summary>Registers a 3D proximity trigger from a <see cref="float3"/> for every connected player.</summary>
  public static void ProximityTriggerAll(string plugin, string id,
      float3 position, float radius,
      string enterWindow = null, string exitWindow = null,
      string enterCommand = null, string exitCommand = null,
      bool mandatory = false, bool oneShot = false) =>
    ProximityTriggerAll(plugin, id, position.x, position.y, position.z, radius,
      enterWindow, exitWindow, enterCommand, exitCommand, mandatory, oneShot);

  /// <summary>Registers a 2D (height-independent) proximity trigger for one player. The
  /// <paramref name="position"/> is (X, Z); the player's Y is ignored.</summary>
  public static void ProximityTrigger(PlayerData player, string plugin, string id,
      float2 position, float radius,
      string enterWindow = null, string exitWindow = null,
      string enterCommand = null, string exitCommand = null,
      bool mandatory = false, bool oneShot = false) =>
    PacketManager.SendPacket(player, ProximityPacket(plugin, id, position.x, 0f, position.y, radius,
      enterWindow, exitWindow, enterCommand, exitCommand, mandatory, oneShot, flat: true));

  /// <summary>Registers a 2D (height-independent) proximity trigger for every connected player. The
  /// <paramref name="position"/> is (X, Z); the player's Y is ignored.</summary>
  public static void ProximityTriggerAll(string plugin, string id,
      float2 position, float radius,
      string enterWindow = null, string exitWindow = null,
      string enterCommand = null, string exitCommand = null,
      bool mandatory = false, bool oneShot = false) =>
    PacketManager.SendPacketToAll(ProximityPacket(plugin, id, position.x, 0f, position.y, radius,
      enterWindow, exitWindow, enterCommand, exitCommand, mandatory, oneShot, flat: true));

  /// <summary>Removes a proximity trigger by id for one player (closing its window if shown).</summary>
  public static void RemoveProximityTrigger(PlayerData player, string plugin, string id) =>
    PacketManager.SendPacket(player, ProximityRemovePacket(plugin, id));

  /// <summary>Removes a proximity trigger by id for every connected player.</summary>
  public static void RemoveProximityTriggerAll(string plugin, string id) =>
    PacketManager.SendPacketToAll(ProximityRemovePacket(plugin, id));

  /// <summary>Removes every proximity trigger this plugin registered on one player's client.</summary>
  public static void ClearProximityTriggers(PlayerData player, string plugin) =>
    PacketManager.SendPacket(player, ProximityClearPacket(plugin));

  /// <summary>Removes every proximity trigger this plugin registered on all players' clients.</summary>
  public static void ClearProximityTriggersAll(string plugin) =>
    PacketManager.SendPacketToAll(ProximityClearPacket(plugin));

  static ScarletPacket ProximityPacket(string plugin, string id,
      float x, float y, float z, float radius,
      string enterWindow, string exitWindow, string enterCommand, string exitCommand,
      bool mandatory, bool oneShot, bool flat) {
    var d = new Dictionary<string, string> {
      ["id"] = id,
      ["wx"] = F(x), ["wy"] = F(y), ["wz"] = F(z),
      ["prd"] = F(radius),
    };
    if (!string.IsNullOrEmpty(enterWindow)) d["ew"] = enterWindow;
    if (!string.IsNullOrEmpty(exitWindow)) d["xw"] = exitWindow;
    if (!string.IsNullOrEmpty(enterCommand)) d["ec"] = enterCommand;
    if (!string.IsNullOrEmpty(exitCommand)) d["xc"] = exitCommand;
    if (mandatory) d["pmd"] = "1";
    if (oneShot) d["pos"] = "1";
    if (flat) d["p2"] = "1";
    return new ScarletPacket { Type = "PXB", Plugin = plugin, Window = "$prox", Data = d };
  }

  static ScarletPacket ProximityRemovePacket(string plugin, string id) =>
    new() { Type = "PXU", Plugin = plugin, Window = "$prox", Data = new() { ["id"] = id } };

  static ScarletPacket ProximityClearPacket(string plugin) =>
    new() { Type = "PXC", Plugin = plugin, Window = "$prox", Data = new() };

  // ── Unit proximity HUD ─────────────────────────────────────────────────────────
  //
  // Registers an entity-anchored HUD "bind" on the player's client: a radius plus a set of match
  // clauses, all client-side. Every entity within <paramref name="radius"/> world units that satisfies
  // the clauses gets a floating window that follows it (like the native nameplate).
  //
  // Two shapes of window drive the instance:
  //   * TEMPLATE — pass <paramref name="templateWindow"/> (the id of a window sent with
  //     <c>new Window(player, plugin, id){ Template = true, ... }.Send()</c>). The client reproduces
  //     that recipe once per matched entity, resolving these tokens at creation:
  //       {Name} {Level} {Hp} {HpMax} {Distance} {NetId} {PrefabGuid}
  //     Put {NetId} in a button's Command to carry the unit's identity back to the server.
  //     LIMITATION: tokens are resolved ONCE, at instance creation. A value that changes while the
  //     window is open (a live health bar) does not follow — cover that by pushing SendUpdate to the
  //     instance window, whose id is InterfaceManager.UnitHudWindowId(plugin, id, entity).
  //   * INSTANCE WINDOW — a bind with a <paramref name="net"/> clause and NO template: the server
  //     pushes a window straight to the deterministic instance id (see UnitHudWindowId). Until the
  //     server sends something to that id, the instance stays hidden — no empty default panel.
  //
  // Match clauses. All optional, combined with AND; each accepts a comma-separated any-of list and a
  // leading '!' to negate the whole clause. None is ever required — in particular PrefabGUID never is.
  //   net    (unt) — NetworkId list, each "index:generation" (identifies specific entities)
  //   prefab (upf) — PrefabGUID hash list
  //   buff   (ubf) — buff PrefabGUID list ("has the buff"; "!guid" for "must not have it")
  //   name   (unm) — substring of the entity name
  //   owned  (uow) — true = must have a living owner, false = must not
  //   team   (utm) — "ally" | "enemy" | "self"
  //
  // Display: show/showCount/hoverLinger, fade ("native"|"off"|"dist"), scale ("native"|"off"),
  // offsetY ("auto" or a world-unit Y offset), interactive (window receives raycast), priority
  // (int Z tie-break between binds). Defaults match the client; only non-defaults go on the wire.
  //
  // The bind is wholesale: sending a bind with the same <paramref name="id"/> replaces the previous
  // one. A radius <= 0 removes the bind (same as RemoveUnitHud). Ids are unique per plugin.

  /// <summary>Registers (or replaces) a Unit HUD bind on one player. See remarks above for the clauses and tokens.</summary>
  public static void UnitHud(PlayerData player, string plugin, string id, float radius,
      string templateWindow = null,
      string net = null, string prefab = null, string buff = null, string name = null,
      bool? owned = null, string team = null,
      UnitHudShow show = UnitHudShow.All, int showCount = 1, float hoverLinger = 0f,
      bool interactive = false, int priority = 0,
      string offsetY = null, string fade = null, string scale = null) =>
    PacketManager.SendPacket(player, UnitHudBindPacket(plugin, id, radius, templateWindow, net, prefab,
      buff, name, owned, team, show, showCount, hoverLinger, interactive, priority, offsetY, fade, scale));

  /// <summary>Registers (or replaces) a Unit HUD bind on every connected player. See <see cref="UnitHud"/>.</summary>
  public static void UnitHudAll(string plugin, string id, float radius,
      string templateWindow = null,
      string net = null, string prefab = null, string buff = null, string name = null,
      bool? owned = null, string team = null,
      UnitHudShow show = UnitHudShow.All, int showCount = 1, float hoverLinger = 0f,
      bool interactive = false, int priority = 0,
      string offsetY = null, string fade = null, string scale = null) =>
    PacketManager.SendPacketToAll(UnitHudBindPacket(plugin, id, radius, templateWindow, net, prefab,
      buff, name, owned, team, show, showCount, hoverLinger, interactive, priority, offsetY, fade, scale));

  /// <summary>Removes a single Unit HUD bind by id for one player, destroying its live instances.</summary>
  public static void RemoveUnitHud(PlayerData player, string plugin, string id) =>
    PacketManager.SendPacket(player, UnitHudUnbindPacket(plugin, id));

  /// <summary>Removes a single Unit HUD bind by id for every connected player.</summary>
  public static void RemoveUnitHudAll(string plugin, string id) =>
    PacketManager.SendPacketToAll(UnitHudUnbindPacket(plugin, id));

  /// <summary>Removes every Unit HUD bind this plugin registered on one player's client.</summary>
  public static void ClearUnitHuds(PlayerData player, string plugin) =>
    PacketManager.SendPacket(player, UnitHudClearPacket(plugin));

  /// <summary>Removes every Unit HUD bind this plugin registered on all players' clients.</summary>
  public static void ClearUnitHudsAll(string plugin) =>
    PacketManager.SendPacketToAll(UnitHudClearPacket(plugin));

  /// <summary>
  /// The deterministic window id of a bind's instance for one entity —
  /// <c>{plugin}:{bindId}#{netIndex}:{netGeneration}</c>. Push a window to this id (via
  /// <c>new Window(player, plugin, UnitHudWindowId(...))</c> or <c>Window.SendUpdate</c>) to fill a
  /// template-less bind's instance, or to live-update a template instance.
  /// </summary>
  public static string UnitHudWindowId(string plugin, string bindId, Entity entity) {
    var net = entity.Read<NetworkId>();
    return $"{plugin}:{bindId}#{net.Normal_Index}:{net.Normal_Generation}";
  }

  static ScarletPacket UnitHudBindPacket(string plugin, string id, float radius,
      string templateWindow, string net, string prefab, string buff, string name,
      bool? owned, string team, UnitHudShow show, int showCount, float hoverLinger,
      bool interactive, int priority, string offsetY, string fade, string scale) {
    var d = new Dictionary<string, string> {
      ["uid"] = id,
      ["urd"] = F(radius),
    };
    if (!string.IsNullOrEmpty(templateWindow)) d["uwn"] = templateWindow;
    if (!string.IsNullOrEmpty(net)) d["unt"] = net;
    if (!string.IsNullOrEmpty(prefab)) d["upf"] = prefab;
    if (!string.IsNullOrEmpty(buff)) d["ubf"] = buff;
    if (!string.IsNullOrEmpty(name)) d["unm"] = name;
    if (owned.HasValue) d["uow"] = owned.Value ? "1" : "0";
    if (!string.IsNullOrEmpty(team)) d["utm"] = team;
    var showToken = ShowToken(show, showCount);
    if (showToken != null) d["ush"] = showToken;
    if (hoverLinger > 0f) d["uhl"] = F(hoverLinger);
    if (interactive) d["uix"] = "1";
    if (priority != 0) d["upr"] = priority.ToString(CultureInfo.InvariantCulture);
    if (!string.IsNullOrEmpty(offsetY)) d["uoy"] = offsetY;
    if (!string.IsNullOrEmpty(fade)) d["ufd"] = fade;
    if (!string.IsNullOrEmpty(scale)) d["usc"] = scale;
    return new ScarletPacket { Type = "UHB", Plugin = plugin, Window = "", Data = d };
  }

  // All → null: "all" is the client default, so it is omitted from the wire.
  static string ShowToken(UnitHudShow show, int showCount) => show switch {
    UnitHudShow.Closest => showCount > 1 ? $"closest:{showCount}" : "closest",
    UnitHudShow.Hover => "hover",
    UnitHudShow.Click => "click",
    _ => null,
  };

  static ScarletPacket UnitHudUnbindPacket(string plugin, string id) =>
    new() { Type = "UHU", Plugin = plugin, Window = "", Data = new() { ["uid"] = id } };

  static ScarletPacket UnitHudClearPacket(string plugin) =>
    new() { Type = "UHC", Plugin = plugin, Window = "", Data = new() };

  // ── Nameplate background ───────────────────────────────────────────────────────
  //
  // Puts a background behind the game's OWN floating nameplate — the plate with the name, health bar
  // and level that a character carries over its head. Same match clauses as UnitHud, so this dresses
  // only the characters you choose; a character matching no bind keeps the vanilla plate untouched.
  //
  // There is no width or height. The client fits the background to the union of the plate parts that
  // are actually being drawn, every few frames, plus padding — the only workable rule, since a
  // nameplate's width is its name and its height depends on whether that character has a level badge
  // at all. Padding accepts negative values to pull the box in.
  //
  // Three layers, drawn in this order and freely combinable:
  //   * FILL   — background: a colour or a gradient, rounded by border's radius.
  //   * ART    — either NINE separate images (the corner/border parameters, exactly the key set
  //              Window.SetCustomTexture uses), or ONE image cut into a 9-slice automatically. The
  //              single-image form is the default for background images here, because this box
  //              changes size with every name it sits behind; pass ImageFit.Stretch to opt out.
  //   * BORDER — border: colour + width, drawn as a ring outside the fill.
  //
  // Sizing the art: a 9-slice draws its corners at their native pixel size, and UGUI shrinks borders
  // that do not fit the rect — so frame art authored at 1200 px does not merely look thick on a 50 px
  // plate, its corners collapse and it renders as a plain stretch. The client therefore scales the
  // slice to the box it measured, and `sliceScale` (0 = automatic) is the override for when you want
  // a specific thickness: it is how large one source pixel is drawn, so 0.1 is a tenth of native.
  // `slice` overrides where the source is cut ("" = auto, a third of the shorter side; or "24",
  // "12,20", "l,b,r,t"). For the nine-image form, `cornerSize` is the drawn corner size in pixels.
  //
  // Sign-horses (the invisible horses used as signs, whose nameplate name IS the sign text) are
  // spared by a bind that does not name them: to dress one, list its PrefabGUID in `prefab`.
  //
  // The bind is wholesale: the same `id` replaces the previous one, live plates included.

  /// <summary>Registers (or replaces) a nameplate-background bind on one player. See the remarks above.</summary>
  public static void NameplateBackground(PlayerData player, string plugin, string id,
      string prefab = null, string net = null, string buff = null, string name = null,
      bool? owned = null, string team = null, int priority = 0,
      UIBackground? background = null, Border? border = null, Spacing? padding = null,
      string slice = null, float sliceScale = 0f, UIColor? imageColor = null,
      string topLeftCorner = null, string topRightCorner = null,
      string bottomLeftCorner = null, string bottomRightCorner = null,
      string topBorder = null, string bottomBorder = null,
      string leftBorder = null, string rightBorder = null,
      int cornerSize = 32, int frameExpand = 0, bool tileArt = false) =>
    PacketManager.SendPacket(player, NameplateBindPacket(plugin, id, prefab, net, buff, name, owned,
      team, priority, background, border, padding, slice, sliceScale, imageColor,
      topLeftCorner, topRightCorner, bottomLeftCorner, bottomRightCorner,
      topBorder, bottomBorder, leftBorder, rightBorder, cornerSize, frameExpand, tileArt));

  /// <summary>Registers (or replaces) a nameplate-background bind on every connected player. See <see cref="NameplateBackground"/>.</summary>
  public static void NameplateBackgroundAll(string plugin, string id,
      string prefab = null, string net = null, string buff = null, string name = null,
      bool? owned = null, string team = null, int priority = 0,
      UIBackground? background = null, Border? border = null, Spacing? padding = null,
      string slice = null, float sliceScale = 0f, UIColor? imageColor = null,
      string topLeftCorner = null, string topRightCorner = null,
      string bottomLeftCorner = null, string bottomRightCorner = null,
      string topBorder = null, string bottomBorder = null,
      string leftBorder = null, string rightBorder = null,
      int cornerSize = 32, int frameExpand = 0, bool tileArt = false) =>
    PacketManager.SendPacketToAll(NameplateBindPacket(plugin, id, prefab, net, buff, name, owned,
      team, priority, background, border, padding, slice, sliceScale, imageColor,
      topLeftCorner, topRightCorner, bottomLeftCorner, bottomRightCorner,
      topBorder, bottomBorder, leftBorder, rightBorder, cornerSize, frameExpand, tileArt));

  /// <summary>Removes a single nameplate-background bind by id, taking its live backgrounds down.</summary>
  public static void RemoveNameplateBackground(PlayerData player, string plugin, string id) =>
    PacketManager.SendPacket(player, NameplateUnbindPacket(plugin, id));

  /// <summary>Removes a single nameplate-background bind by id for every connected player.</summary>
  public static void RemoveNameplateBackgroundAll(string plugin, string id) =>
    PacketManager.SendPacketToAll(NameplateUnbindPacket(plugin, id));

  /// <summary>Removes every nameplate-background bind this plugin registered on one player's client.</summary>
  public static void ClearNameplateBackgrounds(PlayerData player, string plugin) =>
    PacketManager.SendPacket(player, NameplateClearPacket(plugin));

  /// <summary>Removes every nameplate-background bind this plugin registered on all players' clients.</summary>
  public static void ClearNameplateBackgroundsAll(string plugin) =>
    PacketManager.SendPacketToAll(NameplateClearPacket(plugin));

  static ScarletPacket NameplateBindPacket(string plugin, string id,
      string prefab, string net, string buff, string name, bool? owned, string team, int priority,
      UIBackground? background, Border? border, Spacing? padding,
      string slice, float sliceScale, UIColor? imageColor,
      string tlCorner, string trCorner, string blCorner, string brCorner,
      string topBorder, string bottomBorder, string leftBorder, string rightBorder,
      int cornerSize, int frameExpand, bool tileArt) {
    var d = new Dictionary<string, string> { ["uid"] = id };

    // Clause tokens are the UnitHud ones, deliberately: the client reads one clause set for both.
    if (!string.IsNullOrEmpty(prefab)) d["upf"] = prefab;
    if (!string.IsNullOrEmpty(net)) d["unt"] = net;
    if (!string.IsNullOrEmpty(buff)) d["ubf"] = buff;
    if (!string.IsNullOrEmpty(name)) d["unm"] = name;
    if (owned.HasValue) d["uow"] = owned.Value ? "1" : "0";
    if (!string.IsNullOrEmpty(team)) d["utm"] = team;
    if (priority != 0) d["upr"] = priority.ToString(CultureInfo.InvariantCulture);

    // bcl/bgr/bim/bsp/bif — the same background tokens every element uses.
    if (background.HasValue && background.Value.HasValue) {
      background.Value.Apply(d);
      // Everywhere else Stretch is the client's default and so is omitted from the wire. Here it is
      // not: a nameplate background resizes with every name it sits behind, so the client slices art
      // by default. The fit is therefore always spelled out, and asking for Stretch means Stretch.
      if ((d.ContainsKey("bim") || d.ContainsKey("bsp")) && !d.ContainsKey("bif")) d["bif"] = "Stretch";
    }
    if (border.HasValue) {
      d["dc"] = border.Value.Color;
      d["dw"] = F(border.Value.Width);
      d["dr"] = F(border.Value.Radius);
    }
    if (padding.HasValue) {
      d["pt"] = F(padding.Value.Top);
      d["pr"] = F(padding.Value.Right);
      d["pb"] = F(padding.Value.Bottom);
      d["pl"] = F(padding.Value.Left);
    }
    if (!string.IsNullOrEmpty(slice)) d["bsl"] = slice;
    if (sliceScale > 0f) d["bss"] = F(sliceScale);
    if (imageColor.HasValue) d["bic"] = imageColor.Value;

    // Nine-image frame. Only sent when at least one piece is, so the client can tell a frame from an
    // ordinary background image by the payload alone.
    bool hasFrame = tlCorner != null || trCorner != null || blCorner != null || brCorner != null
                 || topBorder != null || bottomBorder != null || leftBorder != null || rightBorder != null;
    if (hasFrame) {
      if (tlCorner != null) d["t1"] = tlCorner;
      if (trCorner != null) d["t2"] = trCorner;
      if (blCorner != null) d["b1"] = blCorner;
      if (brCorner != null) d["b2"] = brCorner;
      if (topBorder != null) d["tb"] = topBorder;
      if (bottomBorder != null) d["bb"] = bottomBorder;
      if (leftBorder != null) d["lb"] = leftBorder;
      if (rightBorder != null) d["rb"] = rightBorder;
      d["cs"] = cornerSize.ToString(CultureInfo.InvariantCulture);
      if (frameExpand != 0) d["fe"] = frameExpand.ToString(CultureInfo.InvariantCulture);
      if (tileArt) d["br"] = "1";
    }

    return new ScarletPacket { Type = "NPB", Plugin = plugin, Window = "", Data = d };
  }

  static ScarletPacket NameplateUnbindPacket(string plugin, string id) =>
    new() { Type = "NPU", Plugin = plugin, Window = "", Data = new() { ["uid"] = id } };

  static ScarletPacket NameplateClearPacket(string plugin) =>
    new() { Type = "NPC", Plugin = plugin, Window = "", Data = new() };
}
