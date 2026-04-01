using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ProjectM.Network;
using ScarletCore.Commanding;
using ScarletCore.Events;
using ScarletCore.Localization;
using ScarletCore.Services;
using ScarletCore.Systems;
using ScarletCore.Interface.Models;
using Unity.Collections;
using Unity.Entities;
using ScarletCore.Utils;

namespace ScarletCore.Interface;

/// <summary>
/// Core service responsible for serializing, chunking, and delivering UI packets
/// to players via the in-game chat channel. Handles client handshake, authentication,
/// and message routing between server plugins and the ScarletInterface client mod.
/// </summary>
internal static class PacketManager {
  const string PREFIX = "[[SCARLET]]";
  const string CHUNK_PREFIX = "[[SCARLET_CHUNK:";
  const int MAX_MESSAGE_LEN = 512;
  // Each chunk payload: 512 minus worst-case header [[SCARLET_CHUNK:9999:999/999]] (30 chars)
  const int CHUNK_PAYLOAD = 480;
  static int _chunkIdCounter = 0;
  // Invisible zero-width chars sent by the client on init to announce presence.
  // Renders as blank if it ever leaks into visible chat.
  const string HELLO_TOKEN = "\u200B\u200C\u200D";

  static readonly JsonSerializerOptions _jsonOptions = new() {
    PropertyNamingPolicy = null,
  };

  // Packets queued for players that haven't authenticated yet.
  static readonly Dictionary<ulong, List<string>> _pendingPackets = [];

  // Timestamp of the last handshake sent to each player.
  static readonly Dictionary<ulong, DateTime> _handshakeSentAt = [];

  /// <summary>Returns true if the player has the ScarletInterface client mod active.</summary>
  /// <param name="player">The player to check.</param>
  public static bool HasInterface(PlayerData player) =>
    player != null && player.HasRole("interface-user");

  // Maps V Rising client language file names to ScarletCore Language enum values.
  static readonly Dictionary<string, Language> _languageMap = new(StringComparer.OrdinalIgnoreCase) {
    ["English"] = Language.English,
    ["Brazilian"] = Language.Portuguese,
    ["French"] = Language.French,
    ["Latam"] = Language.Latam,
    ["German"] = Language.German,
    ["Hungarian"] = Language.Hungarian,
    ["Italian"] = Language.Italian,
    ["Japanese"] = Language.Japanese,
    ["Koreana"] = Language.Korean,
    ["Polish"] = Language.Polish,
    ["Russian"] = Language.Russian,
    ["Spanish"] = Language.Spanish,
    ["SChinese"] = Language.ChineseSimplified,
    ["TChinese"] = Language.ChineseTraditional,
    ["Thai"] = Language.Thai,
    ["Turkish"] = Language.Turkish,
    ["Ukrainian"] = Language.Ukrainian,
    ["Vietnamese"] = Language.Vietnamese,
  };

  // Registered raw-chat callback handlers: cmd prefix → list of handlers
  static readonly Dictionary<string, List<Action<PlayerData, string[]>>> _rawHandlers = [];

  // Registered command callback handlers: cmd name → list of handlers
  static readonly Dictionary<string, List<Action<PlayerData, string[]>>> _cmdHandlers = [];

