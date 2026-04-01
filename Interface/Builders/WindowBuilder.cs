using System.Collections.Generic;
using System.Globalization;
using ScarletCore.Services;
using ScarletCore.Interface.Models;

namespace ScarletCore.Interface.Builders;

/// <summary>Fluent builder for constructing and sending ScarletInterface UI packets to players.</summary>
public class WindowBuilder {
  readonly string _plugin;
  readonly PlayerData _player; // null = send to all
  string _windowId;
  int _rowCounter;
  // Per-row element counters so ElemId is stable across sends
  readonly Dictionary<string, int> _elemCounters = new();
  readonly Queue<ScarletPacket> _queue = new();
  WindowAction _pendingAction = WindowAction.None;
  string _lastElemId;

  internal WindowBuilder(string plugin, PlayerData player = null) {
    _plugin = plugin;
    _player = player;
  }

  /// <summary>Sets the target window ID. Must be called before adding elements.</summary>
  public WindowBuilder Window(string id) {
    _windowId = id;
    _rowCounter = 0;
    _elemCounters.Clear();
    return this;
  }

  // Internal: returns a monotonically-increasing element id for the given parent scope.
  internal string NextElemId(string scope) {
    _elemCounters.TryGetValue(scope, out int c);
    _elemCounters[scope] = c + 1;
    _lastElemId = $"{scope}_e{c}";
    return _lastElemId;
  }

