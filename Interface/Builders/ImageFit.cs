namespace ScarletCore.Interface.Builders;

/// <summary>Controls how an image fills the area defined by its width and height.</summary>
public enum ImageFit {
  /// <summary>Stretches to fill the entire area. Aspect ratio is not preserved.</summary>
  Stretch,
  /// <summary>Scales uniformly to fit entirely within the area (letterbox). Aspect ratio preserved.</summary>
  Fit,
  /// <summary>Scales uniformly to cover the entire area, cropping the excess (cover). Aspect ratio preserved.</summary>
  Fill,
}
