namespace ScarletCore.Interface.Builders;

/// <summary>Controls which characters are accepted by a text input field.</summary>
public enum InputType {
  /// <summary>Accepts any printable character (default).</summary>
  String,
  /// <summary>Accepts only digits, a single decimal point, and an optional leading minus sign.</summary>
  Number,
}
