using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using UnityEngine;

namespace ScarletCorePatches;

[HarmonyPatch]
public static class SavePatch {
  [HarmonyPatch(typeof(TriggerPersistenceSaveSystem), nameof(TriggerPersistenceSaveSystem.TriggerSave))]
  [HarmonyPrefix]
  public static void Prefix(TriggerPersistenceSaveSystem __instance, SaveReason reason, Unity.Collections.FixedString128Bytes saveName, ServerRuntimeSettings saveConfig) {
    if (EventManager.GetSubscriberCount(ServerEvents.OnSave) == 0) return;
    EventManager.Emit(ServerEvents.OnSave, saveName.Value);
  }
}