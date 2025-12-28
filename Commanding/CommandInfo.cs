using System.Reflection;
using ScarletCore.Services;

namespace ScarletCore.Commanding;

internal sealed class CommandInfo {
  public string Name { get; set; }
  public string Group { get; set; }
  public string FullCommandName => string.IsNullOrEmpty(Group) ? Name : $"{Group} {Name}".Trim();
  public int NameTokenCount { get; set; } = 0;
  public int GroupTokenCount { get; set; } = 0;
  public int MinParameterCount { get; set; } = 0;
  public int MaxParameterCount { get; set; } = 0;
  public ParameterInfo[] Parameters { get; set; }
  public int MinTokenCount => NameTokenCount + GroupTokenCount + MinParameterCount;
  public int MaxTokenCount => NameTokenCount + GroupTokenCount + MaxParameterCount;
  public bool AdminOnly { get; set; }
  public bool IsMainCommand { get; set; }
  public CommandAttribute Attribute { get; set; }
  public CommandAliasAttribute AliasAttribute { get; set; }
  public Language Language { get; set; }
  public MethodInfo Method { get; set; }
  public Assembly Assembly { get; set; }
}