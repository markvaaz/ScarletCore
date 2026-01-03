# Data — Overview

The Data module provides multiple persistence solutions for storing plugin data. Choose the storage approach that best fits your needs: lightweight JSON files, structured database storage, configuration settings, or cross-mod data sharing.

## Storage Options

### Database (LiteDB)
Type-safe, structured storage using an embedded database. Best for complex data structures, relationships, and queries.

**Use when:**
- You need structured queries and filtering
- Data has relationships or complex structures
- You want strong type safety with circular reference detection
- Performance with large datasets matters

### JsonDatabase  
Simple JSON file-based storage with in-memory caching. Best for straightforward key-value data and configurations.

**Use when:**
- You want human-readable storage (JSON files)
- Data structure is simple (key-value pairs)
- You need in-memory caching for performance
- Backups should be automatic

### Settings
Configuration management using BepInEx's native config system. Best for plugin settings and configuration values.

**Use when:**
- You're storing plugin configuration/settings
- You want BepInEx config file integration
- Users need to edit settings in `.cfg` files
- You need organized sections

### SharedDatabase
Cross-plugin data sharing without direct references. Best for allowing multiple mods to share data.

**Use when:**
- You need data accessible from other mods
- Mods shouldn't have hard dependencies
- You want namespaced storage (`"modname/key"` format)
- Data is shared across assemblies

## Quick Comparison

| Feature | Database | JsonDatabase | Settings | SharedDatabase |
|---------|----------|--------------|----------|---|
| Storage type | LiteDB | JSON files | BepInEx `.cfg` | LiteDB (shared) |
| Query support | Yes | No | No | No |
| Human-readable | No | Yes | Yes | No |
| Circular references | Detected | Auto-ignored | N/A | Detected |
| Auto-backup | Optional | Optional | N/A | Built-in |
| Cross-mod access | No | No | No | Yes |
| Best for | Structured data | Simple KV pairs | Settings | Data sharing |

## Key Concepts

### Backups
`Database` and `JsonDatabase` support automatic backups on server save events. Configure max backup count to prevent disk overflow.

### Caching
`JsonDatabase` uses in-memory caching for performance. Data is cached on read and timestamps prevent stale reads.

### Namespacing
`SharedDatabase` uses namespace syntax: `"ModName/key"` to organize data from different mods in one database.

### Serialization
- `Database` uses LiteDB's built-in serialization
- `JsonDatabase` uses `System.Text.Json` with cycle handling
- Both support circular reference detection

## Documentation Structure

- **[Database](Database.md)** — LiteDB structured storage (queries, typed data, backups)
- **[JsonDatabase](JsonDatabase.md)** — JSON file storage (simple KV, in-memory cache, auto-backup)
- **[Settings](Settings.md)** — BepInEx configuration (plugin settings, sections)
- **[SharedDatabase](SharedDatabase.md)** — Cross-mod data sharing (static API, namespacing)
- **[Examples](Examples.md)** — Practical examples comparing different approaches

## Getting Started

1. **Simple key-value data?** → Use [JsonDatabase](JsonDatabase.md)
2. **Plugin settings/config?** → Use [Settings](Settings.md)
3. **Complex queries needed?** → Use [Database](Database.md)
4. **Sharing data between mods?** → Use [SharedDatabase](SharedDatabase.md)

See [Examples](Examples.md) for code samples and common patterns.