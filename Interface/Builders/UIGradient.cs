using System;
using System.Globalization;

namespace ScarletCore.Interface.Builders;

/// <summary>A gradient descriptor for background fields.</summary>
public readonly struct UIGradient {
  readonly string _raw;
  UIGradient(string v) => _raw = v;
  internal bool HasValue => !string.IsNullOrEmpty(_raw);
  internal string Raw => _raw;

  /// <summary>Linear gradient. <paramref name="angle"/> in degrees: 0=left→right, 90=bottom→top. Requires 2 or more colors.</summary>
  public static UIGradient Linear(float angle, params UIColor[] colors) {
    if (colors == null || colors.Length < 2) throw new ArgumentException("UIGradient.Linear requires at least 2 colors.", nameof(colors));
    var stops = string.Join("|", Array.ConvertAll(colors, c => (string)c));
    return new($"linear|{angle.ToString(CultureInfo.InvariantCulture)}|{stops}");
  }

  /// <summary>Radial gradient with center at (<paramref name="cx"/>, <paramref name="cy"/>) in 0–1 coords. Requires 2 or more colors.</summary>
  public static UIGradient Radial(float cx, float cy, params UIColor[] colors) {
    if (colors == null || colors.Length < 2) throw new ArgumentException("UIGradient.Radial requires at least 2 colors.", nameof(colors));
    var stops = string.Join("|", Array.ConvertAll(colors, c => (string)c));
    return new($"radial|{cx.ToString(CultureInfo.InvariantCulture)}|{cy.ToString(CultureInfo.InvariantCulture)}|{stops}");
  }
}
