using System;

namespace ScarletCore.Services;

/// <summary>
/// Attribute to mark a static class as a command group.
/// Example: [SCommandGroup("sc", Aliases = new[] { "scarlet", "s" }, AdminOnly = true)]
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SCommandGroupAttribute : Attribute {
  public string Group { get; }
  public string[] Aliases { get; set; } = Array.Empty<string>();
  public bool AdminOnly { get; set; } = false;

  public SCommandGroupAttribute(string group) {
    Group = group;
  }
}

/// <summary>
/// Attribute to mark a method as a command.
/// Example: [SCommand("help", Aliases = new[] { "h", "?" }, Description = "Shows help")]
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class SCommandAttribute : Attribute {
  public string Name { get; }
  public string[] Aliases { get; set; } = Array.Empty<string>();
  public bool AdminOnly { get; set; } = false;
  public string Description { get; set; } = string.Empty;
  public string Usage { get; set; } = string.Empty;

  public SCommandAttribute(string name) {
    Name = name;
  }
}