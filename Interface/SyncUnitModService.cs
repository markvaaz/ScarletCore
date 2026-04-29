using ProjectM;
using ScarletCore.Services;
using ScarletCore.Interface.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ScarletCore.Interface;

/// <summary>
/// Reads <see cref="ModifyUnitStatBuff_DOTS"/> entries from a player's active buffs
/// and sends them to the ScarletInterface client for stat display synchronization.
/// </summary>
internal static class SyncUnitModService {
  static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = null };
  // Minimum interval between sends per player (seconds)
  const double COOLDOWN = 1.0;
  static readonly Dictionary<ulong, DateTime> _lastSent = [];

  /// <summary>Reads all <see cref="ModifyUnitStatBuff_DOTS"/> entries from the player's buffs and sends them to the client.
  /// Calls are throttled to once per second per player.</summary>
  /// <param name="player">The player whose buff stats will be collected and sent.</param>
  public static void SendUnitMods(PlayerData player) {
    // Temporarily disabled.
    return;
    // if (_lastSent.TryGetValue(player.PlatformId, out var last) && (now - last).TotalSeconds < COOLDOWN)
    //   return;
    // _lastSent[player.PlatformId] = now;
    // var mods = new List<UnitModEntry>();
    // var buffBuffer = player.CharacterEntity.ReadBuffer<BuffBuffer>();

    // foreach (var buff in buffBuffer) {
    //   if (!buff.Entity.Exists() || !buff.Entity.Has<ModifyUnitStatBuff_DOTS>()) continue;

    //   var dotBuff = buff.Entity.ReadBuffer<ModifyUnitStatBuff_DOTS>();

    //   foreach (var dot in dotBuff) {
    //     mods.Add(new UnitModEntry(
    //       (int)dot.StatType,
    //       dot.Value,
    //       (int)dot.ModificationType,
    //       dot.AttributeCapType == AttributeCapType.Uncapped
    //     ));
    //   }
    // }

    // PacketManager.SendPacket(player, new ScarletPacket {
    //   Type = "SyncUnitMods",
    //   Plugin = "scarlet-interface",
    //   Window = "__sync__",
    //   Data = new() { { "Mods", JsonSerializer.Serialize(mods, _jsonOptions) } }
    // });
  }
}