using System;

namespace ScarletCore.Data;

/// <summary>
/// Represents a role assignment to a player with optional expiration
/// </summary>
public class PlayerRoleAssignment {
  /// <summary>
  /// Name of the assigned role
  /// </summary>
  public string RoleName { get; set; }

  /// <summary>
  /// When this role assignment was created
  /// </summary>
  public DateTime AssignedAt { get; set; }

  /// <summary>
  /// When this role assignment expires (null = never expires)
  /// </summary>
  public DateTime? ExpiresAt { get; set; }

  /// <summary>
  /// Creates a new role assignment
  /// </summary>
  /// <param name="roleName">Name of the role</param>
  /// <param name="expiresAt">Optional expiration date</param>
  public PlayerRoleAssignment(string roleName, DateTime? expiresAt = null) {
    RoleName = roleName;
    AssignedAt = DateTime.Now;
    ExpiresAt = expiresAt;
  }

  /// <summary>
  /// Parameterless constructor for deserialization
  /// </summary>
  public PlayerRoleAssignment() { }

  /// <summary>
  /// Checks if this role assignment has expired
  /// </summary>
  public bool IsExpired => ExpiresAt.HasValue && DateTime.Now >= ExpiresAt.Value;

  /// <summary>
  /// Gets the remaining time until expiration
  /// </summary>
  public TimeSpan? TimeRemaining => ExpiresAt.HasValue ? ExpiresAt.Value - DateTime.Now : null;

  /// <summary>
  /// Returns a string representation of this role assignment
  /// </summary>
  /// <returns>String containing role name and expiration information</returns>
  public override string ToString() {
    if (ExpiresAt.HasValue) {
      return $"{RoleName} (expires: {ExpiresAt.Value:dd/MM/yyyy HH:mm})";
    }
    return $"{RoleName} (permanent)";
  }
}
