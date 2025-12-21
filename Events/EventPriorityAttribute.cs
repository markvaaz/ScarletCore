using System;

namespace ScarletCore.Events;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventPriorityAttribute : Attribute {
  public int Priority { get; }

  public EventPriorityAttribute(int priority = 0) {
    Priority = priority;
  }
}