using Unity.Entities;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using System.Reflection;
using ProjectM;

namespace ScarletCore.Data;

/// <summary>
/// Represents comprehensive player data and provides convenient access to player-related information.
/// This class serves as a wrapper around Unity ECS entities to simplify player data management.
/// </summary>
public class PlayerData() {
  /// <summary>
  /// The underlying Unity ECS entity representing the user.
  /// This is the primary entity that contains all player-related components.
  /// </summary>
  public Entity UserEntity;

  /// <summary>
  /// Gets the User component from the UserEntity.
  /// Contains core player information like character name, platform ID, and connection status.
  /// </summary>
  public User User => UserEntity.Read<User>();

  /// <summary>
  /// Cached player name to avoid repeated string conversions.
  /// Null until first access, then cached for performance.
  /// </summary>
  private string _name = null;

  /// <summary>
  /// Gets the network identifier for this player.
  /// Used for network communication and player identification.
  /// </summary>
  public NetworkId NetworkId => UserEntity.Read<NetworkId>();

  /// <summary>
  /// Gets the player's character name with lazy loading and caching.
  /// Converts the FixedString64Bytes to string only when needed and caches the result.
  /// </summary>
  public string Name {
    get {
      // Lazy load and cache the name to avoid repeated string conversions
      if (string.IsNullOrEmpty(_name))
        _name = User.CharacterName.ToString();
      return _name;
    }
  }

  /// <summary>
  /// Manually sets the cached player name.
  /// Useful when the name is known from external sources or needs to be updated.
  /// </summary>
  /// <param name="name">The new name to cache</param>
  public void SetName(string name) {
    _name = name;
  }

  /// <summary>
  /// Gets the currently cached name without triggering lazy loading.
  /// Returns null if the name hasn't been loaded or set yet.
  /// </summary>
  public string CachedName => _name;

  /// <summary>
  /// Gets the player's character entity in the game world.
  /// This entity represents the actual vampire character that moves around.
  /// </summary>
  public Entity CharacterEntity => User.LocalCharacter._Entity;

  /// <summary>
  /// Gets the platform-specific user ID (Steam ID, etc.).
  /// Used for cross-platform player identification and external integrations.
  /// </summary>
  public ulong PlatformId => User.PlatformId;

  /// <summary>
  /// Gets whether the player is currently connected to the server.
  /// Useful for checking player availability before performing operations.
  /// </summary>
  public bool IsOnline => User.IsConnected;

  /// <summary>
  /// Gets whether the player has administrator privileges.
  /// Used for permission checks and admin-only functionality.
  /// </summary>
  public bool IsAdmin => User.IsAdmin;

  /// <summary>
  /// Gets the date and time when the player connected to the server.
  /// Converts from UTC ticks to local DateTime for easier handling.
  /// </summary>
  public DateTime ConnectedSince => new DateTime(User.TimeLastConnected, DateTimeKind.Utc).ToLocalTime();

  /// <summary>
  /// Gets the player's clan name if they belong to one.
  /// Returns null if the player is not in a clan or clan data is unavailable.
  /// </summary>
  public string ClanName {
    get {
      var clan = User.ClanEntity.GetEntityOnServer();

      if (Entity.Null.Equals(clan) || !clan.Has<ClanTeam>()) return null; // No clan data available

      var clanName = clan.Read<ClanTeam>().Name.ToString();

      return string.IsNullOrEmpty(clanName) ? null : clanName;
    }
  }

  /// <summary>
  /// Dictionary to store custom data from different mods.
  /// Uses the calling assembly name as the key to prevent conflicts between mods.
  /// </summary>
  private Dictionary<string, object> CustomData { get; set; } = [];

  /// <summary>
  /// Sets or updates custom data for the calling mod.
  /// Each mod gets its own namespace based on the assembly name to prevent conflicts.
  /// </summary>
  /// <typeparam name="T">The type of data to store</typeparam>
  /// <param name="value">The data value to store</param>
  /// <returns>The stored value for method chaining</returns>
  /// <example>
  /// // Store custom mod data
  /// var playerLevel = playerData.SetData(25);
  /// var customConfig = playerData.SetData(new MyModConfig());
  /// </example>
  public T SetData<T>(T value) {
    // Use the calling assembly name as the key to prevent mod data conflicts
    var key = Assembly.GetCallingAssembly().GetName().Name;

    // Update existing data or add new entry
    if (CustomData.ContainsKey(key)) {
      CustomData[key] = value;
    } else {
      CustomData.Add(key, value);
    }
    return value;
  }

  /// <summary>
  /// Retrieves custom data for the calling mod.
  /// Returns the default value for the type if no data is found or type mismatch occurs.
  /// </summary>
  /// <typeparam name="T">The expected type of the stored data</typeparam>
  /// <returns>The stored data or default(T) if not found</returns>
  /// <example>
  /// // Retrieve custom mod data
  /// var playerLevel = playerData.GetData&lt;int&gt;();
  /// var customConfig = playerData.GetData&lt;MyModConfig&gt;();
  /// </example>
  public T GetData<T>() {
    // Use the calling assembly name as the key to retrieve mod-specific data
    var key = Assembly.GetCallingAssembly().GetName().Name;

    // Try to get the data and cast it to the expected type
    if (CustomData.TryGetValue(key, out var value) && value is T typedValue) {
      return typedValue;
    }

    // Return default value if data not found or type mismatch
    return default;
  }
}
