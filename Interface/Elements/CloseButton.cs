using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>A pre-styled × button that closes the window when clicked.</summary>
public class CloseButton : UIElement, ITextElement {
  // ─── ITextElement ────────────────────────────────────────────────────────
  /// <inheritdoc/>
  public UIColor? TextColor { get; set; }
  /// <inheritdoc/>
  public float FontSize { get; set; }
  /// <inheritdoc/>
  public string Font { get; set; }
  /// <inheritdoc/>
  public UITextGradient? TextGradient { get; set; }
  /// <inheritdoc/>
  public UITextShadow? TextShadow { get; set; }
  /// <inheritdoc/>
  public UITextOutline? TextOutline { get; set; }
}
