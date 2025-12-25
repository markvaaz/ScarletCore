using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ProjectM.Network;
using Unity.Mathematics;
using ScarletCore.Data;
using ScarletCore.Events;
using ScarletCore.Utils;
using Unity.Entities;

namespace ScarletCore.Services;

/// <summary>
/// Context passed to command handlers.
/// </summary>
public sealed class CommandContext {
  public Entity MessageEntity { get; }
  public PlayerData Sender { get; }
  public string Raw { get; }
  public string[] Args { get; }

  public CommandContext(Entity messageEntity, PlayerData sender, string raw, string[] args) {
    MessageEntity = messageEntity;
    Sender = sender;
    Raw = raw;
    Args = args;
  }

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
}

/// <summary>
/// Stores command metadata including attributes and method info.
/// </summary>
internal sealed class CommandInfo {
  public MethodInfo Method { get; set; }
  public SCommandAttribute Attribute { get; set; }
  public bool GroupAdminOnly { get; set; }
  public Assembly Assembly { get; set; }
}

/// <summary>
/// Responsible for scanning command classes, detecting `.group` messages and invoking handlers.
/// Supports N parameters with optional and required parameters using default values.
/// </summary>
public static class CommandService {
  private static readonly Dictionary<string, Dictionary<string, List<CommandInfo>>> _groups = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<string, Assembly> _groupToAssembly = new(StringComparer.OrdinalIgnoreCase);
  private static bool _initialized = false;

  public static void Initialize() {
    if (_initialized) return;

    RegisterCommands();

    EventManager.On(PrefixEvents.OnChatMessage, (entities) => {
      foreach (var e in entities) HandleChat(e);
    });

    _initialized = true;
    Log.Info($"CommandService initialized with {_groups.Count} groups");
  }

  /// <summary>
  /// Registers all commands from the executing assembly.
  /// </summary>
  public static void RegisterCommands() {
    var asm = Assembly.GetExecutingAssembly();
    RegisterAssembly(asm);
  }

  /// <summary>
  /// Registers all commands from a specific assembly.
  /// </summary>
  public static void RegisterAssembly(Assembly assembly) {
    foreach (var type in assembly.GetTypes()) {
      var grpAttr = type.GetCustomAttribute<SCommandGroupAttribute>();
      if (grpAttr == null) continue;

      var groupName = grpAttr.Group.ToLower();
      var groupAdminOnly = grpAttr.AdminOnly;

      // Register main group name
      if (!_groups.ContainsKey(groupName)) {
        _groups[groupName] = new Dictionary<string, List<CommandInfo>>(StringComparer.OrdinalIgnoreCase);
        _groupToAssembly[groupName] = assembly;
      }

      // Register group aliases
      foreach (var alias in grpAttr.Aliases) {
        var aliasLower = alias.ToLower();
        if (!_groups.ContainsKey(aliasLower)) {
          _groups[aliasLower] = _groups[groupName];
          _groupToAssembly[aliasLower] = assembly;
        }
      }

      foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
        var cmdAttr = method.GetCustomAttribute<SCommandAttribute>();
        if (cmdAttr == null) continue;

        // Generate usage if empty
        if (string.IsNullOrEmpty(cmdAttr.Usage)) {
          cmdAttr.Usage = GenerateUsage(groupName, cmdAttr.Name, method);
        }

        var cmdInfo = new CommandInfo {
          Method = method,
          Attribute = cmdAttr,
          GroupAdminOnly = groupAdminOnly,
          Assembly = assembly
        };

        var cmdName = cmdAttr.Name.ToLower();
        if (!_groups[groupName].TryGetValue(cmdName, out var list)) {
          list = new List<CommandInfo>();
          _groups[groupName][cmdName] = list;
        }
        list.Add(cmdInfo);

        // Register command aliases
        foreach (var alias in cmdAttr.Aliases) {
          var aliasLower = alias.ToLower();
          if (!_groups[groupName].TryGetValue(aliasLower, out var aliasList)) {
            aliasList = new List<CommandInfo>();
            _groups[groupName][aliasLower] = aliasList;
          }
          aliasList.Add(cmdInfo);
        }
      }
    }

