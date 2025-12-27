using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ProjectM.Network;
using Unity.Mathematics;
using ScarletCore.Data;
using ScarletCore.Utils;
using Unity.Entities;
using Unity.Collections;
using ScarletCore.Services;

namespace ScarletCore.Commanding;

/// <summary>
/// Context passed to command handlers.
/// </summary>
public sealed class CommandContext(Entity messageEntity, PlayerData sender, string raw, string[] args) {
  public Entity MessageEntity { get; } = messageEntity;
  public PlayerData Sender { get; } = sender;
  public string Raw { get; } = raw;
  public string[] Args { get; } = args;
  // The assembly that should be considered the owner of localization keys.
  // When a command from another assembly invokes `ReplyLocalized`, CommandService
  // will set this to the command's assembly so localization resolves correctly.
  public Assembly CallingAssembly { get; set; }

  public void Reply(string message) {
    if (Sender != null) MessageService.SendRaw(Sender, message.Format());
  }

  public void ReplyError(string message) {
    if (Sender != null) MessageService.SendRaw(Sender, message.FormatError());
  }

  public void ReplyWarning(string message) {
    if (Sender != null) MessageService.SendRaw(Sender, message.FormatWarning());
  }

  public void ReplyInfo(string message) {
    if (Sender != null) MessageService.SendRaw(Sender, message.FormatInfo());
  }

  public void ReplySuccess(string message) {
    if (Sender != null) MessageService.SendRaw(Sender, message.FormatSuccess());
  }

  public void ReplyLocalized(string key, params string[] parameters) {
    if (Sender != null) {
      string localized;
      if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
      else localized = LocalizationService.Get(Sender, key, parameters);
      MessageService.SendRaw(Sender, localized.Format());
    }
  }

  public void ReplyLocalizedError(string key, params string[] parameters) {
    if (Sender != null) {
      string localized;
      if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
      else localized = LocalizationService.Get(Sender, key, parameters);
      MessageService.SendRaw(Sender, localized.FormatError());
    }
  }

  public void ReplyLocalizedWarning(string key, params string[] parameters) {
    if (Sender != null) {
      string localized;
      if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
      else localized = LocalizationService.Get(Sender, key, parameters);
      MessageService.SendRaw(Sender, localized.FormatWarning());
    }
  }

  public void ReplyLocalizedInfo(string key, params string[] parameters) {
    if (Sender != null) {
      string localized;
      if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
      else localized = LocalizationService.Get(Sender, key, parameters);
      MessageService.SendRaw(Sender, localized.FormatInfo());
    }
  }

  public void ReplyLocalizedSuccess(string key, params string[] parameters) {
    if (Sender != null) {
      string localized;
      if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
      else localized = LocalizationService.Get(Sender, key, parameters);
      MessageService.SendRaw(Sender, localized.FormatSuccess());
    }
  }
}

/// <summary>
/// Stores command metadata including attributes and method info.
/// </summary>
internal sealed class CommandInfo {
  public MethodInfo Method { get; set; }
  public CommandAttribute Attribute { get; set; }
  public bool GroupAdminOnly { get; set; }
  public Assembly Assembly { get; set; }
  public string GroupName { get; set; } // null if no group
}

/// <summary>
/// Responsible for scanning command classes, detecting `.group` messages and invoking handlers.
/// Supports N parameters with optional and required parameters using default values.
/// Now supports commands without groups - can be invoked directly as `.command`
/// Also supports multi-word commands like `.quest create npc`
/// </summary>
public static class CommandHandler {
  private static readonly Dictionary<string, Dictionary<string, List<CommandInfo>>> _groups = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<string, List<CommandInfo>> _noGroupCommands = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<string, Assembly> _groupToAssembly = new(StringComparer.OrdinalIgnoreCase);
  // Mapping from command key (the string used to lookup in dictionaries) to language code.
  // null or empty = default/main language for the command (no language-specific alias)
  private static readonly Dictionary<string, string> _commandKeyLanguage = new(StringComparer.OrdinalIgnoreCase);
  // Cache for GetAllCommands keyed by player language (normalized). Cleared on register/unregister.
  private static readonly Dictionary<string, Dictionary<string, string[]>> _commandsCache = new(StringComparer.OrdinalIgnoreCase);
  private static bool _initialized = false;

  public static void Initialize() {
    if (_initialized) return;

    RegisterLocalizationKeys();
    RegisterCommands();

    _initialized = true;
    Log.Info($"CommandService initialized with {_groups.Count} groups and {_noGroupCommands.Count} standalone commands");
  }

  /// <summary>
  /// Registers all commands from the calling assembly.
  /// </summary>
  public static void RegisterCommands() {
    // Get the calling assembly (the assembly that called this method)
    var callingAssembly = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Assembly;
    var asm = callingAssembly ?? Assembly.GetCallingAssembly();
    RegisterAssembly(asm);
  }

