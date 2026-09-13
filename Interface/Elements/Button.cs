using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>A clickable button that sends a chat command to the server.</summary>
public class Button : UIElement, ITextElement {
  /// <summary>Button label text.</summary>
  public string Label { get; set; }
  /// <summary>Chat command sent to the server when the button is clicked.</summary>
  public string Command { get; set; }
  /// <summary>Optional sound URLs played on the client when the button is clicked. The client
  /// picks one at random per click. Empty/null = silent (fully opt-in, no default sound).</summary>
  public string[] ClickSounds { get; set; }
  /// <summary>Whether padding is included in or added to the declared size.</summary>
  public BoxSizing BoxSizing { get; set; }
  /// <summary>Background shown when the cursor hovers over the button.</summary>
  public UIBackground? HoverBackground { get; set; }
  /// <summary>Background shown while the button is pressed.</summary>
  public UIBackground? PressedBackground { get; set; }

  /// <summary>Border shown while the pointer is over the button (falls back to <see cref="UIElement.Border"/>).
  /// Content-aware, it traces this state's art (<see cref="HoverBackground"/> image/sprite, else the normal one).</summary>
  public Border? HoverBorder { get; set; }
  /// <summary>Border shown while the button is pressed (falls back to <see cref="HoverBorder"/>, then <see cref="UIElement.Border"/>).</summary>
  public Border? PressedBorder { get; set; }
  /// <summary>Scale applied on hover (e.g. 1.05). 0 or 1 disables the effect.</summary>
  public float HoverScale { get; set; }
  /// <summary>Horizontal alignment of the button label. Default: Left.</summary>
  public TextAlignment TextAlign { get; set; } = TextAlignment.Center;
  /// <summary>Wrap the label onto multiple lines when it exceeds the button width,
  /// instead of overflowing past the edges. Off by default (single-line). Give the button
  /// enough height (or Height="auto") for the extra lines.</summary>
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
