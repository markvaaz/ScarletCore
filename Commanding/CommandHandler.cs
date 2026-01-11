using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ProjectM.Network;
using ScarletCore.Events;
using ScarletCore.Localization;
using ScarletCore.Services;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ScarletCore.Commanding;

/// <summary>
/// Lookup key for stored commands used by <see cref="CommandHandler"/>.
/// </summary>
/// <param name="Language">The language the command is registered for.</param>
/// <param name="TokenCount">Number of tokens in the command name.</param>
/// <param name="CommandName">Lower-cased full command name (including group when present).</param>
internal readonly record struct CommandLookupKey(Language Language, int TokenCount, string CommandName);

/// <summary>
/// Static command system responsible for registering, finding and executing chat commands.
/// </summary>
public static class CommandHandler {
  /// <summary>The prefix used to identify chat commands (e.g. '.')</summary>
  public const char CommandPrefix = '.';

  private static readonly Dictionary<CommandLookupKey, List<CommandInfo>> CommandsByKey = [];
  private static readonly Dictionary<(int TokenCount, string CommandName), List<CommandInfo>> FallbackCommandsByKey = [];
  private static readonly Dictionary<Assembly, List<CommandLookupKey>> CommandKeysByAssembly = [];
  private static readonly Dictionary<Assembly, List<(int, string)>> FallbackKeysByAssembly = [];

  /// <summary>Initializes the command system by registering localization keys and discovering commands.</summary>
  internal static void Initialize() {
    RegisterAll();
  }

  /// <summary>Handles multiple chat message entities (batch entry for the messaging system).</summary>
  /// <param name="messageEntities">Array of chat message entities to process.</param>
  internal static void HandleMessageEvents(NativeArray<Entity> messageEntities) {
    foreach (var messageEntity in messageEntities) {
      HandleChat(messageEntity);
    }
  }

  /// <summary>Processes a single chat message entity and invokes commands when detected.</summary>
  /// <param name="messageEntity">The chat message entity to inspect.</param>
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

  /// <summary>
  /// Tries to execute a command as if it were sent by the specified player.
  /// </summary>
  /// <param name="player">The player who is executing the command.</param>
  /// <param name="commandText">The command text (with or without the prefix).</param>
  /// <returns>True if the command was found and executed, false otherwise.</returns>
  public static bool TryExecuteCommand(PlayerData player, string commandText) {
    if (player == null || string.IsNullOrWhiteSpace(commandText)) {
      return false;
    }

    var text = commandText.Trim();

    // Add prefix if not present
    if (!text.StartsWith(CommandPrefix)) {
      text = CommandPrefix + text;
    }

    try {
      HandleCommand(player, text, Entity.Null);
      return true;
    } catch (Exception ex) {
      Log.Error($"[CommandHandler] Error in TryExecuteCommand for player {player.Name}: {ex}");
      return false;
    }
  }

