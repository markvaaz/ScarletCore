# Symbols

## Overview

`Symbols` is a small utility class exposing commonly used Unicode symbols as string constants for consistent usage across ScarletCore. These symbols are useful for UI labels, chat messages, lists, indicators, and compact status displays.

## Table of Contents

- [Categories](#categories)
- [Usage](#usage)
- [Examples](#examples)
- [Best Practices](#best-practices)
- [Encoding & Compatibility](#encoding--compatibility)
- [Related Utilities](#related-utilities)

---

## Categories

The `Symbols` class organizes symbols into common categories:

- Status & Indicators: `CHECK_MARK`, `WARNING`, `NO_ENTRY`, `BULLET_POINT`
- Shapes & Bullets: `SQUARE_BULLET`, `CIRCLE_FILLED`, `DIAMOND_FILLED`, `TRIANGLE_UP`
- Navigation Arrows: `ARROW_RIGHT`, `ARROW_LEFT`, `ARROW_UP`, `ARROW_DOWN`, `ARROW_CURVE_UP_RIGHT`
- Ratings & Stars: `STAR_FILLED`, `STAR_OUTLINE`
- Math & Operators: `NOT_EQUAL`, `APPROXIMATELY_EQUAL`, `PLUS_MINUS`, `INFINITY`
- Cards & Currency: `CARD_HEART`, `CARD_SPADE`, `COIN`, `PRICE`
- Misc Icons: `SUN`, `CLOUD`, `MUSIC_NOTE_SINGLE`, `CHAIN_LINK`
- Enumerators: `CIRCLED_1`, `CIRCLED_2`, `CIRCLED_3`, `SUPERSCRIPT_1` etc.

Each constant is a plain `string` ready to concatenate into messages or UI labels.

---

## Usage

Import and use the constants directly:

```csharp
using ScarletCore.Utils;

var msg = $"{Symbols.CHECK_MARK} Saved successfully";
MessageService.SendSystemMessage(msg);

var line = $"{Symbols.BULLET_POINT} {"Quest complete"} - {Symbols.STAR_FILLED}";
Log.Info(line);
```

Symbols are safe to include inside `RichTextFormatter` output; they are plain text and will not interfere with markup tags.

---

## Examples

### Example 1 — Status line

```csharp
var status = $"{Symbols.CHECK_MARK} Server online | Players: {playerCount} {Symbols.ARROW_RIGHT}";
MessageService.SendSystemMessage(status);
```

### Example 2 — Bullet lists

```csharp
var list = new[] { "First", "Second", "Third" };
foreach (var item in list) {
    Log.Info($"{Symbols.SQUARE_BULLET} {item}");
}
```

### Example 3 — Compact scoreboard

```csharp
var scoreLine = $"{Symbols.CARD_HEART} {playerHealth}  {Symbols.COIN} {playerGold}  {Symbols.STAR_FILLED} {playerRating}";
```

### Example 4 — Step markers

```csharp
var step = $"{Symbols.CIRCLED_1} Step One";
var step2 = $"{Symbols.CIRCLED_2} Step Two";
```

### Example 5 — Warnings and errors

```csharp
Log.Warning($"{Symbols.WARNING} Low memory detected");
MessageService.SendSystemMessage($"{Symbols.NO_ENTRY} Access denied".AsError());
```

---

## Best Practices

- Prefer `Symbols` constants over hard-coded characters for consistency and discoverability.
- Use symbols sparingly in chat messages to avoid visual clutter.
- When concatenating with `RichTextFormatter`, apply color/formatting to the whole segment rather than wrapping the symbol separately unless you want different styling.
- Avoid using symbols as the only means of conveying critical information — combine with text for accessibility.

---

## Encoding & Compatibility

- These symbols are Unicode characters. Most modern consoles, UIs and chat systems support them, but font support may vary.
- If targeting environments that strip or misrender Unicode, provide a fallback plain-text label.

Example fallback pattern:

```csharp
string SafeSymbol(string symbol, string fallback) {
    // Minimal example: replace known problematic symbols
    return symbol; // or return fallback when necessary
}
```

---

## Related Utilities

- `RichTextFormatter` — combine symbols with colored/styled text ([RichTextFormatter.md](RichTextFormatter.md))
- `Logger` — logging utilities for messages that include symbols ([Logger.md](Logger.md))

---

## File Reference

Symbols are defined in `Utils/Symbols.cs` as public `const string` fields for zero-allocation usage at runtime.
