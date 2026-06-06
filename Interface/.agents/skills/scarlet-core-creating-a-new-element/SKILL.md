---
name: scarlet-core-creating-a-new-element
description: This guide walks you through every file you must touch to add a complete, fully functional element. The system is split across two projects:\n- **ScarletCore** — server-side C#/.NET 6 - declares the element model and serializes it to packets.\n- **ScarletInterface** — client-side Unity/BepInEx/IL2CPP C# - receives packets and creates Unity GameObjects.
---

# Adding a New UI Element to ScarletInterface

This guide walks you through every file you must touch to add a complete, fully functional element. The system is split across two projects:

- **ScarletCore** — server-side C#/.NET 6: declares the element model and serializes it to packets.
- **ScarletInterface** — client-side Unity/BepInEx/IL2CPP C#: receives packets and creates Unity GameObjects.

---

## Overview of the Full Pipeline

```
Server (ScarletCore)                      Client (ScarletInterface)
─────────────────────────────────────     ──────────────────────────────────────────────
Elements/MyElem.cs     (data model)  →    Handlers/MyElemHandler.cs  (create GO)
ElementSerializer.cs   (→ packets)   →    PacketService.cs           (dispatch)
                                          KeyMap.cs                  (token decoding)
```

The server serializes a `Window` tree into a `List<ScarletPacket>` using short 2–4 char token keys.  
The client receives them via chat, decodes tokens back to long names, and dispatches to a handler.

---

## Step 1 — Server: Element Model (`ScarletCore/Interface/Elements/`)

Create `MyElem.cs`. Inherit `UIElement` — you get `Width`, `Height`, `Background`, `Border`,
`Padding`, `Margin`, `Rotation`, `BoxShadow`, `Anchor`, `Position`, `Pivot`, `ElemId`, `Tooltip`
for free. Add only element-specific properties.

```csharp
// ScarletCore/Interface/Elements/MyElem.cs
using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

public class MyElem : UIElement
{
    /// <summary>Primary text label.</summary>
    public string Label { get; set; }

    /// <summary>Value between 0 and 1.</summary>
    public float Value { get; set; }

    // If the element has interactive states, add separate backgrounds:
    public UIBackground? HoverBackground { get; set; }
}
```

**Rules:**
- All properties must have a default that means "not set" (null, 0, empty string).
  Properties that ARE set will be serialized; unset properties emit nothing.
- Use `UIColor?`, `UIBackground?`, `Border?`, `Spacing?` — all nullable structs.
- If the element renders text, implement `ITextElement` (see `Button.cs` or `Text.cs`).
- No serialization logic here. This class is pure data.

---

## Step 2 — Server: Serializer (`ScarletCore/Interface/ElementSerializer.cs`)

### 2a — Choose a packet type token

Pick a 2–3 char uppercase token that isn't already in use. Check `KeyMap.Types` in
`ScarletInterface/KeyMap.cs`.

Example: `"AME"` → `"AddMyElem"`

### 2b — Register data key tokens

For each new property you need to send, choose a short lowercase token (2–4 chars) that
isn't already used. Check the existing keys at the top of `KeyMap.cs`.

| Property | Token |
|---|---|
| Label  | `lbl` |
| Value  | `vl` |

You add these to `KeyMap.cs` in Step 4.

### 2c — Add the serializer method

Add a private static method `SerializeMyElem`. All shared helpers are available:
- `SerializeBackground(d, elem.Background)` — emits `bcl/bgr/bim/bsp/bif/bfr/...`
- `SerializeBorder(d, elem.Border)` — emits `dc/dw/dr`
- `SerializeSpacing(d, 'p', elem.Padding)` — emits `pt/pr/pb/pl`
- `SerializeSpacing(d, 'm', elem.Margin)` — emits `mt/mr/mb/ml`
- `SerializeTextStyle(d, elem)` — emits `tc/fs/fn/tg/ts/to` (requires `ITextElement`)
- `SerializeHoverBackground(d, elem.HoverBackground, null)` — emits `hcl/hgr/...`
- `SerializePosition(d, elem.Position)` — emits `px/py/zi`
- `SerializeTooltip(packets, plugin, windowId, elem, elemId)` — emits `AO` packet

