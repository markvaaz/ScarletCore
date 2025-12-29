using System;
using System.Reflection;
using ScarletCore.Data;
using ScarletCore.Localization;

namespace ScarletCore;

/// <summary>
/// Provides extension methods for localizing strings using the server or player language context.
/// </summary>
public static class LocalizationExtensions {
  /// <summary>
  /// Localize a key using the server language (fallbacks as implemented in LocalizationService.GetText).
  /// Example: "help message".Localize()
  /// </summary>
  public static string Localize(this string key, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    // Try to resolve custom keys registered by other assemblies.
    var assembly = GetOriginatingAssembly();
    if (assembly != null) {
      var composite = assembly.GetName().Name + ":" + key.Trim();
      return Localizer.GetByCompositeKey(null, composite, parameters);
    }

    return Localizer.GetText(key, parameters);
  }

  /// <summary>
  /// Localize a key for a specific player (uses player's preferred language, server default, then first available).
  /// Example: "help message".Localize(playerData)
  /// </summary>
  public static string Localize(this string key, PlayerData player, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (player == null) return Localizer.GetText(key, parameters);

    // Try to determine the originating assembly (the caller outside of ScarletCore)
    var assembly = GetOriginatingAssembly();
    return Localizer.Get(player, key, assembly, parameters);
  }

  // Find the first stack frame assembly that is not ScarletCore
  private static Assembly GetOriginatingAssembly() {
    var scarletName = typeof(LocalizationExtensions).Assembly.GetName().Name;
    var stack = new System.Diagnostics.StackTrace();
    var frames = stack.GetFrames();
    if (frames == null) return null;

    foreach (var frame in frames) {
      var method = frame.GetMethod();
      if (method == null) continue;
      var declaring = method.DeclaringType;
      if (declaring == null) continue;
      var asm = declaring.Assembly;
      if (asm == null) continue;
      var name = asm.GetName().Name;
      if (string.Equals(name, scarletName, StringComparison.OrdinalIgnoreCase)) continue;
      return asm;
    }

    return null;
  }
}