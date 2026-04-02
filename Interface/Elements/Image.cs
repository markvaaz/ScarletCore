using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>An image element loaded from a URL.</summary>
public class Image : UIElement {
  /// <summary>HTTP/HTTPS URL of the image to load.</summary>
  public string Src { get; set; }
  /// <summary>How the image fills its bounds. Default: Stretch.</summary>
  public ImageFit Fit { get; set; }
}