```csharp
static void SerializeMyElem(List<ScarletPacket> packets, string plugin, string windowId,
    MyElem elem, string parentId, Dictionary<string, int> elemCounters)
{
    // Generate a stable element ID
    if (!elemCounters.TryGetValue(parentId, out _)) elemCounters[parentId] = 0;
    string elemId = elem.ElemId ?? $"{parentId}_me{elemCounters[parentId]++}";

    var d = new Dictionary<string, string>(16)
    {
        ["ei"]  = elemId,
        ["pa"]  = parentId,   // parent row/container ID
    };

    // Size (from UIElement base)
    if (elem.Width.HasValue)  d["w"]  = elem.Width.Raw;
    if (elem.Height.HasValue) d["h"]  = elem.Height.Raw;

    // Element-specific keys
    if (!string.IsNullOrEmpty(elem.Label)) d["lbl"] = elem.Label;
    if (elem.Value != 0f) d["vl"] = F(elem.Value);

    // Shared helpers
    SerializeBackground(d, elem.Background);
    SerializeBorder(d, elem.Border);
    SerializeSpacing(d, 'p', elem.Padding);
    SerializeSpacing(d, 'm', elem.Margin);
    SerializeHoverBackground(d, elem.HoverBackground, null);
    SerializePosition(d, elem.Position);
    if (elem.Rotation != 0f) d["ro"] = F(elem.Rotation);
    if (elem.BoxShadow.HasValue) d["bx"] = elem.BoxShadow.Value.Raw;
    if (elem.Pivot.HasValue) d["pv"] = elem.Pivot.Value.ToString();

    packets.Add(Packet(plugin, windowId, "AME", d));

    SerializeTooltip(packets, plugin, windowId, elem, elemId);
}
```

### 2d — Wire it into the dispatch tree

Find the large `foreach (var child in window.Children)` block in `Serialize()`, and add
a branch for your element type:

```csharp
else if (child is MyElem myElem)
    SerializeMyElem(packets, plugin, windowId, myElem, /* parentId */, elemCounters);
```

If the element can also appear inside a `Row` (most leaf elements do), find `SerializeRowElement`
and add the same `else if` there. If it can appear inside a `Container`, add it to
`SerializeContainerElement` too.

---

## Step 3 — Client: Handler (`ScarletInterface/Handlers/`)

Create `MyElemHandler.cs`. Every handler must implement at minimum:
- `Handle(data, win)` — create the GO for a brand-new element
- `DiffPatch(oldData, newData, go, win)` — update an existing GO when the server resends it

### Handle (Create)

