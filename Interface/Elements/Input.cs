using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A text input field. Its value can be read by other elements via <c>{Id}</c> token replacement.
/// </summary>
public class Input : UIElement, ITextElement {
  /// <summary>Unique identifier used to reference this input's value in other elements' commands.</summary>
  public string Id { get; set; }
  /// <summary>Greyed-out hint text shown when the input is empty.</summary>
  public string Placeholder { get; set; }
  /// <summary>Placeholder text color.</summary>
  public UIColor? PlaceholderColor { get; set; }
  /// <summary>Whether padding is included in or added to the declared size. Defaults to BorderBox (declared Width/Height = total element size).</summary>
  public BoxSizing BoxSizing { get; set; } = BoxSizing.BorderBox;
  /// <summary>Pre-filled text value.</summary>
  public string Value { get; set; }
  /// <summary>Horizontal text alignment. Defaults to Left.</summary>
  public TextAlignment TextAlign { get; set; } = TextAlignment.Left;
  /// <summary>Controls which characters are accepted. Number allows only digits, decimal point, and optional leading minus.</summary>
  public InputType InputType { get; set; } = InputType.String;
  /// <summary>Maximum number of characters (0 = unlimited).</summary>
  public int MaxLength { get; set; }
  /// <summary>Background shown while the field is focused. Supports color, gradient, image, and sprite.</summary>
  public UIBackground? FocusBackground { get; set; }
  /// <summary>Border shown while the field is focused.</summary>
  public Border? FocusBorder { get; set; }
  /// <summary>Color of the blinking caret (default: near-white).</summary>
  public UIColor? CaretColor { get; set; }
  /// <summary>Background color of the text selection highlight.</summary>
  public UIColor? SelectionColor { get; set; }
  /// <summary>Text color of selected characters (set on the TMP vertex colors if supported).</summary>
  public UIColor? SelectionTextColor { get; set; }

  // ─── ITextElement ────────────────────────────────────────────────────────
  public UIColor? TextColor { get; set; }
  public float FontSize { get; set; }
  public string Font { get; set; }
  public UITextGradient? TextGradient { get; set; }
  public UITextShadow? TextShadow { get; set; }
  public UITextOutline? TextOutline { get; set; }
}
