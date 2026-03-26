using System.Text.Json.Serialization;

namespace ScarletCore.Interface.Models;

/// <summary>
/// Represents a single buff/stat modification entry serialized to the client
/// for unit stat synchronization (ModifyUnitStatBuff_DOTS entries).
/// </summary>
internal class UnitModEntry {
  /// <summary>Numeric identifier of the stat type.</summary>
  [JsonPropertyName("S")] public int StatType { get; set; }
  /// <summary>Value of the modification.</summary>
  [JsonPropertyName("V")] public float Value { get; set; }
  /// <summary>Modification type enum value as integer.</summary>
  [JsonPropertyName("M")] public int ModType { get; set; }
  /// <summary>Whether the modification is uncapped by attribute limits.</summary>
  [JsonPropertyName("U")] public bool Uncapped { get; set; }

  /// <summary>Parameterless constructor used by serializers.</summary>
  public UnitModEntry() { }

  /// <summary>Creates a new <see cref="UnitModEntry"/> with explicit values.</summary>
  /// <param name="statType">Numeric stat type identifier.</param>
  /// <param name="value">Modification value.</param>
  /// <param name="modType">Modification type as integer.</param>
  /// <param name="uncapped">True if uncapped by attribute limits.</param>
  public UnitModEntry(int statType, float value, int modType, bool uncapped) {
    StatType = statType;
    Value = value;
    ModType = modType;
    Uncapped = uncapped;
  }
}
