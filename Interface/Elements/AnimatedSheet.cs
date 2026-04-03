using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// An element that cycles through a sequence of image frames (sprites or URLs),
/// producing a sprite-sheet–style animation.
/// Frames, timing, triggers, and loop behavior are all configurable.
/// </summary>
public class AnimatedSheet : UIElement {
  /// <summary>HTTP/HTTPS URLs of each animation frame. Takes priority over <see cref="FrameSprites"/> if both are set.</summary>
  public string[] FrameUrls { get; set; }
  /// <summary>Native game sprite names for each animation frame.</summary>
  public string[] FrameSprites { get; set; }
  /// <summary>Duration in seconds of one full animation cycle. Default: 1s.</summary>
  public float Duration { get; set; } = 1f;
  /// <summary>Trigger(s) that start the animation. Multiple values can be combined.</summary>
  public AnimationTrigger Trigger { get; set; } = AnimationTrigger.Always;
  /// <summary>How the animation cycles through frames. Default: Loop (wrap).</summary>
  public AnimationLoopType LoopType { get; set; } = AnimationLoopType.Loop;
  /// <summary>Number of full cycles to play. 0 = infinite. For Click trigger, 0 is treated as 1.</summary>
  public int LoopCount { get; set; }
  /// <summary>Behavior when the interaction trigger is released. Default: Pause.</summary>
  public AnimationReleaseMode ReleaseMode { get; set; } = AnimationReleaseMode.Pause;
  /// <summary>Whether the animation starts in a playing state. Relevant for Manual trigger.</summary>
  public bool Playing { get; set; } = true;
  /// <summary>How frames fill the element bounds.</summary>
  public ImageFit Fit { get; set; }
}
