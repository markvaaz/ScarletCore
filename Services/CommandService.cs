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
public sealed class CommandContext(Entity messageEntity, PlayerData sender, string raw, string[] args) {
  public Entity MessageEntity { get; } = messageEntity;
  public PlayerData Sender { get; } = sender;
  public string Raw { get; } = raw;
  public string[] Args { get; } = args;

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

  public void ReplyLocalized(string key) {
    if (Sender != null) {
      var localized = LocalizationService.Get(Sender, key);
      MessageService.SendRaw(Sender, localized.Format());
    }
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
  public string GroupName { get; set; } // null if no group
}

/// <summary>
/// Responsible for scanning command classes, detecting `.group` messages and invoking handlers.
/// Supports N parameters with optional and required parameters using default values.
/// Now supports commands without groups - can be invoked directly as `.command`
/// Also supports multi-word commands like `.quest create npc`
/// </summary>
public static class CommandService {
  private static readonly Dictionary<string, Dictionary<string, List<CommandInfo>>> _groups = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<string, List<CommandInfo>> _noGroupCommands = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<string, Assembly> _groupToAssembly = new(StringComparer.OrdinalIgnoreCase);
  private static bool _initialized = false;

  public static void Initialize() {
    if (_initialized) return;

    RegisterLocalizationKeys();
    RegisterCommands();

    EventManager.On(PrefixEvents.OnChatMessage, (entities) => {
      foreach (var e in entities) HandleChat(e);
    });

    _initialized = true;
    Log.Info($"CommandService initialized with {_groups.Count} groups and {_noGroupCommands.Count} standalone commands");
  }

  /// <summary>
  /// Registers all commands from the executing assembly.
  /// </summary>
  public static void RegisterCommands() {
    var asm = Assembly.GetExecutingAssembly();
    RegisterAssembly(asm);
  }

