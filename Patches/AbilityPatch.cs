using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Systems;
using Unity.Collections;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal static class AbilityRunScriptsSystemPatch {
  [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
  [HarmonyPriority(Priority.First)]
  [HarmonyPrefix]
  static void Prefix(AbilityRunScriptsSystem __instance) {
    if (!GameSystems.Initialized) return;
    var castStartedEvents = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp); // First event
    var preCastFinishedEvents = __instance._OnPreCastFinishedQuery.ToEntityArray(Allocator.Temp); // Second event
    var postCastEndedEvents = __instance._OnPostCastEndedQuery.ToEntityArray(Allocator.Temp); // Third event
    var interruptedEvents = __instance._OnInterruptedQuery.ToEntityArray(Allocator.Temp);

    try {
      if (castStartedEvents.Length > 0 && EventManager.GetSubscriberCount(PrefixEvents.OnCastStarted) > 0)
        EventManager.Emit(PrefixEvents.OnCastStarted, castStartedEvents);
      if (preCastFinishedEvents.Length > 0 && EventManager.GetSubscriberCount(PrefixEvents.OnPreCastFinished) > 0)
        EventManager.Emit(PrefixEvents.OnPreCastFinished, preCastFinishedEvents);
      if (postCastEndedEvents.Length > 0 && EventManager.GetSubscriberCount(PrefixEvents.OnPostCastEnded) > 0)
        EventManager.Emit(PrefixEvents.OnPostCastEnded, postCastEndedEvents);
      if (interruptedEvents.Length > 0 && EventManager.GetSubscriberCount(PrefixEvents.OnCastInterrupted) > 0)
        EventManager.Emit(PrefixEvents.OnCastInterrupted, interruptedEvents);
    } finally {
      castStartedEvents.Dispose();
      preCastFinishedEvents.Dispose();
      postCastEndedEvents.Dispose();
      interruptedEvents.Dispose();
    }
  }

  [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
  [HarmonyPriority(Priority.First)]
  [HarmonyPostfix]
  static void Postfix(AbilityRunScriptsSystem __instance) {
    if (!GameSystems.Initialized) return;
    var castStartedEvents = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp); // First event
    var preCastFinishedEvents = __instance._OnPreCastFinishedQuery.ToEntityArray(Allocator.Temp); // Second event
    var postCastEndedEvents = __instance._OnPostCastEndedQuery.ToEntityArray(Allocator.Temp); // Third event
    var interruptedEvents = __instance._OnInterruptedQuery.ToEntityArray(Allocator.Temp);

    try {
      if (EventManager.GetSubscriberCount(PostfixEvents.OnCastStarted) > 0)
        EventManager.Emit(PostfixEvents.OnCastStarted, castStartedEvents);
      if (EventManager.GetSubscriberCount(PostfixEvents.OnPreCastFinished) > 0)
        EventManager.Emit(PostfixEvents.OnPreCastFinished, preCastFinishedEvents);
      if (EventManager.GetSubscriberCount(PostfixEvents.OnPostCastEnded) > 0)
        EventManager.Emit(PostfixEvents.OnPostCastEnded, postCastEndedEvents);
      if (EventManager.GetSubscriberCount(PostfixEvents.OnCastInterrupted) > 0)
        EventManager.Emit(PostfixEvents.OnCastInterrupted, interruptedEvents);
    } finally {
      castStartedEvents.Dispose();
      preCastFinishedEvents.Dispose();
      postCastEndedEvents.Dispose();
      interruptedEvents.Dispose();
    }
  }
}