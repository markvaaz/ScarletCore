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
  /// <summary>Whether padding is included in or added to the declared size.</summary>
  public BoxSizing BoxSizing { get; set; }
  /// <summary>Pre-filled text value.</summary>
  public string Value { get; set; }

  // ─── ITextElement ────────────────────────────────────────────────────────
  public UIColor? TextColor { get; set; }
  public float FontSize { get; set; }
  public string Font { get; set; }
  public UITextGradient? TextGradient { get; set; }
  public UITextShadow? TextShadow { get; set; }
  public UITextOutline? TextOutline { get; set; }
}
