using System;
using ScarletCore.Localization;

namespace ScarletCore.Commanding;

/// <summary>
/// Specifies a command group that can contain related commands.
/// </summary>
/// <param name="group">The primary group name.</param>
/// <param name="language">The language this group applies to.</param>
/// <param name="aliases">Optional aliases for the group.</param>
/// <param name="adminOnly">Whether this group requires administrator privileges.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandGroupAttribute(string group, Language language, string[] aliases = null, bool adminOnly = false) : Attribute {
  /// <summary>The primary group name.</summary>
  public string Group { get; } = group;
  /// <summary>The language this group applies to.</summary>
  public Language Language { get; set; } = language;
  /// <summary>Optional aliases for the group.</summary>
  public string[] Aliases { get; set; } = aliases ?? [];
  /// <summary>Whether this group requires administrator privileges.</summary>
  public bool AdminOnly { get; set; } = adminOnly;
}

/// <summary>
/// Adds aliases for a command group in another language or context.
/// </summary>
/// <param name="group">The group name for the alias.</param>
/// <param name="language">The language for the alias.</param>
/// <param name="aliases">Optional alias names.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class CommandGroupAliasAttribute(string group, Language language, string[] aliases = null) : Attribute {
  /// <summary>The group name for the alias.</summary>
  public string Group { get; } = group;
  /// <summary>The language for the alias.</summary>
  public Language Language { get; set; } = language;
  /// <summary>Optional alias names.</summary>
  public string[] Aliases { get; set; } = aliases ?? [];
}

/// <summary>
/// Marks a method as a chat command.
/// </summary>
/// <param name="name">The command name.</param>
/// <param name="language">The language of the command.</param>
/// <param name="aliases">Optional aliases for the command.</param>
/// <param name="adminOnly">Whether the command requires admin privileges.</param>
/// <param name="description">A short description of the command.</param>
/// <param name="usage">Optional usage text for the command.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class CommandAttribute(string name, Language language, string[] aliases = null, bool adminOnly = false, string description = "", string usage = "") : Attribute {
  /// <summary>The command name.</summary>
  public string Name { get; } = name;
  /// <summary>The language of the command.</summary>
  public Language Language { get; set; } = language;
  /// <summary>Optional aliases for the command.</summary>
  public string[] Aliases { get; set; } = aliases ?? [];
  /// <summary>Whether the command requires administrator privileges.</summary>
  public bool AdminOnly { get; set; } = adminOnly;
  /// <summary>Short description used in help output.</summary>
  public string Description { get; set; } = description;
  /// <summary>Optional usage information for the command.</summary>
  public string Usage { get; set; } = usage;
}

/// <summary>
/// Adds an alias for a specific command (used for localization / alternate names).
/// </summary>
/// <param name="name">The command name this alias belongs to.</param>
/// <param name="language">The language of the alias.</param>
/// <param name="aliases">Optional alias names.</param>
/// <param name="description">A short description of the alias.</param>
/// <param name="usage">Optional usage text for the alias.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class CommandAliasAttribute(string name, Language language, string[] aliases = null, string description = "", string usage = "") : Attribute {
  /// <summary>The command name this alias belongs to.</summary>
  public string Name { get; } = name;
  /// <summary>The language of the alias.</summary>
  public Language Language { get; set; } = language;
  /// <summary>Optional alias names.</summary>
  public string[] Aliases { get; set; } = aliases ?? [];
  /// <summary>Short description used in help output for this alias.</summary>
  public string Description { get; set; } = description;
  /// <summary>Optional usage information for the alias.</summary>
  public string Usage { get; set; } = usage;
}
