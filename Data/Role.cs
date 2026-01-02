using System;
using System.Collections.Generic;
using System.Linq;

namespace ScarletCore.Data;

/// <summary>
/// Represents a role that can be assigned to players.
/// Roles define permissions and access levels for players on the server.
/// </summary>
public class Role {
  /// <summary>
  /// Unique identifier for the role (auto-generated)
  /// </summary>
  public string Id { get; set; }

  /// <summary>
  /// Display name of the role (must be unique)
  /// </summary>
  public string Name { get; set; }

  /// <summary>
  /// List of permission strings that this role grants
  /// </summary>
  public List<string> Permissions { get; set; }

  /// <summary>
  /// Priority/weight of the role (higher = more important)
  /// Used for determining which role takes precedence when a player has multiple roles
  /// </summary>
  public int Priority { get; set; }

  /// <summary>
  /// Date and time when the role was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// Date and time when the role was last modified
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Optional description of what this role is for
  /// </summary>
  public string Description { get; set; }

  /// <summary>
  /// Creates a new role with the specified name and permissions
  /// </summary>
  /// <param name="name">The unique name for the role</param>
  /// <param name="permissions">Array of permission strings</param>
  /// <param name="priority">Priority level (default: 0)</param>
  public Role(string name, string[] permissions = null, int priority = 0) {
    if (string.IsNullOrWhiteSpace(name))
      throw new ArgumentException("Role name cannot be null or empty", nameof(name));

    Id = Guid.NewGuid().ToString();
    Name = name.Trim();
    Permissions = permissions?.ToList() ?? [];
    Priority = priority;
    CreatedAt = DateTime.Now;
    UpdatedAt = DateTime.Now;
  }

  /// <summary>
  /// Parameterless constructor for deserialization
  /// </summary>
  public Role() {
    Permissions = [];
  }

  /// <summary>
  /// Checks if this role has a specific permission
  /// </summary>
  /// <param name="permission">The permission to check</param>
  /// <returns>True if the role has the permission, false otherwise</returns>
  public bool HasPermission(string permission) {
    if (string.IsNullOrWhiteSpace(permission)) return false;
    return Permissions.Any(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Adds a permission to this role
  /// </summary>
  /// <param name="permission">The permission to add</param>
  /// <returns>True if added, false if already exists</returns>
  public bool AddPermission(string permission) {
    if (string.IsNullOrWhiteSpace(permission)) return false;
    if (HasPermission(permission)) return false;

    Permissions.Add(permission.Trim());
    UpdatedAt = DateTime.Now;
    return true;
  }

  /// <summary>
  /// Removes a permission from this role
  /// </summary>
  /// <param name="permission">The permission to remove</param>
  /// <returns>True if removed, false if not found</returns>
  public bool RemovePermission(string permission) {
    if (string.IsNullOrWhiteSpace(permission)) return false;

    var removed = Permissions.RemoveAll(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase)) > 0;
    if (removed) {
      UpdatedAt = DateTime.Now;
    }
    return removed;
  }

  /// <summary>
  /// Returns a string representation of this role
  /// </summary>
  /// <returns>String containing role name, permission count, and priority</returns>
  public override string ToString() {
    return $"Role({Name}, Permissions: {Permissions.Count}, Priority: {Priority})";
  }

  /// <summary>
  /// Determines whether the specified object is equal to the current role
  /// </summary>
  /// <param name="obj">The object to compare with the current role</param>
  /// <returns>True if the specified object is equal to the current role; otherwise, false</returns>
  public override bool Equals(object obj) {
    if (obj is Role other) {
      return Id == other.Id;
    }
    return false;
  }

  /// <summary>
  /// Serves as the default hash function
  /// </summary>
  /// <returns>A hash code for the current role</returns>
  public override int GetHashCode() {
    return Id?.GetHashCode() ?? 0;
  }
}
