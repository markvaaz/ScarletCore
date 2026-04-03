namespace ScarletCore.Interface.Builders;

/// <summary>
/// Animation style played when a window opens or closes.
/// Applied via <see cref="Window.OpenAnimation"/> and <see cref="Window.CloseAnimation"/>.
/// </summary>
public enum WindowAnimation {
  /// <summary>No animation — window appears/disappears instantly.</summary>
  None,
  /// <summary>Fades the window in or out using alpha.</summary>
  Fade,
  /// <summary>Scales from 0.85 → 1 (open) or 1 → 0.85 (close) combined with a fade.</summary>
  Scale,
  /// <summary>Slides in from above (open) or out upward (close) combined with a fade.</summary>
  SlideDown,
  /// <summary>Slides in from below (open) or out downward (close) combined with a fade.</summary>
  SlideUp,
}
