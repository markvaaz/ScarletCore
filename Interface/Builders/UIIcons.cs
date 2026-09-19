namespace ScarletCore.Interface.Builders;

/// <summary>Utility for embedding item icons inside text strings sent to ScarletInterface.</summary>
public static class UIIcons {
  /// <summary>
  /// Returns an inline icon token for the item identified by <paramref name="guidHash"/>.
  /// Embed this inside any <c>text</c> parameter:
  /// <code>$"Craft: 5x {UIIcons.Icon(-1234567890)}"</code>
  /// The client resolves the GUID to the sprite via <c>ManagedItemData.Icon</c>
  /// (falling back to <c>ManagedAbilityGroupData.Icon</c> for ability GUIDs)
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

  /// <summary>
  /// Inline item icon that also opens the item's native tooltip on hover (<c>{icon:G:size:spacing:t}</c>).
  /// Opt-in per icon: a plain <see cref="Icon(int)"/> is never a raycast target, so it costs nothing.
  /// Inside a <c>Button</c> the click still reaches the button (the glyph forwards Down/Up/Click).
  /// Shows the BASE item — a glyph carries no entity data; use <c>ItemViewer</c> for level/durability/mods.
  /// </summary>
  /// <param name="guidHash">PrefabGUID hash.</param>
  /// <param name="size">Icon size in pixels. 0 = inherit from font size.</param>
  /// <param name="spacing">Gap on each side. -1 = default (3 px).</param>
  /// <param name="tooltip">Whether hovering the icon opens the item's native tooltip.</param>
  public static string Icon(int guidHash, float size, float spacing, bool tooltip) =>
    Icon(guidHash, size, spacing, tooltip, null);

  /// <summary>
  /// Inline item icon with its OWN border (and optional tooltip): every icon in a label can carry a
  /// different one. <paramref name="border"/> is serialized into the token
  /// (<c>{icon:G:size:spacing:t;bc=…;bw=…;gc=…;gw=…;gf=…;ca}</c>); with <see cref="Border.ContentAware"/>
  /// it hugs the glyph's alpha outline, otherwise it is a box the size of the glyph.
  /// </summary>
  public static string Icon(int guidHash, float size, float spacing, bool tooltip, Border? border) {
    if (!tooltip && !border.HasValue) return spacing < 0f ? Icon(guidHash, size) : Icon(guidHash, size, spacing);
    string sz = size > 0f ? size.ToString(_ic) : "";
    string sp = spacing >= 0f ? spacing.ToString(_ic) : "";
    var opts = new System.Text.StringBuilder();
    if (tooltip) opts.Append("t");
    if (border.HasValue) {
      var b = border.Value;
      void Add(string k, string v) { if (opts.Length > 0) opts.Append(';'); opts.Append(k).Append('=').Append(v); }
      Add("bc", b.Color); Add("bw", b.Width.ToString(_ic));
      if (b.Radius > 0f) Add("br", b.Radius.ToString(_ic));
      if (b.GlowColor.HasValue && b.GlowWidth > 0f) {
        Add("gc", b.GlowColor.Value); Add("gw", b.GlowWidth.ToString(_ic));
        if (b.GlowFalloff != 2f) Add("gf", b.GlowFalloff.ToString(_ic));
      }
      if (b.ContentAware) { if (opts.Length > 0) opts.Append(';'); opts.Append("ca"); }
    }
    return $"{{icon:{guidHash}:{sz}:{sp}:{opts}}}";
  }

  /// <summary>Inline item icon with its own border, no tooltip.</summary>
  public static string Icon(int guidHash, float size, Border border, float spacing = -1f) =>
    Icon(guidHash, size, spacing, false, border);

  /// <summary>
  /// Returns an inline SVG icon token from raw SVG path data.
  /// Embed this inside any text or icon field:
  /// <code>UIIcons.Svg("M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z")</code>
  /// Multiple paths with per-path colors can be separated with <c>|</c>:
  /// <code>UIIcons.Svg("M0 0H10V10@#FF5500|M5 0V10@#00AAFF")</code>
  /// An explicit size in pixels can be appended after a <c>:</c>:
  /// <code>UIIcons.Svg("M0 0H10V10:20")</code>
  /// </summary>
  /// <param name="pathData">SVG path data string (one or more <c>d</c> attribute values, optionally with <c>@#RRGGBB</c> color and <c>:size</c> suffix).</param>
  public static string Svg(string pathData) => $"[svg:{pathData}]";

  /// <summary>
  /// Returns an inline SVG icon token with an explicit display size in pixels.
  /// </summary>
  /// <param name="pathData">SVG path data string.</param>
  /// <param name="size">Icon size in pixels. 0 = inherit from font size.</param>
  public static string Svg(string pathData, float size) =>
    size <= 0f ? $"[svg:{pathData}]" : $"[svg:{pathData}:{size.ToString(_ic)}]";
}
