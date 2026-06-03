using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A multi-line text input field with fixed configurable height and internal vertical scroll.
/// Enter submits (fires <see cref="OnSubmit"/>); Shift+Enter inserts a newline.
/// Its value can be read by other elements via <c>{Id}</c> token replacement.
/// </summary>
public class TextArea : UIElement, ITextElement
{
  /// <summary>Unique identifier used to reference this textarea's value in other elements' commands.</summary>
  public string Id { get; set; }
  /// <summary>Greyed-out hint text shown when the textarea is empty.</summary>
  public string Placeholder { get; set; }
  /// <summary>Placeholder text color.</summary>
  public UIColor? PlaceholderColor { get; set; }
  /// <summary>Whether padding is included in or added to the declared size. Defaults to BorderBox.</summary>
  public BoxSizing BoxSizing { get; set; } = BoxSizing.BorderBox;
  /// <summary>Pre-filled text value. Newlines are supported.</summary>
  public string Value { get; set; }
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
  /// <summary>Text color of selected characters.</summary>
  public UIColor? SelectionTextColor { get; set; }
  /// <summary>
  /// Command sent to the server when the user presses Enter. Supports <c>{Id}</c> token
  /// replacement — use the TextArea's own <see cref="Id"/> or any other input/dropdown Id to
  /// inject its current value into the command string.
  /// </summary>
  public string OnSubmit { get; set; }
  /// <summary>Whether to show the scrollbar when content overflows. Defaults to true.</summary>
  public bool ShowScrollbar { get; set; } = true;
  /// <summary>Color of the scrollbar thumb. Defaults to a semi-transparent white.</summary>
  public UIColor? ScrollbarColor { get; set; }
  /// <summary>Color of the scrollbar track background. Defaults to a dark transparent.</summary>
  public UIColor? ScrollbarBackgroundColor { get; set; }
  /// <summary>Width of the scrollbar in pixels. Defaults to 6.</summary>
  public float ScrollbarWidth { get; set; } = 6f;

  // ─── ITextElement ────────────────────────────────────────────────────────
  public UIColor? TextColor { get; set; }
  public float FontSize { get; set; }
  public string Font { get; set; }
  public UITextGradient? TextGradient { get; set; }
  public UITextShadow? TextShadow { get; set; }
  public UITextOutline? TextOutline { get; set; }
}
