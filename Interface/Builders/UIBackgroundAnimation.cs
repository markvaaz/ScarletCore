using System.Collections.Generic;
using System.Globalization;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Describes frame-based animation parameters for a <see cref="UIBackground"/>.
/// Use <see cref="UIBackground.AnimatedFromUrls"/> or <see cref="UIBackground.AnimatedFromSprites"/>
/// to create an animated background, then chain <c>WithAnim*</c> methods to configure it.
/// </summary>
public readonly struct UIBackgroundAnimation {
  static readonly CultureInfo IC = CultureInfo.InvariantCulture;

  internal readonly string[] Frames;
  internal readonly bool IsUrl;
  internal readonly float Duration;
  internal readonly AnimationTrigger Trigger;
  internal readonly AnimationLoopType LoopType;
  internal readonly int LoopCount;
  internal readonly AnimationReleaseMode ReleaseMode;
  internal readonly bool Playing;

  UIBackgroundAnimation(string[] frames, bool isUrl, float duration, AnimationTrigger trigger,
      AnimationLoopType loopType, int loopCount, AnimationReleaseMode releaseMode, bool playing) {
    Frames = frames; IsUrl = isUrl; Duration = duration; Trigger = trigger;
    LoopType = loopType; LoopCount = loopCount; ReleaseMode = releaseMode; Playing = playing;
  }

  internal static UIBackgroundAnimation FromUrls(string[] urls, float duration,
      AnimationLoopType loopType = AnimationLoopType.Loop) =>
      new(urls, true, duration, AnimationTrigger.Always, loopType, 0, AnimationReleaseMode.Pause, true);

  internal static UIBackgroundAnimation FromSprites(string[] names, float duration,
      AnimationLoopType loopType = AnimationLoopType.Loop) =>
      new(names, false, duration, AnimationTrigger.Always, loopType, 0, AnimationReleaseMode.Pause, true);

  /// <summary>Sets the trigger(s) that start this animation.</summary>
  public UIBackgroundAnimation WithTrigger(AnimationTrigger trigger) =>
      new(Frames, IsUrl, Duration, trigger, LoopType, LoopCount, ReleaseMode, Playing);

  /// <summary>Sets how the animation cycles — Loop (wrap) or Bounce (ping-pong).</summary>
  public UIBackgroundAnimation WithLoopType(AnimationLoopType loopType) =>
      new(Frames, IsUrl, Duration, Trigger, loopType, LoopCount, ReleaseMode, Playing);

  /// <summary>Sets the number of full cycles to play. 0 = infinite.</summary>
  public UIBackgroundAnimation WithLoopCount(int loopCount) =>
      new(Frames, IsUrl, Duration, Trigger, LoopType, loopCount, ReleaseMode, Playing);

  /// <summary>Sets the behavior when the interaction trigger is released.</summary>
  public UIBackgroundAnimation WithReleaseMode(AnimationReleaseMode releaseMode) =>
      new(Frames, IsUrl, Duration, Trigger, LoopType, LoopCount, releaseMode, Playing);

  /// <summary>Sets whether the animation starts in a playing state (relevant for Manual trigger).</summary>
  public UIBackgroundAnimation WithPlaying(bool playing) =>
      new(Frames, IsUrl, Duration, Trigger, LoopType, LoopCount, ReleaseMode, playing);

  /// <summary>Writes animation keys into the packet dictionary under the given prefix.</summary>
  internal void Apply(Dictionary<string, string> data, string prefix) {
    data[$"{prefix}Frames"] = string.Join("\n", Frames);
    data[$"{prefix}FrameType"] = IsUrl ? "Url" : "Sprite";
    data[$"{prefix}AnimTrigger"] = ((int)Trigger).ToString(IC);
    data[$"{prefix}AnimDuration"] = Duration.ToString(IC);
    if (LoopType != AnimationLoopType.Loop)
      data[$"{prefix}AnimLoopType"] = LoopType.ToString();
    if (LoopCount != 0)
      data[$"{prefix}AnimLoopCount"] = LoopCount.ToString(IC);
    if (ReleaseMode != AnimationReleaseMode.Pause)
      data[$"{prefix}AnimReleaseMode"] = ReleaseMode.ToString();
    if (!Playing)
      data[$"{prefix}AnimPlaying"] = "false";
  }
}
