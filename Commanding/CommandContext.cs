using System;
using System.Reflection;
using ScarletCore.Data;
using ScarletCore.Services;
using ScarletCore.Utils;
using Unity.Entities;

namespace ScarletCore.Commanding;

/// <summary>
/// Context passed to command handlers.
/// </summary>
public sealed class CommandContext {
  public Entity MessageEntity { get; }
  public PlayerData Sender { get; }
  public string Raw { get; }
  public string[] Args { get; }
  // The assembly that should be considered the owner of localization keys.
  // When a command from another assembly invokes `ReplyLocalized`, CommandService
  // will set this to the command's assembly so localization resolves correctly.
  public Assembly CallingAssembly { get; set; }

  public CommandContext(Entity messageEntity, PlayerData sender, string raw, string[] args) {
    ArgumentNullException.ThrowIfNull(sender);
    Sender = sender;
    MessageEntity = messageEntity;
    Raw = raw;
    Args = args;
  }

  public void Reply(string message) {
    MessageService.SendRaw(Sender, message.Format());
  }

  public void ReplyError(string message) {
    MessageService.SendRaw(Sender, message.FormatError());
  }

  public void ReplyWarning(string message) {
    MessageService.SendRaw(Sender, message.FormatWarning());
  }

  public void ReplyInfo(string message) {
    MessageService.SendRaw(Sender, message.FormatInfo());
  }

  public void ReplySuccess(string message) {
    MessageService.SendRaw(Sender, message.FormatSuccess());
  }

  public void ReplyLocalized(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
    else localized = LocalizationService.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.Format());
  }

  public void ReplyLocalizedError(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
    else localized = LocalizationService.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.FormatError());
  }

  public void ReplyLocalizedWarning(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
    else localized = LocalizationService.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.FormatWarning());
  }

  public void ReplyLocalizedInfo(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
    else localized = LocalizationService.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.FormatInfo());
  }

  public void ReplyLocalizedSuccess(string key, params string[] parameters) {
    string localized;
    if (CallingAssembly != null) localized = LocalizationService.Get(Sender, key, CallingAssembly, parameters);
    else localized = LocalizationService.Get(Sender, key, parameters);
    MessageService.SendRaw(Sender, localized.FormatSuccess());
  }
}