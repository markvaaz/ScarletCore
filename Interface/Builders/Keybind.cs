namespace ScarletCore.Interface.Builders;

/// <summary>
/// A server keybind that the player can re-map from the in-game Controls options menu.
/// The <see cref="Label"/> is what the player sees next to the rebind button; the
/// <see cref="Command"/> is the stable identity (what runs on press and what the player's
/// override is keyed to). <see cref="Key"/> is the default until the player changes it.
/// <para>
/// Set <see cref="ToggleWindow"/> to a window id to make the key a toggle: pressing it
/// closes that window when it is open, or runs <see cref="Command"/> (to open it) when it
/// is not. This keeps open and close on the same, single re-bindable key — no separate
/// window <c>CloseKey</c> is needed.
/// </para>
/// </summary>
/// <example>
/// InterfaceManager.SetKeybinds(player, "myplugin",
///   new Keybind(InputKey.G,  ".openwindow shop", "Shop", toggleWindow: "shop"),
///   new Keybind(InputKey.F5, ".refreshshop",     "Refresh Shop"));
/// </example>
public readonly struct Keybind {
  /// <summary>Default key, used until the player rebinds it from the Controls menu.</summary>
  public InputKey Key { get; }
  /// <summary>Chat command run on press; also the stable identity the player's override is keyed to.</summary>
  public string Command { get; }
  /// <summary>Text shown next to the rebind button in the Controls menu. Falls back to <see cref="Command"/> when null.</summary>
  public string Label { get; }
  /// <summary>Window id to toggle: press closes it when open, otherwise runs <see cref="Command"/>. Null = not a toggle.</summary>
  public string ToggleWindow { get; }

  /// <summary>Creates a keybind. See the type summary for how <paramref name="toggleWindow"/> turns the key into a window toggle.</summary>
  public Keybind(InputKey key, string command, string label = null, string toggleWindow = null) {
    Key = key;
    Command = command;
    Label = label;
    ToggleWindow = toggleWindow;
  }
}
