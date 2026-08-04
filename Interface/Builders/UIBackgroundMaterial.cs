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
  internal readonly UIColor? Color;

  UIBackgroundMaterial(string name, string spriteName, UIColor? color) {
    Name = name; SpriteName = spriteName; Color = color;
  }

  internal static UIBackgroundMaterial From(string name, string spriteName) =>
      new(name, spriteName, null);

  /// <summary>Sets the sprite that feeds the shader's main texture.</summary>
  public UIBackgroundMaterial WithSprite(string spriteName) => new(Name, spriteName, Color);

  /// <summary>
  /// Tints the material layer. The tint <b>multiplies</b> the rendered result, so it can darken
  /// and shift a fill but never brighten it: a white/grey source (<c>GlowMat</c>,
  /// <c>JournalBackgroundSmoke</c>, <c>TreeBGSmoke_Bottom</c>) takes any color, while an
  /// already-colored one (<c>HUDAlert_Effect</c>, <c>DraculaTrail_Red_01</c>) only moves within
  /// its own hue. Alpha fades the whole layer.
  /// </summary>
  public UIBackgroundMaterial WithColor(UIColor color) => new(Name, SpriteName, color);

  /// <summary>
  /// Writes material keys using a short-token prefix (matches UIBackground.Apply prefix).
  /// Suffixes: mt=Material, ms=MaterialSprite, mc=MaterialColor.
  /// </summary>
  internal void Apply(Dictionary<string, string> data, string prefix) {
    data[$"{prefix}mt"] = Name;
    if (SpriteName != null) data[$"{prefix}ms"] = SpriteName;
    if (Color.HasValue) data[$"{prefix}mc"] = Color.Value;
  }
}
