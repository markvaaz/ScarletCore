using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScarletCore.Interface.Models;
using ScarletCore.Services;

namespace ScarletCore.Interface.Builders;

/// <summary>A number the client can read off a hovered item instance. These are plain game
/// components, so any mod's items carry them without opting into anything.</summary>
public enum ItemVar {
  /// <summary><c>WeaponLevelSource.Level</c>, raw — exactly the value the item carries. A mod that
  /// stores its own scale there samples its curves in that same scale.</summary>
  Level,
  /// <summary><c>Durability.Value</c>.</summary>
  Durability,
  /// <summary><c>Durability.MaxDurability</c>.</summary>
  MaxDurability,
}

/// <summary>
/// Builds an item's client-side appearance: the icon, name and description (which are fixed per
/// item type), plus tooltip field text and stat rows whose numbers come from the hovered item
/// instance.
///
/// A value that varies per instance is declared as a <see cref="Curve(string, ItemVar, ValueTuple{float, float}[])"/>: the mod samples its own
/// function into points, the client interpolates between them and substitutes the result into any
/// <c>{name}</c> placeholder. Nothing is evaluated as an expression, so there is no formula
/// language to learn — the real calculation stays in your code, where it already is.
///
/// Every string must already be localized for <paramref name="player"/>; the client only fills in
/// numbers.
/// </summary>
/// <example>
/// InterfaceManager.ItemVisual(player, "MyMod", itemGuid)
///   .Icon("https://example.com/icon.png")
///   .Name(Localizer.Get(player, "my_item_name"))
///   .Describe(Localizer.Get(player, "my_item_desc"))      // "Heals {heal:F1}/s"
///   .Field("type", "")                                    // hide the "Stackable" line
///   .Curve("heal", ItemVar.Level, ItemVar.Durability, 5, (0f, 3f), (30f, 12f))
///   .Stat(Localizer.Get(player, "stat_power") + " +{power:F0}")
///   .Curve("power", ItemVar.Level, (0f, 5f), (30f, 40f))
///   .When(ItemVar.Durability, 5, r => r.Field("subtext", legendaryText))
///   .Send();
/// </example>
public sealed class ItemVisualBuilder(string plugin, PlayerData player, int itemGuid) {
  readonly string _plugin = plugin;
  readonly PlayerData _player = player;      // null = broadcast to everyone
  readonly int _itemGuid = itemGuid;

  string _icon;
  string _name;
  string _description;
  readonly RuleSetDto _rules = new();

  internal static string Wire(ItemVar v) => v switch {
    ItemVar.Level => "level",
    ItemVar.Durability => "durability",
    ItemVar.MaxDurability => "maxDurability",
    _ => throw new ArgumentOutOfRangeException(nameof(v)),
  };

  /// <summary>The item's icon: an http(s)/file URL, or the name of a sprite already in the game.
  /// Replaces the icon everywhere the item is drawn, not just in the tooltip.</summary>
  public ItemVisualBuilder Icon(string src) { _icon = src; return this; }

  /// <summary>The item's name. Fixed per item type.</summary>
  public ItemVisualBuilder Name(string text) { _name = text; return this; }

  /// <summary>The item's description. Fixed per item type; may contain <c>{curve}</c> placeholders
  /// only if you also set it through <see cref="Field"/> — the description itself is written once,
  /// per type, so it cannot vary per instance.</summary>
  public ItemVisualBuilder Describe(string text) { _description = text; return this; }

  /// <summary>
  /// Replaces one of the tooltip's secondary lines. Keys: <c>type</c> (the "Stackable" subheader),
  /// <c>subtext</c> (the "Salvageable" footer), <c>presubtext</c>, <c>durability</c>, <c>repair</c>,
  /// <c>equipped</c>, <c>bloodpotion</c>. Empty text hides the line. May contain <c>{curve}</c>.
  /// </summary>
  public ItemVisualBuilder Field(string field, string text) {
    if (!string.IsNullOrEmpty(field)) (_rules.Fields ??= [])[field] = text ?? "";
    return this;
  }

  /// <summary>Appends a stat row to the tooltip, in the game's own style. May contain
  /// <c>{curve}</c> placeholders.</summary>
  public ItemVisualBuilder Stat(string text) {
    if (!string.IsNullOrEmpty(text)) (_rules.Stats ??= []).Add(text);
    return this;
  }

  /// <summary>
  /// Declares a number that varies with <paramref name="over"/>, sampled at <paramref name="points"/>
  /// and interpolated on the client. Values beyond the first/last sample clamp to it. Sample as
  /// densely as your curve needs — two points for a straight line, more for a bend.
  /// </summary>
  public ItemVisualBuilder Curve(string name, ItemVar over, params (float X, float Y)[] points) =>
    AddCurve(name, over, null, "*", points);

  /// <summary>
  /// Like <see cref="Curve(string, ItemVar, ValueTuple{float, float}[])"/>, but for one value of a
  /// second variable — call it once per value (e.g. once per tier held in durability). An instance
  /// whose <paramref name="per"/> value has no curve falls back to one declared with the
  /// unsplit overload, if any.
  /// </summary>
  public ItemVisualBuilder Curve(string name, ItemVar over, ItemVar per, int perValue,
      params (float X, float Y)[] points) =>
    AddCurve(name, over, per, perValue.ToString(CultureInfo.InvariantCulture), points);

