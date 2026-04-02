using ScarletCore.Interface.Builders;

namespace ScarletCore.Interface.Elements;

/// <summary>A clickable button that sends a chat command to the server.</summary>
public class Button : UIElement, ITextElement {
  /// <summary>Button label text.</summary>
  public string Label { get; set; }
  /// <summary>Chat command sent to the server when the button is clicked.</summary>
  public string Command { get; set; }
  /// <summary>Whether padding is included in or added to the declared size.</summary>
  public BoxSizing BoxSizing { get; set; }
  /// <summary>Background shown when the cursor hovers over the button.</summary>
  public UIBackground? HoverBackground { get; set; }
  /// <summary>Background shown while the button is pressed.</summary>
  public UIBackground? PressedBackground { get; set; }

  // ─── ITextElement ────────────────────────────────────────────────────────
  public UIColor? TextColor { get; set; }
  public float FontSize { get; set; }
  public string Font { get; set; }
  public UITextGradient? TextGradient { get; set; }
  public UITextShadow? TextShadow { get; set; }
  public UITextOutline? TextOutline { get; set; }
}
