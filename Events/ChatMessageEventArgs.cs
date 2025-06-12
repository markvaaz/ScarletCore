using System;
using ProjectM.Network;
using ScarletCore.Data;

namespace ScarletCore.Events;

/// <remarks>
/// Initializes a new instance of ChatMessageEventArgs
/// </remarks>
/// <param name="sender">The PlayerData of the sender</param>
/// <param name="message">The message content</param>
/// <param name="messageType">The type of chat message (All, Global, Whisper, etc.)</param>
/// <param name="receiver">The target name for whispers (optional)</param>
public class ChatMessageEventArgs(PlayerData sender, string message, ChatMessageType messageType, PlayerData receiver = null) : EventArgs {
  /// <summary>
  /// The PlayerData of the user who sent the message
  /// </summary>
  public PlayerData Sender { get; } = sender;

  /// <summary>
  /// The content of the chat message
  /// </summary>
  public string Message { get; } = message;
  /// <summary>
  /// The type of chat message (Global, Local, Whisper, etc.)
  /// </summary>
  public ChatMessageType MessageType { get; } = messageType;

  /// <summary>
  /// The PlayerData of the user who is receiving the message (if applicable, e.g., for whispers)
  /// </summary>
  public PlayerData Receiver { get; } = receiver;

  /// <summary>
  /// The timestamp when the message was sent
  /// </summary>
  public DateTime Timestamp { get; } = DateTime.Now;

  /// <summary>
  /// Indicates if this message should be cancelled/blocked
  /// </summary>
  public bool CancelMessage { get; set; } = false;
}
