using System.Collections.Generic;
using System.Globalization;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Fluent helper for adding elements to a specific Row inside a <see cref="WindowBuilder"/>.
/// Obtained via <see cref="WindowBuilder.AddRow"/>.
/// </summary>
public sealed class RowBuilder {
  readonly WindowBuilder _window;
  readonly string _rowId;

  internal RowBuilder(WindowBuilder window, string rowId) {
    _window = window;
    _rowId = rowId;
  }

  // ─── Elements ────────────────────────────────────────────────────────────

  /// <summary>Adds a text element to this row.</summary>
  /// <param name="text">The text content to display. Supports inline icons via <see cref="UIIcons"/>.</param>
  /// <param name="width">Element width (pixels or percentage string). Default: auto.</param>
  /// <param name="height">Element height (pixels or percentage string). Default: auto.</param>
  /// <param name="color">Text color. Default: white.</param>
  /// <param name="backgroundColor">Background color behind the text. Default: transparent.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit from window default.</param>
  /// <param name="border">Optional border around the text element.</param>
  /// <param name="padding">Inner spacing between the border and the text content.</param>
  /// <param name="margin">Outer spacing around the text element.</param>
  /// <param name="textAlign">Horizontal text alignment. Default: Left.</param>
  /// <param name="wrap">Wrap text onto multiple lines when it exceeds the element width. Default: false.</param>
  /// <param name="backgroundGradient">Gradient applied to the background. Overrides <paramref name="backgroundColor"/> when set.</param>
  public RowBuilder AddText(string text,
      Position width = default, Position height = default,
      UIColor? color = null, UIColor? backgroundColor = null,
      float fontSize = 0,
      Border? border = null,
      Spacing? padding = null,
      Spacing? margin = null,
      TextAlignment textAlign = TextAlignment.Left,
      bool wrap = false,
      UIGradient backgroundGradient = default) {
    var data = StartData();
    data["Text"] = text;
    data["ElemId"] = _window.NextElemId(_rowId);
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (color.HasValue) data["Color"] = color.Value;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (fontSize > 0) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    ApplySpacing(data, "Margin", margin);
    if (textAlign != TextAlignment.Left) data["TextAlign"] = textAlign.ToString();
    if (wrap) data["Wrap"] = "true";
    if (backgroundGradient.HasValue) data["BgGradient"] = backgroundGradient.Raw;
    return Enqueue("AddText", data);
  }

  /// <summary>Adds a button to this row. Clicking sends <paramref name="cmd"/> to the server.</summary>
  /// <param name="text">Button label text.</param>
  /// <param name="cmd">Chat command sent to the server when the button is clicked.</param>
  /// <param name="width">Button width (pixels or percentage string). Default: auto.</param>
  /// <param name="height">Button height (pixels or percentage string). Default: auto.</param>
  /// <param name="backgroundColor">Button background color. Default: theme default.</param>
  /// <param name="textColor">Button label color. Default: white.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit from window default.</param>
  /// <param name="border">Optional border around the button.</param>
  /// <param name="padding">Inner spacing between the border and the button label.</param>
  /// <param name="margin">Outer spacing around the button.</param>
  /// <param name="boxSizing">Whether padding is included in or added to the declared size.</param>
  /// <param name="backgroundGradient">Gradient applied to the background. Overrides <paramref name="backgroundColor"/> when set.</param>
  public RowBuilder AddButton(string text, string cmd,
      Position width = default, Position height = default,
      UIColor? backgroundColor = null, UIColor? textColor = null,
      float fontSize = 0,
      Border? border = null,
      Spacing? padding = null,
      Spacing? margin = null,
      BoxSizing boxSizing = BoxSizing.ContentBox,
      UIGradient backgroundGradient = default) {
    var data = StartData();
    data["Text"] = text;
    data["Cmd"] = cmd ?? string.Empty;
    data["ElemId"] = _window.NextElemId(_rowId);
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (textColor.HasValue) data["TextColor"] = textColor.Value;
    if (fontSize > 0) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    ApplySpacing(data, "Margin", margin);
    if (boxSizing != BoxSizing.ContentBox) data["BoxSizing"] = boxSizing.ToString();
    if (backgroundGradient.HasValue) data["BgGradient"] = backgroundGradient.Raw;
    return Enqueue("AddButton", data);
  }

