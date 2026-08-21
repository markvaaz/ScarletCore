using System;
using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Entities;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// An item icon that opens the game's NATIVE item tooltip on hover. Two authoring forms that
/// collapse to a single wire payload (the client renders both identically):
/// <list type="bullet">
///   <item><b>Entity form</b> — set <see cref="NetworkId"/> to the item entity's network id; the
///     SERVER resolves it and parses level / durability / stat rolls / legendary tier
///     automatically. The item entity does NOT need to be replicated to the viewing client.</item>
///   <item><b>Manual form</b> — set <see cref="Guid"/> plus any of <see cref="Level"/>,
///     <see cref="Durability"/>, <see cref="MaxDurability"/>, <see cref="LegendaryTier"/>,
///     <see cref="StatMods"/>, <see cref="Lines"/> directly.</item>
/// </list>
/// The icon is chosen entirely by the CLIENT from the guid (honouring
/// <c>InterfaceManager.ItemVisual</c> icon overrides) — the server never sends an icon source. All
/// base box styling (size / border / background / material / radius) applies like any element.
///
/// <para>Resolution runs ONCE (<see cref="EnsureResolved"/> is idempotent): re-sending the SAME
/// instance via <c>SendUpdate</c> will not re-parse or re-register the stat-roll sync id. To push
/// new data, send a fresh <see cref="ItemViewer"/> instance.</para>
/// </summary>
public class ItemViewer : UIElement {
  /// <summary>Entity form: the item entity's network id. The server resolves + parses it.</summary>
  public NetworkId? NetworkId { get; set; }

  /// <summary>
  /// Display item guid (hash). The entity form defaults this to the resolved entity's own
  /// PrefabGUID; set it to override — REQUIRED for disguised carriers (pet / armor-mod / forge
  /// weapons) whose entity prefab is a misleading weapon, not the item's public identity.
  /// </summary>
  public int Guid { get; set; }

  /// <summary>Manual weapon level (RAW units — the client applies the display curve). NaN = unset.</summary>
  public float Level { get; set; } = float.NaN;
  /// <summary>Manual current durability. NaN = unset.</summary>
  public float Durability { get; set; } = float.NaN;
  /// <summary>Manual max durability. NaN = unset.</summary>
  public float MaxDurability { get; set; } = float.NaN;
  /// <summary>Manual legendary tier index (gates the tooltip's stat-roll section). -1 = none.</summary>
  public int LegendaryTier { get; set; } = -1;
  /// <summary>Manual native stat rolls (guid, power) — rendered with the game's own localized text
  /// and tier diamond; registered server-side for a sync id, exactly like a real item's rolls.</summary>
  public (int Guid, float Power)[] StatMods { get; set; }
  /// <summary>Extra pre-rendered tooltip text rows (e.g. custom attributes).</summary>
  public string[] Lines { get; set; }

  // ── Resolved output, computed once. Re-serialize (SendUpdate) must NOT re-register a fresh
  //    SpellMod sync id, so all reads/registration are cached behind the Resolved guard. ──
  internal bool Resolved;
  internal int RGuid;
  internal int RTier = -1;
  internal int RModsSyncId;
  internal float RLevel = float.NaN, RDur = float.NaN, RMaxDur = float.NaN;
  internal List<(int Guid, float Power)> RMods;

  /// <summary>Resolves the entity (if any) and registers stat rolls once. Runs on the main thread
  /// (called from serialization during <c>Window.Send</c>/<c>SendUpdate</c>).</summary>
  internal void EnsureResolved() {
    if (Resolved) return;
    Resolved = true;
    RGuid = Guid;
    RLevel = Level; RDur = Durability; RMaxDur = MaxDurability;
    RTier = LegendaryTier;
    if (StatMods is { Length: > 0 }) {
      RMods = new List<(int, float)>(StatMods.Length);
      foreach (var m in StatMods) if (m.Guid != 0) RMods.Add(m);
    }
    if (NetworkId.HasValue) ResolveEntity(NetworkId.Value);
    if (RMods is { Count: > 0 }) RegisterMods();
  }

  // Mirrors ScarletChannels.ItemShareSystem.ResolveToken's entity read (minus the inventory-slot
  // lookup — we already hold the entity via its network id).
  void ResolveEntity(NetworkId nid) {
    try {
      NetworkIdLookupMap map = GameSystems.NetworkIdSystem.GetNetworkIdLookupRO();
      if (!map.TryGetValue(nid, out var e) || !e.Exists()) return;
      if (RGuid == 0) RGuid = e.GetPrefabGuid().GuidHash;
      if (e.Has<Durability>()) { var d = e.Read<Durability>(); RDur = d.Value; RMaxDur = d.MaxDurability; }
      if (float.IsNaN(RLevel) && e.Has<WeaponLevelSource>())
        RLevel = e.Read<WeaponLevelSource>().Level; // RAW — client curves use raw units
      if (RTier < 0 && e.Has<LegendaryItemInstance>())
        RTier = e.Read<LegendaryItemInstance>().TierIndex;
      if (RMods == null && e.Has<LegendaryItemSpellModSetComponent>()) {
        var set = e.Read<LegendaryItemSpellModSetComponent>().StatMods;
        for (int i = 0; i < 8; i++) {
          var mod = ModAt(set, i);
          if (mod.Id.GuidHash != 0) (RMods ??= new List<(int, float)>()).Add((mod.Id.GuidHash, mod.Power));
        }
      }
    } catch (Exception ex) {
      Log.Warning($"[ItemViewer] entity resolve failed: {ex.Message}");
    }
  }

  // Builds a SpellModSet from the rolls and registers it via the game's own server system so the
  // viewing client resolves the rolls (localized text + tier diamond) from its sync registry by id.
  void RegisterMods() {
    try {
      var set = new SpellModSet();
      int n = Math.Min(RMods.Count, 8);
      for (int i = 0; i < n; i++) {
        var mod = new SpellMod { Id = new PrefabGUID(RMods[i].Guid), Power = RMods[i].Power };
        switch (i) {
          case 0: set.Mod0 = mod; break;
          case 1: set.Mod1 = mod; break;
          case 2: set.Mod2 = mod; break;
          case 3: set.Mod3 = mod; break;
          case 4: set.Mod4 = mod; break;
          case 5: set.Mod5 = mod; break;
          case 6: set.Mod6 = mod; break;
          case 7: set.Mod7 = mod; break;
        }
      }
      set.Count = (byte)n;
      var sys = GameSystems.Server.GetExistingSystemManaged<SpellModSyncSystem_Server>();
      if (sys != null) sys.AddSpellMod(ref set); // stamps a fresh SyncId + queues the client push
      RModsSyncId = set.SyncId;
    } catch (Exception ex) {
      Log.Warning($"[ItemViewer] stat-mod register failed: {ex.Message}");
    }
  }

  static SpellMod ModAt(SpellModSet set, int i) => i switch {
    0 => set.Mod0, 1 => set.Mod1, 2 => set.Mod2, 3 => set.Mod3,
    4 => set.Mod4, 5 => set.Mod5, 6 => set.Mod6, 7 => set.Mod7,
    _ => default,
  };
}
