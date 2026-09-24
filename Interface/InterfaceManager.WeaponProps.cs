using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using ScarletCore.Interface.Models;
using ScarletCore.Services;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Entities;

namespace ScarletCore.Interface;

public static partial class InterfaceManager {

  // ── Props by weapon ─────────────────────────────────────────────────────────────
  //
  // What a character holds in place of the weapon it has equipped. The looks of a weapon go to every
  // client once (retained for later connections), and each client matches them against the weapon it
  // sees in each character's hand — the equipped prefab is replicated for everyone — so equipping,
  // swapping or dropping a weapon costs no packet at all.
  //
  // Conditions are the exception. Level, durability and custom tests read the item, and another
  // player's item never reaches a client; so for a weapon with any conditional variant the server
  // evaluates the equipped item every frame and tells the clients which variant each
  // character shows — only when that answer changes.
  //
  // Clients rank a weapon's look below any buff prop: a tool buff or an interaction's prop takes the
  // hand while it lasts.

  /// <summary>Variant lists per plugin and weapon, in the order they were set; the latest set of a
  /// weapon, by whichever plugin, is the one in effect.</summary>
  static readonly List<(string Plugin, int Weapon, WeaponProp[] Variants)> _weaponProps = new();

  /// <summary>The config packets of each weapon (one per variant), re-sent to clients that connect later.</summary>
  static readonly Dictionary<int, List<ScarletPacket>> _weaponConfigPackets = new();

  /// <summary>For weapons with conditions: the variant each character was last told to show (−1 = the
  /// weapon itself), and its NetworkId in wire form.</summary>
  static readonly Dictionary<Entity, (string Net, int Weapon, int Index)> _weaponPicks = new();

  /// <summary>Pick packets per character NetworkId, re-sent to clients that connect later.</summary>
  static readonly Dictionary<string, ScarletPacket> _weaponPickPackets = new();

  static bool _weaponPropTaskStarted;

  // Equipped items of players holding a weapon with conditions are read every server frame: until the
  // answer for a newly equipped weapon arrives, clients keep it hidden with nothing in its place, so the
  // sooner it goes out the shorter that moment of empty hands. One component read per player.

  /// <summary>
  /// Sets the looks of <paramref name="weaponGuid"/> for every player who equips it, on every client,
  /// now and for anyone who connects later. The first variant whose conditions hold is shown in place
  /// of the weapon (see <see cref="WeaponProp"/>); none holding leaves the weapon as it is. Once set,
  /// nothing else is needed: whoever has the weapon in hand shows its look. Call again to replace the
  /// variants — characters holding it switch on the spot — or with none to remove them.
  /// </summary>
  public static void SetWeaponProps(string plugin, int weaponGuid, params WeaponProp[] variants) {
    if (weaponGuid == 0) throw new ArgumentException("A weapon prop needs a weapon.", nameof(weaponGuid));
    _weaponProps.RemoveAll(e => e.Plugin == plugin && e.Weapon == weaponGuid);
    variants = variants?.Where(v => v?.Prop != null).ToArray() ?? [];
    if (variants.Length > 0) _weaponProps.Add((plugin, weaponGuid, variants));
    PublishWeapon(weaponGuid);
  }

  /// <inheritdoc cref="SetWeaponProps(string, int, WeaponProp[])"/>
  public static void SetWeaponProps(string plugin, PrefabGUID weapon, params WeaponProp[] variants) =>
    SetWeaponProps(plugin, weapon.GuidHash, variants);

  /// <summary>Removes every weapon look this plugin set.</summary>
  public static void ClearWeaponProps(string plugin) {
    var weapons = _weaponProps.Where(e => e.Plugin == plugin).Select(e => e.Weapon).Distinct().ToList();
    _weaponProps.RemoveAll(e => e.Plugin == plugin);
    foreach (int weapon in weapons) PublishWeapon(weapon);
  }

  /// <summary>The variants in effect for a weapon: the latest list set for it. Empty for none.</summary>
  static WeaponProp[] VariantsOf(int weapon) {
    for (int i = _weaponProps.Count - 1; i >= 0; i--)
      if (_weaponProps[i].Weapon == weapon) return _weaponProps[i].Variants;
    return [];
  }

  static bool HasConditions(WeaponProp[] variants) {
    foreach (WeaponProp v in variants) if (v.HasConditions) return true;
    return false;
  }

  /// <summary>Sends a weapon's variants to every client (one packet each; a count of 0 removes the
  /// weapon) and re-evaluates who shows which.</summary>
  static void PublishWeapon(int weapon) {
    WeaponProp[] variants = VariantsOf(weapon);
    string w = weapon.ToString(CultureInfo.InvariantCulture);
    var packets = new List<ScarletPacket>();
    if (variants.Length == 0) {
      packets.Add(new ScarletPacket { Type = "SWC", Plugin = "ScarletCore", Window = "", Data = new() { ["anv"] = w, ["ann"] = "0" } });
      _weaponConfigPackets.Remove(weapon);
    } else {
      for (int i = 0; i < variants.Length; i++) {
        var data = new Dictionary<string, string> {
          ["anv"] = w,
          ["ani"] = i.ToString(CultureInfo.InvariantCulture),
          ["ann"] = variants.Length.ToString(CultureInfo.InvariantCulture),
        };
        if (variants[i].HasConditions) data["anz"] = "1";
        WriteProp(data, variants[i].Prop);
        packets.Add(new ScarletPacket { Type = "SWC", Plugin = "ScarletCore", Window = "", Data = data });
      }
      _weaponConfigPackets[weapon] = packets;
    }
    foreach (ScarletPacket packet in packets) PacketManager.SendPacketToAll(packet);

    // The variants changed under whatever was picked for this weapon: pick again, and send.
    foreach (Entity character in _weaponPicks.Where(p => p.Value.Weapon == weapon).Select(p => p.Key).ToList()) {
      _weaponPickPackets.Remove(_weaponPicks[character].Net);
      _weaponPicks.Remove(character);
    }
    if (HasConditions(variants) && !_weaponPropTaskStarted) {
      _weaponPropTaskStarted = true;
      ActionScheduler.RepeatingFrames(_ => TickWeaponPicks(), 1);
    }
    TickWeaponPicks();
  }

  static readonly List<Entity> _weaponGone = new();

  /// <summary>For every player holding a weapon with conditions: which variant matches the item now.</summary>
  static void TickWeaponPicks() {
    if (_weaponPicks.Count == 0 && !_weaponProps.Exists(e => HasConditions(e.Variants))) return;
    try {
      foreach (PlayerData player in PlayerService.GetAllConnected()) {
        Entity character = player.CharacterEntity;
        if (!character.Exists() || !character.Has<Equipment>() || !character.Has<NetworkId>()) continue;
        EquipmentSlot slot = character.Read<Equipment>().WeaponSlot;
        int weapon = slot.SlotId.GuidHash;
        WeaponProp[] variants = weapon == 0 ? [] : VariantsOf(weapon);
        NetworkId id = character.Read<NetworkId>();
        string net = $"{id.Normal_Index}:{id.Normal_Generation}";

        if (!HasConditions(variants)) {
          // Nothing for the server to decide: the clients match this weapon (or none) by themselves.
          if (_weaponPicks.TryGetValue(character, out var stale)) SendPick(character, stale.Net, 0, -1, clear: true);
          continue;
        }
        Entity item = slot.SlotEntity.GetEntityOnServer();
        int index = -1;
        for (int i = 0; i < variants.Length; i++) {
          try {
            if (variants[i].Matches(item)) { index = i; break; }
          } catch (Exception ex) {
            Log.Warning($"[WeaponProp] a condition for weapon {weapon} threw: {ex.Message}");
          }
        }
        if (_weaponPicks.TryGetValue(character, out var last) && last.Net == net && last.Weapon == weapon && last.Index == index) continue;
        if (last.Net != null && last.Net != net) SendPick(character, last.Net, 0, -1, clear: true);
        SendPick(character, net, weapon, index, clear: false);
      }
      // Drop characters that no longer exist at all (one that only logged off keeps its entry).
      _weaponGone.Clear();
      foreach (Entity character in _weaponPicks.Keys) if (!character.Exists()) _weaponGone.Add(character);
      foreach (Entity character in _weaponGone) {
        _weaponPickPackets.Remove(_weaponPicks[character].Net);
        _weaponPicks.Remove(character);
      }
    } catch (Exception ex) {
      Log.Error($"[WeaponProp] tick failed: {ex}");
    }
  }

  /// <summary>Tells every client which variant of <paramref name="weapon"/> the character shows
  /// (<c>ani</c> −1 = the weapon itself), or with <paramref name="clear"/> that the server has no say
  /// (the client matches by itself again).</summary>
  static void SendPick(Entity character, string net, int weapon, int index, bool clear) {
    var data = new Dictionary<string, string> { ["anq"] = net };
    if (!clear) {
      data["anv"] = weapon.ToString(CultureInfo.InvariantCulture);
      data["ani"] = index.ToString(CultureInfo.InvariantCulture);
    }
    var packet = new ScarletPacket { Type = "SWP", Plugin = "ScarletCore", Window = "", Data = data };
    if (clear) {
      _weaponPicks.Remove(character);
      _weaponPickPackets.Remove(net);
    } else {
      _weaponPicks[character] = (net, weapon, index);
      _weaponPickPackets[net] = packet;
    }
    PacketManager.SendPacketToAll(packet);
  }

  /// <summary>Every retained weapon packet, config first, for a client that just connected.</summary>
  static IEnumerable<ScarletPacket> WeaponPropPackets() {
    foreach (List<ScarletPacket> packets in _weaponConfigPackets.Values)
      foreach (ScarletPacket packet in packets) yield return packet;
    foreach (ScarletPacket packet in _weaponPickPackets.Values) yield return packet;
  }
}
