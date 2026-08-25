using System;
using System.Collections.Generic;
using System.Linq;
using ScarletCore.Services;
using ScarletCore.Utils;

namespace ScarletCore.Interface;

/// <summary>
/// Server-side mirror of which windows each player currently has open on their client.
/// <para>
/// The client is the single source of truth: it emits a tiny <c>[[SIWO]]/[[SIWC]]</c> chat token on
/// every real open/close transition (see the client's <c>ScarletWindow</c>). This service keeps a
/// per-player set keyed by (plugin, window) so mods can skip pushing data to a window the player
/// isn't looking at — query via <see cref="InterfaceManager.IsWindowOpen(PlayerData,string,string)"/>.
/// </para>
/// <para>
/// No desync gaps by construction: the chat channel is reliable and ordered, so no token is lost or
/// reordered within a session; the set is cleared on (re)auth and disconnect (<see cref="Clear"/>),
/// and the client re-emits opens as it rebuilds its UI after a reconnect. A close that races an
/// in-flight update is harmless — the client already drops updates to a non-visible window.
/// </para>
/// </summary>
internal static class WindowStateService {
  readonly record struct Key(string Plugin, string Window);

  // PlatformId → set of open (plugin, window) pairs.
  static readonly Dictionary<ulong, HashSet<Key>> _open = [];

  internal static event Action<PlayerData, string, string> Opened;
  internal static event Action<PlayerData, string, string> Closed;

  /// <summary>Records a window as open. Raises <see cref="Opened"/> only on a real transition.</summary>
  internal static void HandleOpen(PlayerData player, string[] args) {
    if (!TryParse(args, out var plugin, out var window)) return;
    if (!_open.TryGetValue(player.PlatformId, out var set))
      _open[player.PlatformId] = set = [];
    if (!set.Add(new Key(plugin, window))) return; // already open — no duplicate event
    Raise(Opened, player, plugin, window);
  }

  /// <summary>Records a window as closed. Raises <see cref="Closed"/> only on a real transition.</summary>
  internal static void HandleClose(PlayerData player, string[] args) {
    if (!TryParse(args, out var plugin, out var window)) return;
    if (!_open.TryGetValue(player.PlatformId, out var set)) return;
    if (!set.Remove(new Key(plugin, window))) return; // wasn't open — no event
    if (set.Count == 0) _open.Remove(player.PlatformId);
    Raise(Closed, player, plugin, window);
  }

  /// <summary>Drops all tracked state for a player. Called on (re)auth and on disconnect.</summary>
  internal static void Clear(ulong platformId) => _open.Remove(platformId);

  internal static bool IsOpen(ulong platformId, string plugin, string window) =>
    _open.TryGetValue(platformId, out var set) && set.Contains(new Key(plugin, window));

  internal static bool IsOpenAnyPlugin(ulong platformId, string window) =>
    _open.TryGetValue(platformId, out var set) && set.Any(k => k.Window == window);

  internal static List<(string Plugin, string Window)> GetOpen(ulong platformId) =>
    _open.TryGetValue(platformId, out var set)
      ? set.Select(k => (k.Plugin, k.Window)).ToList()
      : [];

  // Wire format: "<plugin> <window...>". Plugin identifiers never contain spaces, so args[0] is the
  // plugin and the remainder (rejoined) is the window id — which may legitimately contain spaces.
  static bool TryParse(string[] args, out string plugin, out string window) {
    plugin = window = null;
    if (args == null || args.Length < 2 || string.IsNullOrEmpty(args[0])) return false;
    plugin = args[0];
    window = args.Length == 2 ? args[1] : string.Join(" ", args.Skip(1));
    return !string.IsNullOrEmpty(window);
  }

  static void Raise(Action<PlayerData, string, string> ev, PlayerData player, string plugin, string window) {
    try { ev?.Invoke(player, plugin, window); }
    catch (Exception ex) { Log.Error($"[ScarletInterface] Window-state event handler error: {ex}"); }
  }
}
