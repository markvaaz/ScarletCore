# CommandHandler — User-facing behavior

This page describes what happens when players type commands and which user-facing rules apply.

## Sending commands
- Commands must start with the prefix `.` (dot). Example: `.help`.
- Use quotes to pass multi-word arguments: `.announce "Hello players"`.

## Argument validation and usage
- The system validates argument count and type before invoking a command.
- If validation fails, the player receives a usage message showing required and optional parameters.
- Use `usage` and `description` in attributes to improve these messages.

## Permissions and admin checks
- `adminOnly` commands require an administrator authentication.
- `requiredPermissions` arrays are checked; the player must have at least one of the specified permissions.
- If the player lacks permissions, a localized error is sent.

## How arguments map to method parameters
- The command system matches tokens to method parameters in order.
- If the method's first parameter is `CommandContext`, it is provided automatically and does not consume a token.
- Optional parameters (with default values) can be omitted; defaults are used.

## Supported token formats
- Numbers: plain numeric text (decimal, floats use `.` with invariant culture).
- Booleans: `true`/`false` or `1`/`0`.
- Enums: case-insensitive match by name.
- Player references: player name or numeric id (see TypeConverter docs).
- Vector types (float2/float3/float4/quaternion): comma-separated values `x,y` or `x,y,z` etc.
- PrefabGUID: GUID int.

## Help and feedback
- Built-in help messages are generated; provide clear `description` and `usage` to improve them.
- Commands may send localized replies when appropriate.

Example (user-facing usage shown to players):

`.spawn wolf 3`  — spawn 3 wolves
`.teleport "Player Name" 100,200,50` — teleport a player to coordinates

For examples of command signatures and implementations see `Examples.md`.