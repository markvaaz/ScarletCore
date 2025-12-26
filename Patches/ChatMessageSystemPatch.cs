using ProjectM;
using Unity.Entities;
using HarmonyLib;
using Unity.Collections;
using System;
using ScarletCore.Events;
using ScarletCore.Utils;
using ScarletCore.Systems;
using ScarletCore.Services;

namespace ScarletCore.Patches;

[HarmonyPatch]
public static class ChatMessageSystemPatch {
  [HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
  [HarmonyPriority(Priority.First)]
  [HarmonyPrefix]
  public static void Prefix(ChatMessageSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers to avoid allocating NativeArray
    if (EventManager.GetSubscriberCount(PrefixEvents.OnChatMessage) == 0) return;
    NativeArray<Entity> entities = __instance.__query_661171423_0.ToEntityArray(Allocator.Temp);

    try {
      if (entities.Length == 0) return;
      CommandService.HandleMessageEvents(entities);
      EventManager.Emit(PrefixEvents.OnChatMessage, entities);
    } catch (Exception e) {
      // Log any exceptions that occur during chat message processing
      // This prevents chat system crashes from affecting the entire game
      Log.Error($"An error occurred while processing chat message: {e.Message}");
    } finally {
      // Always dispose the entities array to prevent memory leaks
      // This is crucial when working with Unity's NativeArray/Allocator.Temp
      entities.Dispose();
    }
  }

  [HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
  [HarmonyPostfix]
  public static void Postfix(ChatMessageSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers to avoid allocating NativeArray
    if (EventManager.GetSubscriberCount(PostfixEvents.OnChatMessage) == 0) return;
    NativeArray<Entity> entities = __instance.__query_661171423_0.ToEntityArray(Allocator.Temp);

    try {
      EventManager.Emit(PostfixEvents.OnChatMessage, entities);
    } catch (Exception e) {
      // Log any exceptions that occur during chat message processing
      // This prevents chat system crashes from affecting the entire game
      Log.Error($"An error occurred while processing chat message: {e.Message}");
    } finally {
      // Always dispose the entities array to prevent memory leaks
      // This is crucial when working with Unity's NativeArray/Allocator.Temp
      entities.Dispose();
    }
  }
}