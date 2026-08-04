using System.Collections.Generic;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Unified background descriptor supporting solid color, gradient, remote image, native
/// sprite, native game material, and frame-based animation — individually or in combination.
/// Use factory methods to create, then chain <c>With*</c> methods to layer additional fills.
/// </summary>
public readonly struct UIBackground {
    internal readonly UIColor? Color;
    internal readonly UIGradient? Gradient;
    internal readonly string ImageUrl;
    internal readonly string SpriteName;
    internal readonly ImageFit? Fit;
    internal readonly UIBackgroundAnimation? Animation;
    internal readonly UIBackgroundMaterial? Material;

    UIBackground(UIColor? color, UIGradient? gradient, string imageUrl, string spriteName,
        ImageFit? fit, UIBackgroundAnimation? animation, UIBackgroundMaterial? material) {
        Color = color; Gradient = gradient; ImageUrl = imageUrl; SpriteName = spriteName;
        Fit = fit; Animation = animation; Material = material;
    }

    internal bool HasValue => Color.HasValue || (Gradient.HasValue && Gradient.Value.HasValue)
                           || ImageUrl != null || SpriteName != null || Animation.HasValue
                           || Material.HasValue;

    // ── Static backgrounds ────────────────────────────────────────────────────

    /// <summary>Solid color background.</summary>
    public static UIBackground FromColor(UIColor color) => new(color, null, null, null, null, null, null);
    /// <summary>Gradient background.</summary>
    public static UIBackground FromGradient(UIGradient gradient) => new(null, gradient, null, null, null, null, null);
    /// <summary>Remote image background.</summary>
    public static UIBackground FromImage(string url, ImageFit fit = ImageFit.Stretch) => new(null, null, url, null, fit, null, null);
    /// <summary>Native game sprite background.</summary>
    public static UIBackground FromSprite(string name, ImageFit fit = ImageFit.Stretch) => new(null, null, null, name, fit, null, null);

    /// <summary>
    /// Background drawn with one of the game's own animated UI materials — the animation runs in
    /// the shader, so it costs nothing to send and nothing to tick.
    /// <para>
    /// Pair the material with the sprite the game feeds it, or it renders flat. Verified pairs:
    /// <c>SpellBookCircleOverlay</c> + <c>Spellbook_BG_Passives_Offset</c> (summoning circle),
    /// <c>JournalBackgroundSmoke</c> + <c>VBloodTracking_BG</c>, <c>GlowMat</c> + <c>Glow03</c>,
    /// <c>TreeBGSmoke_Bottom</c> + <c>VBloodTracking_BG_BottomSmoke</c>,
    /// <c>HUDAlert_Effect</c> + <c>WorldEvent_FrameEffect</c>,
    /// <c>ForgePattern_FlowMap_CircularIn</c> + <c>ForgeMenu_Pattern_Big_Active</c>,
    /// <c>DraculaTrail_Red_01</c> + <c>DraculaTrail_Red01</c>.
    /// </para>
    /// Chain <see cref="WithMaterialColor"/> to tint it. The layer draws above an image/sprite
    /// fill and below the element's content, and is not clipped by a scrolling viewport.
    /// </summary>
    public static UIBackground FromMaterial(string materialName, string spriteName = null) =>
        new(null, null, null, null, null, null, UIBackgroundMaterial.From(materialName, spriteName));

    // ── Animated backgrounds ──────────────────────────────────────────────────

    /// <summary>
    /// Animation background cycling through remote image URLs.
    /// Chain <c>WithAnim*</c> methods to configure trigger, loop, and release behavior.
    /// Use <see cref="WithColor"/> to provide a fallback color shown while frames are loading.
    /// </summary>
    public static UIBackground AnimatedFromUrls(string[] urls, float duration = 1f,
        AnimationLoopType loopType = AnimationLoopType.Loop, ImageFit fit = ImageFit.Stretch) =>
        new(null, null, null, null, fit, UIBackgroundAnimation.FromUrls(urls, duration, loopType), null);

    /// <summary>
    /// Animated background cycling through native game sprites.
    /// Chain <c>WithAnim*</c> methods to configure trigger, loop, and release behavior.
    /// </summary>
    public static UIBackground AnimatedFromSprites(string[] names, float duration = 1f,
        AnimationLoopType loopType = AnimationLoopType.Loop, ImageFit fit = ImageFit.Stretch) =>
        new(null, null, null, null, fit, UIBackgroundAnimation.FromSprites(names, duration, loopType), null);

    // ── Static fill modifiers ─────────────────────────────────────────────────

    /// <summary>Adds a solid-color fallback (shown behind or while the animation loads).</summary>
    public UIBackground WithColor(UIColor color) => new(color, Gradient, ImageUrl, SpriteName, Fit, Animation, Material);
    /// <summary>Layers a gradient on top of the current background.</summary>
    public UIBackground WithGradient(UIGradient gradient) => new(Color, gradient, ImageUrl, SpriteName, Fit, Animation, Material);
    /// <summary>Sets or overrides the image-fit mode.</summary>
    public UIBackground WithFit(ImageFit fit) => new(Color, Gradient, ImageUrl, SpriteName, fit, Animation, Material);

    // ── Material modifiers ────────────────────────────────────────────────────

    /// <summary>
    /// Layers one of the game's animated UI materials on top of the current background.
    /// See <see cref="FromMaterial"/> for the verified material/sprite pairs.
    /// </summary>
    public UIBackground WithMaterial(string materialName, string spriteName = null) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit, Animation,
            UIBackgroundMaterial.From(materialName, spriteName));

    /// <summary>
    /// Tints the material layer. The tint <b>multiplies</b> the rendered result, so it darkens and
    /// shifts but never brightens: a white/grey material (<c>GlowMat</c>,
    /// <c>JournalBackgroundSmoke</c>, <c>TreeBGSmoke_Bottom</c>) takes any color you give it,
    /// while an already-colored one (<c>HUDAlert_Effect</c>, <c>DraculaTrail_Red_01</c>) only
    /// moves within its own hue. Alpha fades the whole layer. No-op without a material.
    /// </summary>
    public UIBackground WithMaterialColor(UIColor color) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit, Animation,
            Material.HasValue ? Material.Value.WithColor(color) : null);

    // ── Animation modifiers ───────────────────────────────────────────────────

    /// <summary>Sets the trigger(s) that start this animation.</summary>
    public UIBackground WithAnimTrigger(AnimationTrigger trigger) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithTrigger(trigger) : null, Material);

    /// <summary>Sets how the animation cycles — Loop (wrap) or Bounce (ping-pong).</summary>
    public UIBackground WithAnimLoopType(AnimationLoopType loopType) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithLoopType(loopType) : null, Material);

    /// <summary>Sets the number of full cycles to play. 0 = infinite.</summary>
    public UIBackground WithAnimLoopCount(int loopCount) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithLoopCount(loopCount) : null, Material);

    /// <summary>Sets the behavior when the interaction trigger is released.</summary>
    public UIBackground WithAnimReleaseMode(AnimationReleaseMode releaseMode) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithReleaseMode(releaseMode) : null, Material);

    /// <summary>Sets whether the animation starts playing (relevant for Manual trigger).</summary>
    public UIBackground WithAnimPlaying(bool playing) =>
        new(Color, Gradient, ImageUrl, SpriteName, Fit,
            Animation.HasValue ? Animation.Value.WithPlaying(playing) : null, Material);

    // ── Serialization ─────────────────────────────────────────────────────────

    /// <summary>Writes the relevant data keys using default short-token prefix "b" (= Bg).</summary>
    internal void Apply(Dictionary<string, string> data) => Apply(data, "b");

    /// <summary>
    /// Writes the relevant data keys using a short-token prefix (e.g. "b"=Bg, "h"=HoverBg,
    /// "q"=PressedBg, "r"=Bar, "d"=HeaderBg, "j"=ContentBg, "f"=FocusBg).
    /// Suffixes: cl=Color, gr=Gradient, im=Image, sp=Sprite, if=ImageFit,
    /// mt=Material, ms=MaterialSprite, mc=MaterialColor.
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
        if (Material.HasValue)
            Material.Value.Apply(data, prefix);
    }

    // ── Implicit conversions ──────────────────────────────────────────────────

    /// <summary>Implicitly wraps a <see cref="UIColor"/> in a solid-color background.</summary>
    public static implicit operator UIBackground(UIColor color) => FromColor(color);
    /// <summary>Implicitly wraps a <see cref="UIGradient"/> in a gradient background.</summary>
    public static implicit operator UIBackground(UIGradient gradient) => FromGradient(gradient);
}
