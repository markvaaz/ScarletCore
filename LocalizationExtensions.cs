using ScarletCore.Data;
using ScarletCore.Services;

namespace ScarletCore;

public static class LocalizationExtensions {
  /// <summary>
  /// Localize a key using the server language (fallbacks as implemented in LocalizationService.GetText).
  /// Example: "help message".Localize()
  /// </summary>
  public static string Localize(this string key) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    return LocalizationService.GetText(key);
  }

  /// <summary>
  /// Localize a key for a specific player (uses player's preferred language, server default, then first available).
  /// Example: "help message".Localize(playerData)
  /// </summary>
  public static string Localize(this string key, PlayerData player) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (player == null) return LocalizationService.GetText(key);
    return LocalizationService.Get(player, key);
  }
}