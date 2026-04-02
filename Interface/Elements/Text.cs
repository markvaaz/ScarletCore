using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>A text label element.</summary>
public class Text : UIElement, ITextElement {
  /// <summary>The text content to display. Supports inline icons via <see cref="UIIcons"/>.</summary>
  public string Content { get; set; }
  /// <summary>Horizontal text alignment. Default: Left.</summary>
  public TextAlignment TextAlign { get; set; }
  /// <summary>Wrap text onto multiple lines when it exceeds the element width.</summary>
  public bool Wrap { get; set; }

  // ─── ITextElement ────────────────────────────────────────────────────────
  public UIColor? TextColor { get; set; }
  public float FontSize { get; set; }
  public string Font { get; set; }
  public UITextGradient? TextGradient { get; set; }
  public UITextShadow? TextShadow { get; set; }
  public UITextOutline? TextOutline { get; set; }
}
