namespace ScarletCore.Interface.Builders;

/// <summary>Controls how an image fills the area defined by its width and height.</summary>
public enum ImageFit {
  /// <summary>Stretches to fill the entire area. Aspect ratio is not preserved.</summary>
  Stretch,
  /// <summary>Scales uniformly to fit entirely within the area (letterbox). Aspect ratio preserved.</summary>
  Fit,
  /// <summary>Scales uniformly to cover the entire area, cropping the excess (cover). Aspect ratio preserved.</summary>
  Fill,
  /// <summary>
  /// 9-slice: the four corners keep their pixel size and only the edges and centre stretch, so one
  /// small source dresses an area of any size without smearing its frame. Where to cut the source is
  /// worked out automatically — a third of the shorter side — unless the caller overrides it.
  /// <para>
  /// Read by windows (<c>Window.Background</c>) and by nameplate backgrounds
  /// (<see cref="InterfaceManager.NameplateBackground"/>). Art authored large draws large corners at
  /// native size: scale them down with <c>sliceScale</c> where the API offers it.
  /// </para>
  /// </summary>
  Slice,
}