  /// <summary>
  /// Adds an input field to this row. On submit, sends <paramref name="cmd"/> with
  /// <c>{<paramref name="id"/>}</c> resolved to the typed value.
  /// </summary>
  /// <param name="id">Unique identifier used as a placeholder token in <paramref name="cmd"/>.</param>
  /// <param name="placeholder">Greyed-out hint text shown when the input is empty.</param>
  /// <param name="cmd">Chat command template sent on submit. <c>{id}</c> is replaced with the typed value.</param>
  /// <param name="width">Input width (pixels or percentage string). Default: auto.</param>
  /// <param name="height">Input height (pixels or percentage string). Default: auto.</param>
  /// <param name="backgroundColor">Input background color.</param>
  /// <param name="textColor">Typed text color.</param>
  /// <param name="placeholderColor">Placeholder text color.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit from window default.</param>
  /// <param name="border">Optional border around the input.</param>
  /// <param name="padding">Inner spacing between the border and the input text.</param>
  /// <param name="margin">Outer spacing around the input.</param>
  /// <param name="boxSizing">Whether padding is included in or added to the declared size.</param>
  /// <param name="value">Pre-filled text value. Default: empty.</param>
  public RowBuilder AddInput(string id, string placeholder, string cmd,
      Position width = default, Position height = default,
      UIColor? backgroundColor = null, UIColor? textColor = null,
      UIColor? placeholderColor = null,
      float fontSize = 0,
      Border? border = null,
      Spacing? padding = null,
      Spacing? margin = null,
      BoxSizing boxSizing = BoxSizing.ContentBox,
      string value = null) {
    var data = StartData();
    data["Id"] = id ?? string.Empty;
    data["Placeholder"] = placeholder ?? string.Empty;
    data["Cmd"] = cmd ?? string.Empty;
    data["ElemId"] = _window.NextElemId(_rowId);
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (textColor.HasValue) data["TextColor"] = textColor.Value;
    if (placeholderColor.HasValue) data["PlaceholderColor"] = placeholderColor.Value;
    if (fontSize > 0) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    if (value != null) data["Value"] = value;
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    ApplySpacing(data, "Margin", margin);
    if (boxSizing != BoxSizing.ContentBox) data["BoxSizing"] = boxSizing.ToString();
    return Enqueue("AddInput", data);
  }

  /// <summary>Adds a horizontal progress bar to this row.</summary>
  /// <param name="value">Current progress value.</param>
  /// <param name="min">Minimum value of the progress range. Default: 0.</param>
  /// <param name="max">Maximum value of the progress range. Default: 100.</param>
  /// <param name="width">Bar width (pixels or percentage string). Default: auto.</param>
  /// <param name="height">Bar height (pixels or percentage string). Default: auto.</param>
  /// <param name="barColor">Fill color. Default: blue.</param>
  /// <param name="backgroundColor">Track (background) color. Default: dark grey.</param>
  /// <param name="border">Optional border around the progress bar.</param>
  /// <param name="margin">Outer spacing around the progress bar.</param>
  public RowBuilder AddProgressBar(float value, float min = 0f, float max = 100f,
      Position width = default, Position height = default,
      UIColor? barColor = null, UIColor? backgroundColor = null,
      Border? border = null,
      Spacing? margin = null) {
    var data = StartData();
    data["Value"] = value.ToString(CultureInfo.InvariantCulture);
    data["Min"] = min.ToString(CultureInfo.InvariantCulture);
    data["Max"] = max.ToString(CultureInfo.InvariantCulture);
    data["ElemId"] = _window.NextElemId(_rowId);
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (barColor.HasValue) data["BarColor"] = barColor.Value;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    ApplyBorder(data, border);
    ApplySpacing(data, "Margin", margin);
    return Enqueue("AddProgressBar", data);
  }

