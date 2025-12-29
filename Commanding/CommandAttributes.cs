using System;
using ScarletCore.Localization;

namespace ScarletCore.Commanding;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandGroupAttribute(string group, Language language, string[] aliases = null, bool adminOnly = false) : Attribute {
  public string Group { get; } = group;
  public Language Language { get; set; } = language;
  public string[] Aliases { get; set; } = aliases ?? [];
  public bool AdminOnly { get; set; } = adminOnly;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class CommandGroupAliasAttribute(string group, Language language, string[] aliases = null) : Attribute {
  public string Group { get; } = group;
  public Language Language { get; set; } = language;
  public string[] Aliases { get; set; } = aliases ?? [];
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class CommandAttribute(string name, Language language, string[] aliases = null, bool adminOnly = false, string description = "", string usage = "") : Attribute {
  public string Name { get; } = name;
  public Language Language { get; set; } = language;
  public string[] Aliases { get; set; } = aliases ?? [];
  public bool AdminOnly { get; set; } = adminOnly;
  public string Description { get; set; } = description;
  public string Usage { get; set; } = usage;
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class CommandAliasAttribute(string name, Language language, string[] aliases = null, string description = "", string usage = "") : Attribute {
  public string Name { get; } = name;
  public Language Language { get; set; } = language;
  public string[] Aliases { get; set; } = aliases ?? [];
  public string Description { get; set; } = description;
  public string Usage { get; set; } = usage;
}
