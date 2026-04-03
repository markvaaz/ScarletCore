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
}
