# RichTextFormatter

## Overview

`RichTextFormatter` is a static utility for composing colored and styled text using Unity-compatible rich text markup. It provides a set of color constants, simple extension helpers (`Bold`, `Italic`, `Underline`, color shortcuts), higher-level markdown-like formatting, background highlighting, and small helpers for commonly used system/player messages.

## Table of Contents

- [Color Constants](#color-constants)
- [Basic Text Formatting](#basic-text-formatting)
- [Advanced Formatting](#advanced-formatting)
- [System & Player Formatters](#system--player-formatters)
- [Examples](#examples)
- [Best Practices](#best-practices)
- [Performance Considerations](#performance-considerations)
- [Technical Details](#technical-details)
- [Related Utilities](#related-utilities)

---

## Color Constants

Common hex color constants are exposed for quick reuse:

- `RichTextFormatter.Red` — `#ff0000`
- `RichTextFormatter.Green` — `#00ff00`
- `RichTextFormatter.Blue` — `#4c7cff`
- `RichTextFormatter.Yellow` — `#ffff00`
- `RichTextFormatter.Orange` — `#ffa500`
- `RichTextFormatter.Purple` — `#800080`
- `RichTextFormatter.Pink` — `#ffc0cb`
- `RichTextFormatter.White` — `#ffffff`
- `RichTextFormatter.Gray` — `#808080`
- `RichTextFormatter.Cyan` — `#00ffff`
- `RichTextFormatter.HighlightColor` — default highlight `#a963ff`
- `RichTextFormatter.HighlightErrorColor`, `HighlightWarningColor`, `TextColor`, `SuccessTextColor`, `ErrorTextColor`, `WarningTextColor`

Use these constants with `WithColor()` or the high-level formatters.

---

## Basic Text Formatting

Extension helpers for common markup:

```csharp
public static string Bold(this string text)
public static string Italic(this string text)
public static string Underline(this string text)
public static string ToRed(this string text)
public static string ToGreen(this string text)
public static string ToBlue(this string text)
public static string ToYellow(this string text)
public static string ToWhite(this string text)
public static string ToBlack(this string text)
public static string ToGray(this string text)
public static string WithColor(this string text, string hex)
```

Examples:

```csharp
var text = "Danger".Bold().WithColor(RichTextFormatter.Red);
var label = "(Info)".Italic().ToBlue();
```

---

## Advanced Formatting

Higher-level formatting supports a lightweight markdown-like syntax and background highlights.

```csharp
public static string Format(this string text, List<string> highlightColors = null)
public static string FormatError(this string text)
public static string FormatWarning(this string text)
public static string FormatSuccess(this string text)
public static string FormatInfo(this string text)
public static string FormatBackground(this string text, List<string> backgroundColors = null)
```

- `Format` processes the following inline tokens:
  - `**bold**` → bold
  - `*italic*` → italic
  - `__underline__` → underline
  - `~highlight~` → colored highlight (cycles provided colors)
- `FormatBackground` processes `^text^` and wraps it using `<mark=color>` tags.
- Variants like `FormatError` and `FormatSuccess` apply preset base and highlight colors.

Example:

```csharp
var msg = "**Warning**: ~This area is restricted~".Format(new List<string>{ RichTextFormatter.HighlightWarningColor });
```

---

## System & Player Formatters

Convenience helpers for common system messages and player events:

```csharp
public static string AsError(this string text)      // red error styling
public static string AsSuccess(this string text)    // green success styling
public static string AsWarning(this string text)    // yellow warning styling
public static string AsInfo(this string text)       // blue/info styling
public static string AsAnnouncement(this string text) // orange announcement (WithColor)
public static string AsList(this string text)

public static string AsPlayerJoin(this string playerName)
public static string AsPlayerLeave(this string playerName)
public static string AsPlayerDeath(this string victimName, string killerName = null)
```

Examples:

```csharp
MessageService.SendSystemMessage("Server restarting".AsWarning());
var join = "Marcos".AsPlayerJoin();
```

---

## Examples

### Example 1 — Quick highlight & bold

```csharp
var title = "Boss Approaching".Bold().WithColor(RichTextFormatter.Orange);
MessageService.SendSystemMessage(title);
```

### Example 2 — Markdown-style formatting

```csharp
var description = "**Legendary** sword drops from ~the dragon~".Format();
Log.Info(description);
```

### Example 3 — Error message

```csharp
var err = "Failed to save player data".AsError();
MessageService.SendSystemMessage(err);
```

### Example 4 — Background mark and mixed formatting

```csharp
var label = "^IMPORTANT^".FormatBackground(new List<string>{ RichTextFormatter.HighlightColor });
var line = $"{label} {"Server will restart".AsInfo()}";
```

### Example 5 — Player events

```csharp
var joinMsg = "Eve".AsPlayerJoin();
var deathMsg = "Bob".AsPlayerDeath("Eve");
```

### Example 6 — Cycle highlight colors

```csharp
var s = "This is ~one~ and ~two~ and ~three~".Format(new List<string>{ "#ff8080", "#80ff80", "#8080ff" });
// Each ~...~ uses the next color in the list
```

---

## Best Practices

- Prefer `Format`/preset variants for user-facing messages to keep styling consistent.
- Avoid heavy formatting inside hot loops or per-frame callbacks; build formatted messages once and reuse them.
- When concatenating many parts, format the combined string to avoid nested markup surprises.
- Use `WithColor(hex)` for single-color overrides; use `Format` when applying mixed inline styles.
- For background highlights verify the target consumer supports `<mark>`; otherwise fallback to `WithColor`.

---

## Performance Considerations

- `Format` uses regex replacements and cycling logic — not free. Use sparingly in performance-critical code.
- Color constants and simple extension methods are cheap; `WithColor` is inexpensive.
- If you need deterministic random color cycles or high-throughput formatting, precompute strings off the main loop.

---

## Technical Details

- `Format` uses regular expressions to detect `**bold**`, `*italic*`, `__underline__`, and `~highlight~` tokens and replaces them with Unity rich text tags.
- Highlight/background processing cycles through the provided color lists and falls back to `HighlightColor` when none provided.
- `FormatBackground` uses `<mark=color>` tags for background highlights.
- `WithColor` wraps text using `<color=hex>...</color>` markup.

Regex patterns used (implementation reference):

- Bold: `\*\*(.*?)\*\*`
- Italic: `\*(.*?)\*`
- Underline: `__(.*?)__`
- Highlight: `~(.*?)~`
- Background: `\^(.*?)\^`

---

## Related Utilities

- [Logger](Logger.md) — use `Log` when producing formatted log entries
- [Symbols](Symbols.md) — small symbols and icons to include in formatted text
- [MessageService](../Services/MessageService.md) — send formatted messages to players

---

## Requirements

- `System.Text.RegularExpressions` (Regex)
- Unity rich text consumer (console, chat, or UI that supports `<color>`, `<b>`, `<i>`, `<u>`, `<mark>`)

---

## Notes

- The formatter is intented for presentation only; do not rely on markup for parsing important data.
- If a display target strips tags, fallback to plain strings or pre-strip formatting before sending.
