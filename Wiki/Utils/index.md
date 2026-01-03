# ScarletCore Utils

## Overview

ScarletCore Utils provide essential utility classes for logging, mathematics, text formatting, and visual enhancement. These utilities are designed to simplify common development tasks and improve code readability across all mod development scenarios.

All utilities are static classes in the `ScarletCore.Utils` namespace and require no initialization.

---

## Utility Categories

### Logging & Debugging

#### [Logger (Log)](Logger.md)
Convenient static logging wrapper with automatic assembly resolution, debug mode with caller information, and specialized logging methods for entities and player data.

**Key Features:**
- **Multiple Log Levels** — Debug, Info, Message, Warning, Error, Fatal
- **Debug Mode** — Optional caller information (class, method, line number)
- **Colored Output** — Cyan-colored Info/Message output for visibility
- **Specialized Loggers** — Log entity components and PlayerData with formatted output
- **Assembly Resolution** — Automatically detects and uses the calling mod's LogSource
- **Performance Modes** — Toggle debug mode for production vs development

**Primary Use Cases:**
- Development debugging and diagnostics
- Runtime logging and error tracking
- Entity inspection and component analysis
- Player state debugging
- Performance monitoring

---

### Mathematics & Geometry

#### [MathUtility](MathUtility.md)
Comprehensive mathematical operations for game development including distance calculations, positioning, random generation, and geometric operations optimized for Unity ECS.

**Key Features:**
- **Distance Calculations** — 2D and 3D distance between entities, positions, and mixed types
- **Range Checks** — Fast proximity testing with 2D/3D variants
- **Random Positioning** — Generate positions in circles, rings, and around entities
- **Angle Operations** — Calculate, normalize, and work with angles between entities/positions
- **Interpolation** — Lerp, clamping, and bounds checking
- **Direction Vectors** — Normalized direction calculations
- **Geometric Operations** — Closest point on line, shape collision detection
- **Grid Conversions** — World-to-grid and grid-to-world coordinate transforms
- **Rotation Operations** — Rotate points around pivots

**Primary Use Cases:**
- Combat range checking and targeting
- Entity spawning and positioning
- Patrol and navigation systems
- Area of effect calculations
- Territory and zone management
- Animation and movement interpolation

---

### Text & Presentation

#### [RichTextFormatter](RichTextFormatter.md)
Rich text formatting utility with color constants, markdown-like syntax support, and preset message formatters for Unity-compatible rich text markup.

**Key Features:**
- **Color Constants** — Predefined hex colors (Red, Green, Blue, Yellow, etc.)
- **Basic Formatting** — Bold, Italic, Underline, and color extensions
- **Markdown Syntax** — Process `**bold**`, `*italic*`, `__underline__`, `~highlight~` inline
- **Background Highlighting** — Mark text with background colors using `^text^`
- **Preset Formatters** — Error, Warning, Success, Info styling
- **System Messages** — Pre-formatted announcements, list items
- **Player Events** — Join, leave, death message formatting
- **Custom Color Cycling** — Multiple highlight colors in one string

**Primary Use Cases:**
- Player-facing messages and notifications
- System announcements and broadcasts
- Error and success feedback
- Chat message enhancement
- UI label formatting
- Tutorial and help text

---

#### [Symbols](Symbols.md)
Collection of commonly used Unicode symbols as string constants for consistent visual enhancement across messages, logs, and UI elements.

**Key Features:**
- **Status Indicators** — Check marks, warnings, no entry symbols
- **Shapes & Bullets** — Squares, circles, diamonds, triangles (filled and empty)
- **Navigation Arrows** — Directional arrows and curved arrows
- **Ratings** — Stars (filled and outline)
- **Math Symbols** — Operators, comparison symbols, infinity
- **Icons** — Sun, cloud, music notes, card suits
- **Enumerators** — Circled numbers, superscripts
- **Decorative** — Chain links, reference marks, hot springs

**Primary Use Cases:**
- Message prefixes and status indicators
- Bullet lists and enumerations
- Health/mana/resource displays
- Achievement and rating systems
- Directional hints and navigation
- Decorative text enhancement

---

## Utility Architecture

### Design Philosophy

**Static Access** — All utilities are static classes for zero-overhead access without instantiation.

