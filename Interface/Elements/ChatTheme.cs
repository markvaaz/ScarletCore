using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using ScarletCore.Interface.Builders;
using ScarletCore.Interface.Models;
using ScarletCore.Services;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A complete, declarative visual configuration for the ScarletInterface client's native chat —
/// styled with the same vocabulary as any ScarletUI element (<see cref="UIBackground"/>,
/// <see cref="Border"/>, <see cref="Spacing"/>, <see cref="UIColor"/>). Every part is optional:
/// what you don't set keeps the vanilla look.
///
/// <code>
/// new ChatTheme {
///   Container = new ChatArea {
///     Background = UIBackground.FromGradient(UIGradient.Linear(270, a, b)),
///     Border = new Border(UIColor.Hex("#5b4487"), 1, 11),
///     Padding = new Spacing(6),
///   },
///   Input = new ChatInputStyle { Background = …, TextColor = …, CaretColor = … },
///   Tabs = new ChatTabsStyle { Gap = 3, TabRadius = 5, SelectedBackground = … },
///   Lines = new ChatLinesStyle { Bubble = UIColor.Hex("#1b142880"), BubbleRadius = 6, Spacing = 4 },
///   Tag = new ChatTagStyle { Background = UIColor.Hex("#9a72d0cc"), Uppercase = true, HideBrackets = true },
///   Timestamp = new ChatTimestampStyle { Color = UIColor.Hex("#8a8398"), Scale = 80 },
/// }.Send(player, "MyMod");
/// </code>
///
/// Persists on the client until replaced or cleared (<see cref="Clear"/>) — resend on
/// <c>PlayerEvents.InterfaceAuth</c> so it survives a relog. No-op for players without the interface.
/// </summary>
public sealed class ChatTheme {
  /// <summary>The message area (the rectangle behind the chat lines). Padding insets the lines.</summary>
  public ChatArea Container { get; set; }
  /// <summary>The text-input box.</summary>
  public ChatInputStyle Input { get; set; }
  /// <summary>The filter-tab bar and its buttons.</summary>
  public ChatTabsStyle Tabs { get; set; }
  /// <summary>The message lines: per-line bubble card, spacing, default text colour.</summary>
  public ChatLinesStyle Lines { get; set; }
  /// <summary>The channel tag ("[Global]" …) rendered as a chip.</summary>
  public ChatTagStyle Tag { get; set; }
  /// <summary>The "[21:30]" timestamp at the start of each line.</summary>
  public ChatTimestampStyle Timestamp { get; set; }
  /// <summary>Per-channel icons, shown on the input box and/or the filter tabs.</summary>
  public ChatIconsStyle Icons { get; set; }
  /// <summary>The "Tab — switch channel" hint row below the input (pulled inside the panel).</summary>
  public ChatHintStyle Hint { get; set; }

  /// <summary>Sends the theme to one player, or every interface client when <paramref name="player"/> is null.</summary>
  public void Send(PlayerData player, string plugin) {
    var packet = new ScarletPacket {
      Type = "SetChatTheme", Plugin = plugin, Window = "",
      Data = new Dictionary<string, string> { ["cth"] = JsonSerializer.Serialize(Sections()) },
    };
    if (player == null) PacketManager.SendPacketToAll(packet);
    else PacketManager.SendPacket(player, packet);
  }

  /// <summary>Sends the theme to every connected interface client.</summary>
  public void SendAll(string plugin) => Send(null, plugin);

  /// <summary>Removes any chat theme on a player's client (null = everyone), restoring vanilla.</summary>
  public static void Clear(PlayerData player, string plugin) {
    var packet = new ScarletPacket {
      Type = "SetChatTheme", Plugin = plugin, Window = "",
      Data = new Dictionary<string, string> { ["cth"] = "{}" },
    };
    if (player == null) PacketManager.SendPacketToAll(packet);
    else PacketManager.SendPacket(player, packet);
  }

  // ── Wire: one flat key/value dict per section, same short keys the element serializer uses ──

  Dictionary<string, Dictionary<string, string>> Sections() {
    var s = new Dictionary<string, Dictionary<string, string>>();
    if (Container != null) s["container"] = Container.Data();
    if (Input != null) s["input"] = Input.Data();
    if (Tabs != null) s["tabs"] = Tabs.Data();
    if (Lines != null) s["lines"] = Lines.Data();
    if (Tag != null) s["tag"] = Tag.Data();
    if (Timestamp != null) s["time"] = Timestamp.Data();
    if (Icons != null) s["icons"] = Icons.Data();
    if (Hint != null) s["hint"] = Hint.Data();
    return s;
  }

