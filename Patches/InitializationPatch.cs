using HarmonyLib;
using ProjectM;
using ScarletCore.Systems;

namespace ScarletCore.Patches;

[HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
internal static class InitializationPatch {
  [HarmonyPostfix]
  public static void Postfix() {
    if (GameSystems.Initialized) return;
    GameSystems.Initialize();
    Plugin.Harmony.Unpatch(typeof(SpawnTeamSystem_OnPersistenceLoad).GetMethod("OnUpdate"), typeof(InitializationPatch).GetMethod("Postfix"));
  }
}