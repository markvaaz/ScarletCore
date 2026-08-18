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
