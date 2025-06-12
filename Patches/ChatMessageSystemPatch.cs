using ProjectM.Network;
using ProjectM;
using Unity.Entities;
using HarmonyLib;
using Unity.Collections;
using System;
using ScarletCore.Services;
using ScarletCore.Events;
using ScarletCore.Data;
using ScarletCore.Utils;
using ScarletCore.Systems;

namespace ScarletCore.Patches;


[HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
public static class ChatMessageSystemPatch {
  [HarmonyPrefix]
  public static void Prefix(ChatMessageSystem __instance) {
    // Early exit optimization - skip processing if no mods are listening to chat events
    if (EventManager.ChatMessageSubscriberCount == 0) return;    // Get all chat message entities from the ECS query
    // Note: __query_661171423_0 is the internal query used by ChatMessageSystem
    NativeArray<Entity> entities = __instance.__query_661171423_0.ToEntityArray(Allocator.Temp);

    try {
      // Process each chat message entity
      foreach (var entity in entities) {
        // Extract character and user information from the message entity
        var fromData = entity.Read<FromCharacter>();     // Who sent the message
        var userData = fromData.User.Read<User>();       // User data of the sender
        var chatData = entity.Read<ChatMessageEvent>(); // The actual chat message data

        // Extract message details
        var messageType = chatData.MessageType;          // Type: All, Global, Whisper, etc.
        var messageText = chatData.MessageText.ToString(); // The actual message content
        // Initialize receiver as null (only used for whisper messages)
        PlayerData receiver = null;

        // Attempt to get sender's PlayerData using their platform ID
        // Skip this message if sender data cannot be found
        if (!PlayerService.TryGetById(userData.PlatformId, out PlayerData sender)) continue;

        // For whisper messages, attempt to get the receiver's PlayerData
        if (messageType == ChatMessageType.Whisper) {
          PlayerService.TryGetByNetworkId(chatData.ReceiverEntity, out receiver);
        }

        // Fire the chat message event and get the result (includes cancellation state)
        var chatEvent = EventManager.InvokeChatMessage(sender, messageText, messageType, receiver, __instance);

        // Check if any mod requested to cancel this message
        if (chatEvent.CancelMessage) {
          // Destroy the chat message entity to prevent it from being processed by the game
          // This effectively cancels the message before it reaches other players
          GameSystems.EntityManager.DestroyEntity(entity);
        }
      }
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