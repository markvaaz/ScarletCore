using System;
using ProjectM;
using Unity.Entities;

namespace ScarletCore.Interface.Models;

/// <summary>
/// One look of a weapon: the prop shown in place of the weapon while it is equipped, optionally only
/// while the item meets some conditions — a level range, a durability range, anything else. Given to
/// <see cref="InterfaceManager.SetWeaponProps(string, int, WeaponProp[])"/> in order; the first variant
/// whose conditions hold is the one shown, so one weapon can look different at every level or wear as
/// the item changes.
///
/// <code>
/// InterfaceManager.SetWeaponProps(MyPlugin, AxeGuid,
///   new WeaponProp(brokenAxe).Durability(max: 25),        // worn down, whatever the level
///   new WeaponProp(goldenAxe).Level(20),                  // level 20 and up
///   new WeaponProp(ironAxe).Level(10, 19),
///   new WeaponProp(woodAxe));                             // everything else
/// </code>
///
/// Conditions left unset don't restrict. Everything is read on the server from the equipped item, so
/// every client sees the same look, and a change in level or wear shows within a fraction of a second.
/// </summary>
public sealed class WeaponProp {
  /// <summary>What to show in place of the weapon. <see cref="AnimationProp.None"/> hides the weapon
  /// with nothing in its place.</summary>
  public AnimationProp Prop { get; set; }

  /// <summary>Lowest item level this variant is for, inclusive — the level the item's tooltip shows.
  /// Null = no lower bound.</summary>
  public int? MinLevel { get; set; }
  /// <summary>Highest item level this variant is for, inclusive. Null = no upper bound.</summary>
  public int? MaxLevel { get; set; }

  /// <summary>Lowest durability this variant is for, in percent of the item's maximum (0–100),
  /// inclusive. Null = no lower bound.</summary>
  public float? MinDurability { get; set; }
  /// <summary>Highest durability this variant is for, in percent (0–100), inclusive. Null = no upper
  /// bound.</summary>
  public float? MaxDurability { get; set; }

  /// <summary>Any other test, on the equipped item entity — a level kept by your own system, an Id,
  /// a stat roll. Null = no extra test. Runs on the main thread a few times a second per player, so
  /// keep it to component reads.</summary>
  public Func<Entity, bool> Condition { get; set; }

  /// <summary>Creates a variant showing <paramref name="prop"/> (set the conditions after).</summary>
  public WeaponProp(AnimationProp prop) { Prop = prop; }

  /// <summary>Only for items from level <paramref name="min"/> to <paramref name="max"/>, inclusive;
  /// either may be left null. Returns this variant, for chaining.</summary>
  public WeaponProp Level(int? min = null, int? max = null) { MinLevel = min; MaxLevel = max; return this; }

  /// <summary>Only while durability is between <paramref name="min"/> and <paramref name="max"/>
  /// percent, inclusive; either may be left null. Returns this variant, for chaining.</summary>
  public WeaponProp Durability(float? min = null, float? max = null) { MinDurability = min; MaxDurability = max; return this; }

  /// <summary>Only while <paramref name="condition"/> holds for the equipped item. Returns this
  /// variant, for chaining.</summary>
  public WeaponProp When(Func<Entity, bool> condition) { Condition = condition; return this; }

  /// <summary>True when any condition is set. A weapon whose variants have none is matched by the
  /// clients themselves from the weapon in hand; conditions need the item, which only the server has.</summary>
  public bool HasConditions => MinLevel.HasValue || MaxLevel.HasValue || MinDurability.HasValue || MaxDurability.HasValue || Condition != null;

  /// <summary>True when the equipped <paramref name="item"/> meets every condition set.</summary>
  public bool Matches(Entity item) {
    if (MinLevel.HasValue || MaxLevel.HasValue) {
      int level = LevelOf(item);
      if (MinLevel.HasValue && level < MinLevel.Value) return false;
      if (MaxLevel.HasValue && level > MaxLevel.Value) return false;
    }
    if (MinDurability.HasValue || MaxDurability.HasValue) {
      float durability = DurabilityOf(item);
      if (MinDurability.HasValue && durability < MinDurability.Value) return false;
      if (MaxDurability.HasValue && durability > MaxDurability.Value) return false;
    }
    return Condition == null || Condition(item);
  }

  /// <summary>The item level its tooltip shows: <c>WeaponLevelSource</c> × 0.3, rounded (a raw 100 is
  /// level 30). 0 for an item without one.</summary>
  public static int LevelOf(Entity item) =>
    item.Exists() && item.Has<WeaponLevelSource>() ? (int)MathF.Round(item.Read<WeaponLevelSource>().Level * 0.3f) : 0;

  /// <summary>Durability in percent of the item's maximum; 100 for an item that doesn't wear.</summary>
  public static float DurabilityOf(Entity item) {
    if (!item.Exists() || !item.Has<ProjectM.Shared.Durability>()) return 100f;
    ProjectM.Shared.Durability d = item.Read<ProjectM.Shared.Durability>();
    return d.MaxDurability > 0f ? d.Value / d.MaxDurability * 100f : 100f;
  }
}
