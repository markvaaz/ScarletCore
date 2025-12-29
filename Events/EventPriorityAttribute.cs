using System;

namespace ScarletCore.Events;

/// <summary>
/// Specifies the priority for an event handler method. Methods with higher priority values are invoked before those with lower values.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventPriorityAttribute(int priority = EventPriority.Normal) : Attribute {
  /// <summary>
  /// Gets the priority value assigned to the event handler. Higher values indicate higher priority.
  /// </summary>
  public int Priority { get; } = priority;
}

/// <summary>
/// Defines commonly used priority values for event handlers.
/// Handlers with higher numeric values are invoked before handlers with lower values.
/// </summary>
public static class EventPriority {
  /// <summary>The lowest priority; handlers with this value run last.</summary>
  public const int Last = -999;

  /// <summary>Very low priority handlers.</summary>
  public const int VeryLow = -300;
  /// <summary>Low priority handlers.</summary>
  public const int Low = -200;
  /// <summary>Slightly lower than normal priority.</summary>
  public const int LowerThanNormal = -100;

  /// <summary>The normal/default priority value.</summary>
  public const int Normal = 0;

  /// <summary>Slightly higher than normal priority.</summary>
  public const int HigherThanNormal = 100;
  /// <summary>High priority handlers.</summary>
  public const int High = 200;
  /// <summary>Very high priority handlers.</summary>
  public const int VeryHigh = 300;

  /// <summary>The highest priority; handlers with this value run first.</summary>
  public const int First = 999;
}
