using ProjectM;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
using System.Collections.Generic;
using Unity.Entities;

namespace ScarletCore.Services;


/// <summary>
/// Represents a single stat modification to be applied to a unit, including value, stat type, and modification type.
/// </summary>
public struct Modifier(float value, UnitStatType statType, ModificationType modificationType = ModificationType.Add) {
  /// <summary>
  /// The value of the modification to apply.
  /// </summary>
  public float Value = value;
  /// <summary>
  /// The type of stat to modify (e.g., health, attack power).
  /// </summary>
  public UnitStatType StatType = statType;
  /// <summary>
  /// The type of modification (e.g., add, multiply).
  /// </summary>
  public ModificationType ModificationType = modificationType;
}

/// <summary>
/// Provides utility methods for applying, removing, and managing stat modifiers on entities using modifier buffs.
/// </summary>
public static class StatModifierService {
  // The V Rising stat system only processes ModifyUnitStatBuff_DOTS once on buff spawn.
  // In-place buffer edits have no effect on live stats, so a remove + re-apply cycle
  // is required for every update.
  //
  // To avoid "modification id doesn't exist" errors on rapid consecutive calls:
  //   - Never cancel a running removal cycle once the buff has been destroyed.
  //     Cancelling and restarting causes the stat system to look for ModificationIDs
  //     that no longer exist, producing errors and corrupted stat values.
  //   - Instead, overwrite the pending modifiers so the already-scheduled re-apply
  //     uses the latest values — coalescing all rapid calls into one clean cycle.

  // ActionId of the scheduled re-apply per (entity, buff) key, if one is in flight.
  private static readonly Dictionary<(Entity, PrefabGUID), ActionId> pendingActions = [];
  // Latest modifiers to apply when the scheduled re-apply fires.
  private static readonly Dictionary<(Entity, PrefabGUID), Modifier[]> pendingModifiers = [];

  /// <summary>
  /// Applies an array of stat modifiers to a character entity using a specified modifier buff.
  /// Removes any existing modifier buff before re-applying with the new values.
  /// Safe to call at high frequency — rapid consecutive calls are coalesced into a
  /// single remove + re-apply cycle, preventing ModificationID errors.
  /// </summary>
  /// <param name="character">The character entity to modify.</param>
  /// <param name="modifierBuff">The prefab GUID of the modifier buff to apply.</param>
  /// <param name="modifiers">An array of modifiers to apply to the character.</param>
  public static void ApplyModifiers(Entity character, PrefabGUID modifierBuff, Modifier[] modifiers) {
    if (!character.Exists()) return;

    var key = (character, modifierBuff);

    // A removal cycle is already in flight — just update the pending modifiers.
    // Do NOT cancel or restart the cycle; the buff is already destroyed and the
    // stat system needs the full delay to clean up its modification entries.
    if (pendingActions.ContainsKey(key)) {
      pendingModifiers[key] = modifiers;
      return;
    }

    // No buff present yet — first-time apply, no removal needed.
    if (!BuffService.HasBuff(character, modifierBuff)) {
      if (modifiers.Length == 0) return;
      ApplyBuffNow(character, modifierBuff, modifiers);
      return;
    }

    // Buff is present — destroy it and schedule re-apply after 2 frames.
    // 2 frames gives the ECS stat system one full cycle to deregister the old
    // modification entries before new ones are registered.
    BuffService.TryRemoveBuff(character, modifierBuff);
    pendingModifiers[key] = modifiers;

    var actionId = ActionScheduler.DelayedFrames(() => {
      pendingActions.Remove(key);
      if (!pendingModifiers.Remove(key, out var latest)) return;
      if (!character.Exists()) return;
      ApplyBuffNow(character, modifierBuff, latest);
    }, 2);

    pendingActions[key] = actionId;
  }

