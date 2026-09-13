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

  /// <summary>
  /// Border drawn around each <c>{icon:...}</c> / inline-SVG glyph in the content — not around
  /// the text box (that is <see cref="UIElement.Border"/>). With <see cref="Border.ContentAware"/>
  /// it hugs the glyph's alpha outline; otherwise it is a box the size of the glyph.
  /// </summary>
  public Border? IconBorder { get; set; }

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
