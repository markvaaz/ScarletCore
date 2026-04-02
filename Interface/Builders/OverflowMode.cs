namespace ScarletCore.Interface.Builders;

/// <summary>Controls how children that overflow the container bounds are handled.</summary>
public enum OverflowMode {
  /// <summary>Content is not clipped and overflows the bounds.</summary>
  Visible,
  /// <summary>Content is clipped at the container bounds.</summary>
  Hidden,
  /// <summary>Scrollable on both axes.</summary>
  Scroll,
  /// <summary>Scrollable horizontally only.</summary>
  ScrollX,
  /// <summary>Scrollable vertically only (VLG-based list).</summary>
  ScrollY,
  /// <summary>Scroll axes are activated automatically when content overflows.</summary>
  Auto,
}
