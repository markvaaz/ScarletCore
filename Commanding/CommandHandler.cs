using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ProjectM.Network;
using ScarletCore.Data;
using ScarletCore.Services;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ScarletCore.Commanding;

internal readonly record struct CommandLookupKey(Language Language, int TokenCount, string CommandName);

public static class CommandHandler {
  public const char CommandPrefix = '.';
  private static readonly Dictionary<CommandLookupKey, List<CommandInfo>> CommandsByKey = [];
  private static readonly Dictionary<(int TokenCount, string CommandName), List<CommandInfo>> FallbackCommandsByKey = [];
  private static readonly Dictionary<Assembly, List<CommandLookupKey>> CommandKeysByAssembly = [];
  private static readonly Dictionary<Assembly, List<(int, string)>> FallbackKeysByAssembly = [];

  internal static void Initialize() {
    RegisterLocalizationKeys();
    RegisterAll();
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

      var text = chat.MessageText.Value?.Trim();

      if (string.IsNullOrEmpty(text)) {
        return;
      }

      if (!text.StartsWith(CommandPrefix)) {
        return;
      }

      PlayerData playerSender = null;

      messageEntity.HasWith((ref FromCharacter fromChar) => {
        playerSender = fromChar.Character.GetPlayerData();
      });

      if (playerSender == null) {
        Log.Fatal($"[CommandHandler] Could not determine player from chat message entity {messageEntity.Index} this should never happen.");
        return;
      }

      HandleCommand(playerSender, text, messageEntity);
    } catch (Exception ex) {
      Log.Error($"[CommandHandler] Error handling chat message entity {messageEntity.Index}: {ex}");
    }
  }

  internal static void HandleCommand(PlayerData player, string fullMessageText, Entity messageEntity) {
    var text = fullMessageText.Trim();
    if (string.IsNullOrEmpty(text) || !text.StartsWith(CommandPrefix)) return;
    text = text[1..].Trim();

    var tokens = Tokenize(text);

    if (tokens.Length == 0) {
      return;
    }

    var playerLanguage = player.Language;
    var commandInfo = FindCommand(playerLanguage, tokens);

    if (commandInfo == null) {
      return;
    }

    if (!IsHelpCommand(commandInfo) || !IsVCFLoaded()) {
      try {
        messageEntity.Destroy(true);
      } catch (Exception ex) {
        Log.Error($"[CommandHandler] Error destroying chat message entity {messageEntity.Index}: {ex}");
      }
    }

    int commandNameTokens = commandInfo.NameTokenCount + commandInfo.GroupTokenCount;
    var args = tokens.AsSpan()[commandNameTokens..].ToArray();

    if (commandInfo.AdminOnly && !player.IsAdmin) {
      player.SendLocalizedErrorMessage(LocalizationKey.CmdRequiresAdmin);
      return;
    }

    if (args.Length < commandInfo.MinParameterCount || args.Length > commandInfo.MaxParameterCount) {
      player.SendLocalizedErrorMessage(LocalizationKey.CmdAvailableUsages, GetCommandUsages(commandInfo));
      return;
    }

    var method = commandInfo.Method;
    var parameters = method.GetParameters();
    var paramValues = new object[parameters.Length];
    int argIndex = 0;

    bool hasTypeError = false;
    int tempArgIndex = 0;

    for (int i = 0; i < parameters.Length; i++) {
      var param = parameters[i];

      if (param.ParameterType == typeof(CommandContext)) {
        continue;
      }

      if (tempArgIndex < args.Length) {
        int typePriority = GetTypeConversionPriority(param.ParameterType, args[tempArgIndex]);
        if (typePriority == 0) {
          hasTypeError = true;
          break;
        }
        tempArgIndex++;
      }
    }

    if (hasTypeError) {
      player.SendLocalizedErrorMessage(LocalizationKey.CmdAvailableUsages, GetCommandUsages(commandInfo));
      return;
    }

    for (int i = 0; i < parameters.Length; i++) {
      var param = parameters[i];

      if (param.ParameterType == typeof(CommandContext)) {
        paramValues[i] = new CommandContext(Entity.Null, player, fullMessageText, args);
        continue;
      }

      if (argIndex < args.Length) {
        if (!TryConvertParameter(args[argIndex], param.ParameterType, out var convertedValue)) {
          player.SendLocalizedErrorMessage(LocalizationKey.CmdInvalidParameter, param.Name, param.ParameterType.Name, args[argIndex]);
          return;
        }
        paramValues[i] = convertedValue;
        argIndex++;
      } else if (param.IsOptional) {
        paramValues[i] = param.DefaultValue;
      } else {
        player.SendLocalizedErrorMessage(LocalizationKey.CmdAvailableUsages, GetCommandUsages(commandInfo));
        return;
      }
    }

    try {
      method.Invoke(null, paramValues);
      Log.Message($"[CommandHandler] {player.Name} executed command: {commandInfo.FullCommandName} with args: {string.Join(", ", args)}");
    } catch (Exception ex) {
      Log.Error($"[CommandHandler] Error executing command '{commandInfo.FullCommandName}': {ex}");
      player.SendLocalizedErrorMessage(LocalizationKey.CmdExecutionError);
    }
  }

  private static bool IsHelpCommand(CommandInfo commandInfo) {
    return commandInfo.Language == Language.English && commandInfo.Method.DeclaringType == typeof(CommandHandler) && commandInfo.Method.Name == nameof(HelpCommand);
  }

  private static bool IsVCFLoaded() {
    try {
      var assemblies = AppDomain.CurrentDomain.GetAssemblies();
      return assemblies.Any(a => a.GetName().Name == "VampireCommandFramework");
    } catch {
      return false;
    }
  }

  public static void RegisterAll(Assembly assembly = null) {
    assembly ??= Assembly.GetCallingAssembly();

    UnregisterAssemblyInternal(assembly);

    var commandKeys = new List<CommandLookupKey>();
    var fallbackKeys = new List<(int, string)>();

    var types = assembly.GetTypes()
      .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                   .Any(m => m.GetCustomAttribute<CommandAttribute>() != null ||
                            m.GetCustomAttribute<CommandAliasAttribute>() != null) ||
                  t.GetCustomAttributes<CommandGroupAttribute>().Any() ||
                  t.GetCustomAttributes<CommandGroupAliasAttribute>().Any());

    foreach (var type in types) {
      var groups = GetCommandGroups(type);
      bool groupAdminOnly = GetGroupAdminOnly(type);

      var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        .Where(m => m.GetCustomAttribute<CommandAttribute>() != null);

      foreach (var method in methods) {
        var commandAttr = method.GetCustomAttribute<CommandAttribute>();
        if (commandAttr != null) {
          bool effectiveAdminOnly = commandAttr.AdminOnly || groupAdminOnly;

          if (groups.Count > 0) {
            foreach (var (groupName, groupLanguage) in groups) {
              var commandInfo = CreateCommandInfo(method, commandAttr, groupName, assembly, isMain: true, effectiveAdminOnly);
              RegisterCommandInfo(commandInfo, commandKeys, fallbackKeys);
            }
          } else {
            var commandInfo = CreateCommandInfo(method, commandAttr, null, assembly, isMain: true, effectiveAdminOnly);
            RegisterCommandInfo(commandInfo, commandKeys, fallbackKeys);
          }
        }

        var aliasAttrs = method.GetCustomAttributes<CommandAliasAttribute>();
        foreach (var aliasAttr in aliasAttrs) {
          bool effectiveAdminOnly = commandAttr?.AdminOnly ?? false || groupAdminOnly;

          if (groups.Count > 0) {
            foreach (var (groupName, groupLanguage) in groups) {
              var commandInfo = CreateCommandInfo(method, aliasAttr, groupName, assembly, isMain: false, effectiveAdminOnly);
              RegisterCommandInfo(commandInfo, commandKeys, fallbackKeys);
            }
          } else {
            var commandInfo = CreateCommandInfo(method, aliasAttr, null, assembly, isMain: false, effectiveAdminOnly);
            RegisterCommandInfo(commandInfo, commandKeys, fallbackKeys);
          }
        }
      }
    }

    CommandKeysByAssembly[assembly] = commandKeys;
    FallbackKeysByAssembly[assembly] = fallbackKeys;
    Log.Message($"[CommandHandler] Registered {commandKeys.Count} commands from assembly '{assembly.GetName().Name}'.");
  }

  public static void UnregisterAssembly(Assembly assembly = null) {
    assembly ??= Assembly.GetCallingAssembly();
    UnregisterAssemblyInternal(assembly);
  }

  private static void UnregisterAssemblyInternal(Assembly assembly) {
    if (CommandKeysByAssembly.TryGetValue(assembly, out var commandKeys)) {
      foreach (var key in commandKeys) {
        if (CommandsByKey.TryGetValue(key, out var commands)) {
          commands.RemoveAll(c => c.Assembly == assembly);
          if (commands.Count == 0) {
            CommandsByKey.Remove(key);
          }
        }
      }
      CommandKeysByAssembly.Remove(assembly);
    }

    if (FallbackKeysByAssembly.TryGetValue(assembly, out var fallbackKeys)) {
      foreach (var key in fallbackKeys) {
        if (FallbackCommandsByKey.TryGetValue(key, out var commands)) {
          commands.RemoveAll(c => c.Assembly == assembly);
          if (commands.Count == 0) {
            FallbackCommandsByKey.Remove(key);
          }
        }
      }
      FallbackKeysByAssembly.Remove(assembly);
    }
  }

  private static List<(string GroupName, Language Language)> GetCommandGroups(Type type) {
    var groups = new List<(string, Language)>();

    // Pega o grupo principal
    var groupAttr = type.GetCustomAttribute<CommandGroupAttribute>();
    if (groupAttr != null) {
      groups.Add((groupAttr.Group, groupAttr.Language));

      // Aliases do grupo principal mantêm a mesma linguagem
      foreach (var alias in groupAttr.Aliases ?? []) {
        groups.Add((alias, groupAttr.Language));
      }
    }

    // Pega os grupos alias (traduções)
    var groupAliases = type.GetCustomAttributes<CommandGroupAliasAttribute>();
    foreach (var aliasAttr in groupAliases) {
      groups.Add((aliasAttr.Group, aliasAttr.Language));

      // Aliases do grupo alias mantêm a linguagem do grupo alias
      foreach (var alias in aliasAttr.Aliases ?? []) {
        groups.Add((alias, aliasAttr.Language));
      }
    }

    return groups;
  }

  private static bool GetGroupAdminOnly(Type type) {
    var groupAttr = type.GetCustomAttribute<CommandGroupAttribute>();
    if (groupAttr != null && groupAttr.AdminOnly) {
      return true;
    }

    return false;
  }

  private static CommandInfo CreateCommandInfo(MethodInfo method, Attribute attr, string group, Assembly assembly, bool isMain, bool effectiveAdminOnly) {
    string name;
    Language language;

    if (attr is CommandAttribute cmdAttr) {
      name = cmdAttr.Name;
      language = cmdAttr.Language;
    } else if (attr is CommandAliasAttribute aliasAttr) {
      name = aliasAttr.Name;
      language = aliasAttr.Language;
    } else {
      return null;
    }

    var parameters = method.GetParameters();

    var commandParams = parameters.Length > 0 && parameters[0].ParameterType == typeof(CommandContext)
      ? [.. parameters.Skip(1)]
      : parameters;

    int minParams = commandParams.Count(p => !p.IsOptional);
    int maxParams = commandParams.Length;

    var commandInfo = new CommandInfo {
      Name = name,
      Group = group,
      NameTokenCount = CountTokens(name),
      GroupTokenCount = string.IsNullOrEmpty(group) ? 0 : CountTokens(group),
      MinParameterCount = minParams,
      MaxParameterCount = maxParams,
      Parameters = commandParams,
      AdminOnly = effectiveAdminOnly,
      Language = language,
      Method = method,
      Attribute = isMain ? (CommandAttribute)attr : null,
      AliasAttribute = isMain ? null : (CommandAliasAttribute)attr,
      Assembly = assembly,
      IsMainCommand = isMain
    };

    return commandInfo;
  }

  private static void RegisterCommandInfo(CommandInfo command, List<CommandLookupKey> commandKeys, List<(int, string)> fallbackKeys) {
    string fullCommandName = command.FullCommandName.ToLowerInvariant();
    int commandNameTokenCount = command.NameTokenCount + command.GroupTokenCount;
    var key = new CommandLookupKey(command.Language, commandNameTokenCount, fullCommandName);

    if (!CommandsByKey.TryGetValue(key, out List<CommandInfo> commandByKeyList)) {
      commandByKeyList = [];
      CommandsByKey[key] = commandByKeyList;
    }

    commandByKeyList.Add(command);
    commandKeys.Add(key);

    if (command.Attribute != null && command.Attribute.Aliases != null) {
      foreach (var alias in command.Attribute.Aliases) {
        string aliasFullName = string.IsNullOrEmpty(command.Group)
          ? alias
          : $"{command.Group} {alias}";

        var aliasKey = new CommandLookupKey(command.Language, commandNameTokenCount, aliasFullName.ToLowerInvariant());
        if (!CommandsByKey.TryGetValue(aliasKey, out List<CommandInfo> commandAliasList)) {
          commandAliasList = [];
          CommandsByKey[aliasKey] = commandAliasList;
        }

        commandAliasList.Add(command);
        commandKeys.Add(aliasKey);

        if (command.IsMainCommand) {
          var fallbackKey = (commandNameTokenCount, aliasFullName.ToLowerInvariant());
          if (!FallbackCommandsByKey.TryGetValue(fallbackKey, out List<CommandInfo> commandList)) {
            commandList = [];
            FallbackCommandsByKey[fallbackKey] = commandList;
          }

          commandList.Add(command);
          fallbackKeys.Add(fallbackKey);
        }
      }
    } else if (command.AliasAttribute != null && command.AliasAttribute.Aliases != null) {
      foreach (var alias in command.AliasAttribute.Aliases) {
        string aliasFullName = string.IsNullOrEmpty(command.Group)
          ? alias
          : $"{command.Group} {alias}";

        var aliasKey = new CommandLookupKey(command.Language, commandNameTokenCount, aliasFullName.ToLowerInvariant());
        if (!CommandsByKey.TryGetValue(aliasKey, out List<CommandInfo> commandAliasList)) {
          commandAliasList = [];
          CommandsByKey[aliasKey] = commandAliasList;
        }

        commandAliasList.Add(command);
        commandKeys.Add(aliasKey);
      }
    }

    if (command.IsMainCommand) {
      var fallbackKey = (commandNameTokenCount, fullCommandName);
      if (!FallbackCommandsByKey.TryGetValue(fallbackKey, out List<CommandInfo> commandList)) {
        commandList = [];
        FallbackCommandsByKey[fallbackKey] = commandList;
      }

      commandList.Add(command);
      fallbackKeys.Add(fallbackKey);
    }
  }

  private static int CountTokens(string text) {
    if (string.IsNullOrWhiteSpace(text)) return 0;
    return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
  }

  internal static CommandInfo FindCommand(Language playerLanguage, ReadOnlySpan<string> tokens) {
    if (tokens.Length == 0) return null;

    int totalTokens = tokens.Length;
    CommandInfo bestMatch = null;
    int bestScore = -1;

    for (int commandTokens = totalTokens; commandTokens > 0; commandTokens--) {
      string commandName = BuildCommandName(tokens, commandTokens);
      var key = new CommandLookupKey(playerLanguage, commandTokens, commandName);

      if (CommandsByKey.TryGetValue(key, out var commands)) {
        foreach (var command in commands) {
          int score = CalculateCommandMatchScore(command, tokens, commandTokens, allowTypeFailure: false);
          if (score >= 0) {
            score += 1000;
            if (score > bestScore) {
              bestMatch = command;
              bestScore = score;
            }
          } else {
            score = CalculateCommandMatchScore(command, tokens, commandTokens, allowTypeFailure: true);
            if (score >= 0) {
              score += 1000;
              if (score > bestScore) {
                bestMatch = command;
                bestScore = score;
              }
            }
          }
        }
      }

      var fallbackKey = (commandTokens, commandName);
      if (FallbackCommandsByKey.TryGetValue(fallbackKey, out var fallbackCommands)) {
        foreach (var fallbackCommand in fallbackCommands) {
          int score = CalculateCommandMatchScore(fallbackCommand, tokens, commandTokens, allowTypeFailure: false);
          if (score >= 0) {
            if (score > bestScore) {
              bestMatch = fallbackCommand;
              bestScore = score;
            }
          } else {
            score = CalculateCommandMatchScore(fallbackCommand, tokens, commandTokens, allowTypeFailure: true);
            if (score >= 0) {
              if (score > bestScore) {
                bestMatch = fallbackCommand;
                bestScore = score;
              }
            }
          }
        }
      }
    }

    return bestMatch;
  }

  private static string BuildCommandName(ReadOnlySpan<string> tokens, int count) {
    Span<char> buffer = stackalloc char[512];
    int position = 0;

    for (int i = 0; i < count && i < tokens.Length; i++) {
      if (i > 0 && position < buffer.Length) {
        buffer[position++] = ' ';
      }

      ReadOnlySpan<char> token = tokens[i].AsSpan();
      foreach (char c in token) {
        if (position >= buffer.Length) break;
        buffer[position++] = char.ToLowerInvariant(c);
      }
    }

    return new string(buffer[..position]);
  }

  private static int GetTypeConversionPriority(Type parameterType, string token) {
    if (parameterType == typeof(string)) {
      return 10;
    }

    if (parameterType == typeof(int) && int.TryParse(token, out _)) return 100;
    if (parameterType == typeof(long) && long.TryParse(token, out _)) return 100;
    if (parameterType == typeof(ulong) && ulong.TryParse(token, out _)) return 100;
    if (parameterType == typeof(uint) && uint.TryParse(token, out _)) return 100;
    if (parameterType == typeof(short) && short.TryParse(token, out _)) return 100;
    if (parameterType == typeof(byte) && byte.TryParse(token, out _)) return 100;
    if (parameterType == typeof(float) && float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return 90;
    if (parameterType == typeof(double) && double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return 90;

    if (parameterType == typeof(bool)) {
      string lower = token.ToLowerInvariant();
      if (lower == "true" || lower == "false" || lower == "1" || lower == "0") return 95;
    }

    if (parameterType.IsEnum && Enum.TryParse(parameterType, token, true, out _)) return 85;

    if (parameterType == typeof(PrefabGUID) && PrefabGUID.TryParse(token, out _)) return 80;

    if (parameterType == typeof(PlayerData)) {
      if (ulong.TryParse(token, out _)) return 75;

      return 60;
    }

    if (parameterType == typeof(float2)) {
      var parts = token.Split(',');
      if (parts.Length == 2 &&
          float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
          float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _)) {
        return 70;
      }
    }

    if (parameterType == typeof(float3)) {
      var parts = token.Split(',');
      if (parts.Length == 3 &&
          float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
          float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
          float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _)) {
        return 70;
      }
    }

    if (parameterType == typeof(float4) || parameterType == typeof(quaternion)) {
      var parts = token.Split(',');
      if (parts.Length == 4 &&
          float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
          float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
          float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
          float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _)) {
        return 70;
      }
    }

    return 0;
  }

  private static int CalculateCommandMatchScore(CommandInfo command, ReadOnlySpan<string> tokens, int commandTokenCount, bool allowTypeFailure = false) {
    int remainingTokens = tokens.Length - commandTokenCount;

    if (remainingTokens < command.MinParameterCount) {
      return -1;
    }

    if (remainingTokens > command.MaxParameterCount) {
      return -1;
    }

    int score = 0;

    score += commandTokenCount * 10000;

    for (int i = 0; i < remainingTokens && i < command.Parameters.Length; i++) {
      string token = tokens[commandTokenCount + i].ToString();
      Type paramType = command.Parameters[i].ParameterType;

      int typePriority = GetTypeConversionPriority(paramType, token);
      if (typePriority == 0) {
        if (allowTypeFailure) {
          score += 1;
        } else {
          return -1;
        }
      } else {
        score += typePriority;
      }
    }

    score += remainingTokens * 5;

    return score;
  }

  private static bool TryConvertParameter(string input, Type targetType, out object result) {
    result = null;

    try {
      result = TypeConverter.ConvertToType(input, targetType);
      return true;
    } catch {
      return false;
    }
  }

  private static string[] Tokenize(string input) {
    var tokens = new List<string>();
    bool inQuotes = false;
    var current = new System.Text.StringBuilder();
    for (int i = 0; i < input.Length; i++) {
      char c = input[i];
      if (c == '"') {
        inQuotes = !inQuotes;
        continue;
      }
      if (char.IsWhiteSpace(c) && !inQuotes) {
        if (current.Length > 0) {
          tokens.Add(current.ToString());
          current.Clear();
        }
      } else {
        current.Append(c);
      }
    }
    if (current.Length > 0) tokens.Add(current.ToString());
    return [.. tokens];
  }

  private static string GetCommandUsages(CommandInfo info) {
    var usage = new System.Text.StringBuilder();
    usage.Append("<mark=#ff3d3d15>");
    usage.Append(CommandPrefix);

    if (!string.IsNullOrEmpty(info.Group)) {
      usage.Append(info.Group);
      usage.Append(' ');
    }

    usage.Append(info.Name);

    foreach (var param in info.Parameters) {
      usage.Append(' ');

      string typeName = TypeConverter.GetFriendlyTypeName(param.ParameterType);

      if (param.IsOptional) {
        usage.Append($"[{param.Name}:{typeName}]");
      } else {
        usage.Append($"<{param.Name}:{typeName}>");
      }
    }

    usage.Append("</mark>");

    return usage.ToString();
  }

  private static string FormatCommandWithParameters(CommandInfo command) {
    var result = new System.Text.StringBuilder();
    result.Append(command.FullCommandName);

    foreach (var param in command.Parameters) {
      result.Append(' ');

      string paramName = param.Name;

      if (param.IsOptional) {
        string defaultValue = FormatDefaultValue(param.DefaultValue);
        if (string.IsNullOrEmpty(defaultValue)) {
          result.Append($"[{paramName}]");
        } else {
          result.Append($"[{paramName}={defaultValue}]");
        }
      } else {
        result.Append($"<{paramName}>");
      }
    }

    return result.ToString();
  }

  private static string FormatDefaultValue(object defaultValue) {
    if (defaultValue == null) return "null";
    if (defaultValue is string s) return string.IsNullOrEmpty(s) ? "null" : s;
    if (defaultValue is bool b) return b ? "true" : "false";
    if (defaultValue is int i && i == 0) return null;
    if (defaultValue is float f && f == 0f) return null;
    if (defaultValue is double d && d == 0.0) return null;

    if (defaultValue.GetType().IsEnum) {
      return defaultValue.ToString();
    }

    return defaultValue.ToString();
  }

  private static Dictionary<string, List<CommandInfo>> GetCommandsByAssembly(Language playerLanguage, bool isAdmin) {
    var result = new Dictionary<string, List<CommandInfo>>();

    var allCommands = CommandsByKey.Values
      .SelectMany(list => list)
      .Where(cmd => isAdmin || !cmd.AdminOnly)
      .GroupBy(cmd => cmd.Assembly.GetName().Name)
      .OrderBy(g => g.Key);

    foreach (var assemblyGroup in allCommands) {
      var assemblyName = assemblyGroup.Key;
      var commandList = new List<CommandInfo>();

      var uniqueCommands = assemblyGroup
        .GroupBy(cmd => cmd.Method)
        .Select(methodGroup => {
          var playerLangCmd = methodGroup
            .Where(c => c.Language == playerLanguage)
            .OrderByDescending(c => IsGroupInLanguage(c, playerLanguage))
            .FirstOrDefault();

          var mainCmd = methodGroup.FirstOrDefault(c => c.Attribute != null);

          return playerLangCmd ?? mainCmd ?? methodGroup.First();
        })
        .OrderBy(cmd => cmd.Group ?? "")
        .ThenBy(cmd => cmd.Name);

      foreach (var cmd in uniqueCommands) {
        commandList.Add(cmd);
      }

      if (commandList.Count > 0) {
        result[assemblyName] = commandList;
      }
    }

    return result;
  }

  private static bool IsGroupInLanguage(CommandInfo command, Language language) {
    if (string.IsNullOrEmpty(command.Group)) {
      return true;
    }

    var method = command.Method;
    if (method?.DeclaringType == null) return false;

    var type = method.DeclaringType;

    var groupAttr = type.GetCustomAttribute<CommandGroupAttribute>();
    if (groupAttr != null) {
      if (groupAttr.Language == language) {
        if (command.Group.Equals(groupAttr.Group, StringComparison.OrdinalIgnoreCase)) {
          return true;
        }
        if (groupAttr.Aliases != null && groupAttr.Aliases.Any(a =>
            a.Equals(command.Group, StringComparison.OrdinalIgnoreCase))) {
          return true;
        }
      }
    }

    var groupAliases = type.GetCustomAttributes<CommandGroupAliasAttribute>();
    foreach (var aliasAttr in groupAliases) {
      if (aliasAttr.Language == language) {
        if (command.Group.Equals(aliasAttr.Group, StringComparison.OrdinalIgnoreCase)) {
          return true;
        }
        if (aliasAttr.Aliases != null && aliasAttr.Aliases.Any(a =>
            a.Equals(command.Group, StringComparison.OrdinalIgnoreCase))) {
          return true;
        }
      }
    }

    return false;
  }

  private class LocalizationKey {
    public const string CmdRequiresAdmin = "cmd_requires_admin";
    public const string CmdExecutionError = "cmd_execution_error";
    public const string CmdAvailableUsages = "cmd_available_usages";
    public const string CmdInvalidParameter = "cmd_invalid_parameter";
    public const string CmdAmbiguousOverload = "cmd_ambiguous_overload";
    public const string CmdGroupNoSubcommand = "cmd_group_no_subcommand";
    public const string CmdUnknownGroupCommand = "cmd_unknown_group_command";
    public const string HelpNoCommands = "help_no_commands";
    public const string HelpAvailableCommands = "help_available_commands";
    public const string HelpNextPage = "help_next_page";
    public const string ServerLanguageCurrent = "server_language_current";
    public const string LanguageNotSupported = "language_not_supported";
    public const string AvailableLanguages = "available_languages";
    public const string ServerLanguageChanged = "server_language_changed";
    public const string LanguageChangeFailed = "language_change_failed";
    public const string PlayerLanguageCurrent = "player_language_current";
    public const string PlayerLanguageChanged = "player_language_changed";
    public const string MustBePlayer = "must_be_player";
  }

  private static void RegisterLocalizationKeys() {
    LocalizationService.NewKey(LocalizationKey.CmdRequiresAdmin, new Dictionary<Language, string> {
      { Language.English, "This command requires ~administrator privileges~." },
      { Language.Portuguese, "Este comando requer ~privilégios de administrador~." },
      { Language.French, "Cette commande nécessite des ~privilèges d'administrateur~." },
      { Language.German, "Dieser Befehl erfordert ~Administratorrechte~." },
      { Language.Hungarian, "Ehhez a parancshoz ~rendszergazdai jogosultság~ szükséges." },
      { Language.Italian, "Questo comando richiede ~privilegi di amministratore~." },
      { Language.Japanese, "このコマンドは~管理者権限~が必要です。" },
      { Language.Korean, "이 명령은 ~관리자 권한~이 필요합니다." },
      { Language.Latam, "Este comando requiere ~privilegios de administrador~." },
      { Language.Polish, "Ta komenda wymaga ~uprawnień administratora~." },
      { Language.Russian, "Для этой команды требуются ~права администратора~." },
      { Language.Spanish, "Este comando requiere ~privilegios de administrador~." },
      { Language.ChineseSimplified, "此命令需要~管理员权限~。" },
      { Language.ChineseTraditional, "此指令需要~管理員權限~。" },
      { Language.Thai, "คำสั่งนี้ต้องการ~สิทธิ์ผู้ดูแลระบบ~" },
      { Language.Turkish, "Bu komut için ~yönetici ayrıcalıkları~ gerekir." },
      { Language.Ukrainian, "Ця команда вимагає ~прав адміністратора~." },
      { Language.Vietnamese, "Lệnh này yêu cầu ~quyền quản trị viên~." }
    });

    LocalizationService.NewKey(LocalizationKey.CmdExecutionError, new Dictionary<Language, string> {
      { Language.English, "An ~error~ occurred while executing the command." },
      { Language.Portuguese, "Ocorreu um ~erro~ ao executar o comando." },
      { Language.French, "Une ~erreur~ est survenue lors de l'exécution de la commande." },
      { Language.German, "Beim Ausführen des Befehls ist ein ~Fehler~ aufgetreten." },
      { Language.Hungarian, "~Hiba~ történt a parancs végrehajtása közben." },
      { Language.Italian, "Si è verificato un ~errore~ durante l'esecuzione del comando." },
      { Language.Japanese, "コマンドの実行中に~エラー~が発生しました。" },
      { Language.Korean, "명령을 실행하는 중에 ~오류~가 발생했습니다." },
      { Language.Latam, "Ocurrió un ~error~ al ejecutar el comando." },
      { Language.Polish, "Wystąpił ~błąd~ podczas wykonywania polecenia." },
      { Language.Russian, "Произошла ~ошибка~ при выполнении команды." },
      { Language.Spanish, "Ocurrió un ~error~ al ejecutar el comando." },
      { Language.ChineseSimplified, "执行命令时发生~错误~。" },
      { Language.ChineseTraditional, "執行指令時發生~錯誤~。" },
      { Language.Thai, "เกิด~ข้อผิดพลาด~ขณะเรียกใช้คำสั่ง" },
      { Language.Turkish, "Komut yürütülürken bir ~hata~ oluştu." },
      { Language.Ukrainian, "Під час виконання команди сталася ~помилка~." },
      { Language.Vietnamese, "Đã xảy ra ~lỗi~ khi thực hiện lệnh." }
    });

    LocalizationService.NewKey(LocalizationKey.CmdAvailableUsages, new Dictionary<Language, string> {
      { Language.English, "~Available usages:~\n{0}" },
      { Language.Portuguese, "~Formas de uso disponíveis:~\n{0}" },
      { Language.French, "~Utilisations disponibles :~\n{0}" },
      { Language.German, "~Verfügbare Aufrufe:~\n{0}" },
      { Language.Hungarian, "~Elérhető használatok:~\n{0}" },
      { Language.Italian, "~Utilizzi disponibili:~\n{0}" },
      { Language.Japanese, "~利用可能な使い方:~\n{0}" },
      { Language.Korean, "~사용 가능한 명령 형식:~\n{0}" },
      { Language.Latam, "~Usos disponibles:~\n{0}" },
      { Language.Polish, "~Dostępne użycia:~\n{0}" },
      { Language.Russian, "~Доступные варианты использования:~\n{0}" },
      { Language.Spanish, "~Usos disponibles:~\n{0}" },
      { Language.ChineseSimplified, "~可用用法：~\n{0}" },
      { Language.ChineseTraditional, "~可用用法：~\n{0}" },
      { Language.Thai, "~รูปแบบที่ใช้ได้: ~\n{0}" },
      { Language.Turkish, "~Kullanılabilir kullanımlar:~\n{0}" },
      { Language.Ukrainian, "~Доступні варіанти використання:~\n{0}" },
      { Language.Vietnamese, "~Cách sử dụng có sẵn:~\n{0}" }
    });

    LocalizationService.NewKey(LocalizationKey.CmdInvalidParameter, new Dictionary<Language, string> {
      { Language.English, "Invalid parameter ~'{0}'~ for type ~{1}~: {2}" },
      { Language.Portuguese, "Parâmetro inválido ~'{0}'~ para o tipo ~{1}~: {2}" },
      { Language.French, "Paramètre invalide ~'{0}'~ pour le type ~{1}~: {2}" },
      { Language.German, "Ungültiger Parameter ~'{0}'~ für Typ ~{1}~: {2}" },
      { Language.Hungarian, "Érvénytelen paraméter ~'{0}'~ a(z) ~{1}~ típushoz: {2}" },
      { Language.Italian, "Parametro non valido ~'{0}'~ per il tipo ~{1}~: {2}" },
      { Language.Japanese, "型 ~{1}~ の無効なパラメータ ~'{0}'~: {2}" },
      { Language.Korean, "유효하지 않은 매개변수 ~'{0}'~ (형식 ~{1}~): {2}" },
      { Language.Latam, "Parámetro inválido ~'{0}'~ para el tipo ~{1}~: {2}" },
      { Language.Polish, "Nieprawidłowy parametr ~'{0}'~ dla typu ~{1}~: {2}" },
      { Language.Russian, "Неверный параметр ~'{0}'~ для типа ~{1}~: {2}" },
      { Language.Spanish, "Parámetro inválido ~'{0}'~ para el tipo ~{1}~: {2}" },
      { Language.ChineseSimplified, "无效参数 ~'{0}'~，类型 ~{1}~：{2}" },
      { Language.ChineseTraditional, "無效參數 ~'{0}'~，類型 ~{1}~：{2}" },
      { Language.Thai, "พารามิเตอร์ไม่ถูกต้อง ~'{0}'~ สำหรับประเภท ~{1}~: {2}" },
      { Language.Turkish, "Geçersiz parametre ~'{0}'~ türü ~{1}~: {2}" },
      { Language.Ukrainian, "Неприпустимий параметр ~'{0}'~ для типу ~{1}~: {2}" },
      { Language.Vietnamese, "Tham số không hợp lệ ~'{0}'~ cho kiểu ~{1}~: {2}" }
    });

    LocalizationService.NewKey(LocalizationKey.CmdAmbiguousOverload, new Dictionary<Language, string> {
      { Language.English, "~Ambiguous command overload~; multiple matches found." },
      { Language.Portuguese, "~Sobrecarga ambígua do comando~; múltiplas correspondências encontradas." },
      { Language.French, "~Surcharge de commande ambiguë~ ; plusieurs correspondances trouvées." },
      { Language.German, "~Mehrdeutige Befehlsüberladung~; mehrere Treffer gefunden." },
      { Language.Hungarian, "~Többértelmű parancs overload~; több egyezés található." },
      { Language.Italian, "~Sovraccarico di comando ambiguo~; trovate più corrispondenze." },
      { Language.Japanese, "~あいまいなコマンドのオーバーロード~です。複数の一致が見つかりました。" },
      { Language.Korean, "~모호한 명령 오버로드~; 여러 일치 항목이 발견되었습니다." },
      { Language.Latam, "~Sobrecarga de comando ambigua~; se encontraron múltiples coincidencias." },
      { Language.Polish, "~Niejednoznaczne przeciążenie polecenia~; znaleziono wiele dopasowań." },
      { Language.Russian, "~Неоднозначная перегрузка команды~; найдено несколько совпадений." },
      { Language.Spanish, "~Sobrecarga de comando ambigua~; se encontraron múltiples coincidencias." },
      { Language.ChineseSimplified, "~命令重载不明确~；找到多个匹配项。" },
      { Language.ChineseTraditional, "~指令重載不明確~；找到多個匹配項。" },
      { Language.Thai, "~การโอเวอร์โหลดคำสั่งไม่ชัดเจน~; พบการจับคู่หลายรายการ" },
      { Language.Turkish, "~Belirsiz komut aşırı yüklemesi~; birden çok eşleşme bulundu." },
      { Language.Ukrainian, "~Двоозначне перевантаження команди~; знайдено кілька збігів." },
      { Language.Vietnamese, "~Overload lệnh không rõ ràng~; tìm thấy nhiều kết quả khớp." }
    });

    LocalizationService.NewKey(LocalizationKey.CmdGroupNoSubcommand, new Dictionary<Language, string> {
      { Language.English, "This command group requires a ~subcommand~." },
      { Language.Portuguese, "Este grupo de comandos requer um ~subcomando~." },
      { Language.French, "Ce groupe de commandes nécessite une ~sous-commande~." },
      { Language.German, "Diese Befehlsgruppe erfordert einen ~Unterbefehl~." },
      { Language.Hungarian, "Ehhez a parancscsoporthoz ~alparancs~ szükséges." },
      { Language.Italian, "Questo gruppo di comandi richiede un ~sottocomando~." },
      { Language.Japanese, "このコマンドグループは~サブコマンド~が必要です。" },
      { Language.Korean, "이 명령 그룹은 ~하위 명령~이 필요합니다." },
      { Language.Latam, "Este grupo de comandos requiere un ~subcomando~." },
      { Language.Polish, "Ta grupa poleceń wymaga ~podpolecenia~." },
      { Language.Russian, "Эта группа команд требует ~подкоманды~." },
      { Language.Spanish, "Este grupo de comandos requiere un ~subcomando~." },
      { Language.ChineseSimplified, "此命令组需要~子命令~。" },
      { Language.ChineseTraditional, "此指令群組需要~子指令~。" },
      { Language.Thai, "กลุ่มคำสั่งนี้ต้องการ~คำสั่งย่อย~" },
      { Language.Turkish, "Bu komut grubu bir ~alt komut~ gerektirir." },
      { Language.Ukrainian, "Ця група команд вимагає ~підкоманди~." },
      { Language.Vietnamese, "Nhóm lệnh này yêu cầu một ~lệnh phụ~." }
    });

    LocalizationService.NewKey(LocalizationKey.CmdUnknownGroupCommand, new Dictionary<Language, string> {
      { Language.English, "Unknown command in group ~'{0}'~: ~{1}~" },
      { Language.Portuguese, "Comando desconhecido no grupo ~'{0}'~: ~{1}~" },
      { Language.French, "Commande inconnue dans le groupe ~'{0}'~ : ~{1}~" },
      { Language.German, "Unbekannter Befehl in Gruppe ~'{0}'~: ~{1}~" },
      { Language.Hungarian, "Ismeretlen parancs a(z) ~'{0}'~ csoportban: ~{1}~" },
      { Language.Italian, "Comando sconosciuto nel gruppo ~'{0}'~: ~{1}~" },
      { Language.Japanese, "グループ ~'{0}'~ の不明なコマンド: ~{1}~" },
      { Language.Korean, "그룹 ~'{0}'~에서 알 수 없는 명령: ~{1}~" },
      { Language.Latam, "Comando desconocido en el grupo ~'{0}'~: ~{1}~" },
      { Language.Polish, "Nieznane polecenie w grupie ~'{0}'~: ~{1}~" },
      { Language.Russian, "Неизвестная команда в группе ~'{0}'~: ~{1}~" },
      { Language.Spanish, "Comando desconocido en el grupo ~'{0}'~: ~{1}~" },
      { Language.ChineseSimplified, "组 ~'{0}'~ 中的未知命令：~{1}~" },
      { Language.ChineseTraditional, "群組 ~'{0}'~ 中的未知指令：~{1}~" },
      { Language.Thai, "คำสั่งไม่รู้จักในกลุ่ม ~'{0}'~: ~{1}~" },
      { Language.Turkish, "~'{0}'~ grubunda bilinmeyen komut: ~{1}~" },
      { Language.Ukrainian, "Невідома команда в групі ~'{0}'~: ~{1}~" },
      { Language.Vietnamese, "Lệnh không xác định trong nhóm ~'{0}'~: ~{1}~" }
    });

    LocalizationService.NewKey(LocalizationKey.HelpNoCommands, new Dictionary<Language, string> {
      { Language.English, "~No commands available.~" },
      { Language.Portuguese, "~Nenhum comando disponível.~" },
      { Language.French, "~Aucune commande disponible.~" },
      { Language.German, "~Keine Befehle verfügbar.~" },
      { Language.Hungarian, "~Nincsenek elérhető parancsok.~" },
      { Language.Italian, "~Nessun comando disponibile.~" },
      { Language.Japanese, "~利用可能なコマンドがありません。~" },
      { Language.Korean, "~사용 가능한 명령이 없습니다.~" },
      { Language.Latam, "~No hay comandos disponibles.~" },
      { Language.Polish, "~Brak dostępnych poleceń.~" },
      { Language.Russian, "~Команды недоступны.~" },
      { Language.Spanish, "~No hay comandos disponibles.~" },
      { Language.ChineseSimplified, "~没有可用的命令。~" },
      { Language.ChineseTraditional, "~沒有可用的指令。~" },
      { Language.Thai, "~ไม่มีคำสั่งที่ใช้ได้~" },
      { Language.Turkish, "~Kullanılabilir komut yok.~" },
      { Language.Ukrainian, "~Немає доступних команд.~" },
      { Language.Vietnamese, "~Không có lệnh nào khả dụng.~" }
    });

    LocalizationService.NewKey(LocalizationKey.HelpAvailableCommands, new Dictionary<Language, string> {
      { Language.English, "~Available Commands~ (Page ~{0}/{1}~):" },
      { Language.Portuguese, "~Comandos Disponíveis~ (Página ~{0}/{1}~):" },
      { Language.French, "~Commandes Disponibles~ (Page ~{0}/{1}~) :" },
      { Language.German, "~Verfügbare Befehle~ (Seite ~{0}/{1}~):" },
      { Language.Hungarian, "~Elérhető Parancsok~ (Oldal ~{0}/{1}~):" },
      { Language.Italian, "~Comandi Disponibili~ (Pagina ~{0}/{1}~):" },
      { Language.Japanese, "~利用可能なコマンド~ (ページ ~{0}/{1}~):" },
      { Language.Korean, "~사용 가능한 명령~ (페이지 ~{0}/{1}~):" },
      { Language.Latam, "~Comandos Disponibles~ (Página ~{0}/{1}~):" },
      { Language.Polish, "~Dostępne Polecenia~ (Strona ~{0}/{1}~):" },
      { Language.Russian, "~Доступные Команды~ (Страница ~{0}/{1}~):" },
      { Language.Spanish, "~Comandos Disponibles~ (Página ~{0}/{1}~):" },
      { Language.ChineseSimplified, "~可用命令~（第 ~{0}/{1}~ 页）：" },
      { Language.ChineseTraditional, "~可用指令~（第 ~{0}/{1}~ 頁）：" },
      { Language.Thai, "~คำสั่งที่ใช้ได้~ (หน้า ~{0}/{1}~):" },
      { Language.Turkish, "~Kullanılabilir Komutlar~ (Sayfa ~{0}/{1}~):" },
      { Language.Ukrainian, "~Доступні Команди~ (Сторінка ~{0}/{1}~):" },
      { Language.Vietnamese, "~Lệnh Khả Dụng~ (Trang ~{0}/{1}~):" }
    });

    LocalizationService.NewKey(LocalizationKey.HelpNextPage, new Dictionary<Language, string> {
      { Language.English, "Type ~{0}help {1}~ for next page" },
      { Language.Portuguese, "Digite ~{0}ajuda {1}~ para a próxima página" },
      { Language.French, "Tapez ~{0}aide {1}~ pour la page suivante" },
      { Language.German, "Geben Sie ~{0}hilfe {1}~ für die nächste Seite ein" },
      { Language.Hungarian, "Írja be: ~{0}segítség {1}~ a következő oldalhoz" },
      { Language.Italian, "Digita ~{0}aiuto {1}~ per la pagina successiva" },
      { Language.Japanese, "次のページには ~{0}ヘルプ {1}~ と入力してください" },
      { Language.Korean, "다음 페이지를 보려면 ~{0}도움말 {1}~을 입력하세요" },
      { Language.Latam, "Escribe ~{0}ayuda {1}~ para la siguiente página" },
      { Language.Polish, "Wpisz ~{0}pomoc {1}~ aby przejść do następnej strony" },
      { Language.Russian, "Введите ~{0}помощь {1}~ для следующей страницы" },
      { Language.Spanish, "Escribe ~{0}ayuda {1}~ para la siguiente página" },
      { Language.ChineseSimplified, "输入 ~{0}帮助 {1}~ 查看下一页" },
      { Language.ChineseTraditional, "輸入 ~{0}幫助 {1}~ 查看下一頁" },
      { Language.Thai, "พิมพ์ ~{0}ช่วยเหลือ {1}~ สำหรับหน้าถัดไป" },
      { Language.Turkish, "Sonraki sayfa için ~{0}yardım {1}~ yazın" },
      { Language.Ukrainian, "Введіть ~{0}допомога {1}~ для наступної сторінки" },
      { Language.Vietnamese, "Gõ ~{0}trợgiúp {1}~ để xem trang tiếp theo" }
    });

    LocalizationService.NewKey(LocalizationKey.ServerLanguageCurrent, new Dictionary<Language, string> {
      { Language.English, "~ScarletCore~ current localization language: ~{0}~" },
      { Language.Portuguese, "Linguagem de localização atual do ~ScarletCore~: ~{0}~" },
      { Language.French, "Langue de localisation actuelle de ~ScarletCore~: ~{0}~" },
      { Language.German, "Aktuelle Lokalisierungssprache von ~ScarletCore~: ~{0}~" },
      { Language.Hungarian, "~ScarletCore~ jelenlegi lokalizációs nyelve: ~{0}~" },
      { Language.Italian, "Lingua di localizzazione attuale di ~ScarletCore~: ~{0}~" },
      { Language.Japanese, "~ScarletCore~の現在のローカライゼーション言語: ~{0}~" },
      { Language.Korean, "~ScarletCore~ 현재 로컬라이제이션 언어: ~{0}~" },
      { Language.Latam, "Idioma de localización actual de ~ScarletCore~: ~{0}~" },
      { Language.Polish, "Bieżący język lokalizacji ~ScarletCore~: ~{0}~" },
      { Language.Russian, "Текущий язык локализации ~ScarletCore~: ~{0}~" },
      { Language.Spanish, "Idioma de localización actual de ~ScarletCore~: ~{0}~" },
      { Language.ChineseSimplified, "~ScarletCore~当前本地化语言: ~{0}~" },
      { Language.ChineseTraditional, "~ScarletCore~目前本地化語言: ~{0}~" },
      { Language.Thai, "ภาษาโลคัลไลเซชันปัจจุบันของ ~ScarletCore~: ~{0}~" },
      { Language.Turkish, "~ScarletCore~ mevcut yerelleştirme dili: ~{0}~" },
      { Language.Ukrainian, "Поточна мова локалізації ~ScarletCore~: ~{0}~" },
      { Language.Vietnamese, "Ngôn ngữ bản địa hóa hiện tại của ~ScarletCore~: ~{0}~" }
    });

    LocalizationService.NewKey(LocalizationKey.LanguageNotSupported, new Dictionary<Language, string> {
      { Language.English, "Language not supported: ~{0}~" },
      { Language.Portuguese, "Idioma não suportado: ~{0}~" },
      { Language.French, "Langue non prise en charge: ~{0}~" },
      { Language.German, "Sprache nicht unterstützt: ~{0}~" },
      { Language.Hungarian, "A nyelv nem támogatott: ~{0}~" },
      { Language.Italian, "Lingua non supportata: ~{0}~" },
      { Language.Japanese, "サポートされていない言語: ~{0}~" },
      { Language.Korean, "지원되지 않는 언어: ~{0}~" },
      { Language.Latam, "Idioma no compatible: ~{0}~" },
      { Language.Polish, "Język nie jest obsługiwany: ~{0}~" },
      { Language.Russian, "Язык не поддерживается: ~{0}~" },
      { Language.Spanish, "Idioma no compatible: ~{0}~" },
      { Language.ChineseSimplified, "不支持的语言: ~{0}~" },
      { Language.ChineseTraditional, "不支援的語言: ~{0}~" },
      { Language.Thai, "ไม่รองรับภาษา: ~{0}~" },
      { Language.Turkish, "Desteklenmeyen dil: ~{0}~" },
      { Language.Ukrainian, "Мова не підтримується: ~{0}~" },
      { Language.Vietnamese, "Ngôn ngữ không được hỗ trợ: ~{0}~" }
    });

    LocalizationService.NewKey(LocalizationKey.AvailableLanguages, new Dictionary<Language, string> {
      { Language.English, "~Available languages:~ {0}" },
      { Language.Portuguese, "~Idiomas disponíveis:~ {0}" },
      { Language.French, "~Langues disponibles:~ {0}" },
      { Language.German, "~Verfügbare Sprachen:~ {0}" },
      { Language.Hungarian, "~Elérhető nyelvek:~ {0}" },
      { Language.Italian, "~Lingue disponibili:~ {0}" },
      { Language.Japanese, "~利用可能な言語:~ {0}" },
      { Language.Korean, "~사용 가능한 언어:~ {0}" },
      { Language.Latam, "~Idiomas disponibles:~ {0}" },
      { Language.Polish, "~Dostępne języki:~ {0}" },
      { Language.Russian, "~Доступные языки:~ {0}" },
      { Language.Spanish, "~Idiomas disponibles:~ {0}" },
      { Language.ChineseSimplified, "~可用语言:~ {0}" },
      { Language.ChineseTraditional, "~可用語言:~ {0}" },
      { Language.Thai, "~ภาษาที่มี:~ {0}" },
      { Language.Turkish, "~Mevcut diller:~ {0}" },
      { Language.Ukrainian, "~Доступні мови:~ {0}" },
      { Language.Vietnamese, "~Ngôn ngữ khả dụng:~ {0}" }
    });

    LocalizationService.NewKey(LocalizationKey.ServerLanguageChanged, new Dictionary<Language, string> {
      { Language.English, "~ScarletCore~ localization language changed to: ~{0}~" },
      { Language.Portuguese, "Linguagem de localização do ~ScarletCore~ alterada para: ~{0}~" },
      { Language.French, "Langue de localisation de ~ScarletCore~ changée en: ~{0}~" },
      { Language.German, "~ScarletCore~-Lokalisierungssprache geändert zu: ~{0}~" },
      { Language.Hungarian, "~ScarletCore~ lokalizációs nyelve megváltoztatva: ~{0}~" },
      { Language.Italian, "Lingua di localizzazione di ~ScarletCore~ cambiata in: ~{0}~" },
      { Language.Japanese, "~ScarletCore~のローカライゼーション言語が次に変更されました: ~{0}~" },
      { Language.Korean, "~ScarletCore~ 로컬라이제이션 언어가 다음으로 변경되었습니다: ~{0}~" },
      { Language.Latam, "Idioma de localización de ~ScarletCore~ cambiado a: ~{0}~" },
      { Language.Polish, "Język lokalizacji ~ScarletCore~ zmieniony na: ~{0}~" },
      { Language.Russian, "Язык локализации ~ScarletCore~ изменен на: ~{0}~" },
      { Language.Spanish, "Idioma de localización de ~ScarletCore~ cambiado a: ~{0}~" },
      { Language.ChineseSimplified, "~ScarletCore~本地化语言已更改为: ~{0}~" },
      { Language.ChineseTraditional, "~ScarletCore~本地化語言已更改為: ~{0}~" },
      { Language.Thai, "เปลี่ยนภาษาโลคัลไลเซชันของ ~ScarletCore~ เป็น: ~{0}~" },
      { Language.Turkish, "~ScarletCore~ yerelleştirme dili şu şekilde değiştirildi: ~{0}~" },
      { Language.Ukrainian, "Мову локалізації ~ScarletCore~ змінено на: ~{0}~" },
      { Language.Vietnamese, "Ngôn ngữ bản địa hóa ~ScarletCore~ đã được thay đổi thành: ~{0}~" }
    });

    LocalizationService.NewKey(LocalizationKey.LanguageChangeFailed, new Dictionary<Language, string> {
      { Language.English, "~Failed~ to change language to: ~{0}~" },
      { Language.Portuguese, "~Falha~ ao alterar idioma para: ~{0}~" },
      { Language.French, "~Échec~ du changement de langue vers: ~{0}~" },
      { Language.German, "~Fehler~ beim Ändern der Sprache zu: ~{0}~" },
      { Language.Hungarian, "~Nem sikerült~ megváltoztatni a nyelvet erre: ~{0}~" },
      { Language.Italian, "~Impossibile~ cambiare lingua in: ~{0}~" },
      { Language.Japanese, "言語の変更に~失敗~しました: ~{0}~" },
      { Language.Korean, "언어 변경 ~실패~: ~{0}~" },
      { Language.Latam, "~Error~ al cambiar el idioma a: ~{0}~" },
      { Language.Polish, "~Nie udało się~ zmienić języka na: ~{0}~" },
      { Language.Russian, "~Не удалось~ изменить язык на: ~{0}~" },
      { Language.Spanish, "~Error~ al cambiar el idioma a: ~{0}~" },
      { Language.ChineseSimplified, "更改语言~失败~: ~{0}~" },
      { Language.ChineseTraditional, "更改語言~失敗~: ~{0}~" },
      { Language.Thai, "~ไม่สามารถ~เปลี่ยนภาษาเป็น: ~{0}~" },
      { Language.Turkish, "Dil ~değiştirilemedi~: ~{0}~" },
      { Language.Ukrainian, "~Не вдалося~ змінити мову на: ~{0}~" },
      { Language.Vietnamese, "~Không thể~ thay đổi ngôn ngữ thành: ~{0}~" }
    });

    LocalizationService.NewKey(LocalizationKey.PlayerLanguageCurrent, new Dictionary<Language, string> {
      { Language.English, "Your current language: ~{0}~" },
      { Language.Portuguese, "Seu idioma atual: ~{0}~" },
      { Language.French, "Votre langue actuelle: ~{0}~" },
      { Language.German, "Ihre aktuelle Sprache: ~{0}~" },
      { Language.Hungarian, "Az Ön jelenlegi nyelve: ~{0}~" },
      { Language.Italian, "La tua lingua attuale: ~{0}~" },
      { Language.Japanese, "あなたの現在の言語: ~{0}~" },
      { Language.Korean, "현재 언어: ~{0}~" },
      { Language.Latam, "Tu idioma actual: ~{0}~" },
      { Language.Polish, "Twój obecny język: ~{0}~" },
      { Language.Russian, "Ваш текущий язык: ~{0}~" },
      { Language.Spanish, "Tu idioma actual: ~{0}~" },
      { Language.ChineseSimplified, "您当前的语言: ~{0}~" },
      { Language.ChineseTraditional, "您目前的語言: ~{0}~" },
      { Language.Thai, "ภาษาปัจจุบันของคุณ: ~{0}~" },
      { Language.Turkish, "Mevcut diliniz: ~{0}~" },
      { Language.Ukrainian, "Ваша поточна мова: ~{0}~" },
      { Language.Vietnamese, "Ngôn ngữ hiện tại của bạn: ~{0}~" }
    });

    LocalizationService.NewKey(LocalizationKey.PlayerLanguageChanged, new Dictionary<Language, string> {
      { Language.English, "Your language has been set to: ~{0}~" },
      { Language.Portuguese, "Seu idioma foi definido para: ~{0}~" },
      { Language.French, "Votre langue a été définie sur: ~{0}~" },
      { Language.German, "Ihre Sprache wurde eingestellt auf: ~{0}~" },
      { Language.Hungarian, "Az Ön nyelve beállítva: ~{0}~" },
      { Language.Italian, "La tua lingua è stata impostata su: ~{0}~" },
      { Language.Japanese, "あなたの言語が次に設定されました: ~{0}~" },
      { Language.Korean, "언어가 다음으로 설정되었습니다: ~{0}~" },
      { Language.Latam, "Tu idioma ha sido configurado a: ~{0}~" },
      { Language.Polish, "Twój język został ustawiony na: ~{0}~" },
      { Language.Russian, "Ваш язык установлен на: ~{0}~" },
      { Language.Spanish, "Tu idioma ha sido configurado a: ~{0}~" },
      { Language.ChineseSimplified, "您的语言已设置为: ~{0}~" },
      { Language.ChineseTraditional, "您的語言已設定為: ~{0}~" },
      { Language.Thai, "ภาษาของคุณถูกตั้งค่าเป็น: ~{0}~" },
      { Language.Turkish, "Diliniz şu şekilde ayarlandı: ~{0}~" },
      { Language.Ukrainian, "Вашу мову встановлено на: ~{0}~" },
      { Language.Vietnamese, "Ngôn ngữ của bạn đã được đặt thành: ~{0}~" }
    });

    LocalizationService.NewKey(LocalizationKey.MustBePlayer, new Dictionary<Language, string> {
      { Language.English, "This command must be run by a ~player~." },
      { Language.Portuguese, "Este comando deve ser executado por um ~jogador~." },
      { Language.French, "Cette commande doit être exécutée par un ~joueur~." },
      { Language.German, "Dieser Befehl muss von einem ~Spieler~ ausgeführt werden." },
      { Language.Hungarian, "Ezt a parancsot egy ~játékosnak~ kell futtatnia." },
      { Language.Italian, "Questo comando deve essere eseguito da un ~giocatore~." },
      { Language.Japanese, "このコマンドは~プレイヤー~が実行する必要があります。" },
      { Language.Korean, "이 명령은 ~플레이어~가 실행해야 합니다." },
      { Language.Latam, "Este comando debe ser ejecutado por un ~jugador~." },
      { Language.Polish, "To polecenie musi być uruchomione przez ~gracza~." },
      { Language.Russian, "Эта команда должна быть выполнена ~игроком~." },
      { Language.Spanish, "Este comando debe ser ejecutado por un ~jugador~." },
      { Language.ChineseSimplified, "此命令必须由~玩家~运行。" },
      { Language.ChineseTraditional, "此指令必須由~玩家~執行。" },
      { Language.Thai, "คำสั่งนี้ต้องรันโดย~ผู้เล่น~" },
      { Language.Turkish, "Bu komut bir ~oyuncu~ tarafından çalıştırılmalıdır." },
      { Language.Ukrainian, "Ця команда повинна бути виконана ~гравцем~." },
      { Language.Vietnamese, "Lệnh này phải được thực thi bởi ~người chơi~." }
    });
  }


  [Command("help", Language.English, description: "Shows available commands")]
  [CommandAlias("ajuda", Language.Portuguese, description: "Mostra os comandos disponíveis")]
  [CommandAlias("aide", Language.French, description: "Affiche les commandes disponibles")]
  [CommandAlias("hilfe", Language.German, description: "Zeigt verfügbare Befehle an")]
  [CommandAlias("segítség", Language.Hungarian, description: "Megjeleníti az elérhető parancsokat")]
  [CommandAlias("aiuto", Language.Italian, description: "Mostra i comandi disponibili")]
  [CommandAlias("ヘルプ", Language.Japanese, description: "利用可能なコマンドを表示します")]
  [CommandAlias("도움말", Language.Korean, description: "사용 가능한 명령을 표시합니다")]
  [CommandAlias("ayuda", Language.Latam, description: "Muestra los comandos disponibles")]
  [CommandAlias("pomoc", Language.Polish, description: "Pokazuje dostępne polecenia")]
  [CommandAlias("помощь", Language.Russian, description: "Показывает доступные команды")]
  [CommandAlias("ayuda", Language.Spanish, description: "Muestra los comandos disponibles")]
  [CommandAlias("帮助", Language.ChineseSimplified, description: "显示可用命令")]
  [CommandAlias("幫助", Language.ChineseTraditional, description: "顯示可用指令")]
  [CommandAlias("ช่วยเหลือ", Language.Thai, description: "แสดงคำสั่งที่ใช้ได้")]
  [CommandAlias("yardım", Language.Turkish, description: "Kullanılabilir komutları gösterir")]
  [CommandAlias("допомога", Language.Ukrainian, description: "Показує доступні команди")]
  [CommandAlias("trợgiúp", Language.Vietnamese, description: "Hiển thị các lệnh có sẵn")]
  internal static void HelpCommand(CommandContext ctx, string language, int page = 1) {
    var targetLanguage = LocalizationService.GetLanguageFromString(language);
    if (targetLanguage == Language.None) targetLanguage = ctx.Sender.Language;
    HelpCommandInternal(ctx, targetLanguage, page);
  }

  [Command("help", Language.English, description: "Shows available commands")]
  [CommandAlias("ajuda", Language.Portuguese, description: "Mostra os comandos disponíveis")]
  [CommandAlias("aide", Language.French, description: "Affiche les commandes disponibles")]
  [CommandAlias("hilfe", Language.German, description: "Zeigt verfügbare Befehle an")]
  [CommandAlias("segítség", Language.Hungarian, description: "Megjeleníti az elérhető parancsokat")]
  [CommandAlias("aiuto", Language.Italian, description: "Mostra i comandi disponibili")]
  [CommandAlias("ヘルプ", Language.Japanese, description: "利用可能なコマンドを表示します")]
  [CommandAlias("도움말", Language.Korean, description: "사용 가능한 명령을 표시합니다")]
  [CommandAlias("ayuda", Language.Latam, description: "Muestra los comandos disponibles")]
  [CommandAlias("pomoc", Language.Polish, description: "Pokazuje dostępne polecenia")]
  [CommandAlias("помощь", Language.Russian, description: "Показывает доступные команды")]
  [CommandAlias("ayuda", Language.Spanish, description: "Muestra los comandos disponibles")]
  [CommandAlias("帮助", Language.ChineseSimplified, description: "显示可用命令")]
  [CommandAlias("幫助", Language.ChineseTraditional, description: "顯示可用指令")]
  [CommandAlias("ช่วยเหลือ", Language.Thai, description: "แสดงคำสั่งที่ใช้ได้")]
  [CommandAlias("yardım", Language.Turkish, description: "Kullanılabilir komutları gösterir")]
  [CommandAlias("допомога", Language.Ukrainian, description: "Показує доступні команди")]
  [CommandAlias("trợgiúp", Language.Vietnamese, description: "Hiển thị các lệnh có sẵn")]
  internal static void HelpCommand(CommandContext ctx, int page = 1) {
    HelpCommandInternal(ctx, ctx.Sender.Language, page);
  }

  private static void HelpCommandInternal(CommandContext ctx, Language targetLanguage, int page) {
    const int commandsPerMessage = 5;
    const int messagesPerPage = 8;
    const int commandsPerPage = commandsPerMessage * messagesPerPage;

    if (page < 1) page = 1;

    bool isCustomLanguage = targetLanguage != ctx.Sender.Language;

    var commandsByAssembly = GetCommandsByAssembly(targetLanguage, ctx.Sender.IsAdmin);

    if (commandsByAssembly.Count == 0) {
      ctx.Reply(LocalizationService.Get(ctx.Sender, LocalizationKey.HelpNoCommands).FormatError());
      return;
    }

    var flatCommandList = new List<(string AssemblyName, CommandInfo Command)>();
    foreach (var (assemblyName, commands) in commandsByAssembly) {
      foreach (var cmd in commands) {
        flatCommandList.Add((assemblyName, cmd));
      }
    }

    int totalCommands = flatCommandList.Count;
    int totalPages = (int)Math.Ceiling(totalCommands / (double)commandsPerPage);

    if (page > totalPages) page = totalPages;

    int startIndex = (page - 1) * commandsPerPage;
    int endIndex = Math.Min(startIndex + commandsPerPage, totalCommands);
    var pageCommands = flatCommandList.Skip(startIndex).Take(endIndex - startIndex).ToList();

    for (int i = 0; i < pageCommands.Count; i += commandsPerMessage) {
      var messageBuilder = new System.Text.StringBuilder();

      if (i == 0) {
        string headerText = LocalizationService.Get(ctx.Sender, LocalizationKey.HelpAvailableCommands, page, totalPages);
        messageBuilder.AppendLine(headerText.Bold());

        if (isCustomLanguage) {
          messageBuilder.AppendLine($"~Language: {targetLanguage}~".WithColor("yellow"));
        }

        messageBuilder.AppendLine();
      }

      string currentAssembly = null;
      int commandsInMessage = 0;

      for (int j = i; j < Math.Min(i + commandsPerMessage, pageCommands.Count); j++) {
        var (assemblyName, command) = pageCommands[j];

        if (currentAssembly != assemblyName) {
          if (currentAssembly != null) {
            messageBuilder.AppendLine();
          }
          messageBuilder.AppendLine($"~[{assemblyName}]~");
          currentAssembly = assemblyName;
        }

        string commandWithParams = FormatCommandWithParameters(command);
        messageBuilder.AppendLine($"  {CommandPrefix}{commandWithParams}".FormatSuccess());
        commandsInMessage++;
      }

      if (i + commandsPerMessage >= pageCommands.Count && page < totalPages) {
        messageBuilder.AppendLine();

        string nextPageText = LocalizationService.Get(ctx.Sender, LocalizationKey.HelpNextPage, CommandPrefix, page + 1);
        messageBuilder.AppendLine(nextPageText.WithColor("white"));
      }

      ctx.Reply(messageBuilder.ToString());
    }
  }



  [Command("language", language: Language.English, aliases: ["lang"], description: "Set your preferred language (e.g. .language portuguese)")]
  [CommandAlias("linguagem", language: Language.Portuguese, aliases: ["ling"], description: "Defina seu idioma preferido (ex: .linguagem portuguese)")]
  [CommandAlias("langue", language: Language.French, aliases: ["lg"], description: "Définissez votre langue préférée (ex: .langue portuguese)")]
  [CommandAlias("sprache", language: Language.German, aliases: ["spr"], description: "Stellen Sie Ihre bevorzugte Sprache ein (z.B. .sprache portuguese)")]
  [CommandAlias("nyelv", language: Language.Hungarian, aliases: ["ny"], description: "Állítsa be az előnyben részesített nyelvét (pl. .nyelv portuguese)")]
  [CommandAlias("lingua", language: Language.Italian, aliases: ["lng"], description: "Imposta la tua lingua preferita (es. .lingua portuguese)")]
  [CommandAlias("言語", language: Language.Japanese, aliases: ["げんご"], description: "希望する言語を設定します (例: .言語 portuguese)")]
  [CommandAlias("언어", language: Language.Korean, aliases: ["언"], description: "선호하는 언어를 설정합니다 (예: .언어 portuguese)")]
  [CommandAlias("idioma", language: Language.Latam, aliases: ["idm"], description: "Establece tu idioma preferido (ej: .idioma portuguese)")]
  [CommandAlias("język", language: Language.Polish, aliases: ["jęz"], description: "Ustaw preferowany język (np. .język portuguese)")]
  [CommandAlias("язык", language: Language.Russian, aliases: ["яз"], description: "Установите предпочитаемый язык (напр. .язык portuguese)")]
  [CommandAlias("idioma", language: Language.Spanish, aliases: ["idm"], description: "Establece tu idioma preferido (ej: .idioma portuguese)")]
  [CommandAlias("语言", language: Language.ChineseSimplified, aliases: ["语"], description: "设置您的首选语言 (例如: .语言 portuguese)")]
  [CommandAlias("語言", language: Language.ChineseTraditional, aliases: ["語"], description: "設定您的偏好語言 (例如: .語言 portuguese)")]
  [CommandAlias("ภาษา", language: Language.Thai, aliases: ["ภษ"], description: "ตั้งค่าภาษาที่คุณต้องการ (เช่น: .ภาษา portuguese)")]
  [CommandAlias("dil", language: Language.Turkish, aliases: ["dl"], description: "Tercih ettiğiniz dili ayarlayın (örn: .dil portuguese)")]
  [CommandAlias("мова", language: Language.Ukrainian, aliases: ["мв"], description: "Встановіть бажану мову (напр: .мова portuguese)")]
  [CommandAlias("ngônngữ", language: Language.Vietnamese, aliases: ["nn"], description: "Đặt ngôn ngữ ưa thích của bạn (ví dụ: .ngônngữ portuguese)")]
  public static void SetLanguage(CommandContext ctx, string language = "") {
    var player = ctx.Sender;
    if (player == null) {
      ctx.ReplyError(LocalizationService.Get(ctx.Sender, LocalizationKey.MustBePlayer));
      return;
    }

    if (string.IsNullOrWhiteSpace(language)) {
      var current = LocalizationService.GetPlayerLanguage(player);
      ctx.ReplyInfo(LocalizationService.Get(ctx.Sender, LocalizationKey.PlayerLanguageCurrent, current));
      return;
    }

    var newLang = LocalizationService.GetLanguageFromString(language);

    if (!LocalizationService.IsLanguageAvailable(newLang)) {
      ctx.ReplyError(LocalizationService.Get(ctx.Sender, LocalizationKey.LanguageNotSupported, newLang));

      var availableLanguages = string.Join(", ", LocalizationService.AvailableServerLanguages);
      ctx.ReplyInfo(LocalizationService.Get(ctx.Sender, LocalizationKey.AvailableLanguages, availableLanguages));
      return;
    }

    LocalizationService.SetPlayerLanguage(player, newLang);
    ctx.Reply(LocalizationService.Get(ctx.Sender, LocalizationKey.PlayerLanguageChanged, newLang).FormatSuccess());
  }

  [CommandGroup("admin", language: Language.English, aliases: ["sc"], adminOnly: true)]
  internal static class ServerCommands {

    [Command("serverlanguage", language: Language.English, aliases: ["svlang"], description: "Set server language")]
    [CommandAlias("linguagemserver", language: Language.Portuguese, aliases: ["lingsv"], description: "Definir linguagem do servidor")]
    [CommandAlias("langueserveur", language: Language.French, aliases: ["lgsv"], description: "Définir la langue du serveur")]
    [CommandAlias("serversprache", language: Language.German, aliases: ["svspr"], description: "Serversprache festlegen")]
    [CommandAlias("szervernyelv", language: Language.Hungarian, aliases: ["sznyelv"], description: "Szerver nyelv beállítása")]
    [CommandAlias("linguaserver", language: Language.Italian, aliases: ["lingsv"], description: "Imposta lingua server")]
    [CommandAlias("サーバー言語", language: Language.Japanese, aliases: ["sv言語"], description: "サーバーの言語を設定")]
    [CommandAlias("서버언어", language: Language.Korean, aliases: ["sv언어"], description: "서버 언어 설정")]
    [CommandAlias("idiomaservidor", language: Language.Latam, aliases: ["idiomasv"], description: "Establecer idioma del servidor")]
    [CommandAlias("językservera", language: Language.Polish, aliases: ["jęzsv"], description: "Ustaw język serwera")]
    [CommandAlias("языксервера", language: Language.Russian, aliases: ["языксв"], description: "Установить язык сервера")]
    [CommandAlias("idiomaservidor", language: Language.Spanish, aliases: ["idiomasv"], description: "Establecer idioma del servidor")]
    [CommandAlias("服务器语言", language: Language.ChineseSimplified, aliases: ["服务器语"], description: "设置服务器语言")]
    [CommandAlias("伺服器語言", language: Language.ChineseTraditional, aliases: ["伺服器語"], description: "設定伺服器語言")]
    [CommandAlias("ภาษาเซิร์ฟเวอร์", language: Language.Thai, aliases: ["ภาษาsv"], description: "ตั้งค่าภาษาเซิร์ฟเวอร์")]
    [CommandAlias("sunucudili", language: Language.Turkish, aliases: ["svdil"], description: "Sunucu dilini ayarla")]
    [CommandAlias("мовасервера", language: Language.Ukrainian, aliases: ["мовасв"], description: "Встановити мову сервера")]
    [CommandAlias("ngônngữmáychủ", language: Language.Vietnamese, aliases: ["ngônngữsv"], description: "Đặt ngôn ngữ máy chủ")]
    public static void SetLanguage(CommandContext ctx, string language = "") {
      var newLanguage = LocalizationService.GetLanguageFromString(language);

      if (string.IsNullOrWhiteSpace(language)) {
        var current = LocalizationService.CurrentServerLanguage;
        ctx.ReplyInfo(LocalizationService.Get(ctx.Sender, LocalizationKey.ServerLanguageCurrent, current));
        return;
      }

      if (!LocalizationService.IsLanguageAvailable(newLanguage)) {
        ctx.ReplyError(LocalizationService.Get(ctx.Sender, LocalizationKey.LanguageNotSupported, newLanguage));

        var availableLanguages = string.Join(", ", LocalizationService.AvailableServerLanguages.Select(l => $"<mark=#a963ff25>{l}</mark>"));
        ctx.ReplyInfo(LocalizationService.Get(ctx.Sender, LocalizationKey.AvailableLanguages, availableLanguages));
        return;
      }

      if (LocalizationService.ChangeLanguage(newLanguage)) {
        Plugin.Settings.Set("PrefabLocalizationLanguage", newLanguage);
        ctx.Reply(LocalizationService.Get(ctx.Sender, LocalizationKey.ServerLanguageChanged, newLanguage).FormatSuccess());
        Log.Info($"ScarletCore localization language changed to: {newLanguage} by admin {ctx.Sender?.Name}");
      } else {
        ctx.ReplyError(LocalizationService.Get(ctx.Sender, LocalizationKey.LanguageChangeFailed, newLanguage));
      }
    }
  }
}