namespace ScarletCore.Interface.Builders;

/// <summary>Controls how children that overflow the container bounds are handled.</summary>
public enum OverflowMode {
  /// <summary>Content is not clipped and overflows the bounds.</summary>
  Visible,
  /// <summary>Content is clipped at the container bounds via a soft RectMask2D (cheap, but only
  /// clips children whose shader honours the UI clip rect — the default UI material does).</summary>
  Hidden,
  /// <summary>Content is clipped via a stencil <see cref="UnityEngine.UI.Mask"/> instead of a
  /// RectMask2D. Use this when children are drawn with the game's own UI materials (native icons,
  /// animated materials): their shaders ignore the RectMask2D clip rect and overflow the box, but
  /// the stencil buffer clips them. Costs a graphic on the host plus a couple of draw calls.</summary>
  Clip,
  /// <summary>Scrollable on both axes.</summary>
  Scroll,
  /// <summary>Scrollable horizontally only.</summary>
  ScrollX,
  /// <summary>Scrollable vertically only (VLG-based list).</summary>
  ScrollY,
  /// <summary>Scroll axes are activated automatically when content overflows.</summary>
  Auto,
}
