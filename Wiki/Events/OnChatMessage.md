# OnChatMessage Event

The `OnChatMessage` event in ScarletCore allows you to listen for, inspect, and optionally block or modify chat messages sent by players. This is useful for chat moderation, logging, custom chat commands, or implementing chat-based features in your mod.

---

## Event Signature

```csharp
public static event EventHandler<ChatMessageEventArgs> OnChatMessage;
```

- **sender**: The `ChatMessageSystem` instance that detected and raised the event.
- **args**: A `ChatMessageEventArgs` object containing all relevant information about the chat message.

---

## ChatMessageEventArgs Properties

- `PlayerData Sender` — The player who sent the message
- `string Message` — The content of the chat message
- `ChatMessageType MessageType` — The type of chat (All, Global, Whisper, etc.)
- `PlayerData Receiver` — The target player (for whispers, etc.)
- `DateTime Timestamp` — When the message was sent
- `bool CancelMessage` — Set to `true` to block the message from being sent

> **Note:** Some code and documentation examples may use legacy property names like `PlayerData`, `TargetPlayer`, or `Cancel`. The correct property names are `Sender`, `Receiver`, and `CancelMessage` as defined in `ChatMessageEventArgs.cs`.

---

## Basic Usage

### Listen for all chat messages
```csharp
EventManager.OnChatMessage += (sender, args) => {
    Log.Info($"{args.Sender.CharacterName}: {args.Message}");
};
```

### Block messages containing forbidden words
```csharp
EventManager.OnChatMessage += (sender, args) => {
    if (args.Message.Contains("forbidden")) {
        args.CancelMessage = true;
        Log.Info("Blocked forbidden chat message.");
    }
};
```

### Respond to a specific command
```csharp
EventManager.OnChatMessage += (sender, args) => {
    if (args.Message.StartsWith("!hello")) {
        MessageService.Send(args.Sender, "Hello there!");
        args.CancelMessage = true; // Prevent the command from appearing in chat
    }
};
```

---

## Using a Method Handler

You can use a named method for your handler. The method must match the event signature:

```csharp
void OnChatMessageHandler(object sender, ChatMessageEventArgs args) {
    // sender is the ChatMessageSystem instance
    if (args.Message.StartsWith("!ping")) {
        MessageService.Send(args.Sender, "Pong!");
        args.CancelMessage = true;
    }
}

EventManager.OnChatMessage += OnChatMessageHandler;
EventManager.OnChatMessage -= OnChatMessageHandler; // Unsubscribe when done
```

> **Note:** When using a method handler, the first parameter must be of type `object` (the sender, which is the ChatMessageSystem instance), and the second parameter must be of type `ChatMessageEventArgs`.

---

## Advanced: Accessing the Sender (ChatMessageSystem)

The `sender` parameter is the `ChatMessageSystem` instance that detected the chat. You can cast it if you need to access system-specific methods or properties:

```csharp
EventManager.OnChatMessage += (sender, args) => {
    var chatSystem = sender as ChatMessageSystem;
    // Use chatSystem if needed
};
```

---

## Cancelling a Message

To block a message from being sent to other players, set `args.CancelMessage = true;` in your handler. You can also send a custom message or warning to the player if desired.

---

## Multiple Handlers

You can register multiple handlers. All will be called in order. If any handler sets `args.CancelMessage = true`, the message will be blocked.

---

## Unsubscribing

Always unsubscribe your handler when it is no longer needed to avoid memory leaks:

```csharp
EventManager.OnChatMessage -= OnChatMessageHandler;
```

---

## When to Use

- Chat moderation and filtering
- Custom chat commands
- Logging or analytics
- Triggering in-game actions from chat

> **Tip:** For more advanced chat features, combine this event with other ScarletCore services like `MessageService`.
