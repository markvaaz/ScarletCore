using System.Collections.Generic;
using System.Globalization;
using ScarletCore.Services;
using ScarletCore.Interface.Models;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Fluent builder for sending operations against existing game GameObjects
/// identified by their normalized hierarchy path (without "(Clone)" suffixes).
/// </summary>
public class NativeElementBuilder {
  readonly string _plugin;
  readonly PlayerData _player; // null = broadcast
  readonly string _path;
  readonly Dictionary<string, string> _data = new();

  internal NativeElementBuilder(string plugin, PlayerData player, string path) {
    _plugin = plugin;
    _player = player;
    _path = path;
  }

  /// <summary>Moves the element's RectTransform to the given anchored position.</summary>
  public NativeElementBuilder SetPosition(float x, float y) {
    _data["PosX"] = x.ToString(CultureInfo.InvariantCulture);
    _data["PosY"] = y.ToString(CultureInfo.InvariantCulture);
    return this;
  }

  /// <summary>Sets only the X component of the anchored position.</summary>
  public NativeElementBuilder SetPositionX(float x) {
    _data["PosX"] = x.ToString(CultureInfo.InvariantCulture);
    return this;
  }

  /// <summary>Sets only the Y component of the anchored position.</summary>
  public NativeElementBuilder SetPositionY(float y) {
    _data["PosY"] = y.ToString(CultureInfo.InvariantCulture);
    return this;
  }

  /// <summary>Shows or hides the target GameObject.</summary>
  public NativeElementBuilder SetActive(bool active) {
    _data["Active"] = active ? "true" : "false";
    return this;
  }

  // ── RectTransform — relative positioning ──────────────────────────────────

  /// <summary>
  /// Positions this element relative to another element's anchored position.<br/>
  /// Final pos = (ref.x + offsetX + self.width * selfWidthFactor,
  ///              ref.y + offsetY + self.height * selfHeightFactor)<br/>
  /// Example — slot45 below slot4 with gap of 18px:<br/>
  ///   .SetPositionRelativeTo(slot4Path, offsetY: -18f, selfHeightFactor: -1f)
  /// </summary>
  public NativeElementBuilder SetPositionRelativeTo(
      string refPath,
      float offsetX = 0f, float offsetY = 0f,
      float selfWidthFactor = 0f, float selfHeightFactor = 0f) {
    _data["PosRelTo"] = refPath;
    _data["PosRelOffsetX"] = F(offsetX);
    _data["PosRelOffsetY"] = F(offsetY);
    _data["PosRelSelfWFactor"] = F(selfWidthFactor);
    _data["PosRelSelfHFactor"] = F(selfHeightFactor);
    return this;
  }

  // ── RectTransform — size / anchors / pivot ────────────────────────────────

  /// <summary>Sets the RectTransform sizeDelta.</summary>
  public NativeElementBuilder SetSizeDelta(float w, float h) {
    _data["SizeDeltaW"] = F(w); _data["SizeDeltaH"] = F(h); return this;
  }

  /// <summary>Sets the RectTransform anchorMin.</summary>
  public NativeElementBuilder SetAnchorMin(float x, float y) {
    _data["AnchorMinX"] = F(x); _data["AnchorMinY"] = F(y); return this;
  }

  /// <summary>Sets the RectTransform anchorMax.</summary>
  public NativeElementBuilder SetAnchorMax(float x, float y) {
    _data["AnchorMaxX"] = F(x); _data["AnchorMaxY"] = F(y); return this;
  }

  /// <summary>Sets the RectTransform pivot.</summary>
  public NativeElementBuilder SetPivot(float x, float y) {
    _data["PivotX"] = F(x); _data["PivotY"] = F(y); return this;
  }

  /// <summary>
  /// Disables (or re-enables) the <c>LayoutGroup</c> component on the <b>parent</b> of the
  /// target element before applying position changes, so Unity doesn't re-override them.
  /// Required when the parent is a GridLayoutGroup / HorizontalLayoutGroup / VerticalLayoutGroup.
  /// </summary>
  public NativeElementBuilder DisableParentLayout(bool disable = true) {
    _data["DisableParentLayout"] = disable ? "true" : "false"; return this;
  }

  /// <summary>
  /// Disables (or re-enables) the <c>LayoutGroup</c> component on the target GO itself.
  /// Use this when you target the container directly rather than a child slot.
  /// </summary>
  public NativeElementBuilder DisableLayout(bool disable = true) {
    _data["DisableLayout"] = disable ? "true" : "false"; return this;
  }

  // ── Anchor / screen-relative positioning ─────────────────────────────────

  /// <summary>
  /// Positions the element relative to a named anchor point of its parent (or screen
  /// when the parent is the canvas root).<br/>
  /// Equivalent to the <c>anchor</c> + <c>x</c>/<c>y</c> parameters in
  /// <see cref="WindowBuilder.SetWindow"/>.<br/>
  /// Example — pin to screen top-left with a 10 px offset:<br/>
  ///   <code>.SetAnchor(Anchor.TopLeft, x: 10f, y: -10f)</code>
  /// </summary>
  public NativeElementBuilder SetAnchor(Anchor anchor, Position x = default, Position y = default, Pivot? pivot = null) {
    _data["Anchor"] = anchor.ToString();
    if (x.HasValue) _data["PosX"] = x.Raw;
    if (y.HasValue) _data["PosY"] = y.Raw;
    if (pivot.HasValue) _data["Pivot"] = pivot.Value.ToString();
    return this;
  }

