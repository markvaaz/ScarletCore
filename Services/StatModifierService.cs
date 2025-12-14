using ProjectM;
using ScarletCore.Data;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Entities;

namespace ScarletCore.Services;

public class Modifier {
  public float Value { get; set; }
  public UnitStatType StatType { get; set; }
}

public static class StatModifierService {
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

  public static void RemoveModifiers(Entity character, PrefabGUID modifierBuff) {
    ApplyModifiers(character, modifierBuff, []);
  }

  public static bool TryRemoveModifierBuff(Entity character, PrefabGUID modifierBuff) {
    return BuffService.TryRemoveBuff(character, modifierBuff);
  }

  private static void UpdateModifiers(Entity buffEntity, Modifier[] modifiers) {
    var modifiersBuffer = GetModifierBuffer(buffEntity);

    if (!modifiersBuffer.IsCreated) return;

    modifiersBuffer.Clear();

    AddModifiers(modifiersBuffer, modifiers);
  }

  public static DynamicBuffer<ModifyUnitStatBuff_DOTS> GetModifierBuffer(Entity buffEntity) {
    if (!buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var modifiersBuffer)) {
      buffEntity.AddBuffer<ModifyUnitStatBuff_DOTS>();
      if (!buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out modifiersBuffer)) {
        Log.Error("Failed to add or get ModifyUnitStatBuff_DOTS buffer on buff entity.");
        return default;
      }
    }

    return modifiersBuffer;
  }

  public static void AddModifiers(DynamicBuffer<ModifyUnitStatBuff_DOTS> modifiersBuffer, Modifier[] modifiers) {
    foreach (var mod in modifiers) {
      var totalValue = mod.Value;
      if (totalValue != 0) {
        var modifier = CreateModifier(mod.StatType, totalValue);
        modifiersBuffer.Add(modifier);
      }
    }
  }

  private static ModifyUnitStatBuff_DOTS CreateModifier(UnitStatType statType, float value) {
    return new ModifyUnitStatBuff_DOTS() {
      AttributeCapType = AttributeCapType.Uncapped,
      StatType = statType,
      Value = value,
      ModificationType = ModificationType.Add,
      Modifier = 1,
      Id = ModificationIDs.Create().NewModificationId()
    };
  }
}