  /// <summary>
  /// Registers all commands from a specific assembly. If `assembly` is null,
  /// the calling assembly will be used.
  /// </summary>
  public static void RegisterAssembly(Assembly assembly = null) {
    if (assembly == null) return;

    foreach (var type in assembly.GetTypes()) {
      var grpAttr = type.GetCustomAttribute<CommandGroupAttribute>();

      if (grpAttr != null) {
        // Has group attribute - register as grouped commands
        var groupName = grpAttr.Group.ToLowerInvariant();
        var groupAdminOnly = grpAttr.AdminOnly;

        // Register main group name
        if (!_groups.ContainsKey(groupName)) {
          _groups[groupName] = new Dictionary<string, List<CommandInfo>>(StringComparer.OrdinalIgnoreCase);
          _groupToAssembly[groupName] = assembly; // Use the type's assembly
        }

        // Register group aliases
        foreach (var alias in grpAttr.Aliases) {
          var aliasLower = alias.ToLowerInvariant();
          if (!_groups.ContainsKey(aliasLower)) {
            _groups[aliasLower] = _groups[groupName];
            _groupToAssembly[aliasLower] = assembly; // Use the type's assembly
          }
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
          var cmdAttr = method.GetCustomAttribute<CommandAttribute>();
          if (cmdAttr == null) continue;

          // Generate usage if empty
          if (string.IsNullOrEmpty(cmdAttr.Usage)) {
            cmdAttr.Usage = GenerateUsage(groupName, cmdAttr.Name, method);
          }

          var cmdInfo = new CommandInfo {
            Method = method,
            Attribute = cmdAttr,
            GroupAdminOnly = groupAdminOnly,
            Assembly = type.Assembly, // Use the type's assembly, not the executing assembly
            GroupName = groupName
          };

          var cmdName = cmdAttr.Name.ToLowerInvariant();
          if (!_groups[groupName].TryGetValue(cmdName, out var list)) {
            list = [];
            _groups[groupName][cmdName] = list;
          }
          list.Add(cmdInfo);
          // mark this command key as default (no language)
          if (!_commandKeyLanguage.ContainsKey(cmdName)) _commandKeyLanguage[cmdName] = null;

          // Register command aliases
          foreach (var alias in cmdAttr.Aliases) {
            var aliasLower = alias.ToLowerInvariant();
            if (!_groups[groupName].TryGetValue(aliasLower, out var aliasList)) {
              aliasList = [];
              _groups[groupName][aliasLower] = aliasList;
            }
            aliasList.Add(cmdInfo);
            if (!_commandKeyLanguage.ContainsKey(aliasLower)) _commandKeyLanguage[aliasLower] = null;
          }

          // Register any multilingual aliases defined on the method
          var multiAliases = method.GetCustomAttributes<CommandAliasAttribute>();
          foreach (var ma in multiAliases) {
            if (string.IsNullOrWhiteSpace(ma.Name)) continue;
            var multiName = ma.Name.ToLowerInvariant();
            if (!_groups[groupName].TryGetValue(multiName, out var multiList)) {
              multiList = [];
              _groups[groupName][multiName] = multiList;
            }
            multiList.Add(cmdInfo);
            _commandKeyLanguage[multiName] = ma.Language?.ToLowerInvariant();

            foreach (var a in ma.Aliases) {
              var aLower = a.ToLowerInvariant();
              if (!_groups[groupName].TryGetValue(aLower, out var aList)) {
                aList = [];
                _groups[groupName][aLower] = aList;
              }
              aList.Add(cmdInfo);
              _commandKeyLanguage[aLower] = ma.Language?.ToLowerInvariant();
            }
          }
        }
      } else {
        // No group attribute - register as standalone commands
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
          var cmdAttr = method.GetCustomAttribute<CommandAttribute>();
          if (cmdAttr == null) continue;

          // Generate usage if empty (no group prefix)
          if (string.IsNullOrEmpty(cmdAttr.Usage)) {
            cmdAttr.Usage = GenerateUsage(null, cmdAttr.Name, method);
          }

          var cmdInfo = new CommandInfo {
            Method = method,
            Attribute = cmdAttr,
            GroupAdminOnly = false,
            Assembly = type.Assembly, // Use the type's assembly, not the executing assembly
            GroupName = null
          };

          var cmdName = cmdAttr.Name.ToLowerInvariant();
          if (!_noGroupCommands.TryGetValue(cmdName, out var list)) {
            list = [];
            _noGroupCommands[cmdName] = list;
          }
          list.Add(cmdInfo);
          if (!_commandKeyLanguage.ContainsKey(cmdName)) _commandKeyLanguage[cmdName] = null;

          // Register command aliases
          foreach (var alias in cmdAttr.Aliases) {
            var aliasLower = alias.ToLowerInvariant();
            if (!_noGroupCommands.TryGetValue(aliasLower, out var aliasList)) {
              aliasList = [];
              _noGroupCommands[aliasLower] = aliasList;
            }
            aliasList.Add(cmdInfo);
            if (!_commandKeyLanguage.ContainsKey(aliasLower)) _commandKeyLanguage[aliasLower] = null;
          }

          // Register multilingual aliases on the method
          var multiAliases = method.GetCustomAttributes<CommandAliasAttribute>();
          foreach (var ma in multiAliases) {
            if (string.IsNullOrWhiteSpace(ma.Name)) continue;
            var multiName = ma.Name.ToLowerInvariant();
            if (!_noGroupCommands.TryGetValue(multiName, out var multiList)) {
              multiList = [];
              _noGroupCommands[multiName] = multiList;
            }
            multiList.Add(cmdInfo);
            _commandKeyLanguage[multiName] = ma.Language?.ToLowerInvariant();

            foreach (var a in ma.Aliases) {
              var aLower = a.ToLowerInvariant();
              if (!_noGroupCommands.TryGetValue(aLower, out var aList)) {
                aList = [];
                _noGroupCommands[aLower] = aList;
              }
              aList.Add(cmdInfo);
              _commandKeyLanguage[aLower] = ma.Language?.ToLowerInvariant();
            }
          }
        }
      }
    }

