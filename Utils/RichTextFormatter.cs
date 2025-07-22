using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace ScarletCore.Utils;

/// <summary>
/// Utility class for formatting text with rich text markup and colors.
/// Provides methods for styling text, creating progress bars, boxes, and system messages.
/// </summary>
public static class RichTextFormatter {

  #region Color Constants

  /// <summary>Red color hex code</summary>
  public static readonly string Red = "#ff0000";
  /// <summary>Green color hex code</summary>
  public static readonly string Green = "#00ff00";
  /// <summary>Blue color hex code</summary>
  public static readonly string Blue = "#0000ff";
  /// <summary>Yellow color hex code</summary>
  public static readonly string Yellow = "#ffff00";
  /// <summary>Orange color hex code</summary>
  public static readonly string Orange = "#ffa500";
  /// <summary>Purple color hex code</summary>
  public static readonly string Purple = "#800080";
  /// <summary>Pink color hex code</summary>
  public static readonly string Pink = "#ffc0cb";
  /// <summary>White color hex code</summary>
  public static readonly string White = "#ffffff";
  /// <summary>Gray color hex code</summary>
  public static readonly string Gray = "#808080";
  /// <summary>Cyan color hex code</summary>
  public static readonly string Cyan = "#00ffff";
  /// <summary>Default highlight color</summary>
  public static readonly string HighlightColor = "#a963ff";
  /// <summary>Error highlight color</summary>
  public static readonly string HighlightErrorColor = "#ff4040";
  /// <summary>Warning highlight color</summary>
  public static readonly string HighlightWarningColor = "#ffff00";
  /// <summary>Default text color</summary>
  public static readonly string TextColor = "#ffffff";
  /// <summary>Success text color</summary>
  public static readonly string SuccessTextColor = "#9cff9c";
  /// <summary>Error text color</summary>
  public static readonly string ErrorTextColor = "#ff8f8f";
  /// <summary>Warning text color</summary>
  public static readonly string WarningTextColor = "#ffff9e";

  #endregion

  #region Basic Text Formatting

  /// <summary>
  /// Makes text bold using HTML-like markup.
  /// </summary>
  /// <param name="text">Text to format</param>
  /// <returns>Bold formatted text</returns>
  public static string Bold(this string text) => $"<b>{text}</b>";

  /// <summary>
  /// Makes text italic using HTML-like markup.
  /// </summary>
  /// <param name="text">Text to format</param>
  /// <returns>Italic formatted text</returns>
  public static string Italic(this string text) => $"<i>{text}</i>";

  /// <summary>
  /// Makes text underlined using HTML-like markup.
  /// </summary>
  /// <param name="text">Text to format</param>
  /// <returns>Underlined formatted text</returns>
  public static string Underline(this string text) => $"<u>{text}</u>";

  /// <summary>
  /// Colors text red using predefined red color.
  /// </summary>
  /// <param name="text">Text to color</param>
  /// <returns>Red colored text</returns>
  public static string ToRed(this string text) => $"<color=red>{text}</color>";

  /// <summary>
  /// Colors text green using predefined green color.
  /// </summary>
  /// <param name="text">Text to color</param>
  /// <returns>Green colored text</returns>
  public static string ToGreen(this string text) => $"<color=green>{text}</color>";

  /// <summary>
  /// Colors text blue using predefined blue color.
  /// </summary>
  /// <param name="text">Text to color</param>
  /// <returns>Blue colored text</returns>
  public static string ToBlue(this string text) => $"<color=blue>{text}</color>";

  /// <summary>
  /// Colors text yellow using predefined yellow color.
  /// </summary>
  /// <param name="text">Text to color</param>
  /// <returns>Yellow colored text</returns>
  public static string ToYellow(this string text) => $"<color=yellow>{text}</color>";

  /// <summary>
  /// Colors text white using predefined white color.
  /// </summary>
  /// <param name="text">Text to color</param>
  /// <returns>White colored text</returns>
  public static string ToWhite(this string text) => $"<color=white>{text}</color>";

  /// <summary>
  /// Colors text black using predefined black color.
  /// </summary>
  /// <param name="text">Text to color</param>
  /// <returns>Black colored text</returns>
  public static string ToBlack(this string text) => $"<color=black>{text}</color>";

  /// <summary>
  /// Colors text gray using a light gray hex color.
  /// </summary>
  /// <param name="text">Text to color</param>
  /// <returns>Gray colored text</returns>
  public static string ToGray(this string text) => $"<color=#cccccc>{text}</color>";

  /// <summary>
  /// Colors text using a custom hex color code.
  /// </summary>
  /// <param name="text">Text to color</param>
  /// <param name="hex">Hex color code (e.g., "#ff0000")</param>
  /// <returns>Custom colored text</returns>
  public static string WithColor(this string text, string hex) => $"<color={hex}>{text}</color>";

  #endregion