  internal static string F(float v) => v.ToString(CultureInfo.InvariantCulture);
  internal static string Sp(Spacing sp) => $"{F(sp.Top)},{F(sp.Right)},{F(sp.Bottom)},{F(sp.Left)}";
}

/// <summary>A styled chat region: background, border and inner padding.</summary>
public class ChatArea {
  public UIBackground? Background { get; set; }
  public Border? Border { get; set; }
  public Spacing? Padding { get; set; }
  /// <summary>
  /// Container only: when true the skin wraps the WHOLE chat (messages + input + tab bar) as one
  /// panel, instead of only the message area. The input keeps its own inset styling on top.
  /// </summary>
  public bool Full { get; set; }

  /// <summary>
  /// Background/border opacity while the chat input is FOCUSED (the player is typing). 0..1,
  /// default 1 (fully opaque). Only the skin fades — messages and tabs stay fully visible.
  /// </summary>
  public float OpacityFocused { get; set; } = 1f;
  /// <summary>
  /// Background/border opacity while the chat is visible but NOT focused (a message just arrived,
  /// no one is typing). 0..1, default 1. Set to 0 so the panel/input box is invisible when idle and
  /// never covers the world during combat — a server-side decision, not the player's. The message
  /// text still shows; only the skin behind it disappears.
  /// </summary>
  public float OpacityUnfocused { get; set; } = 1f;

  internal virtual Dictionary<string, string> Data() {
    var d = new Dictionary<string, string>();
    if (Background.HasValue) Background.Value.Apply(d, "b");
    if (Border.HasValue) {
      d["dc"] = Border.Value.Color;
      d["dw"] = ChatTheme.F(Border.Value.Width);
      d["dr"] = ChatTheme.F(Border.Value.Radius);
    }
    if (Padding.HasValue) d["Padding"] = ChatTheme.Sp(Padding.Value);
    if (Full) d["Full"] = "1";
    if (OpacityFocused < 1f) d["af"] = ChatTheme.F(OpacityFocused);
    if (OpacityUnfocused < 1f) d["au"] = ChatTheme.F(OpacityUnfocused);
    return d;
  }
}

/// <summary>The chat input box: area style plus text/placeholder/caret colours.</summary>
public sealed class ChatInputStyle : ChatArea {
  public UIColor? TextColor { get; set; }
  public UIColor? PlaceholderColor { get; set; }
  public UIColor? CaretColor { get; set; }

  internal override Dictionary<string, string> Data() {
    var d = base.Data();
    if (TextColor.HasValue) d["TextColor"] = TextColor.Value;
    if (PlaceholderColor.HasValue) d["PlaceholderColor"] = PlaceholderColor.Value;
    if (CaretColor.HasValue) d["CaretColor"] = CaretColor.Value;
    return d;
  }
}

/// <summary>The filter-tab bar: area style plus button shape and per-state colours.</summary>
public sealed class ChatTabsStyle : ChatArea {
  /// <summary>Space between tab buttons, px. Negative = keep default.</summary>
  public float Gap { get; set; } = -1;
  /// <summary>Corner radius of each tab button. Negative = keep default.</summary>
  public float TabRadius { get; set; } = -1;
  /// <summary>Inner padding of each tab button.</summary>
  public Spacing? TabPadding { get; set; }
  /// <summary>Tab label font size. Negative = keep default.</summary>
  public float FontSize { get; set; } = -1;
  public UIColor? TabBackground { get; set; }
  public UIColor? TabTextColor { get; set; }
  public UIColor? SelectedBackground { get; set; }
  public UIColor? SelectedTextColor { get; set; }

  internal override Dictionary<string, string> Data() {
    var d = base.Data();
    if (Gap >= 0) d["Gap"] = ChatTheme.F(Gap);
    if (TabRadius >= 0) d["TabRadius"] = ChatTheme.F(TabRadius);
    if (TabPadding.HasValue) d["TabPadding"] = ChatTheme.Sp(TabPadding.Value);
    if (FontSize > 0) d["FontSize"] = ChatTheme.F(FontSize);
    if (TabBackground.HasValue) d["tbg"] = TabBackground.Value;
    if (SelectedBackground.HasValue) d["tsb"] = SelectedBackground.Value;
    if (TabTextColor.HasValue) d["ttx"] = TabTextColor.Value;
    if (SelectedTextColor.HasValue) d["tst"] = SelectedTextColor.Value;
    return d;
  }
}

