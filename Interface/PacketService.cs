using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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
  const string BATCH_PREFIX = "[[SCARLET_BATCH]]";
  const string COMPRESSED_PREFIX = "[[SCARLET_Z]]";
  const int MAX_MESSAGE_LEN = 480;
  // FixedString512Bytes holds at most ~510 bytes of content (512 − 2-byte length prefix).
  // Chunk header [[SCARLET_CHUNK:{id}:{i}/{total}]] grows with server uptime; budget 40 chars.
  // CHUNK_PAYLOAD = MAX_MESSAGE_LEN − header_budget → full chunk always ≤ MAX_MESSAGE_LEN.
  const int CHUNK_HEADER_BUDGET = 40;
  const int CHUNK_PAYLOAD = MAX_MESSAGE_LEN - CHUNK_HEADER_BUDGET; // 440
  static int _chunkIdCounter = 0;
  // Invisible zero-width chars sent by the client on init to announce presence.
  // Renders as blank if it ever leaks into visible chat.
  const string HELLO_TOKEN = "\u200B\u200C\u200D";
  // Minimum JSON length (chars) before attempting DEFLATE compression.
  const int COMPRESSION_THRESHOLD = 256;
  // Rate limit for player-initiated commands (chat commands + button commands).
  // Applied once per command at the entry point — all resulting sends are unconditionally delivered.
  const double CMD_RATE = 5.0;

  // Simple token-bucket rate limiter. One instance per player per category.
  sealed class TokenBucket {
    double _tokens;
    long _lastTick;
    readonly double _rate;
    readonly double _capacity;

    internal TokenBucket(double rate) {
      _rate = rate;
      _capacity = rate;
      _tokens = rate; // start full so first send is never blocked
      _lastTick = Environment.TickCount64;
    }

    // Returns true and consumes one token if the bucket is non-empty, false otherwise.
    internal bool TryConsume() {
      var now = Environment.TickCount64;
      var elapsed = (now - _lastTick) / 1000.0; // ms → seconds
      _lastTick = now;
      _tokens = Math.Min(_capacity, _tokens + elapsed * _rate);
      if (_tokens < 1.0) return false;
      _tokens -= 1.0;
      return true;
    }
  }

  static readonly JsonSerializerOptions _jsonOptions = new() {
    PropertyNamingPolicy = null,
  };

  // Packets queued for players that haven't authenticated yet.
  static readonly Dictionary<ulong, List<string>> _pendingPackets = [];

  // Timestamp of the last handshake sent to each player.
  static readonly Dictionary<ulong, DateTime> _handshakeSentAt = [];

  // Token buckets for player-initiated command requests — 5 req/s per player.
  static readonly Dictionary<ulong, TokenBucket> _cmdBuckets = [];

  // Hash of the last serialized window batch per "platformId:plugin:windowId" key. (Phase 4)
  static readonly Dictionary<string, int> _snapshotHashes = [];

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

    // Remove role and pending packets when the player disconnects.
    EventManager.On(PlayerEvents.PlayerLeft, Deauth);
  }

  /// <summary>Records the join timestamp for pending-packet queueing. Client will initiate the handshake when ready.</summary>
  /// <param name="player">The player that joined.</param>
  [EventPriority(EventPriority.First)]
  public static void Auth(PlayerData player) {
    _handshakeSentAt[player.PlatformId] = DateTime.UtcNow;
  }

  /// <summary>Removes pending packets, handshake state, and the interface role from a disconnecting player.</summary>
  /// <param name="player">The player to deauthenticate.</param>
  [EventPriority(EventPriority.First)]
  public static void Deauth(PlayerData player) {
    _pendingPackets.Remove(player.PlatformId);
    _handshakeSentAt.Remove(player.PlatformId);
    player.RemoveRole("interface-user");
    // Clear per-player rate limit bucket and snapshot hashes so the next session starts fresh.
    _cmdBuckets.Remove(player.PlatformId);
    var prefix = player.PlatformId + ":";
    foreach (var key in _snapshotHashes.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
      _snapshotHashes.Remove(key);
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

  // ── Phase 3: DEFLATE + Base64 compression ───────────────────────────────────────────────────
  // Compresses a JSON string with DEFLATE and encodes as Base64.
  // Returns null when the compressed form is not shorter than the original.
  static string TryCompress(string json) {
    if (json.Length < COMPRESSION_THRESHOLD) return null;
    var bytes = Encoding.UTF8.GetBytes(json);
    using var ms = new MemoryStream();
    using (var ds = new DeflateStream(ms, CompressionLevel.Fastest))
      ds.Write(bytes, 0, bytes.Length);
    var b64 = Convert.ToBase64String(ms.ToArray());
    return b64.Length < json.Length ? b64 : null;
  }

  // Serialises a batch JSON array and delivers it (compressed if beneficial) to one player.
  static void SendBatch(PlayerData player, string batchJson) {
    var compressed = TryCompress(batchJson);
    var raw = compressed != null ? COMPRESSED_PREFIX + compressed : BATCH_PREFIX + batchJson;
    if (!HasInterface(player)) {
      QueueForPendingAuth(player, raw);
      return;
    }
    SendRaw(player, raw);
  }

  // Broadcasts a batch to all connected interface players.
  static void SendBatchToAll(string batchJson) {
    var compressed = TryCompress(batchJson);
    var raw = compressed != null ? COMPRESSED_PREFIX + compressed : BATCH_PREFIX + batchJson;
    foreach (var player in PlayerService.GetAllConnected().Where(HasInterface))
      SendRaw(player, raw);
  }

  // ── Phase 1+2+4: Full-window send with rate limiting, batching, and snapshot dedup ──────────
  /// <summary>
  /// Sends a serialized window batch to a specific player.
  /// Applies a per-(player,plugin,window) rate limit, DEFLATE+Base64 compression,
  /// and skips the full payload when the window content is unchanged (snapshot hash match).
  /// </summary>
  /// <param name="player">Target player.</param>
  /// <param name="plugin">Plugin identifier.</param>
  /// <param name="windowId">Window identifier.</param>
  /// <param name="packets">Element packets produced by ElementSerializer (action packet not yet added).</param>
  /// <param name="actionToken">Short action type token ("OP", "CL", "CR", "RS") or null for none.</param>
  public static void SendWindow(PlayerData player, string plugin, string windowId, List<ScarletPacket> packets, string actionToken) {
    var key = $"{player.PlatformId}:{plugin}:{windowId}";

    // No rate limit here — limiting happens at command entry (OnBeforeExecute / OnChatMessage).
    // Server-initiated sends (level-up, join events, etc.) are always delivered in full.

    // Append the action packet.
    if (actionToken != null) {
      packets.Add(new ScarletPacket { Type = actionToken, Plugin = plugin, Window = windowId, Data = [] });
    }

    var batchJson = JsonSerializer.Serialize(packets, _jsonOptions);

    // Phase 4: Snapshot hash dedup — only for Open ("OP") so Close/Clear/Reset always execute.
    if (actionToken == "OP") {
      var newHash = batchJson.GetHashCode();
      if (_snapshotHashes.TryGetValue(key, out var cachedHash) && cachedHash == newHash) {
        // Content unchanged — send only the Open action so the client shows the existing window.
        var openPacket = new ScarletPacket { Type = "OP", Plugin = plugin, Window = windowId, Data = [] };
        SendPacket(player, openPacket);
        return;
      }
      _snapshotHashes[key] = newHash;
    }

    // Phase 2+3: Deliver as a single batch (with optional compression).
    SendBatch(player, batchJson);
  }

  /// <summary>Broadcasts a full window to all connected interface players (no rate limit or snapshot dedup).</summary>
  public static void SendWindowToAll(string plugin, string windowId, List<ScarletPacket> packets, string actionToken) {
    if (actionToken != null)
      packets.Add(new ScarletPacket { Type = actionToken, Plugin = plugin, Window = windowId, Data = [] });
    SendBatchToAll(JsonSerializer.Serialize(packets, _jsonOptions));
  }

  /// <summary>Sends a ScarletPacket to a specific player.</summary>
  /// <param name="player">The target player. If not yet authenticated, the packet is queued.</param>
  /// <param name="packet">The packet to send.</param>
  public static void SendPacket(PlayerData player, ScarletPacket packet) {
    // No rate limit here — limiting happens at command entry (OnBeforeExecute / OnChatMessage).
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
    foreach (var player in PlayerService.GetAllConnected().Where(HasInterface))
      SendRaw(player, raw);
  }

  // Sends a raw message to the player, splitting into chunks if it exceeds MAX_MESSAGE_LEN.
  // The full raw string (including whatever prefix it starts with) is embedded in the chunk
  // payloads so the client can reconstruct any prefix type without guessing.
  static void SendRaw(PlayerData player, string raw) {
    if (raw.Length <= MAX_MESSAGE_LEN) {
      Deliver(player, raw);
      return;
    }
    var id = (++_chunkIdCounter).ToString();
    int total = (int)Math.Ceiling((double)raw.Length / CHUNK_PAYLOAD);
    for (int i = 0; i < total; i++) {
      int start = i * CHUNK_PAYLOAD;
      int len = Math.Min(CHUNK_PAYLOAD, raw.Length - start);
      Deliver(player, $"{CHUNK_PREFIX}{id}:{i}/{total}]]{raw.Substring(start, len)}");
    }
  }

  static void Deliver(PlayerData player, string text) {
    var user = player.User;
    ChatMessageServerEvent eventData = new() {
      MessageText = new FixedString512Bytes(text),
      TimeUTC = DateTime.UtcNow.Ticks,
      FromUser = NetworkId.Empty,
      FromCharacter = NetworkId.Empty,
      MessageType = ServerChatMessageType.Region,
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
          // System protocol messages (handshake, sync) are never rate-limited.
          // User-facing raw handlers (custom button commands, etc.) consume a command token.
          bool isSystemToken = prefix == HELLO_TOKEN || prefix == "[[SCARLET_SYNC]]";
          if (!isSystemToken && !TryConsumeCommandToken(player)) continue;
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

  // Consumes one command token for the player. Returns false if the player is rate-limited.
  static bool TryConsumeCommandToken(PlayerData player) {
    if (!_cmdBuckets.TryGetValue(player.PlatformId, out var bucket)) {
      bucket = new TokenBucket(CMD_RATE);
      _cmdBuckets[player.PlatformId] = bucket;
    }
    return bucket.TryConsume();
  }

  [EventPriority(EventPriority.High)]
  static void OnBeforeExecute(PlayerData player, CommandInfo commandInfo, string[] args) {
    if (_cmdHandlers.Count == 0) return;
    if (!_cmdHandlers.TryGetValue(commandInfo.Name, out var handlers)) return;
    if (!TryConsumeCommandToken(player)) {
      commandInfo.CancelExecution = true;
      return;
    }
    foreach (var handler in handlers) {
      try { handler(player, args); } catch (Exception ex) { Log.Error($"[ScarletInterface.API] OnCommand handler error: {ex}"); }
    }
  }
}