  #region Advanced Text Formatting

  /// <summary>
  /// Applies markdown-style formatting with custom highlight colors.
  /// Supports **bold**, *italic*, __underline__, and ~highlight~ syntax.
  /// </summary>
  /// <param name="text">Text with markdown formatting</param>
  /// <param name="highlightColors">List of colors for highlights (optional)</param>
  /// <returns>Formatted text with applied styles</returns>
  public static string Format(this string text, List<string> highlightColors = null) {
    highlightColors ??= [HighlightColor];
    return ApplyFormatting(text, TextColor, highlightColors);
  }

  /// <summary>
  /// Formats text with error styling and error highlight colors.
  /// </summary>
  /// <param name="text">Text to format as error</param>
  /// <returns>Error-styled text</returns>
  public static string FormatError(this string text) {
    return ApplyFormatting(text, ErrorTextColor, [HighlightErrorColor]);
  }

  /// <summary>
  /// Formats text with warning styling and warning highlight colors.
  /// </summary>
  /// <param name="text">Text to format as warning</param>
  /// <returns>Warning-styled text</returns>
  public static string FormatWarning(this string text) {
    return ApplyFormatting(text, WarningTextColor, [HighlightWarningColor]);
  }

  /// <summary>
  /// Formats text with success styling and green highlight colors.
  /// </summary>
  /// <param name="text">Text to format as success</param>
  /// <returns>Success-styled text</returns>
  public static string FormatSuccess(this string text) {
    return ApplyFormatting(text, SuccessTextColor, [Green]);
  }

  /// <summary>
  /// Formats text with info styling and blue highlight colors.
  /// </summary>
  /// <param name="text">Text to format as info</param>
  /// <returns>Info-styled text</returns>
  public static string FormatInfo(this string text) {
    return ApplyFormatting(text, TextColor, [Blue]);
  }

  #endregion

  #region System Message Formatters

  /// <summary>
  /// Formats text as an error message with [ERROR] prefix.
  /// </summary>
  /// <param name="text">Error message text</param>
  /// <returns>Formatted error message in red</returns>
  public static string AsError(this string text) => $"[ERROR] {text}".WithColor(Red);

  /// <summary>
  /// Formats text as a success message with [SUCCESS] prefix.
  /// </summary>
  /// <param name="text">Success message text</param>
  /// <returns>Formatted success message in green</returns>
  public static string AsSuccess(this string text) => $"[SUCCESS] {text}".WithColor(Green);

  /// <summary>
  /// Formats text as a warning message with [WARNING] prefix.
  /// </summary>
  /// <param name="text">Warning message text</param>
  /// <returns>Formatted warning message in yellow</returns>
  public static string AsWarning(this string text) => $"[WARNING] {text}".WithColor(Yellow);

  /// <summary>
  /// Formats text as an info message with [INFO] prefix.
  /// </summary>
  /// <param name="text">Info message text</param>
  /// <returns>Formatted info message in blue</returns>
  public static string AsInfo(this string text) => $"[INFO] {text}".WithColor(Blue);

  /// <summary>
  /// Formats text as an announcement with [ANNOUNCEMENT] prefix.
  /// </summary>
  /// <param name="text">Announcement text</param>
  /// <returns>Formatted announcement in orange</returns>
  public static string AsAnnouncement(this string text) => $"[ANNOUNCEMENT] {text}".WithColor(Orange);

  /// <summary>
  /// Formats text as a list item with [LIST] prefix.
  /// </summary>
  /// <param name="text">List item text</param>
  /// <returns>Formatted list item</returns>
  public static string AsList(this string text) => $"[LIST] {text}";

  #endregion

  #region Player Action Formatters

  /// <summary>
  /// Formats a player join message.
  /// </summary>
  /// <param name="playerName">Name of the joining player</param>
  /// <returns>Formatted join message in green</returns>
  public static string AsPlayerJoin(this string playerName) => $"{playerName} has joined the server!".WithColor(Green);

  /// <summary>
  /// Formats a player leave message.
  /// </summary>
  /// <param name="playerName">Name of the leaving player</param>
  /// <returns>Formatted leave message in gray</returns>
  public static string AsPlayerLeave(this string playerName) => $"{playerName} has left the server".WithColor(Gray);

  /// <summary>
  /// Formats a player death message with optional killer information.
  /// </summary>
  /// <param name="victimName">Name of the player who died</param>
  /// <param name="killerName">Name of the killer (optional)</param>
  /// <returns>Formatted death message in red</returns>
  public static string AsPlayerDeath(this string victimName, string killerName = null) {
    return string.IsNullOrEmpty(killerName)
      ? $"{victimName} has died".WithColor(Red)
      : $"{victimName} was killed by {killerName}".WithColor(Red);
  }

  #endregion

  #region Progress and UI Formatters

  /// <summary>
  /// Creates a visual progress bar for console output.
  /// </summary>
  /// <param name="label">Label for the progress bar</param>
  /// <param name="current">Current progress value</param>
  /// <param name="max">Maximum progress value</param>
  /// <param name="barLength">Length of the progress bar in characters</param>
  /// <returns>Formatted progress bar with percentage</returns>
  public static string AsProgressBar(this string label, int current, int max, int barLength = 20) {
    var percentage = (float)current / max;
    var filledLength = (int)(percentage * barLength);
    var emptyLength = barLength - filledLength;

    // Use filled and empty block characters for visual progress
    var bar = new string('█', filledLength) + new string('░', emptyLength);
    return $"{label}: [{bar}] {current}/{max} ({percentage:P0})".WithColor(Cyan);
  }

  /// <summary>
  /// Creates a boxed title with decorative borders.
  /// </summary>
  /// <param name="title">Title text to box</param>
  /// <returns>Title surrounded by box drawing characters</returns>
  public static string AsBoxedTitle(this string title) {
    var border = new string('═', title.Length + 4);
    return $"╔{border}╗\n║  {title}  ║\n╚{border}╝";
  }

  /// <summary>
  /// Creates boxed content with proper padding and borders.
  /// </summary>
  /// <param name="content">Content to box</param>
  /// <param name="title">Title for calculating box width</param>
  /// <returns>Content lines formatted within box borders</returns>
  public static string AsBoxedContent(this string content, string title) {
    var titleLength = title.Length + 4;
    var lines = content.Split('\n');
    var result = "";

    // Process each line and add proper padding
    foreach (var line in lines) {
      var padding = Math.Max(0, titleLength - line.Length - 2);
      result += $"║ {line}{new string(' ', padding)} ║\n";
    }

    return result.TrimEnd('\n');
  }

  /// <summary>
  /// Creates a separator line using repeated characters.
  /// </summary>
  /// <param name="character">Character to repeat</param>
  /// <param name="length">Length of the separator</param>
  /// <returns>Formatted separator line in gray</returns>
  public static string AsSeparator(this char character, int length = 40) {
    return new string(character, length).WithColor(Gray);
  }

  #endregion

  #region Countdown Formatters

  /// <summary>
  /// Formats countdown messages with appropriate urgency colors.
  /// Changes color based on time remaining (minutes=warning, seconds=warning, under 10=error).
  /// </summary>
  /// <param name="action">Action that will occur</param>
  /// <param name="seconds">Seconds remaining</param>
  /// <returns>Formatted countdown message with appropriate color</returns>
  public static string AsCountdown(this string action, int seconds) {
    if (seconds > 60) {
      var minutes = seconds / 60;
      return $"{action} in {minutes} minute{(minutes != 1 ? "s" : "")}!".AsWarning();
    } else if (seconds > 10) {
      return $"{action} in {seconds} seconds!".AsWarning();
    } else if (seconds > 0) {
      return $"{action} in {seconds}!".AsError();
    } else {
      return $"{action} NOW!".AsError();
    }
  }

  #endregion

  #region Private Formatting Methods

  /// <summary>
  /// Applies markdown-style formatting to text using regex patterns.
  /// Processes **bold**, *italic*, __underline__, and ~highlight~ syntax.
  /// </summary>
  /// <param name="text">Text to format</param>
  /// <param name="baseColor">Base color for the text</param>
  /// <param name="highlightColors">Colors to use for highlights</param>
  /// <returns>Formatted text with applied styles and colors</returns>
  private static string ApplyFormatting(string text, string baseColor, List<string> highlightColors) {
    // Define regex patterns for different formatting styles
    var boldPattern = @"\*\*(.*?)\*\*";        // **text**
    var italicPattern = @"\*(.*?)\*";          // *text*
    var underlinePattern = @"__(.*?)__";       // __text__
    var highlightPattern = @"~(.*?)~";         // ~text~

    // Apply formatting in order of precedence
    var result = Regex.Replace(text, boldPattern, m => Bold(m.Groups[1].Value));
    result = Regex.Replace(result, italicPattern, m => Italic(m.Groups[1].Value));
    result = Regex.Replace(result, underlinePattern, m => Underline(m.Groups[1].Value));

    // Apply highlights with cycling colors
    int highlightIndex = 0;
    result = Regex.Replace(result, highlightPattern, m => {
      string color;
      if (highlightColors.Count > 0) {
        // Use the color at current index, or the last color if index exceeds list
        int colorIndex = Math.Min(highlightIndex, highlightColors.Count - 1);
        color = highlightColors[colorIndex];
      } else {
        // Fallback to HighlightColor if no colors provided
        color = HighlightColor;
      }

      if (color == null) color = HighlightColor;
      highlightIndex++;
      return m.Groups[1].Value.WithColor(color);
    });

    // Apply base color to entire text
    return result.WithColor(baseColor);
  }

  #endregion
}