  /// <summary>
  /// Initializes the packet service: registers chat and command listeners, sets up
  /// the client handshake flow, and sends a hello token to all currently online players.
  /// </summary>
  public static void Initialize() {
    EventManager.On(PrefixEvents.OnChatMessage, OnChatMessage);
    EventManager.On(CommandEvents.OnBeforeExecute, OnBeforeExecute);

    // Send a handshake ping when a player joins so the client can respond.
    // HELLO_TOKEN is invisible in chat, so it won't confuse players without the interface.
    EventManager.On(PlayerEvents.PlayerJoined, Auth);

    // Register hello-token handler: client responds to handshake with HELLO_TOKEN + optional language.
    OnMessage(HELLO_TOKEN, (player, args) => {
      if (args.Length > 0 && _languageMap.TryGetValue(args[0], out var lang)) {
        Localizer.SetPlayerLanguage(player, lang);
      }

      if (!player.HasRole("interface-user")) {
        if (!RoleService.RoleExists("interface-user"))
          RoleService.CreateRole("interface-user");

        RoleService.AddPermissionToRole("interface-user", "interface.access");
        player.AddRole("interface-user");
        FlushPendingPackets(player);
        SyncUnitModService.SendUnitMods(player);
        EventManager.Emit(PlayerEvents.InterfaceAuth, player);
      } else {
        // Player reconnected without a clean disconnect (e.g. server restart, abrupt drop).
        // The client-side UIManager is always reset on disconnect, so we must always
        // rebuild the UI — fire InterfaceAuth so server plugins send their window setup.
        FlushPendingPackets(player);
        SyncUnitModService.SendUnitMods(player);
        EventManager.Emit(PlayerEvents.InterfaceAuth, player);
      }
    });

    // When the client opens the inventory, it sends this token to request fresh stat data.
    OnMessage("[[SCARLET_SYNC]]", (player, _) => {
      if (HasInterface(player))
        SyncUnitModService.SendUnitMods(player);
    });

    var onlinePlayers = PlayerService.GetAllConnected();

    foreach (var player in onlinePlayers) {
      Deliver(player, HELLO_TOKEN);
    }

    // Remove role and pending packets when the player disconnects.
    EventManager.On(PlayerEvents.PlayerLeft, Deauth);
  }

  /// <summary>Sends a handshake token to the player and records the timestamp for pending-packet queueing.</summary>
  /// <param name="player">The player to authenticate.</param>
  [EventPriority(EventPriority.First)]
  public static void Auth(PlayerData player) {
    _handshakeSentAt[player.PlatformId] = DateTime.UtcNow;
    Deliver(player, HELLO_TOKEN);
  }

  /// <summary>Removes pending packets, handshake state, and the interface role from a disconnecting player.</summary>
  /// <param name="player">The player to deauthenticate.</param>
  [EventPriority(EventPriority.First)]
  public static void Deauth(PlayerData player) {
    _pendingPackets.Remove(player.PlatformId);
    _handshakeSentAt.Remove(player.PlatformId);
    player.RemoveRole("interface-user");
  }

  static void FlushPendingPackets(PlayerData player) {
    if (!_pendingPackets.TryGetValue(player.PlatformId, out var queue)) return;
    _pendingPackets.Remove(player.PlatformId);
    foreach (var raw in queue)
      SendRaw(player, raw);
  }

  static void QueueForPendingAuth(PlayerData player, string raw) {
    // Only queue if the handshake was sent less than 1 minute ago.
    if (!_handshakeSentAt.TryGetValue(player.PlatformId, out var sentAt) ||
        (DateTime.UtcNow - sentAt).TotalMinutes >= 1)
      return;

    if (!_pendingPackets.TryGetValue(player.PlatformId, out var queue)) {
      queue = [];
      _pendingPackets[player.PlatformId] = queue;
      // Expire after 10s if the player never authenticates.
      ActionScheduler.Delayed(() => {
        _pendingPackets.Remove(player.PlatformId);
      }, 10);
    }
    queue.Add(raw);
  }

  /// <summary>Sends a ScarletPacket to a specific player.</summary>
  /// <param name="player">The target player. If not yet authenticated, the packet is queued.</param>
  /// <param name="packet">The packet to send.</param>
  public static void SendPacket(PlayerData player, ScarletPacket packet) {
    var raw = PREFIX + JsonSerializer.Serialize(packet, _jsonOptions);
    if (!HasInterface(player)) {
      QueueForPendingAuth(player, raw);
      return;
    }
    SendRaw(player, raw);
  }

  /// <summary>Sends a ScarletPacket to all connected players that have ScarletInterface installed.</summary>
  /// <param name="packet">The packet to broadcast.</param>
  public static void SendPacketToAll(ScarletPacket packet) {
    var raw = PREFIX + JsonSerializer.Serialize(packet, _jsonOptions);
    foreach (var player in PlayerService.AllPlayers.Where(p => p.IsOnline && HasInterface(p)))
      SendRaw(player, raw);
  }

