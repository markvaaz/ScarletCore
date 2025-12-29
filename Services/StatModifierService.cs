using ProjectM;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
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
  /// <summary>
  /// Applies an array of stat modifiers to a character entity using a specified modifier buff.
  /// Removes any existing modifier buff before applying the new one.
  /// </summary>
  /// <param name="character">The character entity to modify.</param>
  /// <param name="modifierBuff">The prefab GUID of the modifier buff to apply.</param>
  /// <param name="modifiers">An array of modifiers to apply to the character.</param>
  public static void ApplyModifiers(Entity character, PrefabGUID modifierBuff, Modifier[] modifiers) {
    if (!character.Exists()) return;

    BuffService.TryRemoveBuff(character, modifierBuff);

    // Delay to ensure buff is removed before reapplying otherwise it will throw an error. (I don't want to use a patch just for this)
    ActionScheduler.DelayedFrames(() => {
      if (!BuffService.TryApplyBuff(character, modifierBuff, -1, out var buffEntity)) {
        Log.Error($"Failed to apply modifier buff {modifierBuff} to character entity {character}.");
        return;
      }

      buffEntity.HasWith((ref Buff buff) => {
        buff.MaxStacks = 1;
      });

      if (!buffEntity.Has<Buff_Persists_Through_Death>()) {
        buffEntity.Add<Buff_Persists_Through_Death>();
      }

      if (buffEntity != Entity.Null) {
        UpdateModifiers(buffEntity, modifiers);
      }
    }, 5);
  }

  /// <summary>
  /// Removes all stat modifiers from a character entity by applying an empty modifier array.
  /// </summary>
  /// <param name="character">The character entity to remove modifiers from.</param>
  /// <param name="modifierBuff">The prefab GUID of the modifier buff to remove.</param>
  public static void RemoveModifiers(Entity character, PrefabGUID modifierBuff) {
    ApplyModifiers(character, modifierBuff, []);
  }

  /// <summary>
  /// Attempts to remove a specific modifier buff from a character entity.
  /// </summary>
  /// <param name="character">The character entity to remove the buff from.</param>
  /// <param name="modifierBuff">The prefab GUID of the modifier buff to remove.</param>
  /// <returns>True if the buff was removed; otherwise, false.</returns>
  public static bool TryRemoveModifierBuff(Entity character, PrefabGUID modifierBuff) {
    return BuffService.TryRemoveBuff(character, modifierBuff);
  }

  private static void UpdateModifiers(Entity buffEntity, Modifier[] modifiers) {
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
      var totalValue = mod.Value;
      if (totalValue != 0) {
        var modifier = CreateModifier(mod.StatType, totalValue, mod.ModificationType);
        modifiersBuffer.Add(modifier);
      }
    }
  }

  private static ModifyUnitStatBuff_DOTS CreateModifier(UnitStatType statType, float value, ModificationType modificationType) {
    return new ModifyUnitStatBuff_DOTS() {
      AttributeCapType = AttributeCapType.Uncapped,
      StatType = statType,
      Value = value,
      ModificationType = modificationType,
      Modifier = 1,
      Id = ModificationIDs.Create().NewModificationId()
    };
  }
}