/// <summary>The message lines: an optional per-line bubble card, spacing and text colour.</summary>
public sealed class ChatLinesStyle {
  /// <summary>Card drawn behind each message line (solid colour or gradient).</summary>
  public UIBackground? Bubble { get; set; }
  /// <summary>Bubble corner radius.</summary>
  public float BubbleRadius { get; set; }
  /// <summary>How far the bubble extends past the text on each side.</summary>
  public Spacing? BubblePadding { get; set; }
  /// <summary>Vertical space between lines, px. Negative = keep default.</summary>
  public float Spacing { get; set; } = -1;
  /// <summary>Default text colour for untagged message text.</summary>
  public UIColor? TextColor { get; set; }

  internal Dictionary<string, string> Data() {
    var d = new Dictionary<string, string>();
    if (Bubble.HasValue) Bubble.Value.Apply(d, "b");
    if (BubbleRadius > 0) d["dr"] = ChatTheme.F(BubbleRadius);
    if (BubblePadding.HasValue) d["BubblePadding"] = ChatTheme.Sp(BubblePadding.Value);
    if (Spacing >= 0) d["Spacing"] = ChatTheme.F(Spacing);
    if (TextColor.HasValue) d["TextColor"] = TextColor.Value;
    return d;
  }
}

/// <summary>
/// The channel tag at the start of each line ("[Global]", "[Trade]" …), restyled as a chip — a
/// coloured card behind the label — instead of plain bracketed text.
/// </summary>
public sealed class ChatTagStyle {
  /// <summary>Chip background. Unset = no chip, text-only restyle.</summary>
  public UIColor? Background { get; set; }
  public UIColor? TextColor { get; set; }
  /// <summary>Render the label in UPPERCASE.</summary>
  public bool Uppercase { get; set; }
  /// <summary>Drop the square brackets around the label.</summary>
  public bool HideBrackets { get; set; }
  /// <summary>Remove the tag from the line entirely.</summary>
  public bool Hide { get; set; }
  /// <summary>Chip padding around the label (top/right/bottom/left, px).</summary>
  public Spacing? Padding { get; set; }
  /// <summary>Chip corner radius.</summary>
  public float Radius { get; set; }
  /// <summary>Chip outline — a keycap-style frame around the card. Its Radius, when set, wins over
  /// <see cref="Radius"/>.</summary>
  public Border? Border { get; set; }

  /// <summary>
  /// Per-channel colour overrides — the chip shape (radius/width/padding) stays global, the colours
  /// vary per channel. Keys: native channels by name ("global", "local", "team", "whisper",
  /// "system", "region", "lore") and custom ScarletChannels channels by their key ("trade", …).
  /// Anything a channel doesn't override falls back to the base colours above.
  /// </summary>
  public Dictionary<string, ChatTagColors> Channels { get; set; } = new();

  internal Dictionary<string, string> Data() {
    var d = new Dictionary<string, string>();
    if (Background.HasValue) d["bcl"] = Background.Value;
    float radius = Border.HasValue && Border.Value.Radius > 0 ? Border.Value.Radius : Radius;
    if (radius > 0) d["dr"] = ChatTheme.F(radius);
    if (Border.HasValue) {
      d["dc"] = Border.Value.Color;
      d["dw"] = ChatTheme.F(Border.Value.Width);
    }
    foreach (var (key, c) in Channels) {
      if (string.IsNullOrWhiteSpace(key) || c == null) continue;
      // "bg;text;border" — empty segment = inherit the base colour.
      d["Ch:" + key.Trim().ToLowerInvariant()] =
        $"{(c.Background.HasValue ? (string)c.Background.Value : "")};" +
        $"{(c.TextColor.HasValue ? (string)c.TextColor.Value : "")};" +
        $"{(c.BorderColor.HasValue ? (string)c.BorderColor.Value : "")}";
    }
    if (TextColor.HasValue) d["TextColor"] = TextColor.Value;
    if (Uppercase) d["Uppercase"] = "1";
    if (HideBrackets) d["HideBrackets"] = "1";
    if (Hide) d["Hide"] = "1";
    if (Padding.HasValue) d["Padding"] = ChatTheme.Sp(Padding.Value);
    return d;
  }
}

