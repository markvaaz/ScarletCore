namespace ScarletCore.Interface.Builders;

/// <summary>
/// Action to execute on the window when <see cref="Window.Send"/> is called.
/// The action is always applied <em>after</em> all element packets are sent.
/// </summary>
public enum WindowAction {
  /// <summary>No lifecycle action — only element packets are sent.</summary>
  None,
  /// <summary>Opens the window and makes it visible to the player.</summary>
  Open,
  /// <summary>Hides the window without destroying its content.</summary>
  Close,
  /// <summary>Removes all elements from the window (keeps the window alive).</summary>
  Clear,
  /// <summary>Destroys and fully recreates the window on the next Open.</summary>
  Reset,
}
