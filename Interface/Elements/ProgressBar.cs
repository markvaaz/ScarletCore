using System;
using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>A horizontal progress bar with a fill portion and track background.</summary>
public class ProgressBar : UIElement {
  /// <summary>Current progress value.</summary>
  public float Value { get; set; }
  /// <summary>Minimum value of the progress range. Default: 0.</summary>
  public float Min { get; set; }
  /// <summary>Maximum value of the progress range. Default: 100.</summary>
  public float Max { get; set; } = 100f;
  /// <summary>
  /// Fill portion visual — solid color, gradient, remote image, or native sprite.
  /// Clipped by the progress percentage.
  /// </summary>
  public UIBackground? BarFill { get; set; }
  /// <summary>
  /// When true, changes to <see cref="Value"/> animate smoothly instead of snapping.
  /// </summary>
  public bool AnimateValue { get; set; }
  /// <summary>Duration in seconds of the value-change animation. Default: 0.3s.</summary>
  public float AnimationDuration { get; set; } = 0.3f;
  /// <summary>
  /// When true, the client reads Health from the local character every frame and drives
  /// the fill percentage independently — no server value updates are needed.
  /// </summary>
  public bool IsHealthBar { get; set; }

  // ── Timed fill ────────────────────────────────────────────────────────────
  /// <summary>
  /// Timed fill start. Set together with <see cref="EndDate"/> and the client fills the bar
  /// on its own from empty (at StartDate) to full (at EndDate) — no server value updates.
  /// Overrides <see cref="Value"/>; server clock skew is corrected client-side. Treated as
  /// UTC when Kind is Unspecified. The fill is cancelled when the window is closed.
  /// </summary>
  public DateTime? StartDate { get; set; }
  /// <summary>Timed fill end — the moment the bar reaches full. See <see cref="StartDate"/>.</summary>
  public DateTime? EndDate { get; set; }

  // ── Overlay label ─────────────────────────────────────────────────────────
  /// <summary>
  /// Optional text label overlaid on the bar, rendered as a sibling above it.
  /// Use <see cref="Text.TextAlign"/> to control positioning inside the bar.
  /// All <see cref="ITextElement"/> style properties (color, font size, gradient, etc.) are supported.
  /// For health bars, <c>{healthValue}</c> and <c>{maxHealth}</c> tokens are replaced client-side.
  /// A <see cref="Timer"/> also works here — the label then ticks client-side (pairs well with
  /// the <see cref="StartDate"/>/<see cref="EndDate"/> timed fill).
  /// </summary>
  public Text Label { get; set; }
}
