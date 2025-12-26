using ScarletCore.Data;
using ScarletCore.Services;

namespace ScarletCore;

public static class LocalizationExtensions {
  /// <summary>
  /// Localize a key using the server language (fallbacks as implemented in LocalizationService.GetText).
  /// Example: "help message".Localize()
  /// </summary>
  public static string Localize(this string key, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    return LocalizationService.GetText(key, parameters);
  }

  /// <summary>
  /// Localize a key for a specific player (uses player's preferred language, server default, then first available).
  /// Example: "help message".Localize(playerData)
  /// </summary>
  public static string Localize(this string key, PlayerData player, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (player == null) return LocalizationService.GetText(key, parameters);
    return LocalizationService.Get(player, key, parameters);
  }
}