using System.Collections.Generic;
using System.Globalization;
using ScarletCore.Interface.Builders;
using ScarletCore.Interface.Elements;
using ScarletCore.Interface.Models;

namespace ScarletCore.Interface;

/// <summary>
/// Converts a <see cref="Window"/> element tree into a list of <see cref="ScarletPacket"/>
/// compatible with the ScarletInterface client. All data-key serialization is centralised
/// here — element classes carry only data, no serialization logic.
/// </summary>
internal static class ElementSerializer {
  static readonly CultureInfo IC = CultureInfo.InvariantCulture;

  /// <summary>Serializes the window and all children into an ordered packet list.</summary>
  internal static List<ScarletPacket> Serialize(Window window, string plugin, string windowId) {
    var packets = new List<ScarletPacket>();
    int rowCounter = 0;
    var elemCounters = new Dictionary<string, int>();

    // ── SetWindow packet ───────────────────────────────────────────────────
    var wd = new Dictionary<string, string> {
      ["Anchor"] = window.Anchor.ToString(),
      ["Draggable"] = window.Draggable.ToString().ToLower(),
      ["Transparent"] = window.Transparent.ToString().ToLower(),
      ["Overflow"] = window.Overflow.ToString(),
    };
    if (window.Width.HasValue) wd["W"] = window.Width.Raw;
    if (window.Height.HasValue) wd["H"] = window.Height.Raw;
    if (window.Pivot.HasValue) wd["Pivot"] = window.Pivot.Value.ToString();
    SerializePosition(wd, window.Position);
    SerializeBackground(wd, window.Background);
    SerializeBorder(wd, window.Border);
    SerializeSpacing(wd, "Padding", window.Padding);
    if (window.Gap > 0f) wd["Gap"] = F(window.Gap);
    if (window.ScrollbarColor.HasValue) wd["ScrollbarColor"] = window.ScrollbarColor.Value;
    if (window.ScrollbarBackgroundColor.HasValue) wd["ScrollbarBgColor"] = window.ScrollbarBackgroundColor.Value;
    if (window.ScrollbarWidth != 8f) wd["ScrollbarWidth"] = F(window.ScrollbarWidth);
    if (window.NativeParent != null) wd["NativeParent"] = window.NativeParent;
    if (window.Rotation != 0f) wd["Rotation"] = F(window.Rotation);
    if (window.HideOnMenuOpen) wd["HideOnMenuOpen"] = "true";
    if (window.BoxShadow.HasValue) wd["BoxShadow"] = window.BoxShadow.Value.Raw;
    packets.Add(Packet(plugin, windowId, "SetWindow", wd));

    // ── Custom Texture ─────────────────────────────────────────────────────
    if (window.CustomTexture != null) {
      var ct = window.CustomTexture;
      var td = new Dictionary<string, string> {
        ["CornerSize"] = ct.CornerSize.ToString(IC),
      };
      if (ct.TopLeftCorner != null) td["TLCorner"] = ct.TopLeftCorner;
      if (ct.TopRightCorner != null) td["TRCorner"] = ct.TopRightCorner;
      if (ct.BottomLeftCorner != null) td["BLCorner"] = ct.BottomLeftCorner;
      if (ct.BottomRightCorner != null) td["BRCorner"] = ct.BottomRightCorner;
      if (ct.TopBorder != null) td["TopBorder"] = ct.TopBorder;
      if (ct.BottomBorder != null) td["BottomBorder"] = ct.BottomBorder;
      if (ct.LeftBorder != null) td["LeftBorder"] = ct.LeftBorder;
      if (ct.RightBorder != null) td["RightBorder"] = ct.RightBorder;
      if (ct.Background != null) td["BgImage"] = ct.Background;
      if (ct.BackgroundRepeat) td["BgRepeat"] = "true";
      if (ct.FrameExpand != 0) td["FrameExpand"] = ct.FrameExpand.ToString(IC);
      packets.Add(Packet(plugin, windowId, "SetWindowTexture", td));
    }

    // ── Children ───────────────────────────────────────────────────────────
    foreach (var child in window.Children) {
      if (child is Row row)
        SerializeRow(packets, plugin, windowId, row, ref rowCounter, elemCounters);
      else if (child is Accordion accordion)
        SerializeAccordion(packets, plugin, windowId, accordion, ref rowCounter, elemCounters);
      else
        SerializeStandaloneElement(packets, plugin, windowId, child, elemCounters);
    }

    return packets;
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // Container serializers
  // ═══════════════════════════════════════════════════════════════════════════

  static void SerializeRow(List<ScarletPacket> packets, string plugin, string windowId,
      Row row, ref int rowCounter, Dictionary<string, int> elemCounters) {
    string rowId = $"row_{rowCounter++}";
    elemCounters[rowId] = 0;

    var d = new Dictionary<string, string> {
      ["RowId"] = rowId,
      ["ElemId"] = rowId,
      ["JustifyContent"] = row.JustifyContent.ToString(),
      ["AlignItems"] = row.AlignItems.ToString(),
      ["Overflow"] = row.Overflow.ToString(),
    };
    if (row.Width.HasValue) d["W"] = row.Width.Raw;
    if (row.Height.HasValue) d["H"] = row.Height.Raw;
    SerializeBackground(d, row.Background);
    if (row.Anchor.HasValue) {
      d["Anchor"] = row.Anchor.Value.ToString();
      SerializePosition(d, row.Position);
      if (row.Pivot.HasValue) d["Pivot"] = row.Pivot.Value.ToString();
    }
    SerializeBorder(d, row.Border);
    SerializeSpacing(d, "Padding", row.Padding);
    SerializeSpacing(d, "Margin", row.Margin);
    if (row.Gap > 0f) d["Gap"] = F(row.Gap);
    if (row.ScrollbarColor.HasValue) d["ScrollbarColor"] = row.ScrollbarColor.Value;
    if (row.ScrollbarBackgroundColor.HasValue) d["ScrollbarBgColor"] = row.ScrollbarBackgroundColor.Value;
    if (row.ScrollbarWidth != 8f) d["ScrollbarWidth"] = F(row.ScrollbarWidth);
    if (row.Rotation != 0f) d["Rotation"] = F(row.Rotation);
    if (row.BoxShadow.HasValue) d["BoxShadow"] = row.BoxShadow.Value.Raw;
    packets.Add(Packet(plugin, windowId, "AddRow", d));

    // Row children
    foreach (var child in row.Children)
      SerializeRowElement(packets, plugin, windowId, child, rowId, elemCounters);
  }

  static void SerializeAccordion(List<ScarletPacket> packets, string plugin, string windowId,
      Accordion acc, ref int rowCounter, Dictionary<string, int> elemCounters) {
    string accordionId = $"accordion_{rowCounter++}";
    elemCounters[accordionId] = 0;

    var d = new Dictionary<string, string> {
      ["AccordionId"] = accordionId,
      ["ElemId"] = accordionId,
      ["Title"] = acc.Title ?? string.Empty,
      ["Expanded"] = acc.Expanded.ToString().ToLower(),
      ["HeaderHeight"] = F(acc.HeaderHeight),
    };
    if (acc.Width.HasValue) d["W"] = acc.Width.Raw;
    if (acc.HeaderBackground.HasValue) acc.HeaderBackground.Value.Apply(d, "HeaderBg");
    if (acc.HeaderTextColor.HasValue) d["HeaderTextColor"] = acc.HeaderTextColor.Value;
    if (acc.ChevronColor.HasValue) d["ChevronColor"] = acc.ChevronColor.Value;
    if (acc.ChevronIcon != null) d["ChevronIcon"] = acc.ChevronIcon;
    if (!acc.ShowChevron) d["ShowChevron"] = "false";
    if (acc.ContentBackground.HasValue) acc.ContentBackground.Value.Apply(d, "ContentBg");
    if (acc.FontSize > 0f) d["FontSize"] = F(acc.FontSize);
    SerializeBorder(d, acc.Border);
    SerializeSpacing(d, "Padding", acc.Padding);
    SerializeSpacing(d, "Margin", acc.Margin);
    if (acc.Gap > 0f) d["Gap"] = F(acc.Gap);
    if (acc.Rotation != 0f) d["Rotation"] = F(acc.Rotation);
    if (acc.BoxShadow.HasValue) d["BoxShadow"] = acc.BoxShadow.Value.Raw;
    packets.Add(Packet(plugin, windowId, "AddAccordion", d));

    // Accordion children behave like row children
    foreach (var child in acc.Children)
      SerializeRowElement(packets, plugin, windowId, child, accordionId, elemCounters);
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // Element serializers
  // ═══════════════════════════════════════════════════════════════════════════

  static void SerializeStandaloneElement(List<ScarletPacket> packets, string plugin,
      string windowId, UIElement elem, Dictionary<string, int> elemCounters) {
    string elemId = NextElemId(elemCounters, "_sa");
    var (type, d) = BuildElementData(elem, elemId);
    // Standalone elements always emit anchor/position
    d["Anchor"] = (elem.Anchor ?? Anchor.TopLeft).ToString();
    SerializePosition(d, elem.Position);
    if (elem.Pivot.HasValue) d["Pivot"] = elem.Pivot.Value.ToString();
    packets.Add(Packet(plugin, windowId, type, d));
    SerializeTooltip(packets, plugin, windowId, elem, elemId);
  }

  static void SerializeRowElement(List<ScarletPacket> packets, string plugin,
      string windowId, UIElement elem, string parentId, Dictionary<string, int> elemCounters) {
    string elemId = NextElemId(elemCounters, parentId);
    var (type, d) = BuildElementData(elem, elemId);
    d["Parent"] = parentId;
    packets.Add(Packet(plugin, windowId, type, d));
    SerializeTooltip(packets, plugin, windowId, elem, elemId);
  }

  /// <summary>
  /// Builds the packet Type name and data dictionary for a concrete element.
  /// Common base properties are always applied; type-specific properties added per case.
  /// </summary>
  static (string Type, Dictionary<string, string> Data) BuildElementData(UIElement elem, string elemId) {
    var d = new Dictionary<string, string> { ["ElemId"] = elemId };
    SerializeBase(d, elem);

    switch (elem) {
      case Text t:
        d["Text"] = t.Content ?? string.Empty;
        if (t.TextAlign != TextAlignment.Left) d["TextAlign"] = t.TextAlign.ToString();
        if (t.Wrap) d["Wrap"] = "true";
        SerializeTextStyle(d, t);
        return ("AddText", d);

      case Button b:
        d["Text"] = b.Label ?? string.Empty;
        d["Cmd"] = b.Command ?? string.Empty;
        if (b.BoxSizing != BoxSizing.ContentBox) d["BoxSizing"] = b.BoxSizing.ToString();
        SerializeTextStyle(d, b);
        SerializeHoverBackground(d, b.HoverBackground, b.PressedBackground);
        return ("AddButton", d);

      case Input inp:
        d["Id"] = inp.Id ?? string.Empty;
        d["Placeholder"] = inp.Placeholder ?? string.Empty;
        if (inp.PlaceholderColor.HasValue) d["PlaceholderColor"] = inp.PlaceholderColor.Value;
        if (inp.BoxSizing != BoxSizing.ContentBox) d["BoxSizing"] = inp.BoxSizing.ToString();
        if (inp.Value != null) d["Value"] = inp.Value;
        SerializeTextStyle(d, inp);
        return ("AddInput", d);

      case Dropdown dd:
        d["Id"] = dd.Id ?? string.Empty;
        d["Options"] = dd.Options ?? string.Empty;
        d["Cmd"] = dd.Command ?? string.Empty;
        d["Placeholder"] = dd.Placeholder ?? "Select...";
        if (dd.DropdownBackgroundColor.HasValue) d["DropdownBgColor"] = dd.DropdownBackgroundColor.Value;
        if (dd.DropdownTextColor.HasValue) d["DropdownTextColor"] = dd.DropdownTextColor.Value;
        if (dd.DropdownHoverColor.HasValue) d["DropdownHoverColor"] = dd.DropdownHoverColor.Value;
        if (dd.MaxHeight != 200f) d["MaxH"] = F(dd.MaxHeight);
        if (dd.BoxSizing != BoxSizing.ContentBox) d["BoxSizing"] = dd.BoxSizing.ToString();
        if (dd.Value != null) d["Value"] = dd.Value;
        SerializeTextStyle(d, dd);
        return ("AddDropdown", d);

      case ProgressBar pb:
        d["Value"] = F(pb.Value);
        d["Min"] = F(pb.Min);
        d["Max"] = F(pb.Max);
        if (pb.BarFill.HasValue) pb.BarFill.Value.Apply(d, "Bar");
        return ("AddProgressBar", d);

      case Image img:
        d["Src"] = img.Src ?? string.Empty;
        d["Fit"] = img.Fit.ToString();
        return ("AddImage", d);

      case PortraitCamera cam:
        d["FOV"] = F(cam.FieldOfView);
        d["Orbit"] = F(cam.OrbitAngle);
        if (cam.Distance != 1f) d["Distance"] = F(cam.Distance);
        if (cam.AnchorBone != null) d["AnchorBone"] = cam.AnchorBone;
        if (cam.BackgroundUrl != null) d["BgUrl"] = cam.BackgroundUrl;
        if (cam.BackgroundColor.HasValue) d["BgColor"] = cam.BackgroundColor.Value;
        if (cam.BackgroundSize != 1.6f) d["BgSize"] = F(cam.BackgroundSize);
        if (cam.BackgroundOffsetX != 0f) d["BgUvX"] = F(cam.BackgroundOffsetX);
        if (cam.BackgroundOffsetY != 0f) d["BgUvY"] = F(cam.BackgroundOffsetY);
        if (cam.BackgroundScaleX != 1f) d["BgUvW"] = F(cam.BackgroundScaleX);
        if (cam.BackgroundScaleY != 1f) d["BgUvH"] = F(cam.BackgroundScaleY);
        return ("AddPortraitCamera", d);

      case CloseButton:
        SerializeTextStyle(d, (ITextElement)elem);
        return ("AddCloseButton", d);

      default:
        return ("AddUnknown", d);
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // SSOT: shared property serializers (each type serialized in ONE place)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>Serializes base UIElement properties shared by all elements.</summary>
  static void SerializeBase(Dictionary<string, string> d, UIElement elem) {
    if (elem.Width.HasValue) d["W"] = elem.Width.Raw;
    if (elem.Height.HasValue) d["H"] = elem.Height.Raw;
    SerializeBackground(d, elem.Background);
    SerializeBorder(d, elem.Border);
    SerializeSpacing(d, "Padding", elem.Padding);
    SerializeSpacing(d, "Margin", elem.Margin);
    if (elem.Rotation != 0f) d["Rotation"] = F(elem.Rotation);
    if (elem.BoxShadow.HasValue) d["BoxShadow"] = elem.BoxShadow.Value.Raw;
  }

  /// <summary>Serializes UIBackground into data keys.</summary>
  static void SerializeBackground(Dictionary<string, string> d, UIBackground? bg) {
    if (!bg.HasValue || !bg.Value.HasValue) return;
    bg.Value.Apply(d);
  }

  /// <summary>Serializes ITextElement properties.</summary>
  static void SerializeTextStyle(Dictionary<string, string> d, ITextElement t) {
    if (t.TextColor.HasValue) d["TextColor"] = t.TextColor.Value;
    if (t.FontSize > 0) d["FontSize"] = F(t.FontSize);
    if (t.Font != null) d["Font"] = t.Font;
    if (t.TextGradient.HasValue && t.TextGradient.Value.HasValue) d["TextGradient"] = t.TextGradient.Value.Raw;
    if (t.TextShadow.HasValue) d["TextShadow"] = t.TextShadow.Value.Raw;
    if (t.TextOutline.HasValue) d["TextOutline"] = t.TextOutline.Value.Raw;
  }

  /// <summary>Serializes Border into data keys.</summary>
  static void SerializeBorder(Dictionary<string, string> d, Border? border) {
    if (!border.HasValue) return;
    d["BorderColor"] = border.Value.Color;
    d["BorderWidth"] = F(border.Value.Width);
    d["BorderRadius"] = F(border.Value.Radius);
  }

  /// <summary>Serializes Spacing into prefixed data keys (e.g. PaddingTop, MarginLeft).</summary>
  static void SerializeSpacing(Dictionary<string, string> d, string prefix, Spacing? spacing) {
    if (!spacing.HasValue) return;
    d[$"{prefix}Top"] = F(spacing.Value.Top);
    d[$"{prefix}Right"] = F(spacing.Value.Right);
    d[$"{prefix}Bottom"] = F(spacing.Value.Bottom);
    d[$"{prefix}Left"] = F(spacing.Value.Left);
  }

  /// <summary>Serializes Position (X, Y, ZIndex) into data keys.</summary>
  static void SerializePosition(Dictionary<string, string> d, Position? pos) {
    if (!pos.HasValue) return;
    if (pos.Value.X.HasValue) d["PosX"] = pos.Value.X.Raw;
    if (pos.Value.Y.HasValue) d["PosY"] = pos.Value.Y.Raw;
    if (pos.Value.ZIndex != 0) d["ZIndex"] = pos.Value.ZIndex.ToString(IC);
  }

  /// <summary>Serializes button hover/pressed backgrounds into data keys.</summary>
  static void SerializeHoverBackground(Dictionary<string, string> d,
      UIBackground? hover, UIBackground? pressed) {
    if (hover.HasValue && hover.Value.HasValue) {
      if (hover.Value.ImageUrl != null) d["BgImageHover"] = hover.Value.ImageUrl;
      if (hover.Value.SpriteName != null) d["BgSpriteHover"] = hover.Value.SpriteName;
    }
    if (pressed.HasValue && pressed.Value.HasValue) {
      if (pressed.Value.ImageUrl != null) d["BgImagePressed"] = pressed.Value.ImageUrl;
      if (pressed.Value.SpriteName != null) d["BgSpritePressed"] = pressed.Value.SpriteName;
    }
  }

  /// <summary>Serializes a tooltip if present.</summary>
  static void SerializeTooltip(List<ScarletPacket> packets, string plugin, string windowId,
      UIElement elem, string elemId) {
    if (elem.Tooltip == null) return;
    packets.Add(Packet(plugin, windowId, "AddTooltip", new Dictionary<string, string> {
      ["TargetElemId"] = elemId,
      ["TooltipWindowId"] = elem.Tooltip,
    }));
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // Helpers
  // ═══════════════════════════════════════════════════════════════════════════

  static string NextElemId(Dictionary<string, int> counters, string scope) {
    counters.TryGetValue(scope, out int c);
    counters[scope] = c + 1;
    return $"{scope}_e{c}";
  }

  static string F(float v) => v.ToString(IC);

  static ScarletPacket Packet(string plugin, string windowId, string type, Dictionary<string, string> data) =>
    new() { Type = type, Plugin = plugin, Window = windowId, Data = data };
}