    Log.Info($"Registered commands from assembly: {assembly.GetName().Name}");
    // Invalidate cached command listings
    _commandsCache.Clear();
  }

  /// <summary>
  /// Unregisters all commands from a specific assembly. If `assembly` is null,
  /// the calling assembly will be used.
  /// </summary>
  public static void UnregisterAssembly(Assembly assembly = null) {
    var asm = assembly ?? Assembly.GetCallingAssembly();
    var groupsToRemove = new List<string>();

    // Find all groups from this assembly
    foreach (var kvp in _groupToAssembly) {
      if (kvp.Value == asm) {
        groupsToRemove.Add(kvp.Key);
      }
    }

    // Remove groups and their commands
    foreach (var groupName in groupsToRemove) {
      _groups.Remove(groupName);
      _groupToAssembly.Remove(groupName);
    }

    // Clean up any remaining commands from this assembly in shared groups
    var commandsToRemove = new List<(string group, string command)>();
    foreach (var groupKvp in _groups) {
      foreach (var cmdKvp in groupKvp.Value) {
        if (cmdKvp.Value.Any(ci => ci.Assembly == asm)) {
          commandsToRemove.Add((groupKvp.Key, cmdKvp.Key));
        }
      }
    }

    foreach (var (group, command) in commandsToRemove) {
      if (_groups.TryGetValue(group, out var cmds)) {
        if (cmds.TryGetValue(command, out var list)) {
          list.RemoveAll(ci => ci.Assembly == asm);
          if (list.Count == 0) cmds.Remove(command);
          // remove language mapping for this command key only if no other registration uses it
          if (!IsCommandKeyRegistered(command)) _commandKeyLanguage.Remove(command);
        }
      }
    }

    // Remove standalone commands from this assembly
    var standaloneToRemove = new List<string>();
    foreach (var cmdKvp in _noGroupCommands) {
      if (cmdKvp.Value.Any(ci => ci.Assembly == asm)) {
        standaloneToRemove.Add(cmdKvp.Key);
      }
    }

    foreach (var cmdName in standaloneToRemove) {
      if (_noGroupCommands.TryGetValue(cmdName, out var list)) {
        list.RemoveAll(ci => ci.Assembly == asm);
        if (list.Count == 0) _noGroupCommands.Remove(cmdName);
        // remove language mapping for this standalone key only if no other registration uses it
        if (!IsCommandKeyRegistered(cmdName)) _commandKeyLanguage.Remove(cmdName);
      }
    }

    Log.Info($"Unregistered commands from assembly: {asm.GetName().Name} ({groupsToRemove.Count} groups removed)");
    // Invalidate cached command listings
    _commandsCache.Clear();
  }

  // Returns true if the given command key is still present in any registered command map
  private static bool IsCommandKeyRegistered(string key) {
    if (string.IsNullOrWhiteSpace(key)) return false;
    if (_noGroupCommands.ContainsKey(key)) return true;
    foreach (var groupKvp in _groups) {
      if (groupKvp.Value.ContainsKey(key)) return true;
    }
    return false;
  }

  private static string GenerateUsage(string group, string command, MethodInfo method) {
    var parameters = method.GetParameters();
    var usage = group != null ? $".{group} {command}" : $".{command}";

    foreach (var param in parameters) {
      // Skip CommandContext parameter
      if (param.ParameterType == typeof(CommandContext)) continue;

      var paramName = param.Name;
      var typeName = GetFriendlyTypeName(param.ParameterType);

      if (param.HasDefaultValue) {
        usage += $" [{paramName}:{typeName}]";
      } else {
        usage += $" <{paramName}:{typeName}>";
      }
    }

    return usage;
  }


  // Simple template replacement for named placeholders like {paramName}
  private static string ApplyTemplate(string template, params (string key, string value)[] pairs) {
    if (string.IsNullOrEmpty(template)) return template;
    foreach (var (key, value) in pairs) {
      template = template.Replace("{" + key + "}", value ?? string.Empty);
    }
    return template;
  }

  // Splits a command line into tokens preserving quoted segments (double quotes).
  // Example: .cmd "this is a text" 1 => tokens: [".cmd", "this is a text", "1"]
  private static string[] SplitArgumentsPreservingQuotes(string input) {
    var parts = new List<string>();
    var sb = new StringBuilder();
    bool inQuotes = false;

    for (int i = 0; i < input.Length; i++) {
      var c = input[i];

      if (c == '"') {
        inQuotes = !inQuotes;
        continue;
      }

      if (char.IsWhiteSpace(c) && !inQuotes) {
        if (sb.Length > 0) {
          parts.Add(sb.ToString());
          sb.Clear();
        }
        continue;
      }

      sb.Append(c);
    }

    if (sb.Length > 0) parts.Add(sb.ToString());

    return [.. parts];
  }

  private static string GetFriendlyTypeName(Type type) {
    if (type == typeof(int)) return "int";
    if (type == typeof(float)) return "float";
    if (type == typeof(double)) return "double";
    if (type == typeof(bool)) return "bool";
    if (type == typeof(string)) return "string";
    if (type == typeof(long)) return "long";
    if (type == typeof(uint)) return "uint";
    if (type == typeof(short)) return "short";
    if (type == typeof(byte)) return "byte";
    return type.Name.ToLowerInvariant();
  }

  /// <summary>
  /// Tries to find a multi-word command match in a dictionary.
  /// Searches from longest possible match down to single word.
  /// Returns the matched command key and remaining args.
  /// </summary>
  private static bool TryFindMultiWordCommand(Dictionary<string, List<CommandInfo>> commands, string[] parts, int startIndex, string playerLanguage, out string matchedCommand, out string[] remainingArgs) {
    matchedCommand = null;
    remainingArgs = null;

    // Try matching from longest to shortest
    // e.g., if parts = ["create", "npc", "here", "arg1"]
    // Try: "create npc here", then "create npc", then "create"
    string defaultCandidate = null;
    string anyCandidate = null;

    for (int wordCount = parts.Length - startIndex; wordCount >= 1; wordCount--) {
      var candidateCommand = string.Join(" ", parts.Skip(startIndex).Take(wordCount)).ToLowerInvariant();

      if (!commands.ContainsKey(candidateCommand)) continue;

      // Determine language mapping for this candidate key
      _commandKeyLanguage.TryGetValue(candidateCommand, out var keyLang);

      // Normalize
      if (string.IsNullOrWhiteSpace(keyLang)) keyLang = null;
      if (string.IsNullOrWhiteSpace(playerLanguage)) playerLanguage = null;

      // Prefer exact language match (including both null)
      if (string.Equals(keyLang, playerLanguage, StringComparison.OrdinalIgnoreCase)) {
        matchedCommand = candidateCommand;
        remainingArgs = [.. parts.Skip(startIndex + wordCount)];
        return true;
      }

      // remember a default (null) candidate if present
      if (keyLang == null && defaultCandidate == null) {
        defaultCandidate = candidateCommand;
      }

      // remember any candidate as last resort
      anyCandidate ??= candidateCommand;
    }

    if (defaultCandidate != null) {
      matchedCommand = defaultCandidate;
      remainingArgs = [.. parts.Skip(startIndex + defaultCandidate.Split(' ').Length)];
      return true;
    }

    if (anyCandidate != null) {
      matchedCommand = anyCandidate;
      remainingArgs = [.. parts.Skip(startIndex + anyCandidate.Split(' ').Length)];
      return true;
    }

    return false;
  }

  internal static void HandleMessageEvents(NativeArray<Entity> messageEntities) {
    foreach (var messageEntity in messageEntities) {
      HandleChat(messageEntity);
    }
  }

  private static void HandleChat(Entity messageEntity) {
    try {
      if (!messageEntity.Exists() || !messageEntity.Has<ChatMessageEvent>()) return;
      var chat = messageEntity.Read<ChatMessageEvent>();
      if (chat.MessageType != ChatMessageType.Local) return;

      var text = chat.MessageText.Value?.Trim();
      if (string.IsNullOrEmpty(text)) return;

      if (!text.StartsWith('.')) return; // not a command

      // remove leading dot and split (preserve quoted segments)
      var withoutDot = text[1..].Trim();
      var parts = SplitArgumentsPreservingQuotes(withoutDot);
      if (parts == null || parts.Length == 0) return;

      var firstPart = parts[0].ToLowerInvariant();

      // resolve sender (best-effort)
      PlayerData player = null;
      if (messageEntity.Has<FromCharacter>()) {
        try {
          var fromChar = messageEntity.Read<FromCharacter>();
          player = fromChar.Character.GetPlayerData();
        } catch (Exception ex) {
          Log.Warning($"Failed resolving player from message entity {messageEntity}: {ex}");
        }
      }

      // determine player's language (null if not set)
      var playerLanguage = (player != null) ? LocalizationService.GetPlayerLanguage(player) : null;
      if (!string.IsNullOrWhiteSpace(playerLanguage)) playerLanguage = playerLanguage.ToLowerInvariant().Trim();

      // Try as standalone command first (no group)
      // Try multi-word matching for standalone commands
      if (TryFindMultiWordCommand(_noGroupCommands, parts, 0, playerLanguage, out var standaloneCommand, out var standaloneArgs)) {
        var standaloneCmdInfos = _noGroupCommands[standaloneCommand];
        var ctx = new CommandContext(messageEntity, player, withoutDot, standaloneArgs);

        if (!SelectBestCommand(standaloneCmdInfos, ctx, standaloneArgs, out var selected, out var invokeArgs, out var selectError)) {
          if (!string.IsNullOrEmpty(selectError)) ctx.ReplyError(selectError);
          var usages = string.Join("\n", standaloneCmdInfos.Select(ci => ci.Attribute.Usage).Where(u => !string.IsNullOrEmpty(u)));
          if (!string.IsNullOrEmpty(usages)) {
            var tmpl = LocalizationService.Get(player, "cmd_available_usages");
            ctx.ReplyInfo(ApplyTemplate(tmpl, ("usages", $"~{usages}~")));
          }
          return;
        }

        var requiresAdmin = selected.GroupAdminOnly || selected.Attribute.AdminOnly;
        if (requiresAdmin && (player == null || !player.IsAdmin)) {
          ctx.ReplyError(LocalizationService.Get(player, "cmd_requires_admin"));
          return;
        }

        try {
          // Ensure context uses the command's declaring assembly for localization
          ctx.CallingAssembly = selected.Assembly;
          selected.Method.Invoke(null, invokeArgs);
        } catch (Exception invokeEx) {
          Log.Error($"Error invoking standalone command {standaloneCommand}: {invokeEx}");
          ctx.ReplyError(LocalizationService.Get(player, "cmd_execution_error"));
        }

        messageEntity.Destroy(true);
        return;
      }

      // Try as grouped command (.group command args)
      var group = firstPart;

      if (!_groups.TryGetValue(group, out var cmds)) return;

      // Try to find multi-word command match starting after the group name
      if (parts.Length < 2) {
        // Inform the player that the group requires a subcommand
        if (player != null) {
          var msg = LocalizationService.Get(player, "cmd_group_no_subcommand", group);
          MessageService.SendRaw(player, msg.FormatInfo());
        }
        return;
      }

      if (!TryFindMultiWordCommand(cmds, parts, 1, playerLanguage, out var matchedCommand, out var groupArgs)) {
        // Inform the player that the subcommand is unknown
        if (player != null) {
          var unknown = string.Join(" ", parts.Skip(1));
          var msg = LocalizationService.Get(player, "cmd_unknown_group_command", group, unknown);
          MessageService.SendRaw(player, msg.FormatError());
        }
        return;
      }

      var cmdInfos = cmds[matchedCommand];
      var groupCtx = new CommandContext(messageEntity, player, withoutDot, groupArgs);

      if (!SelectBestCommand(cmdInfos, groupCtx, groupArgs, out var selectedCmd, out var groupInvokeArgs, out var groupSelectError)) {
        if (!string.IsNullOrEmpty(groupSelectError)) groupCtx.ReplyError(groupSelectError);
        var usages = string.Join("\n", cmdInfos.Select(ci => ci.Attribute.Usage).Where(u => !string.IsNullOrEmpty(u)));
        if (!string.IsNullOrEmpty(usages)) {
          var tmpl = LocalizationService.Get(player, "cmd_available_usages");
          groupCtx.ReplyInfo(ApplyTemplate(tmpl, ("usages", $"~{usages}~")));
        }
        return;
      }

      var groupRequiresAdmin = selectedCmd.GroupAdminOnly || selectedCmd.Attribute.AdminOnly;
      if (groupRequiresAdmin && (player == null || !player.IsAdmin)) {
        groupCtx.ReplyError(LocalizationService.Get(player, "cmd_requires_admin"));
        return;
      }

      try {
        // Ensure group context uses the command's declaring assembly for localization
        groupCtx.CallingAssembly = selectedCmd.Assembly;
        selectedCmd.Method.Invoke(null, groupInvokeArgs);
      } catch (Exception invokeEx) {
        Log.Error($"Error invoking command {group} {matchedCommand}: {invokeEx}");
        groupCtx.ReplyError(LocalizationService.Get(player, "cmd_execution_error"));
      }

      messageEntity.Destroy(true);
    } catch (Exception ex) {
      Log.Error($"CommandService.HandleChat failed: {ex}");
    }
  }

  private static bool TryParseParameter(Type paramType, string value, out object result) {
    result = null;

    if (value == null) return false;

    value = value.Trim();

    try {
      var underlying = Nullable.GetUnderlyingType(paramType);
      if (underlying != null) {
        if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase)) {
          result = null;
          return true;
        }
        return TryParseParameter(underlying, value, out result);
      }

      if (paramType == typeof(string)) { result = value; return true; }

      if (paramType.IsEnum) {
        try { result = Enum.Parse(paramType, value, true); return true; } catch { return false; }
      }

      if (paramType == typeof(Guid)) { if (Guid.TryParse(value, out var g)) { result = g; return true; } return false; }

      if (paramType == typeof(DateTime)) {
        if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt)) { result = dt; return true; }
        if (DateTime.TryParse(value, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dt)) { result = dt; return true; }
        return false;
      }

      if (paramType == typeof(TimeSpan)) { if (TimeSpan.TryParse(value, out var ts)) { result = ts; return true; } return false; }

      if (paramType == typeof(int)) {
        if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v)) { result = v; return true; }
        if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.CurrentCulture, out v)) { result = v; return true; }
        return false;
      }

      if (paramType == typeof(long)) {
        if (long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v)) { result = v; return true; }
        if (long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.CurrentCulture, out v)) { result = v; return true; }
        return false;
      }

      if (paramType == typeof(uint)) {
        if (uint.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v)) { result = v; return true; }
        if (uint.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.CurrentCulture, out v)) { result = v; return true; }
        return false;
      }

      if (paramType == typeof(short)) {
        if (short.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v)) { result = v; return true; }
        if (short.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.CurrentCulture, out v)) { result = v; return true; }
        return false;
      }

      if (paramType == typeof(byte)) {
        if (byte.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v)) { result = v; return true; }
        if (byte.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.CurrentCulture, out v)) { result = v; return true; }
        return false;
      }

      if (paramType == typeof(float)) {
        if (float.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var v)) { result = v; return true; }
        if (float.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.CurrentCulture, out v)) { result = v; return true; }
        return false;
      }

      if (paramType == typeof(double)) {
        if (double.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var v)) { result = v; return true; }
        if (double.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.CurrentCulture, out v)) { result = v; return true; }
        return false;
      }

      if (paramType == typeof(bool)) {
        if (bool.TryParse(value, out var b)) { result = b; return true; }
        var vv = value.Trim().ToLowerInvariant();
        if (vv == "1" || vv == "yes" || vv == "y" || vv == "true") { result = true; return true; }
        if (vv == "0" || vv == "no" || vv == "n" || vv == "false") { result = false; return true; }
        return false;
      }

      if (paramType == typeof(float3)) {
        var parts = value.Split(',');
        if (parts.Length != 3) return false;
        if (!float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)) return false;
        if (!float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y)) return false;
        if (!float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var z)) return false;
        result = new float3(x, y, z);
        return true;
      }

      if (paramType == typeof(float2)) {
        var parts = value.Split(',');
        if (parts.Length != 2) return false;
        if (!float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)) return false;
        if (!float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y)) return false;
        result = new float2(x, y);
        return true;
      }

      try {
        result = Convert.ChangeType(value, paramType, System.Globalization.CultureInfo.InvariantCulture);
        return true;
      } catch {
        return false;
      }
    } catch (Exception ex) {
      Log.Debug($"TryParseParameter failed for type {paramType} value '{value}': {ex}");
      return false;
    }
  }

  // Evaluate a candidate CommandInfo against provided args. Returns true if candidate is compatible.
  // Also produces a score used to choose the best overload (higher is better).
  private static bool EvaluateCandidate(CommandInfo cmdInfo, CommandContext ctx, string[] args, out object[] invokeArgs, out int score, out string errorMsg) {
    errorMsg = null;
    score = 0;
    var method = cmdInfo.Method;
    var parameters = method.GetParameters();
    invokeArgs = new object[parameters.Length];

    int contextParamCount = 0;
    if (parameters.Length > 0 && parameters[0].ParameterType == typeof(CommandContext)) {
      invokeArgs[0] = ctx;
      contextParamCount = 1;
    }

    for (int i = contextParamCount; i < parameters.Length; i++) {
      var param = parameters[i];
      var argIndex = i - contextParamCount;
      var hasValue = argIndex < args.Length;
      var providedValue = hasValue ? args[argIndex] : null;
      bool isOptional = param.HasDefaultValue;

      if (!hasValue) {
        if (!isOptional) {
          var tmpl = LocalizationService.Get(ctx?.Sender, "cmd_missing_required_param");
          errorMsg = ApplyTemplate(tmpl, ("paramName", param.Name), ("paramType", GetFriendlyTypeName(param.ParameterType)));
          return false;
        }
        invokeArgs[i] = param.DefaultValue;
        score += 1; // small score for using default
        continue;
      }

      // PlayerData special handling
      if (param.ParameterType == typeof(PlayerData)) {
        if (!PlayerService.TryGetByName(providedValue, out var playerData)) {
          var tmpl = LocalizationService.Get(ctx?.Sender, "cmd_player_not_found");
          errorMsg = ApplyTemplate(tmpl, ("playerName", providedValue));
          return false;
        }
        invokeArgs[i] = playerData;
        score += 5; // strong match for non-string player
        continue;
      }

      // Try parse other types
      if (!TryParseParameter(param.ParameterType, providedValue, out var parsed)) {
        var tmpl = LocalizationService.Get(ctx?.Sender, "cmd_invalid_param");
        errorMsg = ApplyTemplate(tmpl, ("paramName", param.Name), ("paramType", GetFriendlyTypeName(param.ParameterType)));
        return false;
      }

      invokeArgs[i] = parsed;

      // Scoring: prefer non-string typed parameters
      if (param.ParameterType == typeof(string)) {
        // string is lowest priority
        score += 1;
      } else {
        score += 3;
      }
    }

    return true;
  }

  // Selects the best command overload from a list based on argument compatibility and scoring.
  private static bool SelectBestCommand(List<CommandInfo> candidates, CommandContext ctx, string[] args, out CommandInfo selected, out object[] invokeArgs, out string errorMsg) {
    selected = null;
    invokeArgs = null;
    errorMsg = null;

    var viable = new List<(CommandInfo info, object[] invokeArgs, int score, string error)>();

    foreach (var ci in candidates) {
      if (!EvaluateCandidate(ci, ctx, args, out var invArgs, out var score, out var err)) {
        // keep parse error for reporting if none match
        viable.Add((ci, null, -1, err));
        continue;
      }
      viable.Add((ci, invArgs, score, null));
    }

    // Filter only successful matches
    var matches = viable.Where(v => v.score >= 0).ToList();
    if (matches.Count == 0) {
      // prefer first error message that is not null
      var firstErr = viable.Select(v => v.error).FirstOrDefault(e => !string.IsNullOrEmpty(e));
      errorMsg = firstErr ?? LocalizationService.Get(ctx?.Sender, "cmd_no_suitable_overload");
      return false;
    }

    // Choose highest score
    var bestScore = matches.Max(m => m.score);
    var bestMatches = matches.Where(m => m.score == bestScore).ToList();

    if (bestMatches.Count > 1) {
      // Ambiguous
      errorMsg = LocalizationService.Get(ctx?.Sender, "cmd_ambiguous_overload");
      return false;
    }

    var best = bestMatches[0];
    selected = best.info;
    invokeArgs = best.invokeArgs;
    return true;
  }

  // Register localization keys used by CommandService
  private static void RegisterLocalizationKeys() {
    // Use simple english and portuguese translations. Calling assembly is used by LocalizationService.
    LocalizationService.NewKey("cmd_requires_admin", new Dictionary<string, string> {
      { Language.English, "This command requires administrator privileges." },
      { Language.Portuguese, "Este comando requer privilégios de administrador." },
      { Language.French, "Cette commande nécessite des privilèges d'administrateur." },
      { Language.German, "Dieser Befehl erfordert Administratorrechte." },
      { Language.Hungarian, "Ehhez a parancshoz rendszergazdai jogosultság szükséges." },
      { Language.Italian, "Questo comando richiede privilegi di amministratore." },
      { Language.Japanese, "このコマンドは管理者権限が必要です。" },
      { Language.Korean, "이 명령은 관리자 권한이 필요합니다." },
      { Language.Latam, "Este comando requiere privilegios de administrador." },
      { Language.Polish, "Ta komenda wymaga uprawnień administratora." },
      { Language.Russian, "Для этой команды требуются права администратора." },
      { Language.Spanish, "Este comando requiere privilegios de administrador." },
      { Language.ChineseSimplified, "此命令需要管理员权限。" },
      { Language.ChineseTraditional, "此指令需要管理員權限。" },
      { Language.Thai, "คำสั่งนี้ต้องการสิทธิ์ผู้ดูแลระบบ" },
      { Language.Turkish, "Bu komut için yönetici ayrıcalıkları gerekir." },
      { Language.Ukrainian, "Ця команда вимагає прав адміністратора." },
      { Language.Vietnamese, "Lệnh này yêu cầu quyền quản trị viên." }
    });

    LocalizationService.NewKey("cmd_execution_error", new Dictionary<string, string> {
      { Language.English, "An error occurred while executing the command." },
      { Language.Portuguese, "Ocorreu um erro ao executar o comando." },
      { Language.French, "Une erreur est survenue lors de l'exécution de la commande." },
      { Language.German, "Beim Ausführen des Befehls ist ein Fehler aufgetreten." },
      { Language.Hungarian, "Hiba történt a parancs végrehajtása közben." },
      { Language.Italian, "Si è verificato un errore durante l'esecuzione del comando." },
      { Language.Japanese, "コマンドの実行中にエラーが発生しました。" },
      { Language.Korean, "명령을 실행하는 중에 오류가 발생했습니다." },
      { Language.Latam, "Ocurrió un error al ejecutar el comando." },
      { Language.Polish, "Wystąpił błąd podczas wykonywania polecenia." },
      { Language.Russian, "Произошла ошибка при выполнении команды." },
      { Language.Spanish, "Ocurrió un error al ejecutar el comando." },
      { Language.ChineseSimplified, "执行命令时发生错误。" },
      { Language.ChineseTraditional, "執行指令時發生錯誤。" },
      { Language.Thai, "เกิดข้อผิดพลาดขณะเรียกใช้คำสั่ง" },
      { Language.Turkish, "Komut yürütülürken bir hata oluştu." },
      { Language.Ukrainian, "Під час виконання команди сталася помилка." },
      { Language.Vietnamese, "Đã xảy ra lỗi khi thực hiện lệnh." }
    });

    LocalizationService.NewKey("cmd_available_usages", new Dictionary<string, string> {
      { Language.English, "Available usages:\n{usages}" },
      { Language.Portuguese, "Formas de uso disponíveis:\n{usages}" },
      { Language.French, "Utilisations disponibles :\n{usages}" },
      { Language.German, "Verfügbare Aufrufe:\n{usages}" },
      { Language.Hungarian, "Elérhető használatok:\n{usages}" },
      { Language.Italian, "Utilizzi disponibili:\n{usages}" },
      { Language.Japanese, "利用可能な使い方:\n{usages}" },
      { Language.Korean, "사용 가능한 명령 형식:\n{usages}" },
      { Language.Latam, "Usos disponibles:\n{usages}" },
      { Language.Polish, "Dostępne użycia:\n{usages}" },
      { Language.Russian, "Доступные варианты использования:\n{usages}" },
      { Language.Spanish, "Usos disponibles:\n{usages}" },
      { Language.ChineseSimplified, "可用用法：\n{usages}" },
      { Language.ChineseTraditional, "可用用法：\n{usages}" },
      { Language.Thai, "รูปแบบที่ใช้ได้: {usages}" },
      { Language.Turkish, "Kullanılabilir kullanımlar:\n{usages}" },
      { Language.Ukrainian, "Доступні варіанти використання:\n{usages}" },
      { Language.Vietnamese, "Cách sử dụng có sẵn:\n{usages}" }
    });

    LocalizationService.NewKey("cmd_ambiguous_overload", new Dictionary<string, string> {
      { Language.English, "Ambiguous command overload; multiple matches found." },
      { Language.Portuguese, "Sobrecarga ambígua do comando; múltiplas correspondências encontradas." },
      { Language.French, "Surcharge de commande ambiguë ; plusieurs correspondances trouvées." },
      { Language.German, "Mehrdeutige Befehlsüberladung; mehrere Treffer gefunden." },
      { Language.Hungarian, "Többértelmű parancs overload; több egyezés található." },
      { Language.Italian, "Sovraccarico di comando ambiguo; trovate più corrispondenze." },
      { Language.Japanese, "あいまいなコマンドのオーバーロードです。複数の一致が見つかりました。" },
      { Language.Korean, "모호한 명령 오버로드; 여러 일치 항목이 발견되었습니다." },
      { Language.Latam, "Sobrecarga de comando ambigua; se encontraron múltiples coincidencias." },
      { Language.Polish, "Niejednoznaczne przeciążenie polecenia; znaleziono wiele dopasowań." },
      { Language.Russian, "Неоднозначная перегрузка команды; найдено несколько совпадений." },
      { Language.Spanish, "Sobrecarga de comando ambigua; se encontraron múltiples coincidencias." },
      { Language.ChineseSimplified, "命令重载不明确；找到多个匹配项。" },
      { Language.ChineseTraditional, "指令重載不明確；找到多個匹配項。" },
      { Language.Thai, "การโอเวอร์โหลดคำสั่งไม่ชัดเจน; พบการจับคู่หลายรายการ" },
      { Language.Turkish, "Belirsiz komut aşırı yüklemesi; birden çok eşleşme bulundu." },
      { Language.Ukrainian, "Двоозначне перевантаження команди; знайдено кілька збігів." },
      { Language.Vietnamese, "Overload lệnh không rõ ràng; tìm thấy nhiều kết quả khớp." }
    });

    LocalizationService.NewKey("cmd_group_no_subcommand", new Dictionary<string, string> {
      { Language.English, "This command group requires a subcommand." },
      { Language.Portuguese, "Este grupo de comandos requer um subcomando." },
      { Language.French, "Ce groupe de commandes nécessite une sous-commande." },
      { Language.German, "Diese Befehlsgruppe erfordert einen Unterbefehl." },
      { Language.Hungarian, "Ehhez a parancscsoporthoz alparancs szükséges." },
      { Language.Italian, "Questo gruppo di comandi richiede un sottocomando." },
      { Language.Japanese, "このコマンドグループはサブコマンドが必要です。" },
      { Language.Korean, "이 명령 그룹은 하위 명령이 필요합니다." },
      { Language.Latam, "Este grupo de comandos requiere un subcomando." },
      { Language.Polish, "Ta grupa poleceń wymaga podpolecenia." },
      { Language.Russian, "Эта группа команд требует подкоманды." },
      { Language.Spanish, "Este grupo de comandos requiere un subcomando." },
      { Language.ChineseSimplified, "此命令组需要子命令。" },
      { Language.ChineseTraditional, "此指令群組需要子指令。" },
      { Language.Thai, "กลุ่มคำสั่งนี้ต้องการคำสั่งย่อย" },
      { Language.Turkish, "Bu komut grubu bir alt komut gerektirir." },
      { Language.Ukrainian, "Ця група команд вимагає підкоманди." },
      { Language.Vietnamese, "Nhóm lệnh này yêu cầu một lệnh phụ." }
    });

    LocalizationService.NewKey("cmd_unknown_group_command", new Dictionary<string, string> {
      { Language.English, "Unknown command in group '{0}': {1}" },
      { Language.Portuguese, "Comando desconhecido no grupo '{0}': {1}" },
      { Language.French, "Commande inconnue dans le groupe '{0}' : {1}" },
      { Language.German, "Unbekannter Befehl in Gruppe '{0}': {1}" },
      { Language.Hungarian, "Ismeretlen parancs a(z) '{0}' csoportban: {1}" },
      { Language.Italian, "Comando sconosciuto nel gruppo '{0}': {1}" },
      { Language.Japanese, "グループ '{0}' の不明なコマンド: {1}" },
      { Language.Korean, "그룹 '{0}'에서 알 수 없는 명령: {1}" },
      { Language.Latam, "Comando desconocido en el grupo '{0}': {1}" },
      { Language.Polish, "Nieznane polecenie w grupie '{0}': {1}" },
      { Language.Russian, "Неизвестная команда в группе '{0}': {1}" },
      { Language.Spanish, "Comando desconocido en el grupo '{0}': {1}" },
      { Language.ChineseSimplified, "组 '{0}' 中的未知命令：{1}" },
      { Language.ChineseTraditional, "群組 '{0}' 中的未知指令：{1}" },
      { Language.Thai, "คำสั่งไม่รู้จักในกลุ่ม '{0}': {1}" },
      { Language.Turkish, "'{0}' grubunda bilinmeyen komut: {1}" },
      { Language.Ukrainian, "Невідома команда в групі '{0}': {1}" },
      { Language.Vietnamese, "Lệnh không xác định trong nhóm '{0}': {1}" }
    });


  }

  /// <summary>
  /// Returns all commands grouped by assembly name. The optional playerLanguage
  /// will be used to prefer language-specific aliases when available.
  /// </summary>
  public static Dictionary<string, string[]> GetAllCommands(string playerLanguage = null) {
    var cacheKey = string.IsNullOrWhiteSpace(playerLanguage) ? string.Empty : playerLanguage.ToLowerInvariant().Trim();
    if (_commandsCache.TryGetValue(cacheKey, out var cached)) return cached;

    var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    // Map each CommandInfo to its list of candidate representations
    var ciCandidates = new Dictionary<CommandInfo, List<(string rep, string keyLang)>>();

    // Helper to add candidate
    void AddCandidate(CommandInfo ci, string rep, string keyLang) {
      if (!ciCandidates.TryGetValue(ci, out var list)) {
        list = [];
        ciCandidates[ci] = list;
      }
      list.Add((rep, string.IsNullOrWhiteSpace(keyLang) ? null : keyLang.ToLowerInvariant().Trim()));
    }

    // Collect from standalone commands
    foreach (var kvp in _noGroupCommands) {
      var key = kvp.Key.ToLowerInvariant();
      foreach (var ci in kvp.Value) {
        var usage = ci.Attribute?.Usage;
        string rep;
        if (!string.IsNullOrWhiteSpace(usage) && ci.Attribute != null) {
          // replace the original command name in the usage with the key (alias)
          var orig = ci.Attribute.Name ?? string.Empty;
          var idx = usage.IndexOf(orig, StringComparison.OrdinalIgnoreCase);
          if (idx >= 0) {
            rep = string.Concat(usage.AsSpan(0, idx), key, usage.AsSpan(idx + orig.Length));
            // ensure leading dot if missing
            if (!rep.StartsWith('.')) rep = "." + rep;
          } else {
            rep = $".{key}" + (usage.StartsWith('.') ? usage[usage.IndexOf(' ')..] : "");
          }
        } else {
          rep = $".{key}";
        }
        _commandKeyLanguage.TryGetValue(key, out var keyLang);
        AddCandidate(ci, rep, keyLang);
      }
    }

    // Collect from grouped commands
    foreach (var groupKvp in _groups) {
      var group = groupKvp.Key.ToLowerInvariant();
      foreach (var cmdKvp in groupKvp.Value) {
        var cmdKey = cmdKvp.Key.ToLowerInvariant();
        foreach (var ci in cmdKvp.Value) {
          var usage = ci.Attribute?.Usage;
          string rep;
          if (!string.IsNullOrWhiteSpace(usage) && ci.Attribute != null) {
            var orig = ci.Attribute.Name ?? string.Empty;
            var idx = usage.IndexOf(orig, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) {
              rep = string.Concat(usage.AsSpan(0, idx), cmdKey, usage.AsSpan(idx + orig.Length));
              if (!rep.StartsWith('.')) rep = "." + rep;
            } else {
              rep = $".{group} {cmdKey}";
            }
          } else {
            rep = $".{group} {cmdKey}";
          }
          _commandKeyLanguage.TryGetValue(cmdKey, out var keyLang);
          AddCandidate(ci, rep, keyLang);
        }
      }
    }

    // Choose best representation per CommandInfo and add to assembly buckets
    foreach (var kv in ciCandidates) {
      var ci = kv.Key;
      var candidates = kv.Value;
      string chosen = null;

      if (!string.IsNullOrWhiteSpace(playerLanguage)) {
        var pl = playerLanguage.ToLowerInvariant().Trim();
        var match = candidates.FirstOrDefault(c => string.Equals(c.keyLang, pl, StringComparison.OrdinalIgnoreCase));
        if (match != default) chosen = match.rep;
      }

      if (chosen == null) {
        var def = candidates.FirstOrDefault(c => c.keyLang == null);
        if (def != default) chosen = def.rep;
      }

      chosen ??= candidates[0].rep;

      var asmName = ci.Assembly?.GetName()?.Name ?? "(unknown)";
      if (!result.TryGetValue(asmName, out var set)) {
        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        result[asmName] = set;
      }
      set.Add(chosen);
    }

    // Convert HashSets to arrays and cache result
    var final = result.ToDictionary(k => k.Key, v => v.Value.OrderBy(x => x).ToArray(), StringComparer.OrdinalIgnoreCase);
    _commandsCache[cacheKey] = final;
    return final;
  }

  /// <summary>
  /// Returns commands for a specific assembly name (by assembly.GetName().Name).
  /// </summary>
  public static string[] GetAssemblyCommands(string assemblyName, string playerLanguage = null) {
    if (string.IsNullOrWhiteSpace(assemblyName)) return [];
    var all = GetAllCommands(playerLanguage);
    if (all.TryGetValue(assemblyName, out var arr)) return arr;
    return [];
  }
}