```csharp
// ScarletInterface/Handlers/MyElemHandler.cs
using ScarletInterface.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ScarletInterface.Utils.UIBackgroundHelper;
using static ScarletInterface.Utils.UIParseHelpers;

namespace ScarletInterface.Handlers;

internal static class MyElemHandler {

    internal static void Handle(Dictionary<string, string> data, ScarletWindow win) {
        string elemId = G(data, "ElemId", "");

        // ── DiffPatch guard (re-send without Reset) ──────────────────────────
        // Check this FIRST — avoids creating a duplicate GO if the window is
        // re-opened without a prior Reset.
        if (win.TryGetForDiffPatch(elemId, "AddMyElem", out var existing, out var old)) {
            DiffPatch(old, data, existing, win);
            win.TouchElem(elemId, data);
            return;
        }

        // ── Route to parent ──────────────────────────────────────────────────
        // RouteElement resolves Parent/Anchor to a transform and tells you whether
        // this element sits inside a Row (flow) or is standalone (absolute).
        var (parent, isInRow) = win.RouteElement(data, out var parentId);
        if (isInRow) win.InjectSpacerIfNeeded(parentId);

        // ── Create root GO ───────────────────────────────────────────────────
        float w = ParseFloat(G(data, "W", "100"), 100f);
        float h = ParseFloat(G(data, "H", "30"), 30f);
        int   br = (int)ParseFloat(G(data, "BorderRadius", "6"), 6f);

        var go = new GameObject($"MyElem_{elemId}");
        go.transform.SetParent(parent, false);

        var rect = AddComp<RectTransform>(go);
        rect.sizeDelta = new Vector2(w, h);

        // ── Background ───────────────────────────────────────────────────────
        string bgColor  = G(data, "BgColor", "");
        string bgGrad   = G(data, "BgGradient", "");
        string bgImage  = G(data, "BgImage", "");
        string bgSprite = G(data, "BgSprite", "");
        var    bgFit    = ParseImageFit(G(data, "BgImageFit", "Stretch"));
        bool hasBgMedia = !string.IsNullOrEmpty(bgImage) || !string.IsNullOrEmpty(bgSprite);

        var bgImg = AddComp<Image>(go);
        bgImg.sprite         = GetRoundedSprite(br);
        bgImg.type           = Image.Type.Sliced;
        bgImg.color          = hasBgMedia ? Color.clear : ParseColor(bgColor, Color.clear);
        bgImg.raycastTarget  = false;   // true only if the element must receive mouse events

        UIBackgroundAnimHelper.Setup(go, data, "Bg", win);

        if (!hasBgMedia && !string.IsNullOrEmpty(bgGrad))
            ApplyGradientToImage(bgImg, bgGrad, Math.Max(1, (int)w), Math.Max(1, (int)h), br);

        if (hasBgMedia) {
            var layer = ButtonHandler.BuildButtonImageLayer("BgImageLayer_Normal",
                go.transform, bgImage, bgSprite, bgFit);
            layer.transform.SetSiblingIndex(0);
            AddComp<LayoutElement>(layer).ignoreLayout = true;
        }

        // ── Border ───────────────────────────────────────────────────────────
        float  bWidth    = ParseFloat(G(data, "BorderWidth", "0"), 0f);
        string bColorStr = G(data, "BorderColor", "");
        if (bWidth > 0 && !string.IsNullOrEmpty(bColorStr)) {
            int bwPx = Math.Max(1, (int)Math.Round(bWidth));
            var borderGO = CreateBorderContainer("MyElemBorder", go.transform,
                ParseColor(bColorStr, Color.clear), bwPx, br, (int)w, (int)h);
            var bRect = borderGO.GetComponent<RectTransform>();
            bRect.anchorMin  = Vector2.zero;
            bRect.anchorMax  = Vector2.one;
            bRect.offsetMin  = new Vector2(-bwPx, -bwPx);
            bRect.offsetMax  = new Vector2(bwPx, bwPx);
            AddComp<LayoutElement>(borderGO).ignoreLayout = true;
        }

        // ── BoxShadow ────────────────────────────────────────────────────────
        string bxRaw = G(data, "BoxShadow", "");
        if (!string.IsNullOrEmpty(bxRaw))
            CreateElementBoxShadow(go, bxRaw, w, h, br);

        // ── Element-specific content ─────────────────────────────────────────
        string label = G(data, "Label", "");
        if (!string.IsNullOrEmpty(label)) {
            // Build a TextMeshProUGUI child... (see TextHandler for a full example)
        }

        // ── LayoutElement (makes the GO participate in Row/Container layout) ──
        var le = AddComp<LayoutElement>(go);
        le.preferredWidth  = w;
        le.preferredHeight = h;

        // ── Standalone (absolute) positioning ────────────────────────────────
        if (!isInRow && data.ContainsKey("Anchor"))
            ApplyAnchorPosition(rect,
                G(data, "Anchor", "TopLeft"), G(data, "PosX", "0"), G(data, "PosY", "0"),
                win.ConfiguredW, win.ConfiguredH, G(data, "Pivot", ""));

        if (G(data, "Rotation", "") is { Length: > 0 } rotStr)
            ApplyRotation(rect, rotStr);

        // ── Track ────────────────────────────────────────────────────────────
        // TrackElem MUST be the last call. It writes ElemId into _elemGOs/_elemData
        // and adds it to _batchElemOrder so PruneStaleElements doesn't destroy it.
        win.TrackElem(elemId, go, data);

        // Row height accounting (call AFTER TrackElem)
        if (isInRow) win.TrackRowElemH(elemId, parentId, h);
    }

    // ── DiffPatch ─────────────────────────────────────────────────────────────
    internal static void DiffPatch(Dictionary<string, string> oldData,
        Dictionary<string, string> newData, GameObject go, ScarletWindow win)
    {
        // Update only what changed. Use Changed(old, new, "Key") to guard each block.

        if (Changed(oldData, newData, "BgColor")) {
            var img = go.GetComponent<Image>();
            if (img != null) img.color = ParseColor(G(newData, "BgColor", ""), Color.clear);
        }

        // ... diff other properties similarly

        if (Changed(oldData, newData, "Rotation"))
            ApplyRotation(go.GetComponent<RectTransform>(), G(newData, "Rotation", "0"));

        UIBackgroundAnimHelper.Diff(go, oldData, newData, "Bg", win);
    }
}
```

---

## Step 4 — Client: KeyMap (`ScarletInterface/KeyMap.cs`)

Add the packet type token to `Types` and the data key tokens to `Keys`.

```csharp
// In Types dictionary
["AME"] = "AddMyElem",

// In Keys dictionary (pick a logical section to insert near related keys)
["lbl"] = "Label",
["vl"]  = "Value",
```

