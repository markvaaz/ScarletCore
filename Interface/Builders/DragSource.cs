using System;
using System.Globalization;
using ProjectM;
using ProjectM.Network;
using ScarletCore.Services;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Entities;

namespace ScarletCore.Interface.Builders;

/// <summary>Where a dragged item came from.</summary>
public enum DragSourceKind {
  /// <summary>Unknown / not provided (<c>-</c>).</summary>
  None,
  /// <summary>A native inventory grid: player inventory, open chest, servant inventory…</summary>
  Inventory,
  /// <summary>A native equipment slot (the player's, or a servant's).</summary>
  Equipment,
  /// <summary>Another <see cref="Elements.DragSlot"/>.</summary>
  Slot,
}

/// <summary>
/// The <c>{src}</c> token of a <see cref="Elements.DragSlot"/> command, parsed. Wire forms:
/// <c>inv:index:generation:slot</c>, <c>eq:type</c>, <c>eq:type:index:generation</c> (servant),
/// <c>slot:id</c>, <c>-</c>.
/// <para>The values come from the client: <b>always validate</b> that the player may touch what they
/// point at (e.g. <see cref="IsOwnedBy"/> for the player's own inventory/equipment) before acting.</para>
/// </summary>
public readonly struct DragSource {
  public DragSourceKind Kind { get; init; }
  /// <summary>Inventory: the entity owning the grid. Equipment: the servant (Null = the player).</summary>
  public NetworkId Owner { get; init; }
  /// <summary>Inventory slot index (Inventory only).</summary>
  public int SlotIndex { get; init; }
  /// <summary>Equipment slot (Equipment only).</summary>
  public EquipmentType EquipmentType { get; init; }
  /// <summary>Source <see cref="Elements.DragSlot.Id"/> (Slot only).</summary>
  public string SlotId { get; init; }

  bool HasOwner => Owner.Normal_Index != 0 || Owner.Normal_Generation != 0;

  public static bool TryParse(string raw, out DragSource source) {
    source = default;
    if (string.IsNullOrEmpty(raw) || raw == "-") return false;
    var p = raw.Split(':');
    switch (p[0]) {
      case "inv" when p.Length == 4 && TryOwner(p[1], p[2], out var inv) && Int(p[3], out var slot):
        source = new DragSource { Kind = DragSourceKind.Inventory, Owner = inv, SlotIndex = slot };
        return true;
      case "eq" when p.Length >= 2 && Int(p[1], out var type): {
        var owner = default(NetworkId);
        if (p.Length == 4 && !TryOwner(p[2], p[3], out owner)) return false;
        source = new DragSource { Kind = DragSourceKind.Equipment, EquipmentType = (EquipmentType)type, Owner = owner };
        return true;
      }
      case "slot" when p.Length >= 2:
        source = new DragSource { Kind = DragSourceKind.Slot, SlotId = raw.Substring(5) };
        return true;
    }
    return false;
  }

  /// <summary>The entity owning the source grid: the inventory holder (Inventory), the servant or the
  /// player's character (Equipment). Entity.Null for Slot/None or a stale id.</summary>
  public Entity ResolveOwner(PlayerData player) {
    if (Kind == DragSourceKind.Equipment && !HasOwner) return player?.CharacterEntity ?? Entity.Null;
    if (Kind is not (DragSourceKind.Inventory or DragSourceKind.Equipment) || !HasOwner) return Entity.Null;
    try {
      var map = GameSystems.NetworkIdSystem.GetNetworkIdLookupRO();
      return map.TryGetValue(Owner, out var e) && e.Exists() ? e : Entity.Null;
    } catch (Exception ex) {
      Log.Warning($"[DragSource] owner lookup failed: {ex.Message}");
      return Entity.Null;
    }
  }

  /// <summary>True when the source grid belongs to <paramref name="player"/> — their character,
  /// its inventory entity, or their own equipment. Chests and servants return false: check access
  /// to those yourself.</summary>
  public bool IsOwnedBy(PlayerData player) {
    if (player == null) return false;
    if (Kind == DragSourceKind.Equipment) return !HasOwner;
    if (Kind != DragSourceKind.Inventory) return false;
    var owner = ResolveOwner(player);
    if (owner == Entity.Null) return false;
    var character = player.CharacterEntity;
    if (owner == character) return true;
    if (InventoryService.TryGetInventoryEntity(character, out var inv) && owner == inv) return true;
    // Bags and other extra inventories hang off the character as external inventory instances.
    try {
      var em = GameSystems.EntityManager;
      if (!em.HasBuffer<InventoryInstanceElement>(character)) return false;
      var instances = em.GetBuffer<InventoryInstanceElement>(character);
      for (int i = 0; i < instances.Length; i++)
        if (instances[i].ExternalInventoryEntity.GetEntityOnServer() == owner) return true;
    } catch (Exception ex) {
      Log.Warning($"[DragSource] inventory instance lookup failed: {ex.Message}");
    }
    return false;
  }

  /// <summary>Inventory source: reads the item currently in the source slot (it may have changed
  /// since the drag — compare its ItemType with <c>{guid}</c>).</summary>
  public bool TryGetInventoryItem(PlayerData player, out Entity owner, out InventoryBuffer item) {
    item = default;
    owner = Kind == DragSourceKind.Inventory ? ResolveOwner(player) : Entity.Null;
    return owner != Entity.Null && InventoryService.TryGetItemAtSlot(owner, SlotIndex, out item);
  }

  /// <summary>Equipment source: the item entity equipped in the source slot.</summary>
  public bool TryGetEquippedItem(PlayerData player, out Entity itemEntity) {
    itemEntity = Entity.Null;
    if (Kind != DragSourceKind.Equipment) return false;
    var owner = ResolveOwner(player);
    if (owner == Entity.Null || !owner.Has<Equipment>()) return false;
    itemEntity = owner.Read<Equipment>().GetEquipmentEntity(EquipmentType).GetEntityOnServer();
    return itemEntity.Exists();
  }

  public override string ToString() => Kind switch {
    DragSourceKind.Inventory => $"inv:{Owner.Normal_Index}:{Owner.Normal_Generation}:{SlotIndex}",
    DragSourceKind.Equipment => HasOwner
      ? $"eq:{(int)EquipmentType}:{Owner.Normal_Index}:{Owner.Normal_Generation}"
      : $"eq:{(int)EquipmentType}",
    DragSourceKind.Slot => $"slot:{SlotId}",
    _ => "-",
  };

  static bool TryOwner(string index, string generation, out NetworkId id) {
    id = default;
    if (!Int(index, out var i) || !byte.TryParse(generation, NumberStyles.Integer, CultureInfo.InvariantCulture, out var g))
      return false;
    id = NetworkId.CreateNormal(i, g);
    return true;
  }

  static bool Int(string s, out int v) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
}