  // ── Opacity ───────────────────────────────────────────────────────────────

  /// <summary>
  /// Sets the opacity of the element and all its children via a <c>CanvasGroup</c> component.
  /// A value of <c>0</c> is fully transparent; <c>1</c> is fully opaque.
  /// A <c>CanvasGroup</c> is added automatically if the target does not already have one.
  /// </summary>
  public NativeElementBuilder SetOpacity(float alpha) {
    _data["Opacity"] = F(alpha); return this;
  }

  /// <summary>Rotates the element around its pivot. Accepts -360 to 360 degrees.</summary>
  public NativeElementBuilder SetRotation(float degrees) {
    _data["Rotation"] = F(degrees); return this;
  }

  /// <summary>
  /// Sets the Canvas sorting order on this element, controlling render layering.
  /// Adds a <c>Canvas</c> component (with <c>overrideSorting = true</c>) if not already present.
  /// Higher values render on top of lower values.
  /// </summary>
  public NativeElementBuilder SetZIndex(int zIndex) {
    _data["ZIndex"] = zIndex.ToString(CultureInfo.InvariantCulture); return this;
  }

  // ── Individual size setters ────────────────────────────────────────────────

  /// <summary>Sets only the width component of the RectTransform sizeDelta.</summary>
  public NativeElementBuilder SetWidth(float w) {
    _data["SizeDeltaW"] = F(w); return this;
  }

  /// <summary>Sets only the height component of the RectTransform sizeDelta.</summary>
  public NativeElementBuilder SetHeight(float h) {
    _data["SizeDeltaH"] = F(h); return this;
  }

  // ── Hierarchy / parenting ────────────────────────────────────────────────

  /// <summary>
  /// Reparents the target element to another existing native UI element path.
  /// Example:
  /// <code>.ChangeParent("HUDCanvas/BottomBarCanvas/BottomBar/Content/")</code>
  /// </summary>
  /// <param name="newParentPath">
  /// Normalized hierarchy path of the new parent.
  /// Trailing slashes are accepted and ignored.
  /// </param>
  public NativeElementBuilder ChangeParent(string newParentPath) {
    _data["ChangeParentPath"] = newParentPath;
    return this;
  }

  // ── Child elements ────────────────────────────────────────────────────────

  int _childCount;

  /// <summary>
  /// Creates or updates a named child GO inside the target element.
  /// Call <see cref="ChildElementBuilder.Done"/> to return here and continue chaining.
  /// </summary>
  public ChildElementBuilder AddOrUpdateChild(string childName) =>
    new(this, _childCount++, childName);

  /// <summary>Destroys a named direct child GO of the target element.</summary>
  public NativeElementBuilder DestroyChild(string childName) {
    _data[$"DestroyChild[{_childCount++}]"] = childName;
    return this;
  }

  // ── Internal helpers ──────────────────────────────────────────────────────

  internal static string F(float v) => v.ToString(CultureInfo.InvariantCulture);
  internal Dictionary<string, string> Data => _data;

  // ── Send / Clear ──────────────────────────────────────────────────────────

  /// <summary>Sends all queued operations to the target player (or all players if constructed via ForAll).</summary>
  public void Send() {
    var packet = new ScarletPacket {
      Type = "NativeElement",
      Plugin = _plugin,
      // Window field carries the normalized path as the target identifier.
      Window = _path,
      Data = _data,
    };

    if (_player != null)
      PacketManager.SendPacket(_player, packet);
    else
      PacketManager.SendPacketToAll(packet);
  }

  /// <summary>
  /// Removes the persistent listener for this path so the server no longer
  /// re-applies modifications when the UI reloads.
  /// </summary>
  public void Clear() {
    var packet = new ScarletPacket {
      Type = "NativeElementClear",
      Plugin = _plugin,
      Window = _path,
      Data = new Dictionary<string, string>(),
    };

    if (_player != null)
      PacketManager.SendPacket(_player, packet);
    else
      PacketManager.SendPacketToAll(packet);
  }
}

/// <summary>
/// Fluent builder for a single child element inside a <see cref="NativeElementBuilder"/> target.
/// Call <see cref="Done"/> to return to the parent builder.
/// </summary>
public class ChildElementBuilder {
  readonly NativeElementBuilder _parent;
  readonly string _prefix; // e.g. "Child[0]."

  internal ChildElementBuilder(NativeElementBuilder parent, int index, string childName) {
    _parent = parent;
    _prefix = $"Child[{index}].";
    Set("Name", childName);
  }

  void Set(string key, string val) => _parent.Data[_prefix + key] = val;
  static string F(float v) => NativeElementBuilder.F(v);