**Constraints:**
- Type tokens: 2–3 chars, uppercase. Must not clash with any existing entry.
- Data key tokens: 2–4 chars, lowercase. Must not clash with any existing entry.
- Background family keys follow the pattern `{prefix}{suffix}` where prefix is a
  single char (b=Bg, h=HoverBg, q=PressedBg, etc.) and suffix is 2 chars
  (cl=Color, gr=Gradient, im=Image, sp=Sprite…). Don't use those slots for new keys.

---

## Step 5 — Client: PacketService dispatch (`ScarletInterface/Services/PacketService.cs`)

There are **three** switch blocks to update. Search for any existing `case "AddButton":` to find them.

```csharp
// ── Block 1: Normal create (no UpdateOnly flag) ──────────────────────────
case "AddMyElem":
    MyElemHandler.Handle(data, ui.GetOrCreate(packet.Window));
    break;

// ── Block 2: UpdateOnly — DiffPatch existing element ─────────────────────
case "AddMyElem":
    MyElemHandler.DiffPatch(old, data, go, w);
    break;

// ── Block 3: UpdateOnly — type mismatch, recreate ────────────────────────
case "AddMyElem":
    MyElemHandler.Handle(data, w);
    break;
```

---

## Critical Rules and Common Pitfalls

### TrackElem / TouchElem / PruneStaleElements

`PruneStaleElements()` runs before every `Open()`. It destroys any GO whose ElemId was **not** seen in `_batchElemOrder` during the current packet batch. This means:

- **`TrackElem` must always be called** when creating a new GO (it writes to `_elemGOs`, `_elemData`, and `_batchElemOrder`).
- **`TouchElem` must always be called** when the DiffPatch guard returns early (element already exists). Otherwise the element will be pruned as stale on the next open.
- The DiffPatch guard pattern (`TryGetForDiffPatch` + `TouchElem` + return) is **mandatory** in every `Handle()`. Without it, re-opening a window without a Reset creates a duplicate GO and corrupts the tracker.

### ElemId is the identity key

The server generates deterministic IDs from parent ID + counter. If the server uses `ElemId`, the client uses it as the dictionary key in `ElemGOs` and `ElemData`. ElemId must be unique within a window.

### LayoutElement is required for flow elements

Any element that lives inside a `Row` or `Container` must have a `LayoutElement` component with `preferredWidth` and `preferredHeight` set, or Unity's layout system will size it to zero. Call `TrackRowElemH` after `TrackElem` for auto-height rows.

### Border GO must set `ignoreLayout = true`

The border GO is a child of the element GO, not a sibling. If it participates in layout it will push content around. Always `AddComp<LayoutElement>(borderGO).ignoreLayout = true`.

### Gradient images require `Color.white`

`ApplyGradientToImage` bakes a texture into the `Image.sprite`. The image color tints that texture. Set `bgImg.color = Color.white` when a gradient is active (not `Color.clear`), otherwise the texture is invisible.

### DiffPatch: use `Changed(old, new, "Key")` guards

Never unconditionally update Unity components in DiffPatch. Only update what actually changed. This avoids expensive gradient rebakes and unnecessary component thrashing on every re-open.

### Token allocation checklist before merging

1. Type token is unique in `KeyMap.Types` and `ElementSerializer` switch.
2. All data key tokens are unique in `KeyMap.Keys`.
3. Background prefix is not accidentally aliased (b/h/q/r/d/j/f are reserved).
4. Server serializer emits the same token strings the client's KeyMap decodes.
5. `PacketService` handles the new type in all three switch blocks.
6. `ElementSerializer` dispatches the new type in all relevant `SerializeRow/Container/Standalone/Window` element loops.

---

## Quick Checklist

| # | File | What to do |
|---|---|---|
| 1 | `ScarletCore/Interface/Elements/MyElem.cs` | Create element class extending `UIElement` |
| 2 | `ScarletCore/Interface/ElementSerializer.cs` | Add `SerializeMyElem()`, wire into dispatch |
| 3 | `ScarletInterface/Handlers/MyElemHandler.cs` | Create with `Handle()` + `DiffPatch()` |
| 4 | `ScarletInterface/KeyMap.cs` | Add type token to `Types`, data tokens to `Keys` |
| 5 | `ScarletInterface/Services/PacketService.cs` | Add to all 3 switch blocks |

Build both projects and verify **0 errors, 0 warnings** before testing in-game.
