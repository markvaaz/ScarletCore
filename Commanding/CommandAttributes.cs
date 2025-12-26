using System;

namespace ScarletCore.Commanding;

/// <summary>
/// Attribute to mark a static class as a command group.
/// Example: [SCommandGroup("sc", Aliases = new[] { "scarlet", "s" }, AdminOnly = true)]
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandGroupAttribute(string group, string[] aliases = null, bool adminOnly = false) : Attribute {
  public string Group { get; } = group;
  public string[] Aliases { get; set; } = aliases ?? [];
  public bool AdminOnly { get; set; } = adminOnly;
}

/// <summary>
/// Attribute to mark a method as a command.
/// Example: [SCommand("help", Aliases = new[] { "h", "?" }, Description = "Shows help")]
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class CommandAttribute(string name, string[] aliases = null, bool adminOnly = false, string description = "", string usage = "") : Attribute {
  public string Name { get; } = name;
  public string[] Aliases { get; set; } = aliases ?? [];
  public bool AdminOnly { get; set; } = adminOnly;
  public string Description { get; set; } = description;
  public string Usage { get; set; } = usage;
}