using System;

namespace ScarletCore.Events;

/// <summary>
/// Specifies the priority for an event handler method. Methods with higher priority values are invoked before those with lower values.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventPriorityAttribute(int priority = 0) : Attribute {
  /// <summary>
  /// Gets the priority value assigned to the event handler. Higher values indicate higher priority.
  /// </summary>
  public int Priority { get; } = priority;
}