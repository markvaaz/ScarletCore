namespace ScarletCore.Interface.Builders;

/// <summary>
/// Strongly-typed mirror of Unity's <c>UnityEngine.KeyCode</c> enum for use in
/// server-side keybind configuration without a Unity dependency.
/// <para>
/// Each member serializes by name (e.g. <c>InputKey.G</c> → <c>"G"</c>), which
/// the client resolves back to the matching <c>UnityEngine.KeyCode</c> value at runtime.
/// </para>
/// <example>
/// <code>
/// // Window close keybind:
/// new Window(player, "myplugin", "shop") { CloseKey = InputKey.G, ... }.Send();
///
/// // Global command keybinds:
/// InterfaceManager.SetKeybinds(player, "myplugin", new() {
///   [InputKey.G]  = ".openwindow shop",
///   [InputKey.F5] = ".refreshshop",
/// });
/// </code>
/// </example>
/// </summary>
public enum InputKey {
  None = 0,

  // ── Special ────────────────────────────────────────────────────────────────
  Backspace = 8,
  Tab = 9,
  Clear = 12,
  Return = 13,
  Pause = 19,
  Escape = 27,
  Space = 32,
  Delete = 127,

  // ── Punctuation / symbols ──────────────────────────────────────────────────
  Exclaim = 33,   // !
  DoubleQuote = 34,   // "
  Hash = 35,   // #
  Dollar = 36,   // $
  Percent = 37,   // %
  Ampersand = 38,   // &
  Quote = 39,   // '
  LeftParen = 40,   // (
  RightParen = 41,   // )
  Asterisk = 42,   // *
  Plus = 43,   // +
  Comma = 44,   // ,
  Minus = 45,   // -
  Period = 46,   // .
  Slash = 47,   // /
  Colon = 58,   // :
  Semicolon = 59,   // ;
  Less = 60,   // <
  Equals = 61,   // =
  Greater = 62,   // >
  Question = 63,   // ?
  At = 64,   // @
  LeftBracket = 91,   // [
  Backslash = 92,   // \
  RightBracket = 93,   // ]
  Caret = 94,   // ^
  Underscore = 95,   // _
  BackQuote = 96,   // ` (tilde key)

  // ── Number row ─────────────────────────────────────────────────────────────
  Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4,
  Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,

  // ── Letters ────────────────────────────────────────────────────────────────
  A = 97, B, C, D, E, F, G, H, I, J, K, L, M,
  N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

  // ── Numpad ─────────────────────────────────────────────────────────────────
  Keypad0 = 256, Keypad1, Keypad2, Keypad3, Keypad4,
  Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
  KeypadPeriod = 266,
  KeypadDivide = 267,
  KeypadMultiply = 268,
  KeypadMinus = 269,
  KeypadPlus = 270,
  KeypadEnter = 271,
  KeypadEquals = 272,

  // ── Arrow keys ─────────────────────────────────────────────────────────────
  UpArrow = 273,
  DownArrow = 274,
  RightArrow = 275,
  LeftArrow = 276,

  // ── Navigation ─────────────────────────────────────────────────────────────
  Insert = 277,
  Home = 278,
  End = 279,
  PageUp = 280,
  PageDown = 281,

  // ── Function keys ──────────────────────────────────────────────────────────
  F1 = 282, F2, F3, F4, F5, F6,
  F7, F8, F9, F10, F11, F12, F13, F14, F15,

  // ── Lock / system ──────────────────────────────────────────────────────────
  Numlock = 300,
  CapsLock = 301,
  ScrollLock = 302,
  Print = 316,
  SysReq = 317,
  Break = 318,
  Menu = 319,

  // ── Modifier keys ──────────────────────────────────────────────────────────
  RightShift = 303,
  LeftShift = 304,
  RightControl = 305,
  LeftControl = 306,
  RightAlt = 307,
  LeftAlt = 308,
  RightCommand = 309,
  LeftCommand = 310,
  LeftWindows = 311,
  RightWindows = 312,
  AltGr = 313,

  // ── Mouse buttons ──────────────────────────────────────────────────────────
  Mouse0 = 323,
  Mouse1 = 324,
  Mouse2 = 325,
  Mouse3 = 326,
  Mouse4 = 327,
  Mouse5 = 328,
  Mouse6 = 329,
}
