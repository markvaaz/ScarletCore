# Command Attributes (Usage)

This page explains how to expose chat commands using attributes. Keep examples focused on how to use the system.

## Command groups
Use `CommandGroup` on a class to group related commands and apply language or admin restrictions.

Example:

```csharp
[CommandGroup("admin", Language.English, adminOnly: true)]
public static class AdminCommands {
  // methods with [Command] inside become admin commands under the "admin" group
}
```

You can add additional aliases or localizations using `CommandGroupAlias`.

## Single commands
Apply `Command` to a method to declare a command. Provide `name`, `language` and optional fields.

Key properties (user-facing):
- `name`: visible command name.
- `aliases`: alternate names users can type.
- `requiredPermissions`: permission keys users must have to run the command.
- `adminOnly`: restricts execution to server admins.
- `description`: short text shown in help output.
- `usage`: optional usage text shown on errors/help.

Example:

```csharp
[Command("kick", Language.English, aliases: new[] {"k"}, requiredPermissions: new[] {"moderate"}, description: "Kick a player", usage: "kick <player> [reason]")]
public static void KickCommand(CommandContext ctx, PlayerData player, string reason = "") {
  // implementation
  ctx.ReplySuccess($"Kicked ~{player.Name}~");
}
```

## Command localization with CommandAlias

Use the `CommandAlias` attribute to provide localized versions of the same command. Each alias can have its own language, name, aliases array, description, and usage text.

**Important distinctions:**
- `CommandAlias` (the attribute) — creates a separate localized command for a different language. Applied to the same method as the main `Command` attribute.
- `aliases` (the parameter) — provides alternate shorthand names within the same language (e.g., `aliases: new[] {"k"}` for "kick").

Example — Single command with multiple languages:

```csharp
[Command("kick", Language.English, aliases: new[] {"k"}, description: "Kick a player", usage: "kick <player> [reason]")]
[CommandAlias("expulsar", Language.Spanish, aliases: new[] {"exp"}, description: "Expulsar un jugador", usage: "expulsar <jugador> [razón]")]
[CommandAlias("chutar", Language.Portuguese, aliases: new[] {"chu"}, description: "Chutar um jogador", usage: "chutar <jogador> [motivo]")]
public static void KickCommand(CommandContext ctx, PlayerData player, string reason = "") {
  // implementation
  ctx.ReplySuccess($"Kicked {player.Name}");
}
```

Players can now use:
- English: `.kick PlayerName` or `.k PlayerName`
- Spanish: `.expulsar PlayerName` or `.exp PlayerName`
- Portuguese: `.chutar PlayerName` or `.chu PlayerName`

The command system automatically selects the version matching the player's configured language.

## Group localization with CommandGroupAlias

Use `CommandGroupAlias` to localize command groups. Apply it to the class alongside `CommandGroup`.

Example — Localized admin group:

```csharp
[CommandGroup("admin", Language.English, adminOnly: true)]
[CommandGroupAlias("administrador", Language.Spanish)]
[CommandGroupAlias("admin", Language.Portuguese)]
public static class AdminCommands {
  
  [Command("kick", Language.English, description: "Kick a player")]
  [CommandAlias("expulsar", Language.Spanish, description: "Expulsar jugador")]
  [CommandAlias("chutar", Language.Portuguese, description: "Chutar jogador")]
  public static void KickCommand(CommandContext ctx, PlayerData player) {
    ctx.ReplySuccess($"Kicked {player.Name}");
  }
}
```

Players can use:
- English: `.admin kick PlayerName`
- Spanish: `.administrador expulsar PlayerName`
- Portuguese: `.admin chutar PlayerName`

## Notes
- Each `CommandAlias` creates a separate command entry in the player's language.
- Use `CommandGroupAlias` for groups that need localized names.
- Always provide matching localized `description` and `usage` for better help output.
- The command system automatically routes players to their language version based on their `PlayerData.Language`.