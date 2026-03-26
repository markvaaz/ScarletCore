using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScarletCore.Interface.Models;

/// <summary>
/// JSON-serializable packet sent to the ScarletInterface client describing
/// a single UI operation or lifecycle action.
/// </summary>
internal class ScarletPacket {
  /// <summary>Type of packet (e.g. "AddText", "AddButton", "Open").</summary>
  [JsonPropertyName("Type")] public string Type { get; set; } = string.Empty;
  /// <summary>Originating plugin identifier (e.g. "scarlet-interface").</summary>
  [JsonPropertyName("Plugin")] public string Plugin { get; set; } = string.Empty;
  /// <summary>Target window ID the packet applies to.</summary>
  [JsonPropertyName("Window")] public string Window { get; set; } = string.Empty;
  /// <summary>Arbitrary key/value payload for the packet.</summary>
  [JsonPropertyName("Data")] public Dictionary<string, string> Data { get; set; } = [];
}
