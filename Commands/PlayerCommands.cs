using System.Linq;
using ScarletCore.Services;
using ScarletCore.Utils;

namespace ScarletCore.Commands;

internal static class PlayerCommands {
  [SCommand("language", aliases: ["lang"], description: "Set your preferred language (e.g. .language portuguese)")]
  public static void SetLanguage(CommandContext ctx, string language = "") {
    var player = ctx.Sender;
    if (player == null) {
      ctx.ReplyError("This command must be run by a player.");
      return;
    }

    if (string.IsNullOrWhiteSpace(language)) {
      var current = LocalizationService.GetPlayerLanguage(player) ?? Plugin.Settings.Get<string>("DefaultPlayerLanguage") ?? LocalizationService.CurrentServerLanguage;
      ctx.ReplyInfo($"Your current language: ~{current}~");
      return;
    }

    var newLang = language.ToLower().Trim();
    if (!LocalizationService.IsLanguageAvailable(newLang)) {
      ctx.ReplyError($"Language not supported: ~{newLang}~");
      ctx.ReplyInfo($"~Available languages~: {string.Join(", ", LocalizationService.AvailableServerLanguages)}");
      return;
    }

    LocalizationService.SetPlayerLanguage(player, newLang);
    ctx.Reply($"Your language has been set to: ~{newLang}~".FormatSuccess());
  }

  [SCommand("help", aliases: ["h", "commands"], description: "Shows available commands")]
  public static void Help(CommandContext ctx, string modName) {
    var player = ctx.Sender;
    var playerLanguage = player != null ? LocalizationService.GetPlayerLanguage(player) : null;
    if (!string.IsNullOrWhiteSpace(playerLanguage)) playerLanguage = playerLanguage.ToLower().Trim();

    var asmName = modName.Trim();
    var cmds = CommandService.GetAssemblyCommands(asmName, playerLanguage);
    if (cmds == null || cmds.Length == 0) {
      ctx.ReplyInfo($"No commands found for assembly: {asmName}");
      return;
    }

    for (int i = 0; i < cmds.Length; i += 5) {
      var batch = cmds.Skip(i).Take(5);
      var header = i == 0 ? $"~{asmName}~" + "\n" : "\n";
      ctx.Reply($"{header}{string.Join("\n", batch.Select(c => "<mark=#a963ff25>" + c + "</mark>"))}");
    }
  }

  [SCommand("help", aliases: ["h", "commands"], description: "Shows available commands")]
  public static void Help(CommandContext ctx) {
    var player = ctx.Sender;
    var playerLanguage = player != null ? LocalizationService.GetPlayerLanguage(player) : null;
    if (!string.IsNullOrWhiteSpace(playerLanguage)) playerLanguage = playerLanguage.ToLower().Trim();

    var all = CommandService.GetAllCommands(playerLanguage);
    if (all.Count == 0) {
      ctx.ReplyInfo("No commands available.");
      return;
    }

    foreach (var kv in all) {
      var asm = kv.Key;
      var cmdsForAsm = kv.Value.OrderBy(x => x).ToArray();
      for (int i = 0; i < cmdsForAsm.Length; i += 5) {
        var batch = cmdsForAsm.Skip(i).Take(5);
        var header = i == 0 ? $"~{asm}~" + "\n" : "\n";
        ctx.Reply($"{header}{string.Join("\n", batch.Select(c => "<mark=#a963ff25>" + c + "</mark>"))}");
      }
    }
  }
}