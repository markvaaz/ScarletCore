using System.Collections.Generic;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Unified background descriptor supporting solid color, gradient, remote image, and
/// native sprite — individually or in combination. Use factory methods to create, then
/// chain <c>With*</c> methods to layer additional fills.
/// </summary>
public readonly struct UIBackground {
  internal readonly UIColor? Color;
  internal readonly UIGradient? Gradient;
  internal readonly string ImageUrl;
  internal readonly string SpriteName;
  internal readonly ImageFit? Fit;

  UIBackground(UIColor? color, UIGradient? gradient, string imageUrl, string spriteName, ImageFit? fit) {
    Color = color; Gradient = gradient; ImageUrl = imageUrl; SpriteName = spriteName; Fit = fit;
  }

  internal bool HasValue => Color.HasValue || (Gradient.HasValue && Gradient.Value.HasValue)
                         || ImageUrl != null || SpriteName != null;

  /// <summary>Solid color background.</summary>
  public static UIBackground FromColor(UIColor color) => new(color, null, null, null, null);
  /// <summary>Gradient background.</summary>
  public static UIBackground FromGradient(UIGradient gradient) => new(null, gradient, null, null, null);
  /// <summary>Remote image background.</summary>
  public static UIBackground FromImage(string url, ImageFit fit = ImageFit.Stretch) => new(null, null, url, null, fit);
  /// <summary>Native game sprite background.</summary>
  public static UIBackground FromSprite(string name, ImageFit fit = ImageFit.Stretch) => new(null, null, null, name, fit);

  /// <summary>Adds a solid-color fallback behind the current background.</summary>
  public UIBackground WithColor(UIColor color) => new(color, Gradient, ImageUrl, SpriteName, Fit);
  /// <summary>Layers a gradient on top of the current background.</summary>
  public UIBackground WithGradient(UIGradient gradient) => new(Color, gradient, ImageUrl, SpriteName, Fit);
  /// <summary>Sets or overrides the image-fit mode.</summary>
  public UIBackground WithFit(ImageFit fit) => new(Color, Gradient, ImageUrl, SpriteName, fit);

  /// <summary>Writes the relevant data keys into the packet dictionary using default "Bg" prefix.</summary>
  internal void Apply(Dictionary<string, string> data) => Apply(data, "Bg");

  /// <summary>Writes the relevant data keys into the packet dictionary using a custom prefix.</summary>
  internal void Apply(Dictionary<string, string> data, string prefix) {
    if (Color.HasValue) data[$"{prefix}Color"] = Color.Value;
    if (Gradient.HasValue && Gradient.Value.HasValue) data[$"{prefix}Gradient"] = Gradient.Value.Raw;
    if (ImageUrl != null) data[$"{prefix}Image"] = ImageUrl;
    if (SpriteName != null) data[$"{prefix}Sprite"] = SpriteName;
    if ((ImageUrl != null || SpriteName != null) && Fit.HasValue)
      data[$"{prefix}ImageFit"] = Fit.Value.ToString();
  }

  /// <summary>Implicitly wraps a <see cref="UIColor"/> in a solid-color background.</summary>
  public static implicit operator UIBackground(UIColor color) => FromColor(color);
  /// <summary>Implicitly wraps a <see cref="UIGradient"/> in a gradient background.</summary>
  public static implicit operator UIBackground(UIGradient gradient) => FromGradient(gradient);
}