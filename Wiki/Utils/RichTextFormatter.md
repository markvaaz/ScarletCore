# RichTextFormatter

`RichTextFormatter` is a static utility class for formatting strings with Unity rich text markup, colors, and custom styles. It provides extension methods for styling text, coloring, creating system messages, progress bars, boxed content, and more—making it easy to generate visually enhanced messages for chat, UI, or logs in ScarletCore mods and systems.

## Overview

- All methods are static and can be used as string extension methods (e.g., `text.Bold()`, `text.WithColor("#ff0000")`).
- Supports Unity rich text tags: `<b>`, `<i>`, `<u>`, `<color=...>`.
- Includes color constants for common colors and highlights.
- Provides helpers for system messages, player actions, progress bars, boxes, and countdowns.
- Supports markdown-like formatting for bold, italic, underline, and highlight.

## Example Usage

```csharp
using ScarletCore.Utils;

// Basic formatting
string bold = "Hello".Bold();
string red = "Error!".ToRed();
string custom = "Custom Color".WithColor("#a963ff");

// Markdown-style formatting
string fancy = "**Bold** *Italic* __Underline__ ~Highlight~".Format();

// Custom highlight colors for each ~highlight~
string multi = $"This is ~red~ and this is ~green~".Format(new List<string> { "#ff0000", "#00ff00" });

// System messages
string error = "Something went wrong".AsError();
string success = "Operation complete".AsSuccess();
string info = "FYI".AsInfo();

// Player actions
string join = "Alice".AsPlayerJoin();
string leave = "Bob".AsPlayerLeave();
string death = "Alice".AsPlayerDeath("Bob");

// Progress bar
string progress = "Loading".AsProgressBar(3, 10);

// Boxed title/content
string box = "ScarletCore".AsBoxedTitle();
string boxedContent = "Line 1\nLine 2".AsBoxedContent("ScarletCore");

// Countdown
string countdown = "Restart".AsCountdown(30);
```

## Advanced Formatting: Format & Highlights

The `Format` method allows you to use markdown-like syntax for styling and to highlight specific parts of your text with custom colors:

- `**bold**` for bold
- `*italic*` for italic
- `__underline__` for underline
- `~highlight~` for colored highlights

You can pass a list of colors to `Format` to control the color of each highlight in order. For example:

```csharp
// The first ~highlight~ will be red, the second will be green
$"This is ~red~ and this is ~green~".Format(new List<string> { "#ff0000", "#00ff00" });
```

- All non-highlighted text will use the default color (white).
- If you use `FormatError`, the base color is a salmon/red (`#ff8f8f`), and highlights use a strong red (`#ff4040`).
- If you use `FormatWarning`, the base color is yellow (`#ffff9e`), and highlights use a strong yellow (`#ffff00`).
- If you use `FormatSuccess`, the base color is white, and highlights use green.
- If you use `FormatInfo`, the base color is white, and highlights use blue.

### Example

```csharp
// Custom highlight colors
string msg = $"This is ~important~ and this is ~urgent~".Format(new List<string> { "#ff8800", "#ff0000" });
// Result: 'important' will be orange, 'urgent' will be red, rest is white

// Error formatting
string err = $"Something went ~wrong~!".FormatError();
// Result: base color is a weak red, 'wrong' is strong red

// Warning formatting
string warn = $"This is a ~warning~!".FormatWarning();
// Result: base color is weak yellow, 'warning' is strong yellow
```

## API Highlights

### Basic Formatting
- `Bold()`, `Italic()`, `Underline()`
- `ToRed()`, `ToGreen()`, `ToBlue()`, `ToYellow()`, `ToWhite()`, `ToBlack()`, `ToGray()`
- `WithColor(string hex)`

### Advanced Formatting
- `Format(List<string> highlightColors = null)` — Markdown-style formatting with custom highlight colors
- `FormatError()`, `FormatWarning()`, `FormatSuccess()`, `FormatInfo()` — Pre-styled formats for error, warning, success, info

### System Messages
- `AsError()`, `AsSuccess()`, `AsWarning()`, `AsInfo()`, `AsAnnouncement()`, `AsList()`

### Player Actions
- `AsPlayerJoin()`, `AsPlayerLeave()`, `AsPlayerDeath(string killerName = null)`

### Progress & UI
- `AsProgressBar(int current, int max, int barLength = 20)`
- `AsBoxedTitle()`, `AsBoxedContent(string title)`
- `AsSeparator(char character, int length = 40)`

### Countdown
- `AsCountdown(int seconds)`

## Notes

- All color methods accept Unity color names or hex codes.
- Markdown-style formatting supports: `**bold**`, `*italic*`, `__underline__`, `~highlight~`.
- The `Format` method cycles through the provided color list for each highlight in order; if there are more highlights than colors, the default highlight color is used.
- Boxed content and progress bars are useful for console/log output.
- All methods are safe to use in chat.

---

For more details, see the source code in `Utils/RichTextFormatter.cs`.
