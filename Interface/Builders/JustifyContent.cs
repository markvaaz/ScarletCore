namespace ScarletCore.Interface.Builders;

/// <summary>Horizontal distribution of children inside a Row.</summary>
public enum JustifyContent {
  /// <summary>Pack children toward the start of the row.</summary>
  Start,
  /// <summary>Center children within the row.</summary>
  Center,
  /// <summary>Pack children toward the end of the row.</summary>
  End,
  /// <summary>Distribute children with equal space between them.</summary>
  SpaceBetween,
  /// <summary>Distribute children with equal space around each child.</summary>
  SpaceAround,
  /// <summary>Distribute children with equal space between and at the edges.</summary>
  SpaceEvenly
}
