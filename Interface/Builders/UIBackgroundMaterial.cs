using System.Collections.Generic;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Describes a native game material used as a <see cref="UIBackground"/> layer — the animated
/// UI materials the game itself renders with, e.g. <c>SpellBookCircleOverlay</c> (the summoning
/// circle behind the passives panel), <c>JournalBackgroundSmoke</c> or <c>GlowMat</c>.
/// The animation lives in the shader, so there are no frames to send and no cost per element.
/// <para>
/// Most of these shaders distort the sprite fed to them, so pair the material with the sprite
/// the game uses it with (see <see cref="UIBackground.FromMaterial"/>). Without a sprite they
/// render flat.
/// </para>
/// Use <see cref="UIBackground.FromMaterial"/> or <see cref="UIBackground.WithMaterial"/> to
/// create one, then <see cref="UIBackground.WithMaterialColor"/> to tint it.
/// </summary>
public readonly struct UIBackgroundMaterial {
  internal readonly string Name;
  internal readonly string SpriteName;
  internal readonly string ImageUrl;
  internal readonly UIColor? Color;
  internal readonly UIColor? Tint;
  internal readonly string Parameters;

  UIBackgroundMaterial(string name, string spriteName, string imageUrl, UIColor? color,
      UIColor? tint, string parameters) {
    Name = name; SpriteName = spriteName; ImageUrl = imageUrl; Color = color;
    Tint = tint; Parameters = parameters;
  }

  internal static UIBackgroundMaterial From(string name, string spriteName) =>
      new(name, spriteName, null, null, null, null);

  internal static UIBackgroundMaterial FromUrl(string name, string imageUrl) =>
      new(name, null, imageUrl, null, null, null);

  /// <summary>Sets the native game sprite that feeds the shader's main texture.</summary>
  public UIBackgroundMaterial WithSprite(string spriteName) =>
      new(Name, spriteName, ImageUrl, Color, Tint, Parameters);

  /// <summary>Sets a remote image as the art the shader works on. The sprite wins if both are set.</summary>
  public UIBackgroundMaterial WithImage(string imageUrl) =>
      new(Name, SpriteName, imageUrl, Color, Tint, Parameters);

  /// <summary>
  /// Writes the shader's <c>_Color</c>. Unlike <see cref="WithColor"/> this multiplies per channel
  /// and accepts values above 1, so it can brighten and hue-shift instead of only darkening.
  /// See <see cref="UIMaterialTint"/> for measured presets.
  /// </summary>
  public UIBackgroundMaterial WithTint(UIColor tint) =>
      new(Name, SpriteName, ImageUrl, Color, tint, Parameters);

  /// <summary>
  /// Overrides one named float property of the shader, e.g. <c>_MotionStrength</c> (how far the
  /// flowmap drags the art) or <c>_PhaseLength</c> (seconds per cycle — bigger is slower).
  /// Chainable; a name the material does not declare is skipped with a client-side warning.
  /// </summary>
  public UIBackgroundMaterial WithParam(string property, float value) {
    var entry = $"{property}={value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    var merged = string.IsNullOrEmpty(Parameters) ? entry : $"{Parameters}|{entry}";
    return new(Name, SpriteName, ImageUrl, Color, Tint, merged);
  }

  /// <summary>
  /// Tints the material layer. The tint <b>multiplies</b> the rendered result, so it can darken
  /// and shift a fill but never brighten it: a white/grey source (<c>GlowMat</c>,
  /// <c>JournalBackgroundSmoke</c>, <c>TreeBGSmoke_Bottom</c>) takes any color, while an
  /// already-colored one (<c>HUDAlert_Effect</c>, <c>DraculaTrail_Red_01</c>) only moves within
  /// its own hue. Alpha fades the whole layer.
  /// </summary>
  public UIBackgroundMaterial WithColor(UIColor color) =>
      new(Name, SpriteName, ImageUrl, color, Tint, Parameters);

  /// <summary>
  /// Writes material keys using a short-token prefix (matches UIBackground.Apply prefix).
  /// Suffixes: mt=Material, ms=MaterialSprite, mi=MaterialImage, mc=MaterialColor,
  /// mn=MaterialTint, mp=MaterialParams.
  /// </summary>
  internal void Apply(Dictionary<string, string> data, string prefix) {
    data[$"{prefix}mt"] = Name;
    if (SpriteName != null) data[$"{prefix}ms"] = SpriteName;
    if (ImageUrl != null) data[$"{prefix}mi"] = ImageUrl;
    if (Color.HasValue) data[$"{prefix}mc"] = Color.Value;
    if (Tint.HasValue) data[$"{prefix}mn"] = Tint.Value;
    if (Parameters != null) data[$"{prefix}mp"] = Parameters;
  }
}
