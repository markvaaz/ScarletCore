namespace ScarletCore.Interface.Builders;

/// <summary>
/// Short-code aliases for the native V Rising TMP font assets.
/// Use these as the <c>Font</c> property on any text element — the client
/// translates the short code to the full asset name automatically.
/// </summary>
/// <example>
/// <code>
/// new Text { Content = "Title", Font = NativeFonts.NocturneSDF, FontSize = 18 }
/// </code>
/// </example>
public static class NativeFonts {
  // ── Nocturne Serif ────────────────────────────────────────────────────────
  /// <summary>Nocturne Serif — SDF (primary in-game font for most UI text).</summary>
  public const string NocturneSDF = "@ns";
  /// <summary>Nocturne Serif — DropShadow variant.</summary>
  public const string NocturneDropShadow = "@ns_ds";
  /// <summary>Nocturne Serif — MainMenu variant.</summary>
  public const string NocturneMainMenu = "@ns_mm";
  /// <summary>Nocturne Serif — SCT (scrolling combat text) variant.</summary>
  public const string NocturneSCT = "@ns_sct";
  /// <summary>Nocturne Serif — Stroke variant.</summary>
  public const string NocturneStroke = "@ns_st";
  /// <summary>Nocturne Serif — Unknown/Tech Glow variant.</summary>
  public const string NocturneUnknown = "@ns_uk";

  // ── Liberation Sans ──────────────────────────────────────────────────────
  /// <summary>Liberation Sans SDF.</summary>
  public const string Liberation = "@lib";
  /// <summary>Liberation Sans SDF — Fallback variant.</summary>
  public const string LiberationFallback = "@lib_fb";

  // ── Noto Sans ─────────────────────────────────────────────────────────────
  /// <summary>Noto Sans Regular SDF.</summary>
  public const string NotoSans = "@noto";
  /// <summary>Noto Sans Bold SDF.</summary>
  public const string NotoSansBold = "@noto_b";
  /// <summary>Noto Sans Arabic Regular SDF.</summary>
  public const string NotoArabic = "@noto_ar";
  /// <summary>Noto Sans Arabic Bold SDF.</summary>
  public const string NotoArabicBold = "@noto_arb";
  /// <summary>Noto Sans JP Regular SDF.</summary>
  public const string NotoJP = "@noto_jp";
  /// <summary>Noto Sans JP Bold SDF.</summary>
  public const string NotoJPBold = "@noto_jpb";
  /// <summary>Noto Sans KR Regular SDF.</summary>
  public const string NotoKR = "@noto_kr";
  /// <summary>Noto Sans KR Bold SDF.</summary>
  public const string NotoKRBold = "@noto_krb";
  /// <summary>Noto Sans SC (Simplified Chinese) Regular SDF.</summary>
  public const string NotoSC = "@noto_sc";
  /// <summary>Noto Sans SC Bold SDF.</summary>
  public const string NotoSCBold = "@noto_scb";
  /// <summary>Noto Sans TC (Traditional Chinese) Regular SDF.</summary>
  public const string NotoTC = "@noto_tc";
  /// <summary>Noto Sans TC Bold SDF.</summary>
  public const string NotoTCBold = "@noto_tcb";
  /// <summary>Noto Sans Thai Regular SDF.</summary>
  public const string NotoThai = "@noto_th";
  /// <summary>Noto Sans Thai Bold SDF.</summary>
  public const string NotoThaiBold = "@noto_thb";
}
