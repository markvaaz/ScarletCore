namespace ScarletCore.Interface.Builders;

/// <summary>
/// Ready-made values for <see cref="UIBackground.WithMaterialTint"/>.
/// <para>
/// These are <b>not</b> ordinary colours. The tint is written to the shader's <c>_Color</c>,
/// which multiplies its output per channel and accepts values above 1 — that is what lets a
/// colourless material take on a hue instead of only being darkened. The game's own flowmap
/// materials are neutral grey: the passives panel looks purple because of its sprite, not its
/// material, so tinting is the only way to get that look on your own art.
/// </para>
/// <para>
/// The presets below were calibrated by sampling the rendered panels and solving for the
/// multiplier, not picked by eye. <see cref="PassivesPurple"/> lands within ~3% of the native
/// passives panel's channel ratios (R/B 0.639 vs 0.619, G/B 0.562 vs 0.528) over dark art.
/// </para>
/// </summary>
public static class UIMaterialTint {
  /// <summary>The purple of the game's passives panel, at ~1.2x its brightness.</summary>
  public static UIColor PassivesPurple => UIColor.RGBA(1.494f, 1.373f, 2.116f, 1f);

  /// <summary>The same purple matched to the native panel's brightness as well as its hue.</summary>
  public static UIColor PassivesPurpleDim => UIColor.RGBA(1.255f, 1.154f, 1.778f, 1f);

  /// <summary>Neutral, but brighter — lifts a faint effect without shifting its hue.</summary>
  public static UIColor Brighten2x => UIColor.RGBA(2f, 2f, 2f, 1f);

  /// <summary>Neutral, three times brighter.</summary>
  public static UIColor Brighten3x => UIColor.RGBA(3f, 3f, 3f, 1f);

  /// <summary>Builds a tint from a hue plus a brightness multiplier above 1.</summary>
  public static UIColor Of(float r, float g, float b, float gain = 1f) =>
      UIColor.RGBA(r * gain, g * gain, b * gain, 1f);
}