  // ── RectTransform ──────────────────────────────────────────────────────────

  /// <summary>Sets the child's anchored position.</summary>
  public ChildElementBuilder SetPosition(float x, float y) {
    Set("PosX", F(x)); Set("PosY", F(y)); return this;
  }

  /// <summary>Sets the child's RectTransform sizeDelta.</summary>
  public ChildElementBuilder SetSizeDelta(float w, float h) {
    Set("SizeDeltaW", F(w)); Set("SizeDeltaH", F(h)); return this;
  }

  /// <summary>Sets the child's anchorMin (0–1 range).</summary>
  public ChildElementBuilder SetAnchorMin(float x, float y) {
    Set("AnchorMinX", F(x)); Set("AnchorMinY", F(y)); return this;
  }

  /// <summary>Sets the child's anchorMax (0–1 range).</summary>
  public ChildElementBuilder SetAnchorMax(float x, float y) {
    Set("AnchorMaxX", F(x)); Set("AnchorMaxY", F(y)); return this;
  }

  /// <summary>Sets the child's pivot (0–1 range, e.g. (0.5, 0.5) = center).</summary>
  public ChildElementBuilder SetPivot(float x, float y) {
    Set("PivotX", F(x)); Set("PivotY", F(y)); return this;
  }

  /// <summary>Shows or hides the child GO.</summary>
  public ChildElementBuilder SetActive(bool active) {
    Set("Active", active ? "true" : "false"); return this;
  }

  // ── Anchor / screen-relative positioning ─────────────────────────────────

  /// <summary>
  /// Positions the child element relative to a named anchor point of its parent.
  /// See <see cref="NativeElementBuilder.SetAnchor"/> for full documentation.
  /// </summary>
  public ChildElementBuilder SetAnchor(Anchor anchor, Position x = default, Position y = default, Pivot? pivot = null) {
    Set("Anchor", anchor.ToString());
    if (x.HasValue) Set("PosX", x.Raw);
    if (y.HasValue) Set("PosY", y.Raw);
    if (pivot.HasValue) Set("Pivot", pivot.Value.ToString());
    return this;
  }

  // ── Opacity ───────────────────────────────────────────────────────────────

  /// <summary>
  /// Sets the opacity of the child element and all its children via a <c>CanvasGroup</c>.
  /// Range 0 (fully transparent) to 1 (fully opaque).
  /// </summary>
  public ChildElementBuilder SetOpacity(float alpha) {
    Set("Opacity", F(alpha)); return this;
  }

  /// <summary>Rotates the child element around its pivot. Accepts -360 to 360 degrees.</summary>
  public ChildElementBuilder SetRotation(float degrees) {
    Set("Rotation", F(degrees)); return this;
  }

  // ── Individual size setters ────────────────────────────────────────────────

  /// <summary>Sets only the width component of the child's RectTransform sizeDelta.</summary>
  public ChildElementBuilder SetWidth(float w) {
    Set("SizeDeltaW", F(w)); return this;
  }

  /// <summary>Sets only the height component of the child's RectTransform sizeDelta.</summary>
  public ChildElementBuilder SetHeight(float h) {
    Set("SizeDeltaH", F(h)); return this;
  }

  // ── Hierarchy / parenting ────────────────────────────────────────────────

  /// <summary>
  /// Reparents this child element to another existing native UI element path.
  /// </summary>
  public ChildElementBuilder ChangeParent(string newParentPath) {
    Set("ChangeParentPath", newParentPath);
    return this;
  }

  // ── TextMeshPro ────────────────────────────────────────────────────────────

  /// <summary>
  /// Adds or updates a <c>TextMeshProUGUI</c> component on the child.
  /// <para>
  /// <paramref name="fontStyle"/>: "Normal" | "Bold" | "Italic" | "BoldAndItalic"<br/>
  /// <paramref name="alignment"/>: "Left" | "Center" | "Right" | "TopLeft" | "TopCenter" … (TextAlignmentOptions names)
  /// </para>
  /// </summary>
  public ChildElementBuilder AddText(
      string text,
      float? fontSize = null,
      string fontStyle = null,
      string alignment = null,
      float? colorR = null, float? colorG = null, float? colorB = null, float? colorA = null,
      bool raycastTarget = false) {
    Set("Text", text);
    if (fontSize != null) Set("FontSize", F(fontSize.Value));
    if (fontStyle != null) Set("FontStyle", fontStyle);
    if (alignment != null) Set("TextAlign", alignment);
    if (colorR != null) Set("ColorR", F(colorR.Value));
    if (colorG != null) Set("ColorG", F(colorG.Value));
    if (colorB != null) Set("ColorB", F(colorB.Value));
    if (colorA != null) Set("ColorA", F(colorA.Value));
    Set("RaycastTarget", raycastTarget ? "true" : "false");
    return this;
  }

  // ── Back to parent ─────────────────────────────────────────────────────────

  /// <summary>Finishes configuring this child and returns to the parent builder for further chaining.</summary>
  public NativeElementBuilder Done() => _parent;
}