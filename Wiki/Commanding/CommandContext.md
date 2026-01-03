# CommandContext (How to reply and access sender)

`CommandContext` is supplied to command methods and contains helpers and metadata useful when sending replies and inspecting the caller.

Common properties (user-facing):
- `Sender` — the `PlayerData` who executed the command.
- `Raw` — raw message text as typed by the player.
- `Args` — tokenized arguments after the command name.

Reply helpers (use these to send formatted messages back to the sender):
- `Reply(string message)` — plain reply.
- `ReplyError(string message)` — error-styled reply.
- `ReplyWarning(string message)` — warning-styled reply.
- `ReplyInfo(string message)` — informational-styled reply.
- `ReplySuccess(string message)` — success-styled reply.

Localized replies (use a localization key — do not pass raw text):
- `ReplyLocalized(string key, params string[] parameters)` — looks up the translation for `key` using the sender's language and optional formatting parameters. Variants: `ReplyLocalizedError`, `ReplyLocalizedWarning`, `ReplyLocalizedInfo`, `ReplyLocalizedSuccess`.

Example usage:

```csharp
[Command("whoami", Language.English, description: "Show caller info")]
public static void WhoAmI(CommandContext ctx) {
  var name = ctx.Sender.Name;
  ctx.ReplyInfo($"You are {name}");
}

[Command("say", Language.English, usage: "say <message>")]
public static void Say(CommandContext ctx, string message) {
  ctx.Reply(message);
}

// Localized example — pass a localization key, not raw text
[Command("greet", Language.English, description: "Greet the player")]
public static void Greet(CommandContext ctx) {
  ctx.ReplyLocalized("Cmd.Greet", ctx.Sender.Name);
}
```

Tips:
- If your method accepts `CommandContext`, it will be provided as the first parameter; remaining parameters map to tokens.
- Use localized reply methods when your command should support multiple languages.