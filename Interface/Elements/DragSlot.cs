using System;
using ProjectM;
using ScarletCore.Interface.Builders;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A drop target that holds one item. The player fills it by dragging an item from any native item
/// grid (inventory, open chest, servant, equipment) and can drag the item it holds to another
/// <see cref="DragSlot"/> of the same <see cref="Category"/> (swapping when the target is occupied),
/// or out of its window to remove it. Released anywhere else inside the window (a gap, an
/// incompatible slot, its own slot) the item snaps back.
///
/// <para><b>Client first, server confirms.</b> The client moves the icon the moment the player
/// drops, then fires <see cref="OnDrop"/> / <see cref="OnRemove"/>. The native item itself never
/// moves — acting on it is the command's job. Whenever the server (re)sends this slot, the
/// <see cref="Item"/> it carries replaces whatever the client is showing: send it back with the
/// accepted item to confirm, with the old one (or none) to undo, or not at all to keep the
/// client's version.</para>
///
/// <para><b>Command tokens</b> (never empty — a missing value is <c>-</c>):
/// <c>{guid}</c> item prefab guid · <c>{amount}</c> stack size · <c>{src}</c> where the item came
/// from (parse with <see cref="DragSource.TryParse"/>) · <c>{from}</c> source slot <see cref="Id"/>
/// · <c>{to}</c> target slot Id · <c>{swap}</c> guid of the item the drop displaced (0 = none).
/// Input/Dropdown/ColorWheel tokens of the window resolve as well.</para>
///
/// <para>Not rendered inside a <see cref="VirtualList"/> (like ItemViewer/ColorWheel: pooled rows
/// would lose the slot's client-side state while scrolling).</para>
/// </summary>
public class DragSlot : UIElement {
  /// <summary>Slot id, sent as <c>{to}</c> (and as <c>{from}</c> when its item is dragged elsewhere).</summary>
  public string Id { get; set; }
  /// <summary>Only slots of the same category trade items. Default: <c>"items"</c>.</summary>
  public string Category { get; set; } = "items";
  /// <summary>Accept items dragged from the game's own item grids. Default: true.</summary>
  public bool AcceptInventory { get; set; } = true;
  /// <summary>A locked slot accepts nothing and its item can't be dragged out (display only).</summary>
  public bool Locked { get; set; }

  /// <summary>Only these item prefab guids are accepted. Null/empty = no guid filter. Combined with
  /// <see cref="AcceptItemCategories"/> as OR: an item passes when it matches either one.</summary>
  public int[] AcceptGuids { get; set; }
  /// <summary>Only items having at least one of these native categories (e.g.
  /// <c>ItemCategory.Weapon | ItemCategory.Armor</c>) are accepted. <c>NONE</c> = no category filter.</summary>
  public ItemCategory AcceptItemCategories { get; set; } = ItemCategory.NONE;

  /// <summary>The item the slot holds (null = empty). Its box styling is ignored — the slot draws
  /// the frame; only the item data and tooltip placement are used.</summary>
  public ItemViewer Item { get; set; }
  /// <summary>Stack size drawn in the bottom-right corner when above 1.</summary>
  public int Amount { get; set; }

  /// <summary>Command fired on THIS slot when an item lands in it (from a native grid or another slot).</summary>
  public string OnDrop { get; set; }
  /// <summary>Command fired on THIS slot when its item is dragged out and released outside the
  /// slot's window. <c>{from}</c> is this slot, <c>{to}</c> is <c>-</c>.</summary>
  public string OnRemove { get; set; }

  /// <summary>Overlay shown while a compatible item is dragged over the slot. Default: soft gold.</summary>
  public UIColor? HighlightColor { get; set; }
  /// <summary>Space between the slot edge and the item icon, in pixels. Default: 4.</summary>
  public float IconPadding { get; set; } = 4f;

  /// <summary>
  /// Whether <paramref name="guid"/> passes this slot's <see cref="AcceptGuids"/> /
  /// <see cref="AcceptItemCategories"/> filter — the same rule the client applies before dropping.
  /// The client can be modified, so call this in <see cref="OnDrop"/>'s command before acting. For a
  /// slot → slot swap, also check that the source slot accepts the displaced item (<c>{swap}</c>).
  /// </summary>
  public bool Accepts(int guid) {
    // Zeros are ignored (never serialized) — same rule as the client, so both sides agree.
    bool byGuid = AcceptGuids != null && Array.Exists(AcceptGuids, g => g != 0);
    bool byCategory = AcceptItemCategories != ItemCategory.NONE;
    if (!byGuid && !byCategory) return true;
    if (guid == 0) return false;
    if (byGuid && Array.IndexOf(AcceptGuids, guid) >= 0) return true;
    return byCategory && (ItemCategoryOf(guid) & AcceptItemCategories) != 0;
  }

  /// <inheritdoc cref="Accepts(int)"/>
  public bool Accepts(PrefabGUID guid) => Accepts(guid.GuidHash);

  static ItemCategory ItemCategoryOf(int guid) {
    try {
      if (GameSystems.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(new PrefabGUID(guid), out var prefab)
          && prefab.Has<ItemData>())
        return prefab.Read<ItemData>().ItemCategory;
    } catch (Exception ex) {
      Log.Warning($"[DragSlot] item category lookup failed for {guid}: {ex.Message}");
    }
    return ItemCategory.NONE;
  }
}
