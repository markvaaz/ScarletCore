using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>
/// A dropdown selector. On selection, sends a chat command with the chosen value.
/// </summary>
public class Dropdown : UIElement, ITextElement {
  /// <summary>Unique identifier used as the placeholder token in <see cref="Command"/>.</summary>
  public string Id { get; set; }
  /// <summary>Pipe-separated option pairs: <c>"Label A:val1|Label B:val2"</c>. If no colon, label equals value.</summary>
  public string Options { get; set; }
  /// <summary>Chat command template sent on selection. <c>{Id}</c> is replaced with the selected value.</summary>
  public string Command { get; set; }
  /// <summary>Popup panel background color.</summary>
  public UIColor? DropdownBackgroundColor { get; set; }
  /// <summary>Option label color inside the popup.</summary>
  public UIColor? DropdownTextColor { get; set; }
  /// <summary>Option highlight color on hover.</summary>
  public UIColor? DropdownHoverColor { get; set; }
  /// <summary>Maximum popup panel height in pixels before scrolling. Default: 200.</summary>
  public float MaxHeight { get; set; } = 200f;
  /// <summary>Whether padding is included in or added to the declared size.</summary>
  public BoxSizing BoxSizing { get; set; }
  /// <summary>Text shown when no value is selected.</summary>
  public string Placeholder { get; set; } = "Select...";
  /// <summary>Pre-selected value.</summary>
  public string Value { get; set; }

  // ─── ITextElement ────────────────────────────────────────────────────────
  public UIColor? TextColor { get; set; }
  public float FontSize { get; set; }
  public string Font { get; set; }
  public UITextGradient? TextGradient { get; set; }
  public UITextShadow? TextShadow { get; set; }
  public UITextOutline? TextOutline { get; set; }
}