    Log.Info($"Registered commands from assembly: {assembly.GetName().Name}");
  }

  /// <summary>
  /// Unregisters all commands from a specific assembly.
  /// </summary>
  public static void UnregisterAssembly(Assembly assembly) {
    var groupsToRemove = new List<string>();

    // Find all groups from this assembly
    foreach (var kvp in _groupToAssembly) {
      if (kvp.Value == assembly) {
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
        // cmdKvp.Value is List<CommandInfo>
        if (cmdKvp.Value.Any(ci => ci.Assembly == assembly)) {
          commandsToRemove.Add((groupKvp.Key, cmdKvp.Key));
        }
      }
    }

    foreach (var (group, command) in commandsToRemove) {
      if (_groups.TryGetValue(group, out var cmds)) {
        if (cmds.TryGetValue(command, out var list)) {
          list.RemoveAll(ci => ci.Assembly == assembly);
          if (list.Count == 0) cmds.Remove(command);
        }
      }
    }

    Log.Info($"Unregistered commands from assembly: {assembly.GetName().Name} ({groupsToRemove.Count} groups removed)");
  }

  private static string GenerateUsage(string group, string command, MethodInfo method) {
    var parameters = method.GetParameters();
    var usage = $".{group} {command}";

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

    return parts.ToArray();
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
    return type.Name.ToLower();
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
      var withoutDot = text.Substring(1).Trim();
      var parts = SplitArgumentsPreservingQuotes(withoutDot);
      if (parts == null || parts.Length == 0) return;

      var group = parts[0].ToLower();

      // Example usage: .sc help 1 -> group=sc, command=help, args=[1]
      var command = parts.Length > 1 ? parts[1].ToLower() : string.Empty;
      var args = parts.Length > 2 ? parts.Skip(2).ToArray() : Array.Empty<string>();

      if (!_groups.TryGetValue(group, out var cmds)) return;
      if (string.IsNullOrEmpty(command)) {
        // no command provided, just notify — let other handlers process
        Log.Info($"Command group '{group}' invoked without subcommand.");
        return;
      }

      if (!cmds.TryGetValue(command, out var cmdInfos)) {
        Log.Info($"Unknown command '{command}' in group '{group}'");
        return;
      }

      // resolve sender (best-effort)
      PlayerData player = null;
      if (messageEntity.Has<FromCharacter>()) {
        try {
          var fromChar = messageEntity.Read<FromCharacter>();
          player = fromChar.Character.GetPlayerData();
        } catch { }
      }

      var ctx = new CommandContext(messageEntity, player, withoutDot, args);

      // Select best overload among cmdInfos
      if (!SelectBestCommand(cmdInfos, ctx, args, out var selected, out var invokeArgs, out var selectError)) {
        if (!string.IsNullOrEmpty(selectError)) ctx.ReplyError(selectError);
        // Provide usages for ambiguity or parse errors (highlight usages)
        var usages = string.Join(" | ", cmdInfos.Select(ci => ci.Attribute.Usage).Where(u => !string.IsNullOrEmpty(u)));
        if (!string.IsNullOrEmpty(usages)) ctx.ReplyInfo($"Available usages: ~{usages}~");
        // Do not destroy message here so other command frameworks can handle it
        return;
      }

      // Check admin permissions for the selected overload
      var requiresAdmin = selected.GroupAdminOnly || selected.Attribute.AdminOnly;
      if (requiresAdmin && (player == null || !player.IsAdmin)) {
        ctx.ReplyError("~This command requires administrator privileges.~");
        // Do not destroy; allow other frameworks to handle the message
        return;
      }

      try {
        selected.Method.Invoke(null, invokeArgs);
      } catch (Exception invokeEx) {
        Log.Error($"Error invoking command {group} {command}: {invokeEx}");
        ctx.ReplyError("~An error occurred while executing the command.~");
      }

      // hide command message from further processing
      messageEntity.Destroy(true);
    } catch (Exception ex) {
      Log.Error($"CommandService.HandleChat failed: {ex}");
    }
  }

  private static bool TryPrepareInvokeArgs(MethodInfo method, CommandContext ctx, string[] args, out object[] invokeArgs, out string errorMsg) {
    errorMsg = null;
    var parameters = method.GetParameters();
    invokeArgs = new object[parameters.Length];

    // Track if first parameter is CommandContext
    int contextParamCount = 0;
    if (parameters.Length > 0 && parameters[0].ParameterType == typeof(CommandContext)) {
      invokeArgs[0] = ctx;
      contextParamCount = 1;
    }

    // Map remaining parameters to args
    for (int i = contextParamCount; i < parameters.Length; i++) {
      var param = parameters[i];
      var argIndex = i - contextParamCount;
      var hasValue = argIndex < args.Length;
      var providedValue = hasValue ? args[argIndex] : null;

      // Check if parameter is optional
      bool isOptional = param.HasDefaultValue;

      // If parameter is required and no value provided, error
      if (!isOptional && !hasValue) {
        errorMsg = $"Missing required parameter: **{param.Name}** (~{GetFriendlyTypeName(param.ParameterType)}~)";
        return false;
      }

      // Special-case PlayerData: resolve player by name via PlayerService
      if (param.ParameterType == typeof(PlayerData)) {
        if (!hasValue) {
          if (!isOptional) {
            errorMsg = $"Missing required parameter: **{param.Name}** (~{GetFriendlyTypeName(param.ParameterType)}~)";
            return false;
          }
          invokeArgs[i] = param.DefaultValue;
          continue;
        }

        if (!PlayerService.TryGetByName(providedValue, out var playerData)) {
          errorMsg = $"Player not found: ~{providedValue}~";
          return false;
        }

        invokeArgs[i] = playerData;
        continue;
      }

      // Try to parse the value for other parameter types
      if (!TryParseParameter(param.ParameterType, providedValue, out var parsedValue)) {
        if (!isOptional) {
          errorMsg = $"Invalid value for parameter '**{param.Name}**'. Expected ~{GetFriendlyTypeName(param.ParameterType)}~.";
          return false;
        }
        // Use default value for optional parameter
        invokeArgs[i] = param.DefaultValue;
        continue;
      }

      invokeArgs[i] = parsedValue;
    }

    return true;
  }

  private static bool TryParseParameter(Type paramType, string value, out object result) {
    result = null;

    // If no value provided, return false (will use default if optional)
    if (value == null) return false;

    try {
      if (paramType == typeof(string)) {
        result = value;
        return true;
      }

      if (paramType == typeof(int)) {
        if (int.TryParse(value, out var intVal)) {
          result = intVal;
          return true;
        }
        return false;
      }

      if (paramType == typeof(float)) {
        if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatVal)) {
          result = floatVal;
          return true;
        }
        return false;
      }

      if (paramType == typeof(double)) {
        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var doubleVal)) {
          result = doubleVal;
          return true;
        }
        return false;
      }

      if (paramType == typeof(bool)) {
        if (bool.TryParse(value, out var boolVal)) {
          result = boolVal;
          return true;
        }
        // Accept 1/0, yes/no, y/n
        if (value == "1" || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("y", StringComparison.OrdinalIgnoreCase)) {
          result = true;
          return true;
        }
        if (value == "0" || value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("n", StringComparison.OrdinalIgnoreCase)) {
          result = false;
          return true;
        }
        return false;
      }

      if (paramType == typeof(long)) {
        if (long.TryParse(value, out var longVal)) {
          result = longVal;
          return true;
        }
        return false;
      }

      // Support Unity.Mathematics float3 and float2 in format "x,y,z" or "x,y"
      if (paramType == typeof(float3)) {
        var parts = value.Split(',');
        if (parts.Length != 3) return false;
        if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)) return false;
        if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y)) return false;
        if (!float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var z)) return false;
        result = new float3(x, y, z);
        return true;
      }

      if (paramType == typeof(float2)) {
        var parts = value.Split(',');
        if (parts.Length != 2) return false;
        if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)) return false;
        if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y)) return false;
        result = new float2(x, y);
        return true;
      }

      if (paramType == typeof(uint)) {
        if (uint.TryParse(value, out var uintVal)) {
          result = uintVal;
          return true;
        }
        return false;
      }

      if (paramType == typeof(short)) {
        if (short.TryParse(value, out var shortVal)) {
          result = shortVal;
          return true;
        }
        return false;
      }

      if (paramType == typeof(byte)) {
        if (byte.TryParse(value, out var byteVal)) {
          result = byteVal;
          return true;
        }
        return false;
      }

      // Try to use Convert.ChangeType for other types
      result = Convert.ChangeType(value, paramType, System.Globalization.CultureInfo.InvariantCulture);
      return true;
    } catch {
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
          errorMsg = $"Missing required parameter: **{param.Name}** (~{GetFriendlyTypeName(param.ParameterType)}~)";
          return false;
        }
        invokeArgs[i] = param.DefaultValue;
        score += 1; // small score for using default
        continue;
      }

      // PlayerData special handling
      if (param.ParameterType == typeof(PlayerData)) {
        if (!PlayerService.TryGetByName(providedValue, out var playerData)) {
          errorMsg = $"Player not found: ~{providedValue}~";
          return false;
        }
        invokeArgs[i] = playerData;
        score += 5; // strong match for non-string player
        continue;
      }

      // Try parse other types
      if (!TryParseParameter(param.ParameterType, providedValue, out var parsed)) {
        errorMsg = $"Invalid value for parameter '**{param.Name}**'. Expected ~{GetFriendlyTypeName(param.ParameterType)}~.";
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
      errorMsg = firstErr ?? "No suitable overload found for command.";
      return false;
    }

    // Choose highest score
    var bestScore = matches.Max(m => m.score);
    var bestMatches = matches.Where(m => m.score == bestScore).ToList();

    if (bestMatches.Count > 1) {
      // Ambiguous
      errorMsg = "~Ambiguous command overload; multiple matches found.~";
      return false;
    }

    var best = bestMatches[0];
    selected = best.info;
    invokeArgs = best.invokeArgs;
    return true;
  }
}