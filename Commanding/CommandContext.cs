using System;
using System.Collections.Generic;
using System.Reflection;
using ProjectM.Network;
using ScarletCore.Events;
using ScarletCore.Localization;
using ScarletCore.Services;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Collections;
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
  /// <summary>Gets the command name.</summary>
  public string CommandName {
    get {
      if (Args == null || Args.Length == 0) return Raw;

      var argsString = string.Join(" ", Args);
      var index = Raw.IndexOf(argsString, StringComparison.Ordinal);

      if (index == -1) return Raw;

      return Raw.Substring(0, index).TrimEnd();
    }
  }
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
    message = ProcessPlaceholders(message);
    MessageService.SendRaw(Sender, message.Format());
  }

  /// <summary>Sends an error-styled reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void ReplyError(string message) {
    message = ProcessPlaceholders(message);
    MessageService.SendRaw(Sender, message.FormatError());
  }

  /// <summary>Sends a warning-styled reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void ReplyWarning(string message) {
    message = ProcessPlaceholders(message);
    MessageService.SendRaw(Sender, message.FormatWarning());
  }

  /// <summary>Sends an informational-styled reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void ReplyInfo(string message) {
    message = ProcessPlaceholders(message);
    MessageService.SendRaw(Sender, message.FormatInfo());
  }

  /// <summary>Sends a success-styled reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void ReplySuccess(string message) {
    message = ProcessPlaceholders(message);
    MessageService.SendRaw(Sender, message.FormatSuccess());
  }

  /// <summary>Sends a raw (unformatted and unlocalized) reply to the sender.</summary>
  /// <param name="message">Message text to send.</param>
  public void ReplyRaw(string message) {
    message = ProcessPlaceholders(message);
    MessageService.SendRaw(Sender, message);
  }

  /// <summary>Sends a localized reply using the given localization key and optional formatting parameters.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalized(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    localized = ProcessPlaceholders(localized);
    MessageService.SendRaw(Sender, localized.Format());
  }

  /// <summary>Sends a localized error-styled reply using the given localization key.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedError(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    localized = ProcessPlaceholders(localized);
    MessageService.SendRaw(Sender, localized.FormatError());
  }

  /// <summary>Sends a localized warning-styled reply using the given localization key.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedWarning(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    localized = ProcessPlaceholders(localized);
    MessageService.SendRaw(Sender, localized.FormatWarning());
  }

  /// <summary>Sends a localized informational-styled reply using the given localization key.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedInfo(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    localized = ProcessPlaceholders(localized);
    MessageService.SendRaw(Sender, localized.FormatInfo());
  }

  /// <summary>Sends a localized success-styled reply using the given localization key.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedSuccess(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    localized = ProcessPlaceholders(localized);
    MessageService.SendRaw(Sender, localized.FormatSuccess());
  }

  /// <summary>Sends a localized raw (unformatted and unstyled) reply to the sender.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedRaw(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    localized = ProcessPlaceholders(localized);
    MessageService.SendRaw(Sender, localized);
  }

  /// <summary>Sends a localized reply using the given localization key, applies placeholders, and formats the message using the provided colors list.</summary>
  /// <param name="key">Localization key.</param>
  /// <param name="colors">List of color strings used for formatting the localized message.</param>
  /// <param name="parameters">Optional parameters for localization formatting.</param>
  public void ReplyLocalizedFormatted(string key, List<string> colors, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = Localizer.Get(Sender, key, CallingAssembly, parameters);
    else localized = Localizer.Get(Sender, key, parameters);
    localized = ProcessPlaceholders(localized);
    MessageService.SendRaw(Sender, localized.Format(colors));
  }

  /// <summary>Processes placeholders in the message text.</summary>
  /// <param name="message">The message text containing placeholders.</param>
  /// <returns>The message with placeholders replaced.</returns>
  private string ProcessPlaceholders(string message) {
    if (string.IsNullOrEmpty(message)) return message;

    // Replace {playerName} with Sender.Name
    message = message.Replace("{playerName}", Sender.Name);

    // Replace PrefabGuid(...) with localized name
    var prefabPattern = System.Text.RegularExpressions.Regex.Matches(message, @"PrefabGuid\((-?\d+)\)");
    foreach (System.Text.RegularExpressions.Match match in prefabPattern) {
      if (int.TryParse(match.Groups[1].Value, out int guidValue)) {
        var prefabGuid = new PrefabGUID(guidValue);
        var localizedName = prefabGuid.LocalizedName(Sender.Language);
        message = message.Replace(match.Value, localizedName);
      }
    }

    return message;
  }

  /// <summary>
  /// Waits for a chat reply from the sender that matches one of the expected responses.
  /// </summary>
  /// <param name="expectedResponses">Array of expected response strings to match against.</param>
  /// <param name="callback">Action to invoke when a matching response is received. Receives the matched response.</param>
  /// <param name="timeoutSeconds">Time in seconds before automatically unsubscribing. Default is 30 seconds.</param>
  public void WaitForReply(string[] expectedResponses, Action<string> callback, int timeoutSeconds = 30) {
    WaitForReplyInternal(expectedResponses, callback, timeoutSeconds, localized: false);
  }

  /// <summary>
  /// Waits for a chat reply from the sender that matches one of the expected localized response keys.
  /// </summary>
  /// <param name="expectedResponsesKeys">Array of localization keys to match against.</param>
  /// <param name="callback">Action to invoke when a matching response is received. Receives the localization key.</param>
  /// <param name="timeoutSeconds">Time in seconds before automatically unsubscribing. Default is 30 seconds.</param>
  public void WaitForReplyLocalized(string[] expectedResponsesKeys, Action<string> callback, int timeoutSeconds = 30) {
    WaitForReplyInternal(expectedResponsesKeys, callback, timeoutSeconds, localized: true);
  }

  /// <summary>
  /// Internal method that sets up the chat message listener with automatic timeout.
  /// </summary>
  /// <param name="expectedResponses">Array of expected responses or localization keys.</param>
  /// <param name="callback">Action to invoke when a match is found.</param>
  /// <param name="timeoutSeconds">Timeout duration in seconds.</param>
  /// <param name="localized">Whether to treat expected responses as localization keys.</param>
  private void WaitForReplyInternal(string[] expectedResponses, Action<string> callback, int timeoutSeconds, bool localized) {
    var steamId = Sender.PlatformId;

    if (CommandHandler.PlayersWaitingForReply.Contains(steamId)) {
      ReplyLocalizedWarning("already_waiting_for_response");
      return;
    }

    CommandHandler.PlayersWaitingForReply.Add(steamId);

    if (timeoutSeconds <= 0) {
      Log.Warning("[WaitForReply] Timeout seconds must be greater than zero. Using default of 30 seconds.");
      timeoutSeconds = 30;
    }

    void Unsubscribe() {
      EventManager.Off(PrefixEvents.OnChatMessage, OnMessageResponse);
      CommandHandler.PlayersWaitingForReply.Remove(steamId);
    }

    [EventPriority(EventPriority.First)]
    void OnMessageResponse(NativeArray<Entity> entities) {
      HandleReply(entities, expectedResponses, callback, Unsubscribe, localized);
    }

    EventManager.On(PrefixEvents.OnChatMessage, OnMessageResponse);
    ActionScheduler.Delayed(Unsubscribe, timeoutSeconds * 1000);
  }

  /// <summary>
  /// Handles incoming chat messages and checks if they match expected responses.
  /// </summary>
  /// <param name="entities">Array of chat message entities to process.</param>
  /// <param name="expectedResponses">Array of expected responses or localization keys.</param>
  /// <param name="callback">Action to invoke when a match is found.</param>
  /// <param name="unsubscribe">Action to unsubscribe from the event.</param>
  /// <param name="localized">Whether to localize the expected responses before comparison.</param>
  private void HandleReply(NativeArray<Entity> entities, string[] expectedResponses, Action<string> callback, Action unsubscribe, bool localized) {
    foreach (var entity in entities) {
      if (!entity.Exists() || !entity.Has<ChatMessageEvent>()) continue;

      var chatEvent = entity.Read<ChatMessageEvent>();
      var fromCharacter = entity.Read<FromCharacter>().Character;
      var player = fromCharacter.GetPlayerData();
      var message = chatEvent.MessageText.Value;

      if (player != Sender) continue;

      var localizedCancelResponse = Localizer.Get(Sender, "cancel_response");

      // Check for cancel response
      if (string.Equals(message.Trim(), localizedCancelResponse.Trim(), StringComparison.OrdinalIgnoreCase)) {
        Sender.SendLocalizedSuccessMessage("response_cancelled");
        unsubscribe?.Invoke();

        // Destroy the chat message entity to prevent further processing
        entity.Destroy(true);
        return;
      }

      // Check each expected response for a match
      foreach (var expected in expectedResponses) {
        // Get localized value if needed, otherwise use the expected response as-is
        var compareValue = localized
            ? Localizer.Get(Sender, expected, CallingAssembly ?? Assembly.GetExecutingAssembly())
            : expected;

        // Case-insensitive comparison with trimmed strings
        if (string.Equals(message.Trim(), compareValue.Trim(), StringComparison.OrdinalIgnoreCase)) {
          callback?.Invoke(expected);
          unsubscribe?.Invoke();

          // Destroy the chat message entity to prevent further processing
          entity.Destroy(true);
          return;
        }
      }
    }
  }
}