  /// <summary>
  /// Processes a command string from a <see cref="PlayerData"/> sender and invokes the matching method if found.
  /// </summary>
  /// <param name="player">The player who sent the command.</param>
  /// <param name="fullMessageText">The full raw message text, including the prefix.</param>
  /// <param name="messageEntity">The chat message entity (used to destroy the message when consumed).</param>
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
      var baseCommand = FindCommandByNameOnly(playerLanguage, tokens);
      if (baseCommand != null) {
        player.SendLocalizedErrorMessage(LocalizationKey.CmdAvailableUsages, GetCommandUsages(baseCommand));
      }
      return;
    }

    commandInfo.CancelExecution = false;

    if (messageEntity != Entity.Null && (!IsHelpCommand(commandInfo) || !IsVCFLoaded())) {
      try {
        messageEntity.Destroy(true);
      } catch (Exception ex) {
        Log.Error($"[CommandHandler] Error destroying chat message entity {messageEntity.Index}: {ex}");
      }
    }

    int commandNameTokens = commandInfo.NameTokenCount + commandInfo.GroupTokenCount;
    var args = tokens.AsSpan()[commandNameTokens..].ToArray();

    if (commandInfo.AdminOnly && !player.IsAdmin && !player.HasRole(DefaultRoles.Admin) && !player.HasRole(DefaultRoles.Owner)) {
      player.SendLocalizedErrorMessage(LocalizationKey.CmdRequiresAdmin);
      return;
    }

    if (!commandInfo.AdminOnly) {
      bool hasPermission = false;
      foreach (var permission in commandInfo.RequiredPermissions) {
        if (player.HasPermission(permission)) {
          hasPermission = true;
          break;
        }
      }
      if (!hasPermission) {
        var permissions = string.Join(", ", commandInfo.RequiredPermissions);
        player.SendLocalizedErrorMessage(LocalizationKey.CmdRequiresPermission, permissions);
        return;
      }
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
        paramValues[i] = new CommandContext(Entity.Null, player, fullMessageText, args) {
          CallingAssembly = commandInfo.Assembly
        };
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
      EventManager.Emit(CommandEvents.OnBeforeExecute, player, commandInfo, args);
      if (commandInfo.CancelExecution) {
        commandInfo.CancelExecution = false;
      } else {
        method.Invoke(null, paramValues);
        EventManager.Emit(CommandEvents.OnAfterExecute, player, commandInfo, args);
      }
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

  /// <summary>
  /// Scans an assembly for command definitions and registers them with the command system.
  /// </summary>
  /// <param name="assembly">Assembly to scan; defaults to the calling assembly.</param>
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
          bool effectiveAdminOnly = groupAdminOnly || (commandAttr?.AdminOnly == true);

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

    var mainCommandCount = fallbackKeys.Count;
    var aliasCommandCount = commandKeys.Count - mainCommandCount;

    Log.Message($"[CommandHandler] Registered {mainCommandCount} commands and {aliasCommandCount} aliases from '{assembly.GetName().Name}'.");
  }

  /// <summary>
  /// Unregisters all commands that were registered from the given assembly.
  /// </summary>
  /// <param name="assembly">Assembly to remove; defaults to the calling assembly.</param>
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

    var groupAttr = type.GetCustomAttribute<CommandGroupAttribute>();
    if (groupAttr != null) {
      groups.Add((groupAttr.Group, groupAttr.Language));

      foreach (var alias in groupAttr.Aliases ?? []) {
        groups.Add((alias, groupAttr.Language));
      }
    }

    var groupAliases = type.GetCustomAttributes<CommandGroupAliasAttribute>();
    foreach (var aliasAttr in groupAliases) {
      groups.Add((aliasAttr.Group, aliasAttr.Language));

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
      RequiredPermissions = attr is CommandAttribute cmdAttribute ? cmdAttribute.RequiredPermissions : ["basic"],
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

  /// <summary>
  /// Finds a command by name only, ignoring parameter validation.
  /// Used to provide usage information when parameters are incorrect.
  /// </summary>
  private static CommandInfo FindCommandByNameOnly(Language playerLanguage, ReadOnlySpan<string> tokens) {
    if (tokens.Length == 0) return null;

    for (int commandTokens = Math.Min(tokens.Length, 3); commandTokens > 0; commandTokens--) {
      string commandName = BuildCommandName(tokens, commandTokens);
      var key = new CommandLookupKey(playerLanguage, commandTokens, commandName);

      if (CommandsByKey.TryGetValue(key, out var commands) && commands.Count > 0) {
        return commands[0];
      }

      var fallbackKey = (commandTokens, commandName);
      if (FallbackCommandsByKey.TryGetValue(fallbackKey, out var fallbackCommands) && fallbackCommands.Count > 0) {
        return fallbackCommands[0];
      }
    }

    return null;
  }

  /// <summary>
  /// Finds the best matching <see cref="CommandInfo"/> for the provided tokens and player language.
  /// Returns null when no match is found.
  /// </summary>
  internal static CommandInfo FindCommand(Language playerLanguage, ReadOnlySpan<string> tokens) {
    if (tokens.Length == 0) return null;

    int totalTokens = tokens.Length;

    // First: Check if there's a command with exact name match (starting from most tokens)
    // If we find a command whose NAME matches, that's the one we should use (even if params are wrong)
    for (int commandTokens = totalTokens; commandTokens > 0; commandTokens--) {
      string commandName = BuildCommandName(tokens, commandTokens);
      var key = new CommandLookupKey(playerLanguage, commandTokens, commandName);

      // Check if ANY command exists with this exact name
      if (CommandsByKey.TryGetValue(key, out var commands) && commands.Count > 0) {
        // Found command(s) with this exact name - now find the best parameter match
        CommandInfo bestMatch = null;
        int bestScore = -1;

        foreach (var command in commands) {
          // Try with strict type checking first
          int score = CalculateCommandMatchScore(command, tokens, commandTokens, allowTypeFailure: false);
          if (score >= 0) {
            score += 100000 + (commandTokens * 10000);
            if (score > bestScore) {
              bestMatch = command;
              bestScore = score;
            }
          } else {
            // Try with lenient type checking (for usage messages)
            score = CalculateCommandMatchScore(command, tokens, commandTokens, allowTypeFailure: true);
            if (score >= 0) {
              score += 1000 + (commandTokens * 100);
              if (score > bestScore) {
                bestMatch = command;
                bestScore = score;
              }
            }
          }
        }

        // If we found ANY match (even with wrong params), return it
        if (bestMatch != null) {
          return bestMatch;
        }

        // Name matches but no valid parameter combination - return first command for usage
        return commands[0];
      }

      // Check fallback commands
      var fallbackKey = (commandTokens, commandName);
      if (FallbackCommandsByKey.TryGetValue(fallbackKey, out var fallbackCommands) && fallbackCommands.Count > 0) {
        CommandInfo bestMatch = null;
        int bestScore = -1;

        foreach (var command in fallbackCommands) {
          int score = CalculateCommandMatchScore(command, tokens, commandTokens, allowTypeFailure: false);
          if (score >= 0) {
            score += 50000 + (commandTokens * 10000);
            if (score > bestScore) {
              bestMatch = command;
              bestScore = score;
            }
          } else {
            score = CalculateCommandMatchScore(command, tokens, commandTokens, allowTypeFailure: true);
            if (score >= 0) {
              score += 500 + (commandTokens * 100);
              if (score > bestScore) {
                bestMatch = command;
                bestScore = score;
              }
            }
          }
        }

        if (bestMatch != null) {
          return bestMatch;
        }

        return fallbackCommands[0];
      }
    }

    return null;
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

  /// <summary>
  /// Attempts to convert a string token to the target parameter type using <see cref="TypeConverter"/>.
  /// </summary>
  /// <param name="input">Input token.</param>
  /// <param name="targetType">Target type.</param>
  /// <param name="result">Converted value on success.</param>
  /// <returns>True if conversion succeeded, otherwise false.</returns>
  private static bool TryConvertParameter(string input, Type targetType, out object result) {
    result = null;

    try {
      result = TypeConverter.ConvertToType(input, targetType);
      return true;
    } catch {
      return false;
    }
  }

  /// <summary>
  /// Splits an input string into tokens, honoring quoted segments as single tokens.
  /// </summary>
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

  /// <summary>
  /// Builds a colored usage string for a command used by help/error messages.
  /// </summary>
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

  /// <summary>
  /// Formats a command name with its parameters for display (showing optional/default values where applicable).
  /// </summary>
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
    public const string CmdRequiresPermission = "cmd_requires_permission";
    public const string CmdExecutionError = "cmd_execution_error";
    public const string CmdAvailableUsages = "cmd_available_usages";
    public const string CmdInvalidParameter = "cmd_invalid_parameter";
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
    public const string HelpModNotFound = "help_mod_not_found";
    public const string HelpModAvailable = "help_mod_available";
    public const string HelpModHeader = "help_mod_header";
    public const string HelpModNextPage = "help_mod_next_page";
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
    var targetLanguage = Localizer.GetLanguageFromString(language);
    if (targetLanguage == Language.None) {
      ctx.ReplyLocalizedError(LocalizationKey.LanguageNotSupported, language);
      return;
    }
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
    const int commandsPerMessage = 4;
    const int messagesPerPage = 8;
    const int commandsPerPage = commandsPerMessage * messagesPerPage;

    if (page < 1) page = 1;

    bool isCustomLanguage = targetLanguage != ctx.Sender.Language;

    var commandsByAssembly = GetCommandsByAssembly(targetLanguage, ctx.Sender.IsAdmin);

    if (commandsByAssembly.Count == 0) {
      ctx.ReplyLocalizedError(LocalizationKey.HelpNoCommands);
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
        string headerText = Localizer.Get(ctx.Sender, LocalizationKey.HelpAvailableCommands, page, totalPages);
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

        string nextPageText = Localizer.Get(ctx.Sender, LocalizationKey.HelpNextPage, CommandPrefix, page + 1);
        messageBuilder.AppendLine(nextPageText.WithColor("white"));
      }

      ctx.Reply(messageBuilder.ToString());
    }
  }
  [Command("helpmod", Language.English, description: "Shows commands from a specific mod")]
  [CommandAlias("ajudamod", Language.Portuguese, description: "Mostra comandos de um mod específico")]
  [CommandAlias("aidemod", Language.French, description: "Affiche les commandes d'un mod spécifique")]
  [CommandAlias("hilfemod", Language.German, description: "Zeigt Befehle eines bestimmten Mods an")]
  [CommandAlias("segítségmod", Language.Hungarian, description: "Megjeleníti egy adott mod parancsait")]
  [CommandAlias("aiutomod", Language.Italian, description: "Mostra i comandi di un mod specifico")]
  [CommandAlias("ヘルプモッド", Language.Japanese, description: "特定のModのコマンドを表示します")]
  [CommandAlias("도움말모드", Language.Korean, description: "특정 모드의 명령을 표시합니다")]
  [CommandAlias("ayudamod", Language.Latam, description: "Muestra comandos de un mod específico")]
  [CommandAlias("pomocmod", Language.Polish, description: "Pokazuje polecenia z określonego moda")]
  [CommandAlias("помощьмод", Language.Russian, description: "Показывает команды определенного мода")]
  [CommandAlias("ayudamod", Language.Spanish, description: "Muestra comandos de un mod específico")]
  [CommandAlias("帮助模组", Language.ChineseSimplified, description: "显示特定模组的命令")]
  [CommandAlias("幫助模組", Language.ChineseTraditional, description: "顯示特定模組的指令")]
  [CommandAlias("ช่วยเหลือม็อด", Language.Thai, description: "แสดงคำสั่งจากม็อดเฉพาะ")]
  [CommandAlias("yardımmod", Language.Turkish, description: "Belirli bir moddan komutları gösterir")]
  [CommandAlias("допомогамод", Language.Ukrainian, description: "Показує команди певного мода")]
  [CommandAlias("trợgiúpmod", Language.Vietnamese, description: "Hiển thị lệnh từ mod cụ thể")]
  internal static void HelpModCommand(CommandContext ctx, string assemblyName, int page = 1) {
    HelpModCommandInternal(ctx, assemblyName, ctx.Sender.Language, page);
  }

  [Command("helpmod", Language.English, description: "Shows commands from a specific mod")]
  [CommandAlias("ajudamod", Language.Portuguese, description: "Mostra comandos de um mod específico")]
  [CommandAlias("aidemod", Language.French, description: "Affiche les commandes d'un mod spécifique")]
  [CommandAlias("hilfemod", Language.German, description: "Zeigt Befehle eines bestimmten Mods an")]
  [CommandAlias("segítségmod", Language.Hungarian, description: "Megjeleníti egy adott mod parancsait")]
  [CommandAlias("aiutomod", Language.Italian, description: "Mostra i comandi di un mod specifico")]
  [CommandAlias("ヘルプモッド", Language.Japanese, description: "特定のModのコマンドを表示します")]
  [CommandAlias("도움말모드", Language.Korean, description: "특정 모드의 명령을 표시합니다")]
  [CommandAlias("ayudamod", Language.Latam, description: "Muestra comandos de un mod específico")]
  [CommandAlias("pomocmod", Language.Polish, description: "Pokazuje polecenia z określonego moda")]
  [CommandAlias("помощьмод", Language.Russian, description: "Показывает команды определенного мода")]
  [CommandAlias("ayudamod", Language.Spanish, description: "Muestra comandos de un mod específico")]
  [CommandAlias("帮助模组", Language.ChineseSimplified, description: "显示特定模组的命令")]
  [CommandAlias("幫助模組", Language.ChineseTraditional, description: "顯示特定模組的指令")]
  [CommandAlias("ช่วยเหลือม็อด", Language.Thai, description: "แสดงคำสั่งจากม็อดเฉพาะ")]
  [CommandAlias("yardımmod", Language.Turkish, description: "Belirli bir moddan komutları gösterir")]
  [CommandAlias("допомогамод", Language.Ukrainian, description: "Показує команди певного мода")]
  [CommandAlias("trợgiúpmod", Language.Vietnamese, description: "Hiển thị lệnh từ mod cụ thể")]
  internal static void HelpModCommand(CommandContext ctx, string assemblyName, string language, int page = 1) {
    var targetLanguage = Localizer.GetLanguageFromString(language);
    if (targetLanguage == Language.None) {
      ctx.ReplyLocalizedError(LocalizationKey.LanguageNotSupported, language);
      return;
    }
    HelpModCommandInternal(ctx, assemblyName, targetLanguage, page);
  }

  private static void HelpModCommandInternal(CommandContext ctx, string assemblyName, Language targetLanguage, int page) {
    const int commandsPerMessage = 4;
    const int messagesPerPage = 8;
    const int commandsPerPage = commandsPerMessage * messagesPerPage;

    if (page < 1) page = 1;

    bool isCustomLanguage = targetLanguage != ctx.Sender.Language;

    var commandsByAssembly = GetCommandsByAssembly(targetLanguage, ctx.Sender.IsAdmin);

    // Buscar o assembly que corresponde ao nome fornecido (case-insensitive)
    var matchingAssembly = commandsByAssembly.Keys.FirstOrDefault(key =>
      key.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));

    if (matchingAssembly == null) {
      // Assembly não encontrado - listar assemblies disponíveis
      var availableAssemblies = string.Join(", ", commandsByAssembly.Keys);
      string errorMsg = Localizer.Get(ctx.Sender, LocalizationKey.HelpModNotFound, assemblyName);
      string availableMsg = Localizer.Get(ctx.Sender, LocalizationKey.HelpModAvailable, availableAssemblies);

      ctx.ReplyError($"{errorMsg}\n{availableMsg}");
      return;
    }

    var commands = commandsByAssembly[matchingAssembly];

    if (commands.Count == 0) {
      ctx.ReplyLocalizedError(LocalizationKey.HelpNoCommands);
      return;
    }

    int totalCommands = commands.Count;
    int totalPages = (int)Math.Ceiling(totalCommands / (double)commandsPerPage);

    if (page > totalPages) page = totalPages;

    int startIndex = (page - 1) * commandsPerPage;
    int endIndex = Math.Min(startIndex + commandsPerPage, totalCommands);
    var pageCommands = commands.Skip(startIndex).Take(endIndex - startIndex).ToList();

    for (int i = 0; i < pageCommands.Count; i += commandsPerMessage) {
      var messageBuilder = new System.Text.StringBuilder();

      if (i == 0) {
        string headerText = Localizer.Get(ctx.Sender, LocalizationKey.HelpModHeader, matchingAssembly, page, totalPages);
        messageBuilder.AppendLine(headerText.Bold());

        if (isCustomLanguage) {
          messageBuilder.AppendLine($"~Language: {targetLanguage}~".WithColor("yellow"));
        }

        messageBuilder.AppendLine();
      }

      for (int j = i; j < Math.Min(i + commandsPerMessage, pageCommands.Count); j++) {
        var command = pageCommands[j];
        string commandWithParams = FormatCommandWithParameters(command);
        messageBuilder.AppendLine($"  {CommandPrefix}{commandWithParams}".FormatSuccess());
      }

      if (i + commandsPerMessage >= pageCommands.Count && page < totalPages) {
        messageBuilder.AppendLine();
        string nextPageText = Localizer.Get(ctx.Sender, LocalizationKey.HelpModNextPage,
          CommandPrefix, assemblyName, page + 1);
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
  private static void SetLanguage(CommandContext ctx, string language = "") {
    var player = ctx.Sender;
    if (player == null) {
      ctx.ReplyLocalizedError(LocalizationKey.MustBePlayer);
      return;
    }

    if (string.IsNullOrWhiteSpace(language)) {
      var current = Localizer.GetPlayerLanguage(player);
      ctx.ReplyLocalizedInfo(LocalizationKey.PlayerLanguageCurrent, current.ToString());
      return;
    }

    var newLang = Localizer.GetLanguageFromString(language);

    if (!Localizer.IsLanguageAvailable(newLang)) {
      ctx.ReplyLocalizedError(LocalizationKey.LanguageNotSupported, newLang.ToString());

      var availableLanguages = string.Join(", ", Localizer.AvailableServerLanguages);
      ctx.ReplyLocalizedInfo(LocalizationKey.AvailableLanguages, availableLanguages);
      return;
    }

    Localizer.SetPlayerLanguage(player, newLang);
    ctx.ReplyLocalizedSuccess(LocalizationKey.PlayerLanguageChanged, newLang.ToString());
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
      var newLanguage = Localizer.GetLanguageFromString(language);

      if (string.IsNullOrWhiteSpace(language)) {
        var current = Localizer.CurrentServerLanguage;
        ctx.ReplyLocalizedInfo(LocalizationKey.ServerLanguageCurrent, current.ToString());
        return;
      }

      if (!Localizer.IsLanguageAvailable(newLanguage)) {
        ctx.ReplyLocalizedError(LocalizationKey.LanguageNotSupported, newLanguage.ToString());

        var availableLanguages = string.Join(", ", Localizer.AvailableServerLanguages.Select(l => $"<mark=#a963ff25>{l}</mark>"));
        ctx.ReplyLocalizedInfo(LocalizationKey.AvailableLanguages, availableLanguages);
        return;
      }

      if (Localizer.ChangeLanguage(newLanguage)) {
        Plugin.Settings.Set("PrefabLocalizationLanguage", newLanguage);
        ctx.ReplyLocalizedSuccess(LocalizationKey.ServerLanguageChanged, newLanguage.ToString());
        Log.Message($"ScarletCore localization language changed to: {newLanguage} by admin {ctx.Sender?.Name}");
      } else {
        ctx.ReplyLocalizedError(LocalizationKey.LanguageChangeFailed, newLanguage.ToString());
      }
    }
  }
}