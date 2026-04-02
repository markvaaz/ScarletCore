namespace ScarletCore.Interface.Builders;

/// <summary>
/// Controls whether padding is included within the specified width/height (<see cref="BorderBox"/>)
/// or added on top of it, growing the element (<see cref="ContentBox"/>).
/// </summary>
public enum BoxSizing {
  /// <summary>Width/height include padding and border — element stays at the declared size.</summary>
  BorderBox,
  /// <summary>Width/height describe the content area only — padding and border grow the element.</summary>
  ContentBox
}
