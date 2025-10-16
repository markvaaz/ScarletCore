using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using ScarletCore.Utils;
using ScarletCore.Data;
using System.Collections.Generic;
using System.Linq;
using ScarletCore.Systems;
using Stunlock.Core;
using Unity.Mathematics;

namespace ScarletCore.Services;

public static class MessageService {
  public static void Send(User user, string message) {
    var messageBytes = new FixedString512Bytes(message.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
  }

  public static void Send(PlayerData player, string message) {
    var user = player.UserEntity.Read<User>();
    var messageBytes = new FixedString512Bytes(message.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
  }

  public static void SendAll(string message) {
    var messageBytes = new FixedString512Bytes(message.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }
  public static void SendAdmins(string message) {
    var messageBytes = new FixedString512Bytes(message.Format());
    var admins = PlayerService.GetAdmins();

    foreach (var admin in admins) {
      var user = admin.UserEntity.Read<User>();
      ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
    }
  }
  public static void SendSCT(PlayerData player, PrefabGUID prefab, string assetGuid, float3 color, int value) {
    ScrollingCombatTextMessage.Create(
      GameSystems.EntityManager,
      GameSystems.EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
      AssetGuid.FromString(assetGuid),
      player.Position,
      color,
      player.CharacterEntity,
      value,
      prefab,
      player.UserEntity
    );
  }
  #region Enhanced Send Methods

  /// <summary>
  /// Sends a colored message to a user
  /// </summary>
  public static void SendColored(User user, string message, string color) {
    var coloredMessage = message.WithColor(color);
    var messageBytes = new FixedString512Bytes(coloredMessage.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
  }

  /// <summary>
  /// Sends a colored message to a player
  /// </summary>
  public static void SendColored(PlayerData player, string message, string color) {
    var user = player.UserEntity.Read<User>();
    SendColored(user, message, color);
  }

  /// <summary>
  /// Sends a colored message to all players
  /// </summary>
  public static void SendAllColored(string message, string color) {
    var coloredMessage = message.WithColor(color);
    var messageBytes = new FixedString512Bytes(coloredMessage.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }

  /// <summary>
  /// Sends an error message (red color)
  /// </summary>
  public static void SendError(User user, string message) {
    var formattedMessage = message.AsError();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
  }

  /// <summary>
  /// Sends an error message to a player (red color)
  /// </summary>
  public static void SendError(PlayerData player, string message) {
    var user = player.UserEntity.Read<User>();
    SendError(user, message);
  }

  /// <summary>
  /// Sends a success message (green color)
  /// </summary>
  public static void SendSuccess(User user, string message) {
    var formattedMessage = message.AsSuccess();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
  }

  /// <summary>
  /// Sends a success message to a player (green color)
  /// </summary>
  public static void SendSuccess(PlayerData player, string message) {
    var user = player.UserEntity.Read<User>();
    SendSuccess(user, message);
  }

  /// <summary>
  /// Sends a warning message (yellow color)
  /// </summary>
  public static void SendWarning(User user, string message) {
    var formattedMessage = message.AsWarning();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
  }

  /// <summary>
  /// Sends a warning message to a player (yellow color)
  /// </summary>
  public static void SendWarning(PlayerData player, string message) {
    var user = player.UserEntity.Read<User>();
    SendWarning(user, message);
  }

  /// <summary>
  /// Sends an info message (blue color)
  /// </summary>
  public static void SendInfo(User user, string message) {
    var formattedMessage = message.AsInfo();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
  }

  /// <summary>
  /// Sends an info message to a player (blue color)
  /// </summary>
  public static void SendInfo(PlayerData player, string message) {
    var user = player.UserEntity.Read<User>();
    SendInfo(user, message);
  }

  #endregion
  #region Broadcast Methods

  /// <summary>
  /// Sends an announcement to all players (orange color with prefix)
  /// </summary>
  public static void Announce(string message) {
    var formattedMessage = message.AsAnnouncement();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }

  /// <summary>
  /// Sends a global error message to all players
  /// </summary>
  public static void BroadcastError(string message) {
    var formattedMessage = message.AsError();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }

  /// <summary>
  /// Sends a global warning to all players
  /// </summary>
  public static void BroadcastWarning(string message) {
    var formattedMessage = message.AsWarning();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }

  /// <summary>
  /// Sends a global info message to all players
  /// </summary>
  public static void BroadcastInfo(string message) {
    var formattedMessage = message.AsInfo();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }

  #endregion

  #region Template Messages
  /// <summary>
  /// Sends a welcome message to a new player
  /// </summary>
  public static void SendWelcome(PlayerData player) {
    var messages = new[]
    {
      $"Welcome to the server, {player.Name}!",
      "Type /help for available commands",
      "Have fun and follow the rules!"
    };

    foreach (var msg in messages) {
      SendSuccess(player, msg);
    }
  }

  /// <summary>
  /// Sends a player join notification to all players
  /// </summary>
  public static void NotifyPlayerJoined(PlayerData player) {
    var formattedMessage = player.Name.AsPlayerJoin();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }

  /// <summary>
  /// Sends a player leave notification to all players
  /// </summary>
  public static void NotifyPlayerLeft(PlayerData player) {
    var formattedMessage = player.Name.AsPlayerLeave();
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }

  /// <summary>
  /// Sends a death notification
  /// </summary>
  public static void NotifyDeath(string victimName, string killerName = null) {
    var formattedMessage = victimName.AsPlayerDeath(killerName);
    var messageBytes = new FixedString512Bytes(formattedMessage.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }

  /// <summary>
  /// Sends a server restart warning
  /// </summary>
  public static void NotifyRestart(int minutesRemaining) {
    if (minutesRemaining > 1) {
      BroadcastWarning($"Server restart in {minutesRemaining} minutes!");
    } else {
      BroadcastWarning("Server restart in 1 minute! Prepare for disconnection!");
    }
  }

  #endregion

  #region Utility Methods

  /// <summary>
  /// Sends a formatted message with placeholders
  /// </summary>
  public static void SendFormatted(User user, string template, params object[] args) {
    var message = string.Format(template, args);
    Send(user, message);
  }

  /// <summary>
  /// Sends a formatted message with placeholders to a player
  /// </summary>
  public static void SendFormatted(PlayerData player, string template, params object[] args) {
    var message = string.Format(template, args);
    Send(player, message);
  }

  /// <summary>
  /// Sends a message to multiple specific players
  /// </summary>
  public static void SendToPlayers(IEnumerable<PlayerData> players, string message) {
    var messageBytes = new FixedString512Bytes(message.Format());

    foreach (var player in players) {
      var user = player.UserEntity.Read<User>();
      ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
    }
  }
  /// <summary>
  /// Sends a colored message to multiple specific players
  /// </summary>
  public static void SendToPlayersColored(IEnumerable<PlayerData> players, string message, string color) {
    var coloredMessage = message.WithColor(color);
    var messageBytes = new FixedString512Bytes(coloredMessage.Format());

    foreach (var player in players) {
      var user = player.UserEntity.Read<User>();
      ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
    }
  }
  /// <summary>
  /// Sends a message to players within a certain range of a position
  /// </summary>
  public static void SendToPlayersInRadius(Unity.Mathematics.float3 position, float radius, string message) {
    var playersInRange = PlayerService.GetAllConnected()
      .Where(p => MathUtility.IsInRange(p.CharacterEntity, position, radius));

    SendToPlayers(playersInRange, message);
  }  /// <summary>
     /// Creates a formatted list message
     /// </summary>
  public static void SendList(User user, string title, IEnumerable<string> items, string color = null) {
    color ??= RichTextFormatter.White;

    var listTitle = title.AsList().WithColor(color);
    var messageBytes = new FixedString512Bytes(listTitle.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);

    var itemList = items.ToList();
    for (int i = 0; i < itemList.Count; i++) {
      var itemMessage = $"  {i + 1}. {itemList[i]}".WithColor(color);
      var itemBytes = new FixedString512Bytes(itemMessage.Format());
      ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref itemBytes);
    }
  }

  /// <summary>
  /// Creates a formatted list message for a player
  /// </summary>
  public static void SendList(PlayerData player, string title, IEnumerable<string> items, string color = null) {
    var user = player.UserEntity.Read<User>();
    SendList(user, title, items, color);
  }

  /// <summary>
  /// Sends a progress bar message
  /// </summary>
  public static void SendProgressBar(User user, string label, int current, int max, int barLength = 20) {
    var progressMessage = label.AsProgressBar(current, max, barLength);
    var messageBytes = new FixedString512Bytes(progressMessage.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
  }

  /// <summary>
  /// Sends a progress bar message to a player
  /// </summary>
  public static void SendProgressBar(PlayerData player, string label, int current, int max, int barLength = 20) {
    var user = player.UserEntity.Read<User>();
    SendProgressBar(user, label, current, max, barLength);
  }

  /// <summary>
  /// Sends a colored message to players within a certain range of a position
  /// </summary>
  public static void SendToPlayersInRadiusColored(Unity.Mathematics.float3 position, float radius, string message, string color) {
    var playersInRange = PlayerService.GetAllConnected()
      .Where(p => MathUtility.IsInRange(p.CharacterEntity, position, radius));

    SendToPlayersColored(playersInRange, message, color);
  }

  /// <summary>
  /// Sends a message only if the condition is met
  /// </summary>
  public static void SendConditional(User user, string message, System.Func<bool> condition) {
    if (condition()) {
      Send(user, message);
    }
  }

  /// <summary>
  /// Sends a message only if the condition is met
  /// </summary>
  public static void SendConditional(PlayerData player, string message, System.Func<bool> condition) {
    if (condition()) {
      Send(player, message);
    }
  }

  /// <summary>
  /// Sends different messages based on a condition
  /// </summary>
  public static void SendConditional(User user, string trueMessage, string falseMessage, System.Func<bool> condition) {
    Send(user, condition() ? trueMessage : falseMessage);
  }

  /// <summary>
  /// Sends different messages based on a condition
  /// </summary>
  public static void SendConditional(PlayerData player, string trueMessage, string falseMessage, System.Func<bool> condition) {
    Send(player, condition() ? trueMessage : falseMessage);
  }  /// <summary>
     /// Creates a boxed message with borders
     /// </summary>
  public static void SendBoxed(User user, string title, string content, string color = null) {
    color ??= RichTextFormatter.White;

    var boxedTitle = title.AsBoxedTitle().WithColor(color);
    var titleLines = boxedTitle.Split('\n');

    foreach (var line in titleLines) {
      var messageBytes = new FixedString512Bytes(line.Format());
      ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
    }

    var boxedContent = content.AsBoxedContent(title).WithColor(color);
    var contentLines = boxedContent.Split('\n');

    foreach (var line in contentLines) {
      var messageBytes = new FixedString512Bytes(line.Format());
      ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
    }
  }

  /// <summary>
  /// Creates a boxed message with borders for a player
  /// </summary>
  public static void SendBoxed(PlayerData player, string title, string content, string color = null) {
    var user = player.UserEntity.Read<User>();
    SendBoxed(user, title, content, color);
  }

  /// <summary>
  /// Sends a separator line
  /// </summary>
  public static void SendSeparator(User user, char character = '-', int length = 40, string color = null) {
    color ??= RichTextFormatter.Gray;
    var separatorMessage = character.AsSeparator(length);
    if (color != RichTextFormatter.Gray) {
      separatorMessage = new string(character, length).WithColor(color);
    }
    var messageBytes = new FixedString512Bytes(separatorMessage.Format());
    ServerChatUtils.SendSystemMessageToClient(GameSystems.EntityManager, user, ref messageBytes);
  }

  /// <summary>
  /// Sends a separator line to a player
  /// </summary>
  public static void SendSeparator(PlayerData player, char character = '-', int length = 40, string color = null) {
    var user = player.UserEntity.Read<User>();
    SendSeparator(user, character, length, color);
  }

  /// <summary>
  /// Sends a countdown message to all players
  /// </summary>
  public static void SendCountdown(int seconds, string action) {
    var countdownMessage = action.AsCountdown(seconds);
    var messageBytes = new FixedString512Bytes(countdownMessage.Format());
    ServerChatUtils.SendSystemMessageToAllClients(GameSystems.EntityManager, ref messageBytes);
  }

  #endregion
}