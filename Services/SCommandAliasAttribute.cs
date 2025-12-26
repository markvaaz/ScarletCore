using System;

namespace ScarletCore.Services;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class SCommandAliasAttribute(string name, string language, string[] aliases = null, string description = "", string usage = "") : Attribute {
  public string Name { get; } = name;
  public string Language { get; set; } = (language ?? string.Empty).ToLower();
  public string[] Aliases { get; set; } = aliases ?? [];
  public string Description { get; set; } = description;
  public string Usage { get; set; } = usage;
}
