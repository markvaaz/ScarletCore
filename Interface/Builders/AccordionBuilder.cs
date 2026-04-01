using System.Collections.Generic;
using System.Globalization;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Fluent helper for adding elements inside an expandable Accordion created via
/// <see cref="WindowBuilder.AddAccordion"/>. Obtained from that method's return value.
/// </summary>
public sealed class AccordionBuilder {
  readonly WindowBuilder _window;
  readonly string _accordionId;

  internal AccordionBuilder(WindowBuilder window, string accordionId) {
    _window = window;
    _accordionId = accordionId;
  }

  // ─── Elements ────────────────────────────────────────────────────────────

  /// <summary>Adds a text element inside the accordion content area.</summary>
  public AccordionBuilder AddText(string text,
      Position width = default, Position height = default,
      UIColor? color = null, UIColor? backgroundColor = null,
      float fontSize = 0,
      Border? border = null,
      Spacing? padding = null,
      Spacing? margin = null,
      TextAlignment textAlign = TextAlignment.Left,
      bool wrap = false,
      UIGradient backgroundGradient = default,
      BoxShadow? boxShadow = null,
      string font = null) {
    var data = StartData();
    data["Text"] = text;
    data["ElemId"] = _window.NextElemId(_accordionId);
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
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    if (font != null) data["Font"] = font;
    return Enqueue("AddText", data);
  }

  /// <summary>Adds a button inside the accordion content area. Clicking sends <paramref name="cmd"/> to the server.</summary>
  public AccordionBuilder AddButton(string text, string cmd,
      Position width = default, Position height = default,
      UIColor? backgroundColor = null, UIColor? textColor = null,
      float fontSize = 0,
      Border? border = null,
      Spacing? padding = null,
      Spacing? margin = null,
      BoxSizing boxSizing = BoxSizing.ContentBox,
      UIGradient backgroundGradient = default,
      BoxShadow? boxShadow = null,
      string backgroundImage = null,
      string backgroundSprite = null,
      ImageFit backgroundImageFit = ImageFit.Stretch,
      string backgroundImageHover = null,
      string backgroundSpriteHover = null,
      string backgroundImagePressed = null,
      string backgroundSpritePressed = null,
      string font = null) {
    var data = StartData();
    data["Text"] = text;
    data["Cmd"] = cmd ?? string.Empty;
    data["ElemId"] = _window.NextElemId(_accordionId);
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
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    if (backgroundImage != null) data["BgImage"] = backgroundImage;
    if (backgroundSprite != null) data["BgSprite"] = backgroundSprite;
    if (backgroundImage != null || backgroundSprite != null || backgroundImageFit != ImageFit.Stretch)
      data["BgImageFit"] = backgroundImageFit.ToString();
    if (backgroundImageHover != null) data["BgImageHover"] = backgroundImageHover;
    if (backgroundSpriteHover != null) data["BgSpriteHover"] = backgroundSpriteHover;
    if (backgroundImagePressed != null) data["BgImagePressed"] = backgroundImagePressed;
    if (backgroundSpritePressed != null) data["BgSpritePressed"] = backgroundSpritePressed;
    if (font != null) data["Font"] = font;
    return Enqueue("AddButton", data);
  }

  /// <summary>
  /// Adds an input field inside the accordion content area. On submit, sends <paramref name="cmd"/>
  /// with <c>{<paramref name="id"/>}</c> resolved to the typed value.
  /// </summary>
  public AccordionBuilder AddInput(string id, string placeholder, string cmd,
      Position width = default, Position height = default,
      UIColor? backgroundColor = null, UIColor? textColor = null,
      UIColor? placeholderColor = null,
      float fontSize = 0,
      Border? border = null,
      Spacing? padding = null,
      Spacing? margin = null,
      BoxSizing boxSizing = BoxSizing.ContentBox,
      string value = null,
      BoxShadow? boxShadow = null) {
    var data = StartData();
    data["Id"] = id ?? string.Empty;
    data["Placeholder"] = placeholder ?? string.Empty;
    data["Cmd"] = cmd ?? string.Empty;
    data["ElemId"] = _window.NextElemId(_accordionId);
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
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    return Enqueue("AddInput", data);
  }

  /// <summary>Adds a horizontal progress bar inside the accordion content area.</summary>
  public AccordionBuilder AddProgressBar(float value, float min = 0f, float max = 100f,
      Position width = default, Position height = default,
      UIColor? barColor = null, UIColor? backgroundColor = null,
      Border? border = null,
      Spacing? margin = null,
      BoxShadow? boxShadow = null) {
    var data = StartData();
    data["Value"] = value.ToString(CultureInfo.InvariantCulture);
    data["Min"] = min.ToString(CultureInfo.InvariantCulture);
    data["Max"] = max.ToString(CultureInfo.InvariantCulture);
    data["ElemId"] = _window.NextElemId(_accordionId);
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (barColor.HasValue) data["BarColor"] = barColor.Value;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    ApplyBorder(data, border);
    ApplySpacing(data, "Margin", margin);
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    return Enqueue("AddProgressBar", data);
  }

  /// <summary>
  /// Adds a dropdown selector inside the accordion content area. On selection, sends
  /// <paramref name="cmd"/> with <c>{<paramref name="id"/>}</c> resolved to the chosen value.
  /// </summary>
  /// <param name="id">Unique identifier used as the placeholder token in <paramref name="cmd"/>.</param>
  /// <param name="options">Pipe-separated option pairs: <c>"Label A:val1|Label B:val2"</c>. If no colon, label equals value.</param>
  /// <param name="cmd">Chat command template sent on selection. <c>{id}</c> is replaced with the selected value.</param>
  /// <param name="width">Dropdown header width. Default: 150.</param>
  /// <param name="height">Dropdown header height. Default: 30.</param>
  /// <param name="backgroundColor">Header background color.</param>
  /// <param name="textColor">Header label color.</param>
  /// <param name="dropdownBackgroundColor">Popup panel background color.</param>
  /// <param name="dropdownTextColor">Option label color inside the popup.</param>
  /// <param name="dropdownHoverColor">Option highlight color on hover.</param>
  /// <param name="maxHeight">Maximum popup panel height before scrolling. Default: 200.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit.</param>
  /// <param name="border">Optional border around the header.</param>
  /// <param name="padding">Inner spacing between header border and label.</param>
  /// <param name="margin">Outer spacing around the header.</param>
  /// <param name="boxSizing">Whether padding is included in or added to the declared size.</param>
  /// <param name="placeholder">Text shown when no value is selected.</param>
  /// <param name="value">Pre-selected value.</param>
  /// <param name="boxShadow">Optional shadow around the header.</param>
  public AccordionBuilder AddDropdown(string id, string options, string cmd,
      Position width = default, Position height = default,
      UIColor? backgroundColor = null, UIColor? textColor = null,
      UIColor? dropdownBackgroundColor = null, UIColor? dropdownTextColor = null,
      UIColor? dropdownHoverColor = null,
      float maxHeight = 200f,
      float fontSize = 0,
      Border? border = null,
      Spacing? padding = null,
      Spacing? margin = null,
      BoxSizing boxSizing = BoxSizing.ContentBox,
      string placeholder = "Select...",
      string value = null,
      BoxShadow? boxShadow = null) {
    var data = StartData();
    data["Id"] = id ?? string.Empty;
    data["Options"] = options ?? string.Empty;
    data["Cmd"] = cmd ?? string.Empty;
    data["ElemId"] = _window.NextElemId(_accordionId);
    data["Placeholder"] = placeholder ?? "Select...";
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (textColor.HasValue) data["TextColor"] = textColor.Value;
    if (dropdownBackgroundColor.HasValue) data["DropdownBgColor"] = dropdownBackgroundColor.Value;
    if (dropdownTextColor.HasValue) data["DropdownTextColor"] = dropdownTextColor.Value;
    if (dropdownHoverColor.HasValue) data["DropdownHoverColor"] = dropdownHoverColor.Value;
    if (maxHeight != 200f) data["MaxH"] = maxHeight.ToString(CultureInfo.InvariantCulture);
    if (fontSize > 0) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    ApplySpacing(data, "Margin", margin);
    if (boxSizing != BoxSizing.ContentBox) data["BoxSizing"] = boxSizing.ToString();
    if (value != null) data["Value"] = value;
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    return Enqueue("AddDropdown", data);
  }

  // ─── Navigation ──────────────────────────────────────────────────────────

  /// <summary>Finishes building this accordion's content and returns to the parent <see cref="WindowBuilder"/>.</summary>
  public WindowBuilder Done() => _window;

  // ─── Helpers ─────────────────────────────────────────────────────────────

  Dictionary<string, string> StartData() => new() { ["Parent"] = _accordionId };

  AccordionBuilder Enqueue(string type, Dictionary<string, string> data) {
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
