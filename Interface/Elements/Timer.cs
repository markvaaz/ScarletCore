using System;

namespace ScarletCore.Interface.Elements;

/// <summary>How a <see cref="Timer"/> interprets its <see cref="Timer.Date"/>.</summary>
public enum TimerMode {
  /// <summary>Counts down from now to <see cref="Timer.Date"/>, clamping at zero.</summary>
  Countdown,
  /// <summary>Counts up from <see cref="Timer.Date"/> (elapsed time).</summary>
  Countup,
  /// <summary>Displays <see cref="Timer.Date"/> as a running clock advancing in real time.</summary>
  Clock,
}

/// <summary>
/// A self-updating text element. The server sends one absolute date; the client re-renders the
/// formatted value locally every second — no further packets. All <see cref="Text"/> styling
/// applies. The client cancels and discards the timer when its window is closed.
/// </summary>
public class Timer : Text {
  /// <summary>Reference date. Treated as UTC when Kind is Unspecified.</summary>
  public DateTime Date { get; set; }
  /// <summary>
  /// Display format, two modes.
  /// <para>Placeholder mode — the string contains <c>{...}</c>: everything outside braces is
  /// literal (words are safe) and the tokens are named: <c>{days}</c>/<c>{dias}</c>/<c>{dd}</c>,
  /// <c>{hours}</c>/<c>{hh}</c>, <c>{minutes}</c>/<c>{mm}</c>, <c>{seconds}</c>/<c>{ss}</c>,
  /// <c>{month}</c>/<c>{mo}</c>, <c>{year}</c>/<c>{yyyy}</c>. Repeated-letter names zero-pad
  /// (<c>{mm}</c> → "05"), word names don't. E.g. <c>"Faltam {minutes} minutos"</c>.</para>
  /// <para>Run mode — no braces: token runs in any order/combination: dd=days, HH=hours,
  /// SS=seconds, AAAA/yyyy=year, and mm/MM resolves to minutes next to time tokens (HH/SS) or
  /// month next to date tokens (dd/yyyy). Everything else is literal, but token LETTERS inside
  /// words are consumed — use placeholder mode for prose. E.g. "dd/mm/AAAA HH:MM:SS".</para>
  /// In Countdown/Countup the largest unit present absorbs the rest (no HH → total minutes).
  /// </summary>
  public string Format { get; set; } = "HH:MM:SS";
  /// <summary>Countdown (default), Countup, or Clock.</summary>
  public TimerMode Mode { get; set; } = TimerMode.Countdown;
  /// <summary>
  /// Command executed client-side the moment a Countdown reaches zero — same syntax as
  /// <see cref="Button.Command"/> (server command, <c>close-window</c>, comma for multiple).
  /// Fires exactly once, and only if the countdown was still running when it arrived (a timer
  /// that arrives already expired never fires — prevents resend loops). Dies with the window:
  /// closing it before zero cancels the command. Ignored on Countup/Clock.
  /// Set <see cref="Format"/> to <c>""</c> for an invisible utility timer that only runs the
  /// command — a client-side scheduler instead of a server one.
  /// </summary>
  public string Command { get; set; }
}