  /// <summary>
  /// Removes all stat modifiers from a character entity and cancels any pending re-apply.
  /// </summary>
  /// <param name="character">The character entity to remove modifiers from.</param>
  /// <param name="modifierBuff">The prefab GUID of the modifier buff to remove.</param>
  public static void RemoveModifiers(Entity character, PrefabGUID modifierBuff) {
    if (!character.Exists()) return;

    var key = (character, modifierBuff);

    // Cancel any pending re-apply so we don't resurrect the buff after removal.
    if (pendingActions.Remove(key, out var existingActionId))
      ActionScheduler.CancelAction(existingActionId);
    pendingModifiers.Remove(key);

    BuffService.TryRemoveBuff(character, modifierBuff);
  }

  /// <summary>
  /// Attempts to remove a specific modifier buff from a character entity.
  /// </summary>
  /// <param name="character">The character entity to remove the buff from.</param>
  /// <param name="modifierBuff">The prefab GUID of the modifier buff to remove.</param>
  /// <returns>True if the buff was removed; otherwise, false.</returns>
  public static bool TryRemoveModifierBuff(Entity character, PrefabGUID modifierBuff) {
    var key = (character, modifierBuff);
    if (pendingActions.Remove(key, out var existingActionId))
      ActionScheduler.CancelAction(existingActionId);
    pendingModifiers.Remove(key);

    return BuffService.TryRemoveBuff(character, modifierBuff);
  }

  private static void ApplyBuffNow(Entity character, PrefabGUID modifierBuff, Modifier[] modifiers) {
    if (!BuffService.TryApplyBuff(character, modifierBuff, -1, out var buffEntity)) {
      Log.Error($"Failed to apply modifier buff {modifierBuff} to character entity {character}.");
      return;
    }

    buffEntity.HasWith((ref Buff buff) => buff.MaxStacks = 1);

    if (!buffEntity.Has<Buff_Persists_Through_Death>())
      buffEntity.Add<Buff_Persists_Through_Death>();

    var modifiersBuffer = GetModifierBuffer(buffEntity);
    if (!modifiersBuffer.IsCreated) return;

    modifiersBuffer.Clear();
    AddModifiers(modifiersBuffer, modifiers);
  }

  /// <summary>
  /// Gets or creates the modifier buffer on a buff entity for storing stat modifications.
  /// </summary>
  /// <param name="buffEntity">The buff entity to get or create the modifier buffer on.</param>
  /// <returns>The dynamic buffer for stat modifications.</returns>
  public static DynamicBuffer<ModifyUnitStatBuff_DOTS> GetModifierBuffer(Entity buffEntity) {
    if (!buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var modifiersBuffer)) {
      buffEntity.AddBuffer<ModifyUnitStatBuff_DOTS>();
      if (!buffEntity.TryGetBuffer(out modifiersBuffer)) {
        Log.Error("Failed to add or get ModifyUnitStatBuff_DOTS buffer on buff entity.");
        return default;
      }
    }
    return modifiersBuffer;
  }

  /// <summary>
  /// Adds an array of stat modifiers to the specified modifier buffer.
  /// </summary>
  /// <param name="modifiersBuffer">The buffer to add modifiers to.</param>
  /// <param name="modifiers">The array of modifiers to add.</param>
  public static void AddModifiers(DynamicBuffer<ModifyUnitStatBuff_DOTS> modifiersBuffer, Modifier[] modifiers) {
    foreach (var mod in modifiers) {
      if (mod.Value == 0) continue;
      modifiersBuffer.Add(CreateModifier(mod.StatType, mod.Value, mod.ModificationType));
    }
  }

  private static ModifyUnitStatBuff_DOTS CreateModifier(UnitStatType statType, float value, ModificationType modificationType) {
    return new ModifyUnitStatBuff_DOTS {
      AttributeCapType = AttributeCapType.Uncapped,
      StatType = statType,
      Value = value,
      ModificationType = modificationType,
      Modifier = 1,
      Id = ModificationIDs.Create().NewModificationId()
    };
  }
}
