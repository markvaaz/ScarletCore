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
      UIGradient backgroundGradient = default) {
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
      UIGradient backgroundGradient = default) {
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
      string value = null) {
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
    return Enqueue("AddInput", data);
  }

  /// <summary>Adds a horizontal progress bar inside the accordion content area.</summary>
  public AccordionBuilder AddProgressBar(float value, float min = 0f, float max = 100f,
      Position width = default, Position height = default,
      UIColor? barColor = null, UIColor? backgroundColor = null,
      Border? border = null,
      Spacing? margin = null) {
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
    return Enqueue("AddProgressBar", data);
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
