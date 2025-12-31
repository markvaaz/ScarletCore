using System.Reflection;
using ScarletCore.Localization;

namespace ScarletCore.Commanding;

/// <summary>
/// Holds metadata for a registered command (name, parameters, attributes and origin assembly).
/// </summary>
public sealed class CommandInfo {
  /// <summary>The command name (without group).</summary>
  public string Name { get; set; }
  /// <summary>Optional command group name.</summary>
  public string Group { get; set; }
  /// <summary>Full command name including group if present.</summary>
  public string FullCommandName => string.IsNullOrEmpty(Group) ? Name : $"{Group} {Name}".Trim();
  /// <summary>Number of tokens in the command's name.</summary>
  public int NameTokenCount { get; set; } = 0;
  /// <summary>Number of tokens in the command's group.</summary>
  public int GroupTokenCount { get; set; } = 0;
  /// <summary>Minimum number of parameters (excluding optional ones).</summary>
  public int MinParameterCount { get; set; } = 0;
  /// <summary>Maximum number of parameters.</summary>
  public int MaxParameterCount { get; set; } = 0;
  /// <summary>Parameter info array for the method's parameters (excluding context if present).</summary>
  public ParameterInfo[] Parameters { get; set; }
  /// <summary>Minimum token count required to match this command.</summary>
  public int MinTokenCount => NameTokenCount + GroupTokenCount + MinParameterCount;
  /// <summary>Maximum token count permitted to match this command.</summary>
  public int MaxTokenCount => NameTokenCount + GroupTokenCount + MaxParameterCount;
  /// <summary>Whether the command is restricted to admins.</summary>
  public bool AdminOnly { get; set; }
  /// <summary>Whether this is the main command definition (as opposed to an alias).</summary>
  public bool IsMainCommand { get; set; }
  /// <summary>The <see cref="CommandAttribute"/> used to define the command (if main).</summary>
  public CommandAttribute Attribute { get; set; }
  /// <summary>The <see cref="CommandAliasAttribute"/> when this entry represents an alias.</summary>
  public CommandAliasAttribute AliasAttribute { get; set; }
  /// <summary>Language the command is registered for.</summary>
  public Language Language { get; set; }
  /// <summary>The reflected method info for the command implementation.</summary>
  public MethodInfo Method { get; set; }
  /// <summary>The assembly where the command was defined.</summary>
  public Assembly Assembly { get; set; }
  /// <summary>Flag to cancel command execution when emitted in OnBeforeExecute event.</summary>
  public bool CancelExecution { get; set; } = false;
}