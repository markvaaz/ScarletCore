using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>A pre-styled × button that closes the window when clicked.</summary>
public class CloseButton : UIElement, ITextElement {
  // ─── ITextElement ────────────────────────────────────────────────────────
  public UIColor? TextColor { get; set; }
  public float FontSize { get; set; }
  public string Font { get; set; }
  public UITextGradient? TextGradient { get; set; }
  public UITextShadow? TextShadow { get; set; }
  public UITextOutline? TextOutline { get; set; }
}