  ItemVisualBuilder AddCurve(string name, ItemVar over, ItemVar? per, string key,
      (float X, float Y)[] points) {
    if (string.IsNullOrEmpty(name) || points == null || points.Length == 0) return this;

    _rules.Curves ??= [];
    if (!_rules.Curves.TryGetValue(name, out var curve)) {
      curve = new CurveDto { Over = Wire(over), Per = per.HasValue ? Wire(per.Value) : null };
      _rules.Curves[name] = curve;
    }

    // Sorted here so the client can interpolate by walking the points once, and so a caller that
    // samples its function backwards still gets the curve it meant.
    curve.Points[key] = [.. points.OrderBy(p => p.X).Select(p => new[] { p.X, p.Y })];
    return this;
  }

  /// <summary>Field text and stat rows applied only to instances whose <paramref name="v"/> equals
  /// <paramref name="value"/>.</summary>
  public ItemVisualBuilder When(ItemVar v, float value, Action<ItemRuleScope> build) =>
    When(v, value, value, build);

  /// <summary>Field text and stat rows applied only to instances whose <paramref name="v"/> falls
  /// within [<paramref name="min"/>, <paramref name="max"/>], both inclusive. Later rules win over
  /// earlier ones and over <see cref="Field"/>; stat rows accumulate instead of replacing.</summary>
  public ItemVisualBuilder When(ItemVar v, float min, float max, Action<ItemRuleScope> build) {
    if (build == null) return this;

    var rule = new RuleDto { When = [new ConditionDto { Var = Wire(v), Min = min, Max = max }] };
    build(new ItemRuleScope(rule));
    if (rule.Fields is { Count: > 0 } || rule.Stats is { Count: > 0 }) (_rules.Rules ??= []).Add(rule);
    return this;
  }

  /// <summary>Sends the appearance. Overrides are per item type and persist on the client until
  /// changed or cleared, so this normally runs once per player.</summary>
  public void Send() {
    foreach (var packet in Packets()) {
      if (_player == null) PacketManager.SendPacketToAll(packet);
      else PacketManager.SendPacket(_player, packet);
    }
  }

  IEnumerable<ScarletPacket> Packets() {
    if (_icon != null)
      yield return Packet("SII", new() { ["igid"] = Guid, ["iic"] = _icon });

    if (_name != null || _description != null)
      yield return Packet("SIT", new() {
        ["igid"] = Guid, ["itl"] = _name ?? "", ["ide"] = _description ?? "",
      });

    if (_rules.Fields != null || _rules.Stats != null || _rules.Rules != null || _rules.Curves != null)
      yield return Packet("SIR", new() { ["igid"] = Guid, ["irs"] = JsonSerializer.Serialize(_rules) });
  }

  string Guid => _itemGuid.ToString(CultureInfo.InvariantCulture);

  ScarletPacket Packet(string type, Dictionary<string, string> data) =>
    new() { Type = type, Plugin = _plugin, Window = "$item", Data = data };

  // ── wire shapes; the property names are the short keys the client reads ──────────────
  internal sealed class RuleSetDto {
    [JsonPropertyName("c")] public Dictionary<string, CurveDto> Curves { get; set; }
    [JsonPropertyName("f")] public Dictionary<string, string> Fields { get; set; }
    [JsonPropertyName("r")] public List<RuleDto> Rules { get; set; }
    [JsonPropertyName("s")] public List<string> Stats { get; set; }
  }

  internal sealed class CurveDto {
    [JsonPropertyName("o")] public string Over { get; set; }
    [JsonPropertyName("p")] public string Per { get; set; }
    [JsonPropertyName("pts")] public Dictionary<string, float[][]> Points { get; set; } = [];
  }

  internal sealed class RuleDto {
    [JsonPropertyName("w")] public List<ConditionDto> When { get; set; }
    [JsonPropertyName("f")] public Dictionary<string, string> Fields { get; set; }
    [JsonPropertyName("s")] public List<string> Stats { get; set; }
  }

  internal sealed class ConditionDto {
    [JsonPropertyName("v")] public string Var { get; set; }
    [JsonPropertyName("mn")] public float Min { get; set; }
    [JsonPropertyName("mx")] public float Max { get; set; }
  }
}

/// <summary>What a <see cref="ItemVisualBuilder.When(ItemVar, float, Action{ItemRuleScope})"/> rule may set.</summary>
public sealed class ItemRuleScope(object rule) {
  readonly ItemVisualBuilder.RuleDto _rule = (ItemVisualBuilder.RuleDto)rule;

  /// <summary>Field text for instances matching this rule. Same keys as
  /// <see cref="ItemVisualBuilder.Field"/>.</summary>
  public ItemRuleScope Field(string field, string text) {
    if (!string.IsNullOrEmpty(field)) (_rule.Fields ??= [])[field] = text ?? "";
    return this;
  }

  /// <summary>A stat row shown only on instances matching this rule, appended after the rows from
  /// <see cref="ItemVisualBuilder.Stat"/>. May contain <c>{curve}</c> placeholders.</summary>
  public ItemRuleScope Stat(string text) {
    if (!string.IsNullOrEmpty(text)) (_rule.Stats ??= []).Add(text);
    return this;
  }

  /// <summary>Narrows the rule further: every condition must match.</summary>
  public ItemRuleScope And(ItemVar v, float min, float max) {
    _rule.When.Add(new ItemVisualBuilder.ConditionDto {
      Var = ItemVisualBuilder.Wire(v), Min = min, Max = max,
    });
    return this;
  }
}
