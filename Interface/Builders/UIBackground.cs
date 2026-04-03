using System.Collections.Generic;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Unified background descriptor supporting solid color, gradient, remote image, native
/// sprite, and frame-based animation — individually or in combination. Use factory methods
/// to create, then chain <c>With*</c> methods to layer additional fills.
/// </summary>
public readonly struct UIBackground {
    internal readonly UIColor? Color;
    internal readonly UIGradient? Gradient;
    internal readonly string ImageUrl;
    internal readonly string SpriteName;
    internal readonly ImageFit? Fit;
    internal readonly UIBackgroundAnimation? Animation;

    UIBackground(UIColor? color, UIGradient? gradient, string imageUrl, string spriteName,
        ImageFit? fit, UIBackgroundAnimation? animation) {
        Color = color; Gradient = gradient; ImageUrl = imageUrl; SpriteName = spriteName;
        Fit = fit; Animation = animation;
    }

    internal bool HasValue => Color.HasValue || (Gradient.HasValue && Gradient.Value.HasValue)
                           || ImageUrl != null || SpriteName != null || Animation.HasValue;

    // ── Static backgrounds ────────────────────────────────────────────────────

    /// <summary>Solid color background.</summary>
    public static UIBackground FromColor(UIColor color) => new(color, null, null, null, null, null);
    /// <summary>Gradient background.</summary>
    public static UIBackground FromGradient(UIGradient gradient) => new(null, gradient, null, null, null, null);
    /// <summary>Remote image background.</summary>
    public static UIBackground FromImage(string url, ImageFit fit = ImageFit.Stretch) => new(null, null, url, null, fit, null);
    /// <summary>Native game sprite background.</summary>
    public static UIBackground FromSprite(string name, ImageFit fit = ImageFit.Stretch) => new(null, null, null, name, fit, null);

    // ── Animated backgrounds ──────────────────────────────────────────────────

    /// <summary>
    /// Animation background cycling through remote image URLs.
    /// Chain <c>WithAnim*</c> methods to configure trigger, loop, and release behavior.
    /// Use <see cref="WithColor"/> to provide a fallback color shown while frames are loading.
    /// </summary>
    public static UIBackground AnimatedFromUrls(string[] urls, float duration = 1f,
        AnimationLoopType loopType = AnimationLoopType.Loop, ImageFit fit = ImageFit.Stretch) =>
        new(null, null, null, null, fit, UIBackgroundAnimation.FromUrls(urls, duration, loopType));

    /// <summary>
    /// Animated background cycling through native game sprites.
    /// Chain <c>WithAnim*</c> methods to configure trigger, loop, and release behavior.
    /// </summary>
    public static UIBackground AnimatedFromSprites(string[] names, float duration = 1f,
        AnimationLoopType loopType = AnimationLoopType.Loop, ImageFit fit = ImageFit.Stretch) =>
        new(null, null, null, null, fit, UIBackgroundAnimation.FromSprites(names, duration, loopType));

    // ── Static fill modifiers ─────────────────────────────────────────────────

    /// <summary>Adds a solid-color fallback (shown behind or while the animation loads).</summary>
    public UIBackground WithColor(UIColor color) => new(color, Gradient, ImageUrl, SpriteName, Fit, Animation);
    /// <summary>Layers a gradient on top of the current background.</summary>
    public UIBackground WithGradient(UIGradient gradient) => new(Color, gradient, ImageUrl, SpriteName, Fit, Animation);
    /// <summary>Sets or overrides the image-fit mode.</summary>
    public UIBackground WithFit(ImageFit fit) => new(Color, Gradient, ImageUrl, SpriteName, fit, Animation);

    // ── Animation modifiers ───────────────────────────────────────────────────

    /// <summary>Sets the trigger(s) that start this animation.</summary>
    public UIBackground WithAnimTrigger(AnimationTrigger trigger) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithTrigger(trigger) : null);

    /// <summary>Sets how the animation cycles — Loop (wrap) or Bounce (ping-pong).</summary>
    public UIBackground WithAnimLoopType(AnimationLoopType loopType) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithLoopType(loopType) : null);

    /// <summary>Sets the number of full cycles to play. 0 = infinite.</summary>
    public UIBackground WithAnimLoopCount(int loopCount) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithLoopCount(loopCount) : null);

    /// <summary>Sets the behavior when the interaction trigger is released.</summary>
    public UIBackground WithAnimReleaseMode(AnimationReleaseMode releaseMode) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithReleaseMode(releaseMode) : null);

    /// <summary>Sets whether the animation starts playing (relevant for Manual trigger).</summary>
    public UIBackground WithAnimPlaying(bool playing) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithPlaying(playing) : null);

    // ── Serialization ─────────────────────────────────────────────────────────

    /// <summary>Writes the relevant data keys using default short-token prefix "b" (= Bg).</summary>
    internal void Apply(Dictionary<string, string> data) => Apply(data, "b");

    /// <summary>
    /// Writes the relevant data keys using a short-token prefix (e.g. "b"=Bg, "h"=HoverBg,
    /// "q"=PressedBg, "r"=Bar, "d"=HeaderBg, "j"=ContentBg, "f"=FocusBg).
    /// Suffixes: cl=Color, gr=Gradient, im=Image, sp=Sprite, if=ImageFit.
    /// </summary>
    internal void Apply(Dictionary<string, string> data, string prefix) {
        if (Color.HasValue) data[$"{prefix}cl"] = Color.Value;
        if (Gradient.HasValue && Gradient.Value.HasValue) data[$"{prefix}gr"] = Gradient.Value.Raw;
        if (ImageUrl != null) data[$"{prefix}im"] = ImageUrl;
        if (SpriteName != null) data[$"{prefix}sp"] = SpriteName;
        if (Fit.HasValue && (Fit.Value != ImageFit.Stretch) &&
            (ImageUrl != null || SpriteName != null || Animation.HasValue))
            data[$"{prefix}if"] = Fit.Value.ToString();
        if (Animation.HasValue)
            Animation.Value.Apply(data, prefix);
    }

    // ── Implicit conversions ──────────────────────────────────────────────────

    /// <summary>Implicitly wraps a <see cref="UIColor"/> in a solid-color background.</summary>
    public static implicit operator UIBackground(UIColor color) => FromColor(color);
    /// <summary>Implicitly wraps a <see cref="UIGradient"/> in a gradient background.</summary>
    public static implicit operator UIBackground(UIGradient gradient) => FromGradient(gradient);
}