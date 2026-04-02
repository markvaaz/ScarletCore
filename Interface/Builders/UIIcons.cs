namespace ScarletCore.Interface.Builders;

/// <summary>Utility for embedding item icons inside text strings sent to ScarletInterface.</summary>
public static class UIIcons {
  /// <summary>
  /// Returns an inline icon token for the item identified by <paramref name="guidHash"/>.
  /// Embed this inside any <c>text</c> parameter:
  /// <code>$"Craft: 5x {UIIcons.Icon(-1234567890)}"</code>
  /// The client resolves the GUID to the item's sprite via <c>ManagedItemData.Icon</c>
  /// and renders it as an inline <c>Image</c> component.
  /// </summary>
  /// <param name="guidHash">The integer hash of the <c>PrefabGUID</c> (i.e. the raw int32 GUID value).</param>
  public static string Icon(int guidHash) => $"{{icon:{guidHash}}}";

  static readonly System.Globalization.CultureInfo _ic = System.Globalization.CultureInfo.InvariantCulture;

  /// <summary>
  /// Returns an inline icon token with an explicit pixel size.
  /// </summary>
  /// <param name="guidHash">PrefabGUID hash.</param>
  /// <param name="size">Icon size in pixels (width = height). 0 = inherit from font size.</param>
  public static string Icon(int guidHash, float size) =>
    size <= 0f
      ? $"{{icon:{guidHash}}}"
      : $"{{icon:{guidHash}:{size.ToString(_ic)}}}";

  /// <summary>
  /// Returns an inline icon token with explicit pixel size and horizontal spacing.
  /// </summary>
  /// <param name="guidHash">PrefabGUID hash.</param>
  /// <param name="size">Icon size in pixels. 0 = inherit from font size.</param>
  /// <param name="spacing">Gap in pixels added on each side of the icon. 0 = default (3 px).</param>
  public static string Icon(int guidHash, float size, float spacing) =>
    $"{{icon:{guidHash}:{size.ToString(_ic)}:{spacing.ToString(_ic)}}}";
}
