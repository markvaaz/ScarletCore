using HarmonyLib;
using ProjectM.Gameplay.WarEvents;
using ScarletCore.Events;
using ScarletCore.Systems;

namespace ScarletCore.Patches;

[HarmonyPatch(typeof(WarEventRegistrySystem), nameof(WarEventRegistrySystem.RegisterWarEventEntities))]
internal static class InitializationPatch {
  [HarmonyPostfix]
  public static void Postfix() {
    if (GameSystems.Initialized) return;
    GameSystems.Initialize();
    EventManager.Emit(ServerEvents.OnInitialize);
    Plugin.Harmony.Unpatch(AccessTools.Method(
      typeof(WarEventRegistrySystem),
      nameof(WarEventRegistrySystem.RegisterWarEventEntities)
    ), HarmonyPatchType.Postfix, Plugin.Harmony.Id);
  }
}