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
      ["an"] = window.Anchor.ToString(),
      ["dg"] = window.Draggable.ToString().ToLower(),
      ["tr"] = window.Transparent.ToString().ToLower(),
      ["ov"] = window.Overflow.ToString(),
    };
    if (window.Width.HasValue) wd["w"] = window.Width.Raw;
    if (window.Height.HasValue) wd["h"] = window.Height.Raw;
    if (window.Pivot.HasValue) wd["pv"] = window.Pivot.Value.ToString();
    SerializePosition(wd, window.Position);
    SerializeBackground(wd, window.Background);
    SerializeBorder(wd, window.Border);
    SerializeSpacing(wd, 'p', window.Padding);
    if (window.Gap > 0f) wd["gp"] = F(window.Gap);
    if (window.ScrollbarColor.HasValue) wd["sc"] = window.ScrollbarColor.Value;
    if (window.ScrollbarBackgroundColor.HasValue) wd["sb"] = window.ScrollbarBackgroundColor.Value;
    if (window.ScrollbarWidth != 8f) wd["sw"] = F(window.ScrollbarWidth);
    if (window.NativeParent != null) wd["np"] = window.NativeParent;
    if (window.Rotation != 0f) wd["ro"] = F(window.Rotation);
    if (window.HideOnMenuOpen) wd["hm"] = "true";
    if (window.BoxShadow.HasValue) wd["bx"] = window.BoxShadow.Value.Raw;
    if (window.OpenAnimation != WindowAnimation.None) wd["oa"] = window.OpenAnimation.ToString();
    if (window.CloseAnimation != WindowAnimation.None) wd["ca"] = window.CloseAnimation.ToString();
    if (window.AnimationDuration != 0.2f) wd["ad"] = F(window.AnimationDuration);
    packets.Add(Packet(plugin, windowId, "SW", wd));

    // ── Custom Texture ─────────────────────────────────────────────────────
    if (window.CustomTexture != null) {
      var ct = window.CustomTexture;
      var td = new Dictionary<string, string> {
        ["cs"] = ct.CornerSize.ToString(IC),
      };
      if (ct.TopLeftCorner != null) td["t1"] = ct.TopLeftCorner;
      if (ct.TopRightCorner != null) td["t2"] = ct.TopRightCorner;
      if (ct.BottomLeftCorner != null) td["b1"] = ct.BottomLeftCorner;
      if (ct.BottomRightCorner != null) td["b2"] = ct.BottomRightCorner;
      if (ct.TopBorder != null) td["tb"] = ct.TopBorder;
      if (ct.BottomBorder != null) td["bb"] = ct.BottomBorder;
      if (ct.LeftBorder != null) td["lb"] = ct.LeftBorder;
      if (ct.RightBorder != null) td["rb"] = ct.RightBorder;
      if (ct.Background != null) td["bim"] = ct.Background;
      if (ct.BackgroundRepeat) td["br"] = "true";
      if (ct.FrameExpand != 0) td["fe"] = ct.FrameExpand.ToString(IC);
      packets.Add(Packet(plugin, windowId, "SX", td));
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
      ["rd"] = rowId,
      ["ei"] = rowId,
      ["jc"] = row.JustifyContent.ToString(),
      ["ali"] = row.AlignItems.ToString(),
      ["ov"] = row.Overflow.ToString(),
    };
    if (row.Width.HasValue) d["w"] = row.Width.Raw;
    if (row.Height.HasValue) d["h"] = row.Height.Raw;
    SerializeBackground(d, row.Background);
    if (row.Anchor.HasValue) {
      d["an"] = row.Anchor.Value.ToString();
      SerializePosition(d, row.Position);
      if (row.Pivot.HasValue) d["pv"] = row.Pivot.Value.ToString();
    }
    SerializeBorder(d, row.Border);
    SerializeSpacing(d, 'p', row.Padding);
    SerializeSpacing(d, 'm', row.Margin);
    if (row.Gap > 0f) d["gp"] = F(row.Gap);
    if (row.ScrollbarColor.HasValue) d["sc"] = row.ScrollbarColor.Value;
    if (row.ScrollbarBackgroundColor.HasValue) d["sb"] = row.ScrollbarBackgroundColor.Value;
    if (row.ScrollbarWidth != 8f) d["sw"] = F(row.ScrollbarWidth);
    if (row.Rotation != 0f) d["ro"] = F(row.Rotation);
    if (row.BoxShadow.HasValue) d["bx"] = row.BoxShadow.Value.Raw;
    packets.Add(Packet(plugin, windowId, "AR", d));

    // Row children
    foreach (var child in row.Children)
      SerializeRowElement(packets, plugin, windowId, child, rowId, elemCounters);
  }

  static void SerializeAccordion(List<ScarletPacket> packets, string plugin, string windowId,
      Accordion acc, ref int rowCounter, Dictionary<string, int> elemCounters) {
    string accordionId = $"accordion_{rowCounter++}";
    elemCounters[accordionId] = 0;

    var d = new Dictionary<string, string> {
      ["ak"] = accordionId,
      ["ei"] = accordionId,
      ["ti"] = acc.Title ?? string.Empty,
      ["ex"] = acc.Expanded.ToString().ToLower(),
      ["hh"] = F(acc.HeaderHeight),
    };
    if (acc.Width.HasValue) d["w"] = acc.Width.Raw;
    if (acc.HeaderBackground.HasValue) acc.HeaderBackground.Value.Apply(d, "d");
    if (acc.HeaderTextColor.HasValue) d["htc"] = acc.HeaderTextColor.Value;
    if (acc.ChevronColor.HasValue) d["chc"] = acc.ChevronColor.Value;
    if (acc.ChevronIcon != null) d["chi"] = acc.ChevronIcon;
    if (!acc.ShowChevron) d["shc"] = "false";
    if (acc.ContentBackground.HasValue) acc.ContentBackground.Value.Apply(d, "j");
    if (acc.FontSize > 0f) d["fs"] = F(acc.FontSize);
    SerializeBorder(d, acc.Border);
    SerializeSpacing(d, 'p', acc.Padding);
    SerializeSpacing(d, 'm', acc.Margin);
    if (acc.Gap > 0f) d["gp"] = F(acc.Gap);
    if (acc.Rotation != 0f) d["ro"] = F(acc.Rotation);
    if (acc.BoxShadow.HasValue) d["bx"] = acc.BoxShadow.Value.Raw;
    packets.Add(Packet(plugin, windowId, "AA", d));

    // Accordion children behave like row children
    foreach (var child in acc.Children)
      SerializeRowElement(packets, plugin, windowId, child, accordionId, elemCounters);
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // Element serializers
  // ═══════════════════════════════════════════════════════════════════════════

  static void SerializeStandaloneElement(List<ScarletPacket> packets, string plugin,
      string windowId, UIElement elem, Dictionary<string, int> elemCounters) {
    string elemId = elem.ElemId ?? NextElemId(elemCounters, "_sa");
    var (type, d) = BuildElementData(elem, elemId);
    // Standalone elements always emit anchor/position
    d["an"] = (elem.Anchor ?? Anchor.TopLeft).ToString();
    SerializePosition(d, elem.Position);
    if (elem.Pivot.HasValue) d["pv"] = elem.Pivot.Value.ToString();
    packets.Add(Packet(plugin, windowId, type, d));
    SerializeTooltip(packets, plugin, windowId, elem, elemId);
  }

  static void SerializeRowElement(List<ScarletPacket> packets, string plugin,
      string windowId, UIElement elem, string parentId, Dictionary<string, int> elemCounters) {
    string elemId = elem.ElemId ?? NextElemId(elemCounters, parentId);
    var (type, d) = BuildElementData(elem, elemId);
    d["pa"] = parentId;
    packets.Add(Packet(plugin, windowId, type, d));
    SerializeTooltip(packets, plugin, windowId, elem, elemId);
  }

  /// <summary>
  /// Builds the packet Type name and data dictionary for a concrete element.
  /// Common base properties are always applied; type-specific properties added per case.
  /// </summary>
  static (string Type, Dictionary<string, string> Data) BuildElementData(UIElement elem, string elemId) {
    var d = new Dictionary<string, string> { ["ei"] = elemId };
    SerializeBase(d, elem);

    switch (elem) {
      case Text t:
        d["tx"] = t.Content ?? string.Empty;
        if (t.TextAlign != TextAlignment.Left) d["ta"] = t.TextAlign.ToString();
        if (t.Wrap) d["wr"] = "true";
        SerializeTextStyle(d, t);
        return ("AT", d);

      case Button b:
        d["tx"] = b.Label ?? string.Empty;
        d["cm"] = b.Command ?? string.Empty;
        if (b.BoxSizing != BoxSizing.ContentBox) d["bs"] = b.BoxSizing.ToString();
        SerializeTextStyle(d, b);
        SerializeHoverBackground(d, b.HoverBackground, b.PressedBackground);
        if (b.HoverScale) d["hs"] = "true";
        return ("AB", d);

      case Input inp:
        d["id"] = inp.Id ?? string.Empty;
        d["ph"] = inp.Placeholder ?? string.Empty;
        if (inp.PlaceholderColor.HasValue) d["pc"] = inp.PlaceholderColor.Value;
        if (inp.BoxSizing != BoxSizing.BorderBox) d["bs"] = inp.BoxSizing.ToString();
        if (inp.Value != null) d["vl"] = inp.Value;
        if (inp.TextAlign != TextAlignment.Left) d["ta"] = inp.TextAlign.ToString();
        if (inp.InputType != InputType.String) d["it"] = inp.InputType.ToString();
        if (inp.MaxLength > 0) d["mx"] = inp.MaxLength.ToString(IC);
        if (inp.FocusBackground.HasValue && inp.FocusBackground.Value.HasValue)
          inp.FocusBackground.Value.Apply(d, "f");
        if (inp.FocusBorder.HasValue) {
          d["fc"] = inp.FocusBorder.Value.Color;
          d["fw"] = F(inp.FocusBorder.Value.Width);
        }
        if (inp.CaretColor.HasValue) d["cc"] = inp.CaretColor.Value;
        if (inp.SelectionColor.HasValue) d["sl"] = inp.SelectionColor.Value;
        if (inp.SelectionTextColor.HasValue) d["st"] = inp.SelectionTextColor.Value;
        SerializeTextStyle(d, inp);
        return ("AI", d);

      case Dropdown dd:
        d["id"] = dd.Id ?? string.Empty;
        d["op"] = dd.Options ?? string.Empty;
        d["cm"] = dd.Command ?? string.Empty;
        d["ph"] = dd.Placeholder ?? "Select...";
        if (dd.DropdownBackgroundColor.HasValue) d["xb"] = dd.DropdownBackgroundColor.Value;
        if (dd.DropdownTextColor.HasValue) d["xt"] = dd.DropdownTextColor.Value;
        if (dd.DropdownHoverColor.HasValue) d["xh"] = dd.DropdownHoverColor.Value;
        if (dd.MaxHeight != 200f) d["mh"] = F(dd.MaxHeight);
        if (dd.BoxSizing != BoxSizing.ContentBox) d["bs"] = dd.BoxSizing.ToString();
        if (dd.Value != null) d["vl"] = dd.Value;
        SerializeTextStyle(d, dd);
        return ("AD", d);

      case ProgressBar pb:
        d["vl"] = F(pb.Value);
        d["mn"] = F(pb.Min);
        d["ma"] = F(pb.Max);
        if (pb.Background.HasValue && pb.Background.Value.HasValue) pb.Background.Value.Apply(d);
        if (pb.BarFill.HasValue && pb.BarFill.Value.HasValue) pb.BarFill.Value.Apply(d, "r");
        if (pb.AnimateValue) d["av"] = "true";
        if (pb.AnimationDuration != 0.3f) d["ad"] = F(pb.AnimationDuration);
        return ("AP", d);

      case AnimatedSheet anim:
        var animFrames = anim.FrameUrls is { Length: > 0 } ? anim.FrameUrls : anim.FrameSprites;
        d["fm"] = animFrames != null ? string.Join("\n", animFrames) : string.Empty;
        d["fy"] = anim.FrameUrls is { Length: > 0 } ? "Url" : "Sprite";
        d["tl"] = ((int)anim.Trigger).ToString(IC);
        d["du"] = F(anim.Duration);
        if (anim.Fit != ImageFit.Stretch) d["ft"] = anim.Fit.ToString();
        if (anim.LoopType != AnimationLoopType.Loop) d["lt"] = anim.LoopType.ToString();
        if (anim.LoopCount != 0) d["lc"] = anim.LoopCount.ToString(IC);
        if (anim.ReleaseMode != AnimationReleaseMode.Pause) d["rm"] = anim.ReleaseMode.ToString();
        if (!anim.Playing) d["pg"] = "false";
        return ("AS", d);

      case Image img:
        d["sr"] = img.Src ?? string.Empty;
        d["ft"] = img.Fit.ToString();
        return ("AM", d);

      case PortraitCamera cam:
        d["fv"] = F(cam.FieldOfView);
        d["ob"] = F(cam.OrbitAngle);
        if (cam.Distance != 1f) d["ds"] = F(cam.Distance);
        if (cam.AnchorBone != null) d["ab"] = cam.AnchorBone;
        if (cam.BackgroundUrl != null) d["bu"] = cam.BackgroundUrl;
        if (cam.BackgroundColor.HasValue) d["bcl"] = cam.BackgroundColor.Value;
        if (cam.BackgroundSize != 1.6f) d["bz"] = F(cam.BackgroundSize);
        if (cam.BackgroundOffsetX != 0f) d["ux"] = F(cam.BackgroundOffsetX);
        if (cam.BackgroundOffsetY != 0f) d["uy"] = F(cam.BackgroundOffsetY);
        if (cam.BackgroundScaleX != 1f) d["uw"] = F(cam.BackgroundScaleX);
        if (cam.BackgroundScaleY != 1f) d["uh"] = F(cam.BackgroundScaleY);
        return ("AC", d);

      case CloseButton:
        SerializeTextStyle(d, (ITextElement)elem);
        return ("AZ", d);

      default:
        return ("AU", d);
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // SSOT: shared property serializers (each type serialized in ONE place)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>Serializes base UIElement properties shared by all elements.</summary>
  static void SerializeBase(Dictionary<string, string> d, UIElement elem) {
    if (elem.Width.HasValue) d["w"] = elem.Width.Raw;
    if (elem.Height.HasValue) d["h"] = elem.Height.Raw;
    SerializeBackground(d, elem.Background);
    SerializeBorder(d, elem.Border);
    SerializeSpacing(d, 'p', elem.Padding);
    SerializeSpacing(d, 'm', elem.Margin);
    if (elem.Rotation != 0f) d["ro"] = F(elem.Rotation);
    if (elem.BoxShadow.HasValue) d["bx"] = elem.BoxShadow.Value.Raw;
  }

  /// <summary>Serializes UIBackground into data keys.</summary>
  static void SerializeBackground(Dictionary<string, string> d, UIBackground? bg) {
    if (!bg.HasValue || !bg.Value.HasValue) return;
    bg.Value.Apply(d);
  }

  /// <summary>Serializes ITextElement properties.</summary>
  static void SerializeTextStyle(Dictionary<string, string> d, ITextElement t) {
    if (t.TextColor.HasValue) d["tc"] = t.TextColor.Value;
    if (t.FontSize > 0) d["fs"] = F(t.FontSize);
    if (t.Font != null) d["fn"] = t.Font;
    if (t.TextGradient.HasValue && t.TextGradient.Value.HasValue) d["tg"] = t.TextGradient.Value.Raw;
    if (t.TextShadow.HasValue) d["ts"] = t.TextShadow.Value.Raw;
    if (t.TextOutline.HasValue) d["to"] = t.TextOutline.Value.Raw;
  }

  /// <summary>Serializes Border into data keys (dc=BorderColor, dw=BorderWidth, dr=BorderRadius).</summary>
  static void SerializeBorder(Dictionary<string, string> d, Border? border) {
    if (!border.HasValue) return;
    d["dc"] = border.Value.Color;
    d["dw"] = F(border.Value.Width);
    d["dr"] = F(border.Value.Radius);
  }

  /// <summary>Serializes Spacing using a 1-char prefix: 'p'=Padding, 'm'=Margin → pt/pr/pb/pl or mt/mr/mb/ml.</summary>
  static void SerializeSpacing(Dictionary<string, string> d, char pfx, Spacing? spacing) {
    if (!spacing.HasValue) return;
    d[$"{pfx}t"] = F(spacing.Value.Top);
    d[$"{pfx}r"] = F(spacing.Value.Right);
    d[$"{pfx}b"] = F(spacing.Value.Bottom);
    d[$"{pfx}l"] = F(spacing.Value.Left);
  }

  /// <summary>Serializes Position (px=PosX, py=PosY, zi=ZIndex).</summary>
  static void SerializePosition(Dictionary<string, string> d, Position? pos) {
    if (!pos.HasValue) return;
    if (pos.Value.X.HasValue) d["px"] = pos.Value.X.Raw;
    if (pos.Value.Y.HasValue) d["py"] = pos.Value.Y.Raw;
    if (pos.Value.ZIndex != 0) d["zi"] = pos.Value.ZIndex.ToString(IC);
  }

  /// <summary>
  /// Serializes button hover/pressed backgrounds.
  /// h=HoverBg prefix → hcl/hgr/him/hsp/hif/hfr/htt/hat/had/hlt/hlc/hrm/hpl
  /// q=PressedBg prefix → qcl/qgr/qim/qsp/qif/qfr/qtt/qat/qad/qlt/qlc/qrm/qpl
  /// </summary>
  static void SerializeHoverBackground(Dictionary<string, string> d,
      UIBackground? hover, UIBackground? pressed) {
    if (hover.HasValue && hover.Value.HasValue) hover.Value.Apply(d, "h");
    if (pressed.HasValue && pressed.Value.HasValue) pressed.Value.Apply(d, "q");
  }

  /// <summary>Serializes a tooltip if present (te=TargetElemId, tw=TooltipWindowId).</summary>
  static void SerializeTooltip(List<ScarletPacket> packets, string plugin, string windowId,
      UIElement elem, string elemId) {
    if (elem.Tooltip == null) return;
    packets.Add(Packet(plugin, windowId, "AO", new Dictionary<string, string> {
      ["te"] = elemId,
      ["tw"] = elem.Tooltip,
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

  internal static ScarletPacket SerializeElement(string plugin, string windowId, UIElement elem, string elemId) {
    var (type, d) = BuildElementData(elem, elemId);
    d["ue"] = "1";
    return Packet(plugin, windowId, type, d);
  }

  internal static ScarletPacket SerializeDeleteElement(string plugin, string windowId, string elemId) =>
    Packet(plugin, windowId, "DE", new Dictionary<string, string> { ["ei"] = elemId });

  static string F(float v) => v.ToString(IC);

  static ScarletPacket Packet(string plugin, string windowId, string type, Dictionary<string, string> data) =>
    new() { Type = type, Plugin = plugin, Window = windowId, Data = data };
}