  /// <summary>Adds an image element loaded from a URL (external or local server endpoint).</summary>
  /// <param name="src">HTTP/HTTPS URL of the image to load.</param>
  /// <param name="width">Image width (pixels or percentage string). Default: auto.</param>
  /// <param name="height">Image height (pixels or percentage string). Default: auto.</param>
  /// <param name="backgroundColor">Color displayed while the image is loading.</param>
  /// <param name="fit">
  /// How the image fills its bounds: <c>Stretch</c> (default, may distort),
  /// <c>Fit</c> (letterbox, preserves ratio), <c>Fill</c> (crop, preserves ratio).
  /// </param>
  /// <param name="border">Optional border around the image.</param>
  /// <param name="margin">Outer spacing around the image.</param>
  public RowBuilder AddImage(string src,
      Position width = default, Position height = default,
      UIColor? backgroundColor = null,
      ImageFit fit = ImageFit.Stretch,
      Border? border = null,
      Spacing? margin = null) {
    var data = StartData();
    data["Src"] = src ?? string.Empty;
    data["Fit"] = fit.ToString();
    data["ElemId"] = _window.NextElemId(_rowId);
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    ApplyBorder(data, border);
    ApplySpacing(data, "Margin", margin);
    return Enqueue("AddImage", data);
  }

  /// <summary>Adds a pre-styled close button (×) that closes the window when clicked.</summary>
  /// <param name="backgroundColor">Button background color.</param>
  /// <param name="textColor">Color of the × icon.</param>
  /// <param name="padding">Inner spacing between the × icon and the button edge.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit from window default.</param>
  /// <param name="border">Optional border around the close button.</param>
  /// <param name="margin">Outer spacing around the close button.</param>
  public RowBuilder AddCloseButton(
      UIColor? backgroundColor = null, UIColor? textColor = null,
      Spacing? padding = null, float fontSize = 0,
      Border? border = null,
      Spacing? margin = null) {
    var data = StartData();
    data["ElemId"] = _window.NextElemId(_rowId);
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (textColor.HasValue) data["TextColor"] = textColor.Value;
    ApplySpacing(data, "Padding", padding);
    if (fontSize > 0) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    ApplyBorder(data, border);
    ApplySpacing(data, "Margin", margin);
    return Enqueue("AddCloseButton", data);
  }

  // ─── Tooltip ─────────────────────────────────────────────────────────────

  /// <summary>
  /// Attaches a tooltip to the last-added element in this row.
  /// The tooltip is an existing window identified by <paramref name="tooltipWindowId"/>
  /// that is shown when the mouse enters the element and hidden when it leaves.
  /// </summary>
  /// <param name="tooltipWindowId">ID of the window to show as tooltip.</param>
  public RowBuilder WithTooltip(string tooltipWindowId) {
    _window.WithTooltip(tooltipWindowId);
    return this;
  }

  // ─── Navigation ──────────────────────────────────────────────────────────

  /// <summary>Finishes building this row and returns to the parent <see cref="WindowBuilder"/>.</summary>
  public WindowBuilder Done() => _window;

  // ─── Helpers ─────────────────────────────────────────────────────────────

  Dictionary<string, string> StartData() => new() { ["Parent"] = _rowId };

  RowBuilder Enqueue(string type, Dictionary<string, string> data) {
    _window.EnqueuePacket(type, data);
    return this;
  }

  static void ApplyBorder(Dictionary<string, string> data, Border? border) {
    if (!border.HasValue) return;
    data["BorderColor"] = border.Value.Color;
    data["BorderWidth"] = border.Value.Width.ToString(CultureInfo.InvariantCulture);
    data["BorderRadius"] = border.Value.Radius.ToString(CultureInfo.InvariantCulture);
  }

  static void ApplySpacing(Dictionary<string, string> data, string prefix, Spacing? spacing) {
    if (!spacing.HasValue) return;
    data[$"{prefix}Top"] = spacing.Value.Top.ToString(CultureInfo.InvariantCulture);
    data[$"{prefix}Right"] = spacing.Value.Right.ToString(CultureInfo.InvariantCulture);
    data[$"{prefix}Bottom"] = spacing.Value.Bottom.ToString(CultureInfo.InvariantCulture);
    data[$"{prefix}Left"] = spacing.Value.Left.ToString(CultureInfo.InvariantCulture);
  }
}