/// <summary>
/// Per-channel icons, drawn on the input box (the current send channel) and/or each filter tab.
/// Frees the input from a text label — the icon plus a placeholder padding shows the channel without
/// fighting the chat-history ghost text. Keys are the same channel ids as <see cref="ChatTagStyle.Channels"/>
/// (native names + custom keys). A value can be:
/// <list type="bullet">
///   <item><see cref="Builders.UIIcons.Svg"/> — an inline SVG token. The recommended kind: every
///     icon rasterizes into the same fit-square border box, so sizes stay pixel-consistent, and a
///     colour-free SVG is tinted live (tab icons follow the label's normal/selected colour).</item>
///   <item><see cref="Builders.UIIcons.Icon"/> — a game item/ability icon by PrefabGUID hash.</item>
///   <item>a game sprite name or an http(s) image URL. Beware: game sprites carry arbitrary padding
///     and aspect ratios, so a mixed set will NOT look uniform — prefer SVG.</item>
/// </list>
/// </summary>
public sealed class ChatIconsStyle {
  public Dictionary<string, string> Channels { get; set; } = new();
  /// <summary>Icon square size in px.</summary>
  public float Size { get; set; } = 16;
  /// <summary>Show the current channel's icon at the left of the input box (with matching padding).</summary>
  public bool OnInput { get; set; }
  /// <summary>Show each channel's icon on its filter tab, before the label.</summary>
  public bool OnTabs { get; set; }
  /// <summary>Tint for colour-free SVG icons outside the tabs (the input box). Unset = untinted.</summary>
  public UIColor? Color { get; set; }

  internal Dictionary<string, string> Data() {
    var d = new Dictionary<string, string>();
    if (Size > 0) d["Size"] = ChatTheme.F(Size);
    if (OnInput) d["OnInput"] = "1";
    if (OnTabs) d["OnTabs"] = "1";
    if (Color.HasValue) d["Color"] = Color.Value;
    foreach (var (k, v) in Channels)
      if (!string.IsNullOrWhiteSpace(k) && !string.IsNullOrEmpty(v))
        d["Ic:" + k.Trim().ToLowerInvariant()] = v;
    return d;
  }
}

/// <summary>
/// The native "Tab — Cycle Chat Channel" hint below the input. When the theme's Container is Full,
/// the client pulls this row inside the panel; this section restyles it — a keycap chip behind the
/// "Tab" key name, plus label text/colour overrides (the label follows the game language unless
/// <see cref="Text"/> replaces it).
/// </summary>
public sealed class ChatHintStyle {
  /// <summary>The key name on the keycap (default "Tab").</summary>
  public string Key { get; set; }
  /// <summary>Replaces the "Cycle Chat Channel" label (e.g. "Trocar canal"). Null = keep native.</summary>
  public string Text { get; set; }
  /// <summary>Label colour.</summary>
  public UIColor? TextColor { get; set; }
  /// <summary>Keycap chip colour behind the "Tab" key name.</summary>
  public UIColor? KeyBackground { get; set; }
  /// <summary>"Tab" key-name text colour.</summary>
  public UIColor? KeyTextColor { get; set; }
  /// <summary>Keycap corner radius (default 4).</summary>
  public float KeyRadius { get; set; } = -1;
  /// <summary>Remove the hint row entirely.</summary>
  public bool Hide { get; set; }

  internal Dictionary<string, string> Data() {
    var d = new Dictionary<string, string>();
    if (!string.IsNullOrEmpty(Key)) d["Key"] = Key;
    if (!string.IsNullOrEmpty(Text)) d["Text"] = Text;
    if (TextColor.HasValue) d["TextColor"] = TextColor.Value;
    if (KeyBackground.HasValue) d["KeyBgColor"] = KeyBackground.Value;
    if (KeyTextColor.HasValue) d["KeyTextColor"] = KeyTextColor.Value;
    if (KeyRadius >= 0) d["KeyRadius"] = ChatTheme.F(KeyRadius);
    if (Hide) d["Hide"] = "1";
    return d;
  }
}

/// <summary>Per-channel chip colours (see <see cref="ChatTagStyle.Channels"/>). Unset = inherit.</summary>
public sealed class ChatTagColors {
  public UIColor? Background { get; set; }
  public UIColor? TextColor { get; set; }
  public UIColor? BorderColor { get; set; }
}

/// <summary>The "[21:30]" timestamp at the start of each line.</summary>
public sealed class ChatTimestampStyle {
  /// <summary>Remove the timestamp from lines entirely.</summary>
  public bool Hide { get; set; }
  public UIColor? Color { get; set; }
  /// <summary>Size relative to the line text, percent (e.g. 80). Zero/negative = keep.</summary>
  public float Scale { get; set; } = -1;

  internal Dictionary<string, string> Data() {
    var d = new Dictionary<string, string>();
    if (Hide) d["Hide"] = "1";
    if (Color.HasValue) d["TextColor"] = Color.Value;
    if (Scale > 0) d["Scale"] = ChatTheme.F(Scale);
    return d;
  }
}
