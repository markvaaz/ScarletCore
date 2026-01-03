````markdown
# Localization (Localizer)

This page documents how to provide translations for ScarletCore-powered mods and how to use the `Localizer` API.

**Overview**
- The `Localizer` service loads translation keys from embedded JSON resources or runtime files and exposes lookup helpers for player-specific or server-language translations.
- Translation keys are stored with a composite key of `{AssemblyName}:{Key}` so multiple mods can safely use the same short key names.

**JSON format**
Translation files must be valid JSON with the shape:

```json
{
  "Cmd.Greet": {
    "English": "Hello, {0}!",
    "Portuguese": "Olá, {0}!"
  },
  "Item.Sword": {
    "English": "Sword",
    "Portuguese": "Espada"
  }
}
```

Each top-level property is a key (string) and its value is a mapping of `Language` enum names to translated text.

**Embedding JSON files**
Include translation JSON files as embedded resources in your mod's project. Add to your `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Localization\**\*.json" />
</ItemGroup>
```

When `Localizer.AutoLoadFromLocalizationFolder()` runs, it will search the calling assembly for embedded resources under the `Localization` prefix and load all `.json` files found.

**API & usage**
- `Localizer.Get(PlayerData player, string key, params object[] parameters)` — get localized string for a specific player (resolves player language).
- `Localizer.Get(PlayerData player, string key, Assembly assembly, params object[] parameters)` — same, but force a specific assembly (useful when calling from shared code).
- `Localizer.GetServer(string key, params object[] parameters)` — lookup using the server's current language.
- `Localizer.NewKey(string key, IDictionary<Language,string> translations)` — register a custom key at runtime for the calling assembly.
- `Localizer.LoadFromFile(string path)` — load additional translations from a JSON file at runtime.
- `Localizer.SetPlayerLanguage(PlayerData player, Language language)` / `Localizer.GetPlayerLanguage(PlayerData player)` — manage per-player language preferences.
- `Localizer.GetText(PrefabGUID prefabGuid)` / `Localizer.GetPrefabName(PrefabGUID prefabGuid)` — lookup game text by prefab mapping.

Example — using in commands (prefer `ReplyLocalized` helpers on `CommandContext`):

```csharp
// Using Localizer directly
var greeting = Localizer.Get(player, "Cmd.Greet", player.Name);
player.SendMessage(greeting);

// From a command (preferred)
ctx.ReplyLocalized("Cmd.Greet", ctx.Sender.Name);
```

Example — runtime registration

```csharp
Localizer.NewKey("MyMod.Welcome", new Dictionary<Language,string> {
  { Language.English, "Welcome to MyMod, {0}!" },
  { Language.Portuguese, "Bem-vindo ao MyMod, {0}!" }
});

var msg = Localizer.Get(player, "MyMod.Welcome", player.Name);
player.SendMessage(msg);
```

**Prefab mappings**
The built-in `PrefabToGuidMap.json` maps game prefab hashes to localization GUIDs. Use `Localizer.GetText(PrefabGUID)` or `Localizer.GetPrefabName` to resolve names for in-game prefabs.

**Tips & gotchas**
- Keys are composite: `{AssemblyName}:{Key}`. If you want your translations to be found when another assembly calls `Localizer`, pass the assembly explicitly or register keys from the intended assembly.
- Use format placeholders like `{0}`, `{1}` in translations and pass parameters to `Localizer.Get(...)` or to `ReplyLocalized(...)`.
- `AutoLoadFromLocalizationFolder()` searches the calling assembly at initialization; ensure your JSON files are embedded resources.

**See also**
- `Localization/PrefabToGuidMap.json` — included prefab mappings (embedded resource).
- `CommandContext.ReplyLocalized` — helper for sending localized replies from commands.

````