  /// <summary>
  /// Registers all commands from a specific assembly. If `assembly` is null,
  /// the currently executing assembly will be used.
  /// </summary>
  public static void RegisterAssembly(Assembly assembly = null) {
    var asm = assembly ?? Assembly.GetExecutingAssembly();
    foreach (var type in asm.GetTypes()) {
      var grpAttr = type.GetCustomAttribute<SCommandGroupAttribute>();

      if (grpAttr != null) {
        // Has group attribute - register as grouped commands
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
            Assembly = asm,
            GroupName = groupName
          };

          var cmdName = cmdAttr.Name.ToLower();
          if (!_groups[groupName].TryGetValue(cmdName, out var list)) {
            list = [];
            _groups[groupName][cmdName] = list;
          }
          list.Add(cmdInfo);

          // Register command aliases
          foreach (var alias in cmdAttr.Aliases) {
            var aliasLower = alias.ToLower();
            if (!_groups[groupName].TryGetValue(aliasLower, out var aliasList)) {
              aliasList = [];
              _groups[groupName][aliasLower] = aliasList;
            }
            aliasList.Add(cmdInfo);
          }
        }
      } else {
        // No group attribute - register as standalone commands
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
          var cmdAttr = method.GetCustomAttribute<SCommandAttribute>();
          if (cmdAttr == null) continue;

          // Generate usage if empty (no group prefix)
          if (string.IsNullOrEmpty(cmdAttr.Usage)) {
            cmdAttr.Usage = GenerateUsage(null, cmdAttr.Name, method);
          }

          var cmdInfo = new CommandInfo {
            Method = method,
            Attribute = cmdAttr,
            GroupAdminOnly = false,
            Assembly = asm,
            GroupName = null
          };

          var cmdName = cmdAttr.Name.ToLower();
          if (!_noGroupCommands.TryGetValue(cmdName, out var list)) {
            list = [];
            _noGroupCommands[cmdName] = list;
          }
          list.Add(cmdInfo);

          // Register command aliases
          foreach (var alias in cmdAttr.Aliases) {
            var aliasLower = alias.ToLower();
            if (!_noGroupCommands.TryGetValue(aliasLower, out var aliasList)) {
              aliasList = [];
              _noGroupCommands[aliasLower] = aliasList;
            }
            aliasList.Add(cmdInfo);
          }
        }
      }
    }

    Log.Info($"Registered commands from assembly: {asm.GetName().Name}");
  }

  /// <summary>
  /// Unregisters all commands from a specific assembly. If `assembly` is null,
  /// the currently executing assembly will be used.
  /// </summary>
  public static void UnregisterAssembly(Assembly assembly = null) {
    var asm = assembly ?? Assembly.GetExecutingAssembly();
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
      }
    }

    Log.Info($"Unregistered commands from assembly: {asm.GetName().Name} ({groupsToRemove.Count} groups removed)");
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
    return type.Name.ToLower();
  }

  /// <summary>
  /// Tries to find a multi-word command match in a dictionary.
  /// Searches from longest possible match down to single word.
  /// Returns the matched command key and remaining args.
  /// </summary>
  private static bool TryFindMultiWordCommand(Dictionary<string, List<CommandInfo>> commands, string[] parts, int startIndex, out string matchedCommand, out string[] remainingArgs) {
    matchedCommand = null;
    remainingArgs = null;

    // Try matching from longest to shortest
    // e.g., if parts = ["create", "npc", "here", "arg1"]
    // Try: "create npc here", then "create npc", then "create"
    for (int wordCount = parts.Length - startIndex; wordCount >= 1; wordCount--) {
      var candidateCommand = string.Join(" ", parts.Skip(startIndex).Take(wordCount)).ToLower();

      if (commands.ContainsKey(candidateCommand)) {
        matchedCommand = candidateCommand;
        remainingArgs = [.. parts.Skip(startIndex + wordCount)];
        return true;
      }
    }

    return false;
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

      var firstPart = parts[0].ToLower();

      // resolve sender (best-effort)
      PlayerData player = null;
      if (messageEntity.Has<FromCharacter>()) {
        try {
          var fromChar = messageEntity.Read<FromCharacter>();
          player = fromChar.Character.GetPlayerData();
        } catch { }
      }

      // Try as standalone command first (no group)
      // Try multi-word matching for standalone commands
      if (TryFindMultiWordCommand(_noGroupCommands, parts, 0, out var standaloneCommand, out var standaloneArgs)) {
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
        Log.Info($"Command group '{group}' invoked without subcommand.");
        return;
      }

      if (!TryFindMultiWordCommand(cmds, parts, 1, out var matchedCommand, out var groupArgs)) {
        Log.Info($"Unknown command in group '{group}': {string.Join(" ", parts.Skip(1))}");
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
      { "english", "This command requires administrator privileges." },
      { "portuguese", "Este comando requer privilégios de administrador." },
      { "french", "Cette commande nécessite des privilèges d'administrateur." },
      { "german", "Dieser Befehl erfordert Administratorrechte." },
      { "hungarian", "Ehhez a parancshoz rendszergazdai jogosultság szükséges." },
      { "italian", "Questo comando richiede privilegi di amministratore." },
      { "japanese", "このコマンドは管理者権限が必要です。" },
      { "korean", "이 명령은 관리자 권한이 필요합니다." },
      { "latam", "Este comando requiere privilegios de administrador." },
      { "polish", "Ta komenda wymaga uprawnień administratora." },
      { "russian", "Для этой команды требуются права администратора." },
      { "spanish", "Este comando requiere privilegios de administrador." },
      { "chinese_simplified", "此命令需要管理员权限。" },
      { "chinese_traditional", "此指令需要管理員權限。" },
      { "thai", "คำสั่งนี้ต้องการสิทธิ์ผู้ดูแลระบบ" },
      { "turkish", "Bu komut için yönetici ayrıcalıkları gerekir." },
      { "ukrainian", "Ця команда вимагає прав адміністратора." },
      { "vietnamese", "Lệnh này yêu cầu quyền quản trị viên." }
    });

    LocalizationService.NewKey("cmd_execution_error", new Dictionary<string, string> {
      { "english", "An error occurred while executing the command." },
      { "portuguese", "Ocorreu um erro ao executar o comando." },
      { "french", "Une erreur est survenue lors de l'exécution de la commande." },
      { "german", "Beim Ausführen des Befehls ist ein Fehler aufgetreten." },
      { "hungarian", "Hiba történt a parancs végrehajtása közben." },
      { "italian", "Si è verificato un errore durante l'esecuzione del comando." },
      { "japanese", "コマンドの実行中にエラーが発生しました。" },
      { "korean", "명령을 실행하는 중에 오류가 발생했습니다." },
      { "latam", "Ocurrió un error al ejecutar el comando." },
      { "polish", "Wystąpił błąd podczas wykonywania polecenia." },
      { "russian", "Произошла ошибка при выполнении команды." },
      { "spanish", "Ocurrió un error al ejecutar el comando." },
      { "chinese_simplified", "执行命令时发生错误。" },
      { "chinese_traditional", "執行指令時發生錯誤。" },
      { "thai", "เกิดข้อผิดพลาดขณะเรียกใช้คำสั่ง" },
      { "turkish", "Komut yürütülürken bir hata oluştu." },
      { "ukrainian", "Під час виконання команди сталася помилка." },
      { "vietnamese", "Đã xảy ra lỗi khi thực hiện lệnh." }
    });

    LocalizationService.NewKey("cmd_available_usages", new Dictionary<string, string> {
      { "english", "Available usages:\n{usages}" },
      { "portuguese", "Formas de uso disponíveis:\n{usages}" },
      { "french", "Utilisations disponibles :\n{usages}" },
      { "german", "Verfügbare Aufrufe:\n{usages}" },
      { "hungarian", "Elérhető használatok:\n{usages}" },
      { "italian", "Utilizzi disponibili:\n{usages}" },
      { "japanese", "利用可能な使い方:\n{usages}" },
      { "korean", "사용 가능한 명령 형식:\n{usages}" },
      { "latam", "Usos disponibles:\n{usages}" },
      { "polish", "Dostępne użycia:\n{usages}" },
      { "russian", "Доступные варианты использования:\n{usages}" },
      { "spanish", "Usos disponibles:\n{usages}" },
      { "chinese_simplified", "可用用法：\n{usages}" },
      { "chinese_traditional", "可用用法：\n{usages}" },
      { "thai", "รูปแบบที่ใช้ได้: {usages}" },
      { "turkish", "Kullanılabilir kullanımlar:\n{usages}" },
      { "ukrainian", "Доступні варіанти використання:\n{usages}" },
      { "vietnamese", "Cách sử dụng có sẵn:\n{usages}" }
    });

    LocalizationService.NewKey("cmd_ambiguous_overload", new Dictionary<string, string> {
      { "english", "Ambiguous command overload; multiple matches found." },
      { "portuguese", "Sobrecarga ambígua do comando; múltiplas correspondências encontradas." },
      { "french", "Surcharge de commande ambiguë ; plusieurs correspondances trouvées." },
      { "german", "Mehrdeutige Befehlsüberladung; mehrere Treffer gefunden." },
      { "hungarian", "Többértelmű parancs overload; több egyezés található." },
      { "italian", "Sovraccarico di comando ambiguo; trovate più corrispondenze." },
      { "japanese", "あいまいなコマンドのオーバーロードです。複数の一致が見つかりました。" },
      { "korean", "모호한 명령 오버로드; 여러 일치 항목이 발견되었습니다." },
      { "latam", "Sobrecarga de comando ambigua; se encontraron múltiples coincidencias." },
      { "polish", "Niejednoznaczne przeciążenie polecenia; znaleziono wiele dopasowań." },
      { "russian", "Неоднозначная перегрузка команды; найдено несколько совпадений." },
      { "spanish", "Sobrecarga de comando ambigua; se encontraron múltiples coincidencias." },
      { "chinese_simplified", "命令重载不明确；找到多个匹配项。" },
      { "chinese_traditional", "指令重載不明確；找到多個匹配項。" },
      { "thai", "การโอเวอร์โหลดคำสั่งไม่ชัดเจน; พบการจับคู่หลายรายการ" },
      { "turkish", "Belirsiz komut aşırı yüklemesi; birden çok eşleşme bulundu." },
      { "ukrainian", "Двоозначне перевантаження команди; знайдено кілька збігів." },
      { "vietnamese", "Overload lệnh không rõ ràng; tìm thấy nhiều kết quả khớp." }
    });

    LocalizationService.NewKey("cmd_no_suitable_overload", new Dictionary<string, string> {
      { "english", "No suitable overload found for command." },
      { "portuguese", "Nenhuma sobrecarga adequada encontrada para o comando." },
      { "french", "Aucune surcharge adaptée trouvée pour la commande." },
      { "german", "Keine geeignete Überladung für den Befehl gefunden." },
      { "hungarian", "Nem található megfelelő overload a parancshoz." },
      { "italian", "Nessuna overload adatta trovata per il comando." },
      { "japanese", "コマンドに適したオーバーロードが見つかりませんでした。" },
      { "korean", "명령에 적합한 오버로드를 찾을 수 없습니다." },
      { "latam", "No se encontró una sobrecarga adecuada para el comando." },
      { "polish", "Nie znaleziono odpowiedniego przeciążenia dla polecenia." },
      { "russian", "Для команды не найдено подходящей перегрузки." },
      { "spanish", "No se encontró una sobrecarga adecuada para el comando." },
      { "chinese_simplified", "未找到适合该命令的重载。" },
      { "chinese_traditional", "未找到適合該指令的重載。" },
      { "thai", "ไม่พบการโอเวอร์โหลดที่เหมาะสมสำหรับคำสั่ง" },
      { "turkish", "Komut için uygun bir overload bulunamadı." },
      { "ukrainian", "Не знайдено відповідного перевантаження для команди." },
      { "vietnamese", "Không tìm thấy overload phù hợp cho lệnh." }
    });

    LocalizationService.NewKey("cmd_player_not_found", new Dictionary<string, string> {
      { "english", "Player not found: {playerName}" },
      { "portuguese", "Jogador não encontrado: {playerName}" },
      { "french", "Joueur introuvable : {playerName}" },
      { "german", "Spieler nicht gefunden: {playerName}" },
      { "hungarian", "A játékos nem található: {playerName}" },
      { "italian", "Giocatore non trovato: {playerName}" },
      { "japanese", "プレイヤーが見つかりません: {playerName}" },
      { "korean", "플레이어를 찾을 수 없음: {playerName}" },
      { "latam", "Jugador no encontrado: {playerName}" },
      { "polish", "Nie znaleziono gracza: {playerName}" },
      { "russian", "Игрок не найден: {playerName}" },
      { "spanish", "Jugador no encontrado: {playerName}" },
      { "chinese_simplified", "未找到玩家：{playerName}" },
      { "chinese_traditional", "未找到玩家：{playerName}" },
      { "thai", "ไม่พบผู้เล่น: {playerName}" },
      { "turkish", "Oyuncu bulunamadı: {playerName}" },
      { "ukrainian", "Гравця не знайдено: {playerName}" },
      { "vietnamese", "Không tìm thấy người chơi: {playerName}" }
    });

    LocalizationService.NewKey("cmd_missing_required_param", new Dictionary<string, string> {
      { "english", "Missing required parameter: **{paramName}** (~{paramType}~)" },
      { "portuguese", "Falta parâmetro obrigatório: **{paramName}** (~{paramType}~)" },
      { "french", "Paramètre requis manquant : **{paramName}** (~{paramType}~)" },
      { "german", "Fehlender erforderlicher Parameter: **{paramName}** (~{paramType}~)" },
      { "hungarian", "Hiányzó kötelező paraméter: **{paramName}** (~{paramType}~)" },
      { "italian", "Manca il parametro richiesto: **{paramName}** (~{paramType}~)" },
      { "japanese", "必須パラメーターがありません: **{paramName}** (~{paramType}~)" },
      { "korean", "필수 매개변수가 누락되었습니다: **{paramName}** (~{paramType}~)" },
      { "latam", "Falta un parámetro requerido: **{paramName}** (~{paramType}~)" },
      { "polish", "Brak wymaganego parametru: **{paramName}** (~{paramType}~)" },
      { "russian", "Отсутствует обязательный параметр: **{paramName}** (~{paramType}~)" },
      { "spanish", "Falta un parámetro requerido: **{paramName}** (~{paramType}~)" },
      { "chinese_simplified", "缺少必需的参数：**{paramName}** (~{paramType}~)" },
      { "chinese_traditional", "缺少必要的參數：**{paramName}** (~{paramType}~)" },
      { "thai", "ขาดพารามิเตอร์ที่จำเป็น: **{paramName}** (~{paramType}~)" },
      { "turkish", "Gerekli parametre eksik: **{paramName}** (~{paramType}~)" },
      { "ukrainian", "Відсутній обов'язковий параметр: **{paramName}** (~{paramType}~)" },
      { "vietnamese", "Thiếu tham số bắt buộc: **{paramName}** (~{paramType}~)" }
    });

    LocalizationService.NewKey("cmd_invalid_param", new Dictionary<string, string> {
      { "english", "Invalid value for parameter '**{paramName}**'. Expected ~{paramType}~." },
      { "portuguese", "Valor inválido para o parâmetro '**{paramName}**'. Esperado ~{paramType}~." },
      { "french", "Valeur invalide pour le paramètre '**{paramName}**'. Attendu ~{paramType}~." },
      { "german", "Ungültiger Wert für den Parameter '**{paramName}**'. Erwartet ~{paramType}~." },
      { "hungarian", "Érvénytelen érték a '**{paramName}**' paraméterhez. Várt ~{paramType}~." },
      { "italian", "Valore non valido per il parametro '**{paramName}**'. Previsto ~{paramType}~." },
      { "japanese", "パラメーター '**{paramName}**' の値が無効です。期待される型: ~{paramType}~。" },
      { "korean", "매개변수 '**{paramName}**'에 대한 잘못된 값입니다. 예상 ~{paramType}~." },
      { "latam", "Valor inválido para el parámetro '**{paramName}**'. Se esperaba ~{paramType}~." },
      { "polish", "Nieprawidłowa wartość parametru '**{paramName}**'. Oczekiwano ~{paramType}~." },
      { "russian", "Неверное значение для параметра '**{paramName}**'. Ожидалось ~{paramType}~." },
      { "spanish", "Valor inválido para el parámetro '**{paramName}**'. Se esperaba ~{paramType}~." },
      { "chinese_simplified", "参数 '**{paramName}**' 的值无效。预期 ~{paramType}~。" },
      { "chinese_traditional", "參數 '**{paramName}**' 的值無效。預期 ~{paramType}~。" },
      { "thai", "ค่าที่ไม่ถูกต้องสำหรับพารามิเตอร์ '**{paramName}**' คาดหวัง ~{paramType}~" },
      { "turkish", "'**{paramName}**' parametresi için geçersiz değer. Beklenen ~{paramType}~." },
      { "ukrainian", "Недійсне значення для параметра '**{paramName}**'. Очікувано ~{paramType}~." },
      { "vietnamese", "Giá trị không hợp lệ cho tham số '**{paramName}**'. Mong đợi ~{paramType}~." }
    });
  }
}