  // Window configuration
  /// <summary>
  /// Configures the window's layout and visual properties. Call before adding elements
  /// to set the target window size, position, and appearance for subsequent packets.
  /// </summary>
  /// <param name="width">Window width (pixels or percentage string).</param>
  /// <param name="height">Window height (pixels or percentage string).</param>
  /// <param name="backgroundColor">Window background color.</param>
  /// <param name="anchor">Parent anchor used to position the window.</param>
  /// <param name="pivot">Internal pivot of the window (overrides default placement).</param>
  /// <param name="x">X offset relative to the <paramref name="anchor"/>.</param>
  /// <param name="y">Y offset relative to the <paramref name="anchor"/>.</param>
  /// <param name="border">Optional border around the window.</param>
  /// <param name="padding">Inner spacing between the border and child content.</param>
  /// <param name="gap">Gap in pixels between rows inside the window.</param>
  /// <param name="overflow">How overflowing child content is handled.</param>
  /// <param name="scrollbarColor">Scrollbar thumb color.</param>
  /// <param name="scrollbarBackgroundColor">Scrollbar track color.</param>
  /// <param name="scrollbarWidth">Scrollbar width in pixels.</param>
  /// <param name="draggable">Whether the player can drag the window.</param>
  /// <param name="transparent">If true, renders the window background transparent.</param>
  /// <param name="backgroundGradient">Optional background gradient string.</param>
  /// <param name="nativeParent">Optional native UI parent identifier to attach to.</param>
  /// <param name="rotation">Rotation in degrees applied to the window; 0 = no rotation.</param>
  /// <param name="hideOnMenuOpen">If true, this window is hidden when any in-game menu opens (inventory, map, etc.).</param>
  /// <param name="zIndex">Canvas sorting order used to layer this window above or below others. Higher values render on top.</param>
  /// <param name="boxShadow">Optional box shadow rendered behind the element.</param>
  public WindowBuilder SetWindow(
    Position width = default, Position height = default,
    UIColor? backgroundColor = null,
    Anchor anchor = Anchor.MiddleCenter,
    Pivot? pivot = null,
    Position x = default, Position y = default,
    Border? border = null,
    Spacing? padding = null,
    float gap = 0f,
    OverflowMode overflow = OverflowMode.Visible,
    UIColor? scrollbarColor = null,
    UIColor? scrollbarBackgroundColor = null,
    float scrollbarWidth = 8f,
    bool draggable = true,
    bool transparent = false,
    UIGradient backgroundGradient = default,
    string nativeParent = null,
    float rotation = 0f,
    bool hideOnMenuOpen = true,
    int zIndex = 0,
    BoxShadow? boxShadow = null) {
    // Reset element counters when configuring a new window layout
    _rowCounter = 0;
    _elemCounters.Clear();
    var data = new Dictionary<string, string> {
      ["Anchor"] = anchor.ToString(),
      ["Draggable"] = draggable.ToString().ToLower(),
      ["Transparent"] = transparent.ToString().ToLower(),
      ["Overflow"] = overflow.ToString(),
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
    if (x.HasValue) data["PosX"] = x.Raw;
    if (y.HasValue) data["PosY"] = y.Raw;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    if (gap > 0f) data["Gap"] = gap.ToString(CultureInfo.InvariantCulture);
    if (backgroundGradient.HasValue) data["BgGradient"] = backgroundGradient.Raw;
    if (scrollbarColor.HasValue) data["ScrollbarColor"] = scrollbarColor.Value;
    if (scrollbarBackgroundColor.HasValue) data["ScrollbarBgColor"] = scrollbarBackgroundColor.Value;
    if (scrollbarWidth != 8f) data["ScrollbarWidth"] = scrollbarWidth.ToString(CultureInfo.InvariantCulture);
    if (nativeParent != null) data["NativeParent"] = nativeParent;
    if (rotation != 0f) data["Rotation"] = rotation.ToString(CultureInfo.InvariantCulture);
    if (hideOnMenuOpen) data["HideOnMenuOpen"] = "true";
    if (zIndex != 0) data["ZIndex"] = zIndex.ToString(CultureInfo.InvariantCulture);
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    return Enqueue("SetWindow", data);
  }

  // Row
  /// <summary>Adds a new horizontal row container to the window and returns a <see cref="RowBuilder"/> for populating it.</summary>
  /// <param name="width">Row width (pixels or percentage string).</param>
  /// <param name="height">Row height (pixels or percentage string).</param>
  /// <param name="background">Optional background color for the row.</param>
  /// <param name="anchor">If set, anchors the row to a parent point and allows position offsets.</param>
  /// <param name="x">X offset when <paramref name="anchor"/> is provided.</param>
  /// <param name="y">Y offset when <paramref name="anchor"/> is provided.</param>
  /// <param name="pivot">Optional pivot for the row when anchored.</param>
  /// <param name="border">Optional border around the row.</param>
  /// <param name="padding">Inner spacing for the row's children.</param>
  /// <param name="margin">Outer spacing around the row.</param>
  /// <param name="gap">Gap between child elements inside the row.</param>
  /// <param name="justifyContent">Horizontal distribution of row children.</param>
  /// <param name="alignItems">Vertical alignment of row children.</param>
  /// <param name="overflow">How overflowing content is handled for this row.</param>
  /// <param name="scrollbarColor">Scrollbar thumb color for overflowed rows.</param>
  /// <param name="scrollbarBackgroundColor">Scrollbar track color for overflowed rows.</param>
  /// <param name="scrollbarWidth">Scrollbar width in pixels.</param>
  /// <param name="rotation">Rotation in degrees applied to the row container; 0 = no rotation.</param>
  /// <param name="boxShadow">Optional box shadow rendered behind the element.</param>
  public RowBuilder AddRow(
    Position width = default, Position height = default,
    UIColor? background = null,
    Anchor? anchor = null,
    Position x = default, Position y = default,
    Pivot? pivot = null,
    Border? border = null,
    Spacing? padding = null,
    Spacing? margin = null,
    float gap = 0f,
    JustifyContent justifyContent = JustifyContent.Start,
    AlignItems alignItems = AlignItems.Start,
    OverflowMode overflow = OverflowMode.Visible,
    UIColor? scrollbarColor = null,
    UIColor? scrollbarBackgroundColor = null,
    float scrollbarWidth = 8f,
    float rotation = 0f,
    BoxShadow? boxShadow = null) {
    string rowId = $"row_{_rowCounter++}";
    _elemCounters[rowId] = 0; // initialize element counter for this row
    var data = new Dictionary<string, string> {
      ["RowId"] = rowId,
      ["JustifyContent"] = justifyContent.ToString(),
      ["AlignItems"] = alignItems.ToString(),
      ["Overflow"] = overflow.ToString(),
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (background.HasValue) data["Background"] = background.Value;
    if (anchor.HasValue) {
      data["Anchor"] = anchor.Value.ToString();
      if (x.HasValue) data["PosX"] = x.Raw;
      if (y.HasValue) data["PosY"] = y.Raw;
      if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
    }
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    ApplySpacing(data, "Margin", margin);
    if (gap > 0f) data["Gap"] = gap.ToString(CultureInfo.InvariantCulture);
    if (scrollbarColor.HasValue) data["ScrollbarColor"] = scrollbarColor.Value;
    if (scrollbarBackgroundColor.HasValue) data["ScrollbarBgColor"] = scrollbarBackgroundColor.Value;
    if (scrollbarWidth != 8f) data["ScrollbarWidth"] = scrollbarWidth.ToString(CultureInfo.InvariantCulture);
    if (rotation != 0f) data["Rotation"] = rotation.ToString(CultureInfo.InvariantCulture);
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    data["ElemId"] = rowId; // row's own ElemId equals its RowId
    Enqueue("AddRow", data);
    return new RowBuilder(this, rowId);
  }

  // Accordion
  /// <summary>
  /// Adds a collapsible accordion element. The header is always visible; clicking it toggles
  /// the content area client-side with no server round-trip.
  /// Use the returned <see cref="AccordionBuilder"/> to populate the content, then call
  /// <see cref="AccordionBuilder.Done"/> to return here.
  /// </summary>
  public AccordionBuilder AddAccordion(
      string title,
      bool expanded = false,
      Position width = default,
      UIColor? headerBackgroundColor = null,
      UIColor? headerTextColor = null,
      UIColor? chevronColor = null,
      UIColor? contentBackgroundColor = null,
      float headerHeight = 32f,
      float fontSize = 0f,
      Border? border = null,
      Spacing? padding = null,
      float gap = 0f,
      string chevronIcon = null,
      bool showChevron = true) {
    string accordionId = $"accordion_{_rowCounter++}";
    _elemCounters[accordionId] = 0;
    var data = new Dictionary<string, string> {
      ["AccordionId"] = accordionId,
      ["Title"] = title ?? string.Empty,
      ["Expanded"] = expanded.ToString().ToLower(),
      ["HeaderHeight"] = headerHeight.ToString(CultureInfo.InvariantCulture),
      ["ElemId"] = accordionId,
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (headerBackgroundColor.HasValue) data["HeaderBgColor"] = headerBackgroundColor.Value;
    if (headerTextColor.HasValue) data["HeaderTextColor"] = headerTextColor.Value;
    if (chevronColor.HasValue) data["ChevronColor"] = chevronColor.Value;
    if (chevronIcon != null) data["ChevronIcon"] = chevronIcon;
    if (!showChevron) data["ShowChevron"] = "false";
    if (contentBackgroundColor.HasValue) data["ContentBgColor"] = contentBackgroundColor.Value;
    if (fontSize > 0f) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    if (gap > 0f) data["Gap"] = gap.ToString(CultureInfo.InvariantCulture);
    Enqueue("AddAccordion", data);
    return new AccordionBuilder(this, accordionId);
  }

  // Standalone elements
  /// <summary>Adds a standalone text element positioned relative to the window.</summary>
  /// <param name="text">The text content to display (supports inline icons).</param>
  /// <param name="width">Element width (pixels or percentage string).</param>
  /// <param name="height">Element height (pixels or percentage string).</param>
  /// <param name="color">Text color.</param>
  /// <param name="backgroundColor">Background color behind the text.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit from window default.</param>
  /// <param name="anchor">Anchor point on the parent used to position the element.</param>
  /// <param name="x">X offset relative to the anchor.</param>
  /// <param name="y">Y offset relative to the anchor.</param>
  /// <param name="pivot">Element pivot controlling internal origin.</param>
  /// <param name="border">Optional border around the text element.</param>
  /// <param name="padding">Inner spacing between border and text.</param>
  /// <param name="margin">Outer spacing around the element.</param>
  /// <param name="textAlign">Horizontal text alignment.</param>
  /// <param name="wrap">If true, wraps text to multiple lines when it exceeds width.</param>
  /// <param name="backgroundGradient">Optional background gradient.</param>
  /// <param name="textGradient">Optional vertex-color gradient applied directly to text characters.</param>
  /// <param name="textShadow">Optional drop shadow rendered behind the text.</param>
  /// <param name="textOutline">Optional outline around each glyph.</param>
  /// <param name="rotation">Rotation in degrees applied to the element; 0 = no rotation.</param>
  /// <param name="boxShadow">Optional box shadow rendered behind the element.</param>
  /// <param name="font">TMP font asset name, e.g. "BORUTTA GROUP - Nocturne Serif Regular SDF". Default: game default.</param>
  public WindowBuilder AddText(string text,
    Position width = default, Position height = default,
    UIColor? color = null, UIColor? backgroundColor = null,
    float fontSize = 0,
    Anchor anchor = Anchor.TopLeft,
    Position x = default, Position y = default,
    Pivot? pivot = null,
    Border? border = null,
    Spacing? padding = null,
    Spacing? margin = null,
    TextAlignment textAlign = TextAlignment.Left,
    bool wrap = false,
    UIGradient backgroundGradient = default,
    UITextGradient textGradient = default,
    UITextShadow? textShadow = null,
    UITextOutline? textOutline = null,
    float rotation = 0f,
    BoxShadow? boxShadow = null,
    string font = null) {
    var data = new Dictionary<string, string> {
      ["Text"] = text,
      ["Anchor"] = anchor.ToString(),
      ["ElemId"] = NextElemId("_sa"),
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (x.HasValue) data["PosX"] = x.Raw;
    if (y.HasValue) data["PosY"] = y.Raw;
    if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
    if (color.HasValue) data["Color"] = color.Value;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (fontSize > 0) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    ApplySpacing(data, "Margin", margin);
    if (textAlign != TextAlignment.Left) data["TextAlign"] = textAlign.ToString();
    if (wrap) data["Wrap"] = "true";
    if (backgroundGradient.HasValue) data["BgGradient"] = backgroundGradient.Raw;
    if (textGradient.HasValue) data["TextGradient"] = textGradient.Raw;
    if (textShadow.HasValue) data["TextShadow"] = textShadow.Value.Raw;
    if (textOutline.HasValue) data["TextOutline"] = textOutline.Value.Raw;
    if (rotation != 0f) data["Rotation"] = rotation.ToString(CultureInfo.InvariantCulture);
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    if (font != null) data["Font"] = font;
    return Enqueue("AddText", data);
  }

  /// <summary>Adds a standalone button positioned relative to the window.</summary>
  /// <param name="text">Button label text.</param>
  /// <param name="cmd">Chat command sent when the button is clicked.</param>
  /// <param name="width">Button width (pixels or percentage string).</param>
  /// <param name="height">Button height (pixels or percentage string).</param>
  /// <param name="backgroundColor">Background color for the button.</param>
  /// <param name="textColor">Label color for the button.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit from window default.</param>
  /// <param name="anchor">Anchor point on the parent used to position the element.</param>
  /// <param name="x">X offset relative to the anchor.</param>
  /// <param name="y">Y offset relative to the anchor.</param>
  /// <param name="pivot">Element pivot controlling internal origin.</param>
  /// <param name="border">Optional border around the button.</param>
  /// <param name="padding">Inner spacing between border and label.</param>
  /// <param name="margin">Outer spacing around the button.</param>
  /// <param name="boxSizing">Whether padding is included in the declared size.</param>
  /// <param name="backgroundGradient">Optional background gradient.</param>
  /// <param name="rotation">Rotation in degrees applied to the element; 0 = no rotation.</param>
  /// <param name="boxShadow">Optional box shadow rendered behind the element.</param>
  /// <param name="backgroundImage">URL of a remote image to use as button background. Overrides <paramref name="backgroundColor"/>.</param>
  /// <param name="backgroundSprite">Name of a game sprite (e.g. "Poneti_Icon_Blacksmith_01_stick") to use as button background. Overrides <paramref name="backgroundColor"/>.</param>
  /// <param name="backgroundImageFit">How the background image/sprite is sized inside the button. Default: Stretch.</param>
  /// <param name="backgroundImageHover">URL of the image shown when the cursor hovers over the button.</param>
  /// <param name="backgroundSpriteHover">Sprite name shown when the cursor hovers over the button.</param>
  /// <param name="backgroundImagePressed">URL of the image shown while the button is pressed.</param>
  /// <param name="backgroundSpritePressed">Sprite name shown while the button is pressed.</param>
  /// <param name="font">TMP font asset name, e.g. "BORUTTA GROUP - Nocturne Serif Regular SDF". Default: game default.</param>
  public WindowBuilder AddButton(string text, string cmd,
    Position width = default, Position height = default,
    UIColor? backgroundColor = null, UIColor? textColor = null,
    float fontSize = 0,
    Anchor anchor = Anchor.TopLeft,
    Position x = default, Position y = default,
    Pivot? pivot = null,
    Border? border = null,
    Spacing? padding = null,
    Spacing? margin = null,
    BoxSizing boxSizing = BoxSizing.ContentBox,
    UIGradient backgroundGradient = default,
    float rotation = 0f,
    BoxShadow? boxShadow = null,
    string backgroundImage = null,
    string backgroundSprite = null,
    ImageFit backgroundImageFit = ImageFit.Stretch,
    string backgroundImageHover = null,
    string backgroundSpriteHover = null,
    string backgroundImagePressed = null,
    string backgroundSpritePressed = null,
    string font = null) {
    var data = new Dictionary<string, string> {
      ["Text"] = text,
      ["Cmd"] = cmd ?? string.Empty,
      ["Anchor"] = anchor.ToString(),
      ["ElemId"] = NextElemId("_sa"),
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (x.HasValue) data["PosX"] = x.Raw;
    if (y.HasValue) data["PosY"] = y.Raw;
    if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (textColor.HasValue) data["TextColor"] = textColor.Value;
    if (fontSize > 0) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    ApplySpacing(data, "Margin", margin);
    if (boxSizing != BoxSizing.ContentBox) data["BoxSizing"] = boxSizing.ToString();
    if (backgroundGradient.HasValue) data["BgGradient"] = backgroundGradient.Raw;
    if (rotation != 0f) data["Rotation"] = rotation.ToString(CultureInfo.InvariantCulture);
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
  /// Adds a standalone input field positioned relative to the window. On submit, the
  /// configured <paramref name="cmd"/> is sent with the <paramref name="id"/> token
  /// replaced by the typed value.
  /// </summary>
  /// <param name="id">Unique identifier used as a placeholder token in <paramref name="cmd"/>.</param>
  /// <param name="placeholder">Placeholder hint text shown when empty.</param>
  /// <param name="cmd">Chat command template sent on submit (use <c>{id}</c> token).</param>
  /// <param name="width">Input width (pixels or percentage string).</param>
  /// <param name="height">Input height (pixels or percentage string).</param>
  /// <param name="backgroundColor">Input background color.</param>
  /// <param name="textColor">Typed text color.</param>
  /// <param name="placeholderColor">Placeholder text color.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit from window default.</param>
  /// <param name="anchor">Anchor point on the parent used to position the element.</param>
  /// <param name="x">X offset relative to the anchor.</param>
  /// <param name="y">Y offset relative to the anchor.</param>
  /// <param name="pivot">Element pivot controlling internal origin.</param>
  /// <param name="border">Optional border around the input.</param>
  /// <param name="padding">Inner spacing between border and text.</param>
  /// <param name="margin">Outer spacing around the input.</param>
  /// <param name="boxSizing">Whether padding is included in the declared size.</param>
  /// <param name="value">Optional pre-filled value.</param>
  /// <param name="rotation">Rotation in degrees applied to the element; 0 = no rotation.</param>
  /// <param name="boxShadow">Optional box shadow rendered behind the element.</param>
  public WindowBuilder AddInput(string id, string placeholder, string cmd,
    Position width = default, Position height = default,
    UIColor? backgroundColor = null, UIColor? textColor = null,
    UIColor? placeholderColor = null,
    float fontSize = 0,
    Anchor anchor = Anchor.TopLeft,
    Position x = default, Position y = default,
    Pivot? pivot = null,
    Border? border = null,
    Spacing? padding = null,
    Spacing? margin = null,
    BoxSizing boxSizing = BoxSizing.ContentBox,
    string value = null,
    float rotation = 0f,
    BoxShadow? boxShadow = null) {
    var data = new Dictionary<string, string> {
      ["Id"] = id ?? string.Empty,
      ["Placeholder"] = placeholder ?? string.Empty,
      ["Cmd"] = cmd ?? string.Empty,
      ["Anchor"] = anchor.ToString(),
      ["ElemId"] = NextElemId("_sa"),
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (x.HasValue) data["PosX"] = x.Raw;
    if (y.HasValue) data["PosY"] = y.Raw;
    if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (textColor.HasValue) data["TextColor"] = textColor.Value;
    if (placeholderColor.HasValue) data["PlaceholderColor"] = placeholderColor.Value;
    if (fontSize > 0) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    if (value != null) data["Value"] = value;
    ApplyBorder(data, border);
    ApplySpacing(data, "Padding", padding);
    ApplySpacing(data, "Margin", margin);
    if (boxSizing != BoxSizing.ContentBox) data["BoxSizing"] = boxSizing.ToString();
    if (rotation != 0f) data["Rotation"] = rotation.ToString(CultureInfo.InvariantCulture);
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    return Enqueue("AddInput", data);
  }

  /// <summary>
  /// Adds a standalone dropdown selector positioned relative to the window. On selection,
  /// <paramref name="cmd"/> is sent with <c>{<paramref name="id"/>}</c> resolved to the chosen value.
  /// </summary>
  /// <param name="id">Unique identifier used as the placeholder token in <paramref name="cmd"/>.</param>
  /// <param name="options">Pipe-separated option pairs: <c>"Label A:val1|Label B:val2"</c>. If no colon, label equals value.</param>
  /// <param name="cmd">Chat command sent on selection. <c>{id}</c> is replaced with the selected value.</param>
  /// <param name="width">Dropdown header width.</param>
  /// <param name="height">Dropdown header height.</param>
  /// <param name="backgroundColor">Header background color.</param>
  /// <param name="textColor">Header label color.</param>
  /// <param name="dropdownBackgroundColor">Popup panel background color.</param>
  /// <param name="dropdownTextColor">Option label color inside the popup.</param>
  /// <param name="dropdownHoverColor">Option highlight color on hover.</param>
  /// <param name="maxHeight">Maximum popup panel height before scrolling. Default: 200.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit from window default.</param>
  /// <param name="anchor">Anchor point on the parent used to position the element.</param>
  /// <param name="x">X offset relative to the anchor.</param>
  /// <param name="y">Y offset relative to the anchor.</param>
  /// <param name="pivot">Element pivot controlling internal origin.</param>
  /// <param name="border">Optional border around the header.</param>
  /// <param name="padding">Inner spacing between header border and label.</param>
  /// <param name="margin">Outer spacing around the header.</param>
  /// <param name="boxSizing">Whether padding is included in or added to the declared size.</param>
  /// <param name="placeholder">Text shown when no value is selected.</param>
  /// <param name="value">Pre-selected value.</param>
  /// <param name="rotation">Rotation in degrees applied to the element; 0 = no rotation.</param>
  /// <param name="boxShadow">Optional box shadow rendered behind the element.</param>
  public WindowBuilder AddDropdown(string id, string options, string cmd,
    Position width = default, Position height = default,
    UIColor? backgroundColor = null, UIColor? textColor = null,
    UIColor? dropdownBackgroundColor = null, UIColor? dropdownTextColor = null,
    UIColor? dropdownHoverColor = null,
    float maxHeight = 200f,
    float fontSize = 0,
    Anchor anchor = Anchor.TopLeft,
    Position x = default, Position y = default,
    Pivot? pivot = null,
    Border? border = null,
    Spacing? padding = null,
    Spacing? margin = null,
    BoxSizing boxSizing = BoxSizing.ContentBox,
    string placeholder = "Select...",
    string value = null,
    float rotation = 0f,
    BoxShadow? boxShadow = null) {
    var data = new Dictionary<string, string> {
      ["Id"] = id ?? string.Empty,
      ["Options"] = options ?? string.Empty,
      ["Cmd"] = cmd ?? string.Empty,
      ["Anchor"] = anchor.ToString(),
      ["ElemId"] = NextElemId("_sa"),
      ["Placeholder"] = placeholder ?? "Select...",
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (x.HasValue) data["PosX"] = x.Raw;
    if (y.HasValue) data["PosY"] = y.Raw;
    if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
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
    if (rotation != 0f) data["Rotation"] = rotation.ToString(CultureInfo.InvariantCulture);
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    return Enqueue("AddDropdown", data);
  }

  /// <summary>Adds a standalone horizontal progress bar positioned relative to the window.</summary>
  /// <param name="value">Current progress value.</param>
  /// <param name="min">Minimum of the value range.</param>
  /// <param name="max">Maximum of the value range.</param>
  /// <param name="width">Bar width (pixels or percentage string).</param>
  /// <param name="height">Bar height (pixels or percentage string).</param>
  /// <param name="barColor">Fill color for the progress portion.</param>
  /// <param name="backgroundColor">Track/background color.</param>
  /// <param name="barGradient">Gradient applied to the fill portion. Overrides <paramref name="barColor"/> when set.</param>
  /// <param name="backgroundGradient">Gradient applied to the track. Overrides <paramref name="backgroundColor"/> when set.</param>
  /// <param name="anchor">Anchor point on the parent used to position the element.</param>
  /// <param name="x">X offset relative to the anchor.</param>
  /// <param name="y">Y offset relative to the anchor.</param>
  /// <param name="pivot">Element pivot controlling internal origin.</param>
  /// <param name="border">Optional border around the progress bar.</param>
  /// <param name="margin">Outer spacing around the bar.</param>
  /// <param name="rotation">Rotation in degrees applied to the element; 0 = no rotation.</param>
  /// <param name="boxShadow">Optional box shadow rendered behind the element.</param>
  public WindowBuilder AddProgressBar(float value, float min = 0f, float max = 100f,
    Position width = default, Position height = default,
    UIColor? barColor = null, UIColor? backgroundColor = null,
    UIGradient barGradient = default,
    UIGradient backgroundGradient = default,
    Anchor anchor = Anchor.TopLeft,
    Position x = default, Position y = default,
    Pivot? pivot = null,
    Border? border = null,
    Spacing? margin = null,
    float rotation = 0f,
    BoxShadow? boxShadow = null) {
    var data = new Dictionary<string, string> {
      ["Value"] = value.ToString(CultureInfo.InvariantCulture),
      ["Min"] = min.ToString(CultureInfo.InvariantCulture),
      ["Max"] = max.ToString(CultureInfo.InvariantCulture),
      ["Anchor"] = anchor.ToString(),
      ["ElemId"] = NextElemId("_sa"),
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (x.HasValue) data["PosX"] = x.Raw;
    if (y.HasValue) data["PosY"] = y.Raw;
    if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
    if (barColor.HasValue) data["BarColor"] = barColor.Value;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (barGradient.HasValue) data["BarGradient"] = barGradient.Raw;
    if (backgroundGradient.HasValue) data["BgGradient"] = backgroundGradient.Raw;
    ApplyBorder(data, border);
    ApplySpacing(data, "Margin", margin);
    if (rotation != 0f) data["Rotation"] = rotation.ToString(CultureInfo.InvariantCulture);
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    return Enqueue("AddProgressBar", data);
  }

  /// <summary>
  /// Adds an image element loaded from a URL (external or local server endpoint).
  /// The client will fetch and render the image inside the UI.
  /// </summary>
  /// <param name="src">HTTP/HTTPS URL of the image to load.</param>
  /// <param name="width">Image width (pixels or percentage string). Default: auto.</param>
  /// <param name="height">Image height (pixels or percentage string). Default: auto.</param>
  /// <param name="backgroundColor">Color displayed while the image is loading.</param>
  /// <param name="anchor">Anchor point on the parent used to position the element.</param>
  /// <param name="x">X offset relative to the anchor.</param>
  /// <param name="y">Y offset relative to the anchor.</param>
  /// <param name="pivot">Element pivot controlling internal origin.</param>
  /// <param name="fit">How the image fits into the bounds (<see cref="ImageFit"/>).</param>
  /// <param name="border">Optional border around the image.</param>
  /// <param name="margin">Outer spacing around the image.</param>
  /// <param name="rotation">Rotation in degrees applied to the element; 0 = no rotation.</param>
  /// <param name="boxShadow">Optional box shadow rendered behind the element.</param>
  public WindowBuilder AddImage(string src,
    Position width = default, Position height = default,
    UIColor? backgroundColor = null,
    Anchor anchor = Anchor.TopLeft,
    Position x = default, Position y = default,
    Pivot? pivot = null,
    ImageFit fit = ImageFit.Stretch,
    Border? border = null,
    Spacing? margin = null,
    float rotation = 0f,
    BoxShadow? boxShadow = null) {
    var data = new Dictionary<string, string> {
      ["Src"] = src ?? string.Empty,
      ["Fit"] = fit.ToString(),
      ["Anchor"] = anchor.ToString(),
      ["ElemId"] = NextElemId("_sa"),
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (x.HasValue) data["PosX"] = x.Raw;
    if (y.HasValue) data["PosY"] = y.Raw;
    if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    ApplyBorder(data, border);
    ApplySpacing(data, "Margin", margin);
    if (rotation != 0f) data["Rotation"] = rotation.ToString(CultureInfo.InvariantCulture);
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    return Enqueue("AddImage", data);
  }

  /// <summary>
  /// Adds a portrait camera element that displays the local character's camera feed.
  /// Requires the ScarletInterface client mod.
  /// </summary>
  /// <param name="width">Element width in pixels. Default: 200.</param>
  /// <param name="height">Element height in pixels. Default: 200.</param>
  /// <param name="fieldOfView">Camera field of view in degrees. Default: 60.</param>
  /// <param name="orbitAngle">Orbit angle in degrees — rotates the camera around the anchor bone's vertical axis while always pointing at it. At 0° the camera is at its default calibration position. Positive = clockwise when viewed from above. Default: 0.</param>
  /// <param name="distance">Physical distance multiplier from the anchor bone. 1.0 = default calibration distance. Values greater than 1 move the camera farther away. Default: 1.</param>
  /// <param name="anchorBone">Name of the character bone the camera is attached to. Defaults to "Head_JNT".</param>
  /// <param name="backgroundUrl">URL of the background texture rendered behind the character.</param>
  /// <param name="backgroundColor">Solid tint applied to the background quad. Applied even without a URL.</param>
  /// <param name="backgroundSize">World-space size (metres) of the background quad. 1.6 = default. Larger values widen the visible background.</param>
  /// <param name="backgroundOffsetX">UV offset X (0–1) for panning within the background texture. Default: 0.</param>
  /// <param name="backgroundOffsetY">UV offset Y (0–1) for panning within the background texture. Default: 0.</param>
  /// <param name="backgroundScaleX">UV scale X (0–1) for zooming within the background texture. Default: 1.</param>
  /// <param name="backgroundScaleY">UV scale Y (0–1) for zooming within the background texture. Default: 1.</param>
  /// <param name="anchor">Anchor point on the parent used to position the element.</param>
  /// <param name="x">X offset relative to the anchor.</param>
  /// <param name="y">Y offset relative to the anchor.</param>
  /// <param name="pivot">Element pivot controlling internal origin.</param>
  /// <param name="border">Optional border around the camera feed.</param>
  /// <param name="margin">Outer spacing around the element.</param>
  /// <param name="zIndex">Canvas sorting order. Higher = rendered on top.</param>
  public WindowBuilder AddPortraitCamera(
    Position width = default, Position height = default,
    float fieldOfView = 60f,
    float orbitAngle = 0f,
    float distance = 1f,
    string anchorBone = null,
    string backgroundUrl = null,
    UIColor? backgroundColor = null,
    float backgroundSize = 1.6f,
    float backgroundOffsetX = 0f, float backgroundOffsetY = 0f,
    float backgroundScaleX = 1f, float backgroundScaleY = 1f,
    Anchor anchor = Anchor.TopLeft,
    Position x = default, Position y = default,
    Pivot? pivot = null,
    Border? border = null,
    Spacing? margin = null,
    int zIndex = 0) {
    var data = new Dictionary<string, string> {
      ["FOV"] = fieldOfView.ToString(CultureInfo.InvariantCulture),
      ["Anchor"] = anchor.ToString(),
      ["ElemId"] = NextElemId("_sa"),
    };
    if (width.HasValue) data["W"] = width.Raw;
    if (height.HasValue) data["H"] = height.Raw;
    if (x.HasValue) data["PosX"] = x.Raw;
    if (y.HasValue) data["PosY"] = y.Raw;
    if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
    data["Orbit"] = orbitAngle.ToString(CultureInfo.InvariantCulture);
    if (distance != 1f) data["Distance"] = distance.ToString(CultureInfo.InvariantCulture);
    if (anchorBone != null) data["AnchorBone"] = anchorBone;
    if (backgroundUrl != null) data["BgUrl"] = backgroundUrl;
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (backgroundSize != 1.6f) data["BgSize"] = backgroundSize.ToString(CultureInfo.InvariantCulture);
    if (backgroundOffsetX != 0f) data["BgUvX"] = backgroundOffsetX.ToString(CultureInfo.InvariantCulture);
    if (backgroundOffsetY != 0f) data["BgUvY"] = backgroundOffsetY.ToString(CultureInfo.InvariantCulture);
    if (backgroundScaleX != 1f) data["BgUvW"] = backgroundScaleX.ToString(CultureInfo.InvariantCulture);
    if (backgroundScaleY != 1f) data["BgUvH"] = backgroundScaleY.ToString(CultureInfo.InvariantCulture);
    ApplyBorder(data, border);
    ApplySpacing(data, "Margin", margin);
    if (zIndex != 0) data["ZIndex"] = zIndex.ToString(CultureInfo.InvariantCulture);
    return Enqueue("AddPortraitCamera", data);
  }

  /// <summary>
  /// Adds a pre-styled close button (×) that closes the window when clicked.
  ///</summary>
  /// <param name="backgroundColor">Button background color.</param>
  /// <param name="textColor">Color of the × icon.</param>
  /// <param name="padding">Inner spacing between the × icon and the button edge.</param>
  /// <param name="fontSize">Font size in pixels. 0 = inherit from window default.</param>
  /// <param name="anchor">Anchor point on the parent used to position the element.</param>
  /// <param name="x">X offset relative to the anchor.</param>
  /// <param name="y">Y offset relative to the anchor.</param>
  /// <param name="pivot">Element pivot controlling internal origin.</param>
  /// <param name="border">Optional border around the close button.</param>
  /// <param name="margin">Outer spacing around the close button.</param>
  /// <param name="rotation">Rotation in degrees applied to the element; 0 = no rotation.</param>
  /// <param name="boxShadow">Optional box shadow rendered behind the element.</param>
  public WindowBuilder AddCloseButton(
    UIColor? backgroundColor = null, UIColor? textColor = null,
    Spacing? padding = null, float fontSize = 0,
    Anchor anchor = Anchor.TopRight,
    Position x = default, Position y = default,
    Pivot? pivot = null,
    Border? border = null,
    Spacing? margin = null,
    float rotation = 0f,
    BoxShadow? boxShadow = null) {
    var data = new Dictionary<string, string> {
      ["Anchor"] = anchor.ToString(),
      ["ElemId"] = NextElemId("_sa"),
    };
    if (x.HasValue) data["PosX"] = x.Raw;
    if (y.HasValue) data["PosY"] = y.Raw;
    if (pivot.HasValue) data["Pivot"] = pivot.Value.ToString();
    if (backgroundColor.HasValue) data["BgColor"] = backgroundColor.Value;
    if (textColor.HasValue) data["TextColor"] = textColor.Value;
    ApplySpacing(data, "Padding", padding);
    if (fontSize > 0) data["FontSize"] = fontSize.ToString(CultureInfo.InvariantCulture);
    ApplyBorder(data, border);
    ApplySpacing(data, "Margin", margin);
    if (rotation != 0f) data["Rotation"] = rotation.ToString(CultureInfo.InvariantCulture);
    if (boxShadow.HasValue) data["BoxShadow"] = boxShadow.Value.Raw;
    return Enqueue("AddCloseButton", data);
  }

  // Window control — these set a flag; the action packet is always sent LAST by Send(),
  // regardless of call order. Use Send(WindowAction) to pass the action inline.
  /// <summary>Mark the window to be opened when <see cref="Send"/> is called.</summary>
  public WindowBuilder Open() { _pendingAction = WindowAction.Open; return this; }
  /// <summary>Mark the window to be closed when <see cref="Send"/> is called.</summary>
  public WindowBuilder Close() { _pendingAction = WindowAction.Close; return this; }
  /// <summary>Mark the window to be cleared (remove elements) when <see cref="Send"/> is called.</summary>
  public WindowBuilder Clear() { _pendingAction = WindowAction.Clear; return this; }
  /// <summary>Mark the window to be reset (destroy and recreate) when <see cref="Send"/> is called.</summary>
  public WindowBuilder Reset() { _pendingAction = WindowAction.Reset; return this; }

  /// <summary>
  /// Sends all queued element packets, then the lifecycle action packet (if any).
  /// Pass <paramref name="action"/> to specify the action inline — it takes precedence over
  /// any prior <see cref="Open"/>, <see cref="Close"/>, <see cref="Clear"/>, or <see cref="Reset"/> call.
  /// </summary>
  /// <param name="action">Action to perform after sending elements. Defaults to <see cref="WindowAction.Open"/>. Overridden by any prior fluent call (<see cref="Open"/>, <see cref="Close"/>, etc.).</param>
  public void Send(WindowAction action = WindowAction.Open) {
    // Fluent flag wins when set; fall back to caller-supplied action.
    var resolved = _pendingAction != WindowAction.None ? _pendingAction : action;

    // Send all element packets first (order guaranteed).
    while (_queue.Count > 0) {
      var packet = _queue.Dequeue();
      if (_player != null) PacketManager.SendPacket(_player, packet);
      else PacketManager.SendPacketToAll(packet);
    }

    // Action packet last � always after every element.
    if (resolved != WindowAction.None) {
      string typeName = resolved.ToString(); // "Open", "Close", etc.
      var actionPacket = new ScarletPacket {
        Type = typeName, Plugin = _plugin, Window = _windowId ?? string.Empty, Data = [],
      };
      if (_player != null) PacketManager.SendPacket(_player, actionPacket);
      else PacketManager.SendPacketToAll(actionPacket);
    }

    _pendingAction = WindowAction.None;
  }

  /// <summary>
  /// Sets a 9-piece tiled image frame on the window (4 corners, 4 borders, background).
  /// Pieces that overflow their allocated area are clipped; borders and background are
  /// tiled (pixel-perfect at 1920×1080) instead of stretched to preserve texture quality.
  /// Calling this method with all parameters null removes an existing frame.
  /// </summary>
  /// <param name="topLeftCorner">URL for the top-left corner image.</param>
  /// <param name="topRightCorner">URL for the top-right corner image.</param>
  /// <param name="bottomLeftCorner">URL for the bottom-left corner image.</param>
  /// <param name="bottomRightCorner">URL for the bottom-right corner image.</param>
  /// <param name="topBorder">URL for the top edge image (tiled horizontally).</param>
  /// <param name="bottomBorder">URL for the bottom edge image (tiled horizontally).</param>
  /// <param name="leftBorder">URL for the left edge image (tiled vertically).</param>
  /// <param name="rightBorder">URL for the right edge image (tiled vertically).</param>
  /// <param name="background">URL for the center background image.</param>
  /// <param name="backgroundRepeat">If <c>true</c>, the background image tiles to fill the area.
  /// If <c>false</c> (default), it is stretched to fill the area as a single image.</param>
  /// <param name="cornerSize">Width and height of each corner in canvas units (default 32).</param>
  /// <param name="frameExpand">Pixels to expand the frame beyond the window boundary on each side.
  /// Use this when your sprite images have built-in transparent bleed space around the artwork:
  /// setting frameExpand to the bleed width pushes the transparent area outside the window
  /// background so the visible border aligns flush with the window edge (default 0).</param>
  public WindowBuilder AddCustomTexture(
    string topLeftCorner = null,
    string topRightCorner = null,
    string bottomLeftCorner = null,
    string bottomRightCorner = null,
    string topBorder = null,
    string bottomBorder = null,
    string leftBorder = null,
    string rightBorder = null,
    string background = null,
    bool backgroundRepeat = false,
    int cornerSize = 32,
    int frameExpand = 0) {
    var data = new Dictionary<string, string> {
      ["CornerSize"] = cornerSize.ToString(CultureInfo.InvariantCulture),
    };
    if (topLeftCorner != null) data["TLCorner"] = topLeftCorner;
    if (topRightCorner != null) data["TRCorner"] = topRightCorner;
    if (bottomLeftCorner != null) data["BLCorner"] = bottomLeftCorner;
    if (bottomRightCorner != null) data["BRCorner"] = bottomRightCorner;
    if (topBorder != null) data["TopBorder"] = topBorder;
    if (bottomBorder != null) data["BottomBorder"] = bottomBorder;
    if (leftBorder != null) data["LeftBorder"] = leftBorder;
    if (rightBorder != null) data["RightBorder"] = rightBorder;
    if (background != null) data["BgImage"] = background;
    if (backgroundRepeat) data["BgRepeat"] = "true";
    if (frameExpand != 0) data["FrameExpand"] = frameExpand.ToString(CultureInfo.InvariantCulture);
    return Enqueue("SetWindowTexture", data);
  }

  // Internal
  internal void EnqueuePacket(string type, Dictionary<string, string> data) =>
    Enqueue(type, data);

  /// <summary>
  /// Attaches a tooltip to the last-added element. The tooltip is an existing window identified by
  /// <paramref name="tooltipWindowId"/> that is shown on hover and hidden when the mouse leaves.
  /// </summary>
  /// <param name="tooltipWindowId">ID of the window to show as tooltip.</param>
  public WindowBuilder WithTooltip(string tooltipWindowId) {
    if (string.IsNullOrEmpty(_lastElemId)) return this;
    var data = new Dictionary<string, string> {
      ["TargetElemId"] = _lastElemId,
      ["TooltipWindowId"] = tooltipWindowId ?? string.Empty,
    };
    return Enqueue("AddTooltip", data);
  }

  WindowBuilder Enqueue(string type, Dictionary<string, string> data) {
    _queue.Enqueue(new ScarletPacket {
      Type = type,
      Plugin = _plugin,
      Window = _windowId ?? string.Empty,
      Data = data,
    });
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
