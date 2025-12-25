using ScarletCore.Services;
using ScarletCore.Utils;

namespace ScarletCore.Commands;

[SCommandGroup("scarletcore", aliases: ["sc"], adminOnly: true)]
internal static class AdminCommands {
  [SCommand("setlanguage", description: "Set localization language")]
  public static void SetLanguage(CommandContext ctx, string language = "") {
    var newLanguage = language.ToLower().Trim();
    if (!LocalizationService.IsLanguageAvailable(newLanguage)) {
      ctx.ReplyError($"Language not supported: {newLanguage}");
      ctx.ReplyInfo($"Available languages: {string.Join(", ", LocalizationService.AvailableServerLanguages)}");
      return;
    }

    if (LocalizationService.ChangeLanguage(newLanguage)) {
      Plugin.Settings.Set("PrefabLocalizationLanguage", newLanguage);
      ctx.Reply($"~ScarletCore~ localization language changed to: {newLanguage}".FormatSuccess());
      Utils.Log.Info($"ScarletCore localization language changed to: {newLanguage} by admin {ctx.Sender?.Name}");
    } else {
      ctx.ReplyError($"Failed to change language to: {newLanguage}");
    }
  }
}