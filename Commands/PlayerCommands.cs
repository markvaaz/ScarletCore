using ScarletCore.Services;
using ScarletCore.Utils;

namespace ScarletCore.Commands;

internal static class PlayerCommands {
  [SCommand("setlanguage", aliases: ["setlang"], description: "Set your preferred language (e.g. .setlang portuguese)")]
  public static void SetLanguage(CommandContext ctx, string language = "") {
    var player = ctx.Sender;
    if (player == null) {
      ctx.ReplyError("This command must be run by a player.");
      return;
    }

    if (string.IsNullOrWhiteSpace(language)) {
      var current = LocalizationService.GetPlayerLanguage(player) ?? Plugin.Settings.Get<string>("DefaultPlayerLanguage") ?? LocalizationService.CurrentServerLanguage;
      ctx.ReplyInfo($"Your current language: {current}");
      ctx.ReplyInfo($"Available languages: {string.Join(", ", LocalizationService.AvailableServerLanguages)}");
      return;
    }

    var newLang = language.ToLower().Trim();
    if (!LocalizationService.IsLanguageAvailable(newLang)) {
      ctx.ReplyError($"Language not supported: {newLang}");
      ctx.ReplyInfo($"Available languages: {string.Join(", ", LocalizationService.AvailableServerLanguages)}");
      return;
    }

    LocalizationService.SetPlayerLanguage(player, newLang);
    ctx.Reply($"Your language has been set to: {newLang}".FormatSuccess());
  }
}
