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
  /// Fill portion background (color, gradient, image, or sprite).
  /// Separate from <see cref="UIElement.Background"/> which controls the track.
  /// </summary>
  public UIBackground? BarFill { get; set; }
}