  // Sends a raw message to the player, splitting into chunks if it exceeds MAX_MESSAGE_LEN.
  static void SendRaw(PlayerData player, string raw) {
    if (raw.Length <= MAX_MESSAGE_LEN) {
      Deliver(player, raw);
      return;
    }
    var json = raw[PREFIX.Length..];
    var id = (++_chunkIdCounter).ToString();
    int total = (int)Math.Ceiling((double)json.Length / CHUNK_PAYLOAD);
    for (int i = 0; i < total; i++) {
      int start = i * CHUNK_PAYLOAD;
      int len = Math.Min(CHUNK_PAYLOAD, json.Length - start);
      Deliver(player, $"{CHUNK_PREFIX}{id}:{i}/{total}]]{json.Substring(start, len)}");
    }
  }

  static void Deliver(PlayerData player, string text) {
    var user = player.User;
    ChatMessageServerEvent eventData = new() {
      MessageText = new FixedString512Bytes(text),
      TimeUTC = DateTime.UtcNow.Ticks,
      FromUser = NetworkId.Empty,
      FromCharacter = NetworkId.Empty,
      MessageType = ServerChatMessageType.Lore,
    };
    NetworkEvents.SendEvent(GameSystems.EntityManager, eventData, ref user);
  }

  /// <summary>
  /// Registers a callback invoked when a player sends a raw chat message matching the given prefix.
  /// Useful for handling button commands that don't use the ScarletCore command system (no '.' prefix).
  /// </summary>
  /// <param name="prefix">The exact string prefix to match at the start of the chat message.</param>
  /// <param name="handler">Callback receiving the player and any space-separated arguments after the prefix.</param>
  public static void OnMessage(string prefix, Action<PlayerData, string[]> handler) {
    if (!_rawHandlers.TryGetValue(prefix, out var list)) {
      _rawHandlers[prefix] = list = [];
    }
    list.Add(handler);
  }

  /// <summary>
  /// Registers a callback invoked when a player runs a ScarletCore command matching the given name.
  /// </summary>
  /// <param name="commandName">The command name to listen for (with or without the leading dot).</param>
  /// <param name="handler">Callback receiving the player and the command arguments.</param>
  public static void OnCommand(string commandName, Action<PlayerData, string[]> handler) {
    var key = commandName.TrimStart('.');
    if (!_cmdHandlers.TryGetValue(key, out var list)) {
      _cmdHandlers[key] = list = [];
    }
    list.Add(handler);
  }

  [EventPriority(EventPriority.High)]
  static void OnChatMessage(NativeArray<Entity> entities) {
    if (_rawHandlers.Count == 0) return;
    try {
      foreach (var entity in entities) {
        if (!entity.Exists()) continue;
        var messageEvent = entity.Read<ChatMessageEvent>();
        var text = messageEvent.MessageText.Value;
        if (string.IsNullOrEmpty(text)) continue;

        var fromChar = entity.Read<FromCharacter>().Character;
        if (!fromChar.TryGetPlayerData(out var player)) continue;

        foreach (var (prefix, handlers) in _rawHandlers) {
          if (!text.StartsWith(prefix, StringComparison.Ordinal)) continue;
          var rest = prefix.Length < text.Length ? text.Substring(prefix.Length).Trim() : string.Empty;
          var args = rest.Length > 0 ? rest.Split(' ', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
          foreach (var handler in handlers) {
            try { handler(player, args); } catch (Exception ex) { Log.Error($"[ScarletInterface.API] OnMessage handler error: {ex}"); }
          }
        }
      }
    } catch (Exception ex) {
      Log.Error($"[ScarletInterface.API] Error in OnChatMessage: {ex}");
    }
  }

  [EventPriority(EventPriority.High)]
  static void OnBeforeExecute(PlayerData player, CommandInfo commandInfo, string[] args) {
    if (_cmdHandlers.Count == 0) return;
    if (!_cmdHandlers.TryGetValue(commandInfo.Name, out var handlers)) return;
    foreach (var handler in handlers) {
      try { handler(player, args); } catch (Exception ex) { Log.Error($"[ScarletInterface.API] OnCommand handler error: {ex}"); }
    }
  }
}