**No State** — Utilities maintain no internal state (except Logger's cache); they're pure function libraries.

**Composability** — Utilities work together (e.g., Logger + RichTextFormatter + Symbols for rich logging).

**Performance-First** — Critical paths optimized; expensive operations clearly documented.

---

## Common Patterns

### Logging with Context
Combine Logger with debug mode to get detailed caller information during development, then disable for production performance.

### Formatted Messages
Chain RichTextFormatter with Symbols and MessageService for visually appealing player communication.

### Spatial Operations
Use MathUtility's 2D variants for ground-based gameplay to avoid unnecessary height calculations.

### Symbol Enhancement
Prefix messages with Symbols constants for visual consistency and professional appearance.

---

## Integration Examples

### Logger + RichTextFormatter
Format rich text then log it for development visibility in colored console output.

### MathUtility + EntityLookupService
Calculate positions with MathUtility, then query nearby entities with EntityLookupService spatial methods.

### Symbols + RichTextFormatter + MessageService
Build rich, symbol-enhanced messages and send to players for professional presentation.

---

## Best Practices

**Use Logger Appropriately** — Debug for development details, Info for status, Warning for issues, Error for failures.

**Cache Formatted Strings** — RichTextFormatter uses regex; cache results when used repeatedly.

**Prefer 2D for Ground** — MathUtility 2D variants are faster for ground-based gameplay.

**Consistent Symbols** — Use Symbols constants instead of hard-coding Unicode characters.

**Debug Mode Control** — Enable Logger debug mode during development, disable for production.

**Validate Math Inputs** — Check for Entity.Null and validate positions before math operations.

---

## Performance Considerations

### Logger
- **Debug Mode Off** — Minimal overhead, fast string concatenation
- **Debug Mode On** — Stack trace walking adds 1-5ms per call; use only during development
- **Assembly Cache** — First call from assembly uses reflection; subsequent calls are cached

### MathUtility
- **Distance vs Range** — `IsInRange` internally calls `Distance`; use Range for boolean checks
- **2D vs 3D** — 2D operations skip Y-axis math, slightly faster
- **Random Position** — Uses UnityEngine.Random (fast but not deterministic)
- **Grid Conversions** — O(1) simple arithmetic, very fast

### RichTextFormatter
- **Regex Processing** — `Format` uses regex replacements; cache results for repeated use
- **Simple Extensions** — `Bold`, `Italic`, `WithColor` are fast string concatenations
- **Color Constants** — Zero overhead, compile-time strings

### Symbols
- **Zero Overhead** — Compile-time constants, no runtime cost
- **Unicode Support** — Relies on font/console support; fallback to ASCII if needed

---

## Thread Safety

**Logger** — Thread-safe for scheduling, but callbacks execute on main thread. Assembly cache uses ConcurrentDictionary.

**MathUtility** — Stateless and thread-safe; pure functions with no shared state.

**RichTextFormatter** — Stateless and thread-safe; regex processing is isolated per call.

**Symbols** — Constants only; inherently thread-safe.

**Recommendation:** While utilities are thread-safe, avoid calling game APIs (EntityManager, etc.) from background threads.

---

## Documentation Standards

Each utility documentation includes:
- Complete API reference with method signatures
- Practical usage patterns and examples
- Performance characteristics and optimization tips
- Best practices and common pitfalls
- Integration patterns with other utilities

---

## Utility Combinations

### Professional Player Messages
**RichTextFormatter + Symbols + MessageService** — Create polished, visually appealing player notifications.

### Spatial Debugging
**MathUtility + Logger + EntityLookupService** — Debug entity positions, distances, and spatial queries.

### Rich Logging
**Logger + RichTextFormatter** — Colorful, formatted console output for development.

### Enhanced UI
**Symbols + RichTextFormatter** — Build rich UI labels with icons and formatting.

---

## Additional Resources

- [Services Documentation](../Services/) — High-level APIs that use these utilities
- [Systems Documentation](../Systems/) — Core systems (GameSystems, ActionScheduler)
- [Extensions](../Extensions.md) — ECS, Localization, and PlayerData extensions
- [Events](../Events/) — Event system documentation

---

## Future Utilities

Potential future additions to ScarletCore Utils:
- **ColorUtility** — Color manipulation, gradients, and palette management
- **StringUtility** — Additional string operations and parsing helpers
- **ValidationUtility** — Input validation and sanitization
- **CacheUtility** — Generic caching with TTL and size limits
- **RandomUtility** — Advanced random utilities with seeding and distributions
