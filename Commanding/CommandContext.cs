using System;
using System.Reflection;
using ScarletCore.Data;
using ScarletCore.Localization;
using ScarletCore.Services;
using ScarletCore.Utils;
using Unity.Entities;

namespace ScarletCore.Commanding;

/// <summary>
/// Context passed to command handlers.
/// </summary>
/// <summary>
/// Context passed to command handlers. Contains information about the sender, the raw message and helpers to reply.
/// </summary>
public sealed class CommandContext {
  /// <summary>The chat message entity that originated this command.</summary>
  public Entity MessageEntity { get; }
  /// <summary>The <see cref="PlayerData"/> of the player who sent the command.</summary>
  public PlayerData Sender { get; }
  /// <summary>The raw message text as sent by the player.</summary>
  public string Raw { get; }
  /// <summary>Tokenized arguments parsed from the raw message.</summary>
  public string[] Args { get; }
  /// <summary>
  /// The assembly to use when resolving localization keys for localized replies.
  /// When commands are registered from another assembly, the command system sets this so lookups resolve correctly.
  /// </summary>
  public Assembly CallingAssembly { get; set; }

  /// <summary>
  /// Creates a new <see cref="CommandContext"/> instance.
  /// </summary>
  /// <param name="messageEntity">The chat message entity.</param>
  /// <param name="sender">The player who sent the command.</param>
  /// <param name="raw">The raw message text.</param>
  /// <param name="args">Tokenized arguments.</param>
  public CommandContext(Entity messageEntity, PlayerData sender, string raw, string[] args) {
    ArgumentNullException.ThrowIfNull(sender);
    Sender = sender;
    MessageEntity = messageEntity;
    Raw = raw;
    Args = args;
  }

  /// <summary>Sends a plain (unlocalized) reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void Reply(string message) {
    MessageService.SendRaw(Sender, message.Format());
  }

  /// <summary>Sends an error-styled reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void ReplyError(string message) {
    MessageService.SendRaw(Sender, message.FormatError());
  }

  /// <summary>Sends a warning-styled reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void ReplyWarning(string message) {
    MessageService.SendRaw(Sender, message.FormatWarning());
  }

  /// <summary>Sends an informational-styled reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void ReplyInfo(string message) {
    MessageService.SendRaw(Sender, message.FormatInfo());
  }

  /// <summary>Sends a success-styled reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void ReplySuccess(string message) {
    MessageService.SendRaw(Sender, message.FormatSuccess());
  }

  /// <summary>Sends a localized reply using the given localization key and optional formatting parameters.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalized(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.Format());
  }

  /// <summary>Sends a localized error-styled reply using the given localization key.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedError(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.FormatError());
  }

  /// <summary>Sends a localized warning-styled reply using the given localization key.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedWarning(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.FormatWarning());
  }

  /// <summary>Sends a localized informational-styled reply using the given localization key.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedInfo(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.FormatInfo());
  }

  /// <summary>Sends a localized success-styled reply using the given localization key.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedSuccess(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.FormatSuccess());
  }
}