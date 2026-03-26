using HarmonyLib;
using ProjectM;
using ScarletCore.Interface;
using ScarletCore.Systems;
using Unity.Collections;

namespace ScarletCore.Patches;

/// <summary>
/// Harmony patch hooking into the buff debug system to forward DOTS-based unit
/// stat modifications to the ScarletInterface client for synchronization.
/// </summary>
[HarmonyPatch]
public static class BuffSystemPatch {
  /// <summary>Prefix executed on <see cref="BuffDebugSystem.OnUpdate"/> that scans for DOTS buff entries and triggers sync.</summary>
  /// <param name="__instance">The patched <see cref="BuffDebugSystem"/> instance.</param>
  [HarmonyPatch(typeof(BuffDebugSystem), nameof(BuffDebugSystem.OnUpdate))]
  [HarmonyPrefix]
  public static void Prefix(BuffDebugSystem __instance) {
    if (!GameSystems.Initialized) return;

    var query = __instance.__query_401358787_0.ToEntityArray(Allocator.Temp);

    foreach (var entity in query) {
      if (!entity.Exists() || !entity.Has<ModifyUnitStatBuff_DOTS>() || !entity.Has<EntityOwner>()) continue;

      var owner = entity.Read<EntityOwner>().Owner;

      if (!owner.Exists() || !owner.TryGetPlayerData(out var player)) continue;

      SyncUnitModService.SendUnitMods(player);
    }
  }
}