using ProjectM;
using ProjectM.Network;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Entities;

namespace ScarletCore.Services;

/// <summary>
/// Service responsible for handling ability-related operations.
/// Provides methods to cast abilities and modify NPC ability slots.
/// </summary>
public static class AbilityService {

  /// <summary>
  /// Casts an ability for the specified entity using the given ability group.
  /// </summary>
  /// <param name="entity">The entity that will cast the ability (player or NPC)</param>
  /// <param name="abilityGroup">The GUID of the ability group to be cast</param>
  /// <remarks>
  /// This method handles both player and NPC entities, automatically determining
  /// the appropriate user context for the ability casting event.
  /// </remarks>
  public static void CastAbility(Entity entity, PrefabGUID abilityGroup) {
    // Check if the entity is a player to handle user context properly
    bool isPlayer = entity.IsPlayer();

    // Create the debug event structure for ability casting
    CastAbilityServerDebugEvent castAbilityServerDebugEvent = new() {
      AbilityGroup = abilityGroup,
      Who = entity.Read<NetworkId>(),
    };

    // Set up the character context for the ability cast
    FromCharacter fromCharacter = new() {
      Character = entity,
      User = isPlayer ? entity.GetUserEntity() : entity // Use userEntity for players, entity for NPCs
    };

    // Get the user index for the debug system (0 for NPCs)
    int userIndex = isPlayer ? entity.GetUserEntity().Read<User>().Index : 0;

    // Execute the ability cast through the debug events system
    GameSystems.DebugEventsSystem.CastAbilityServerDebugEvent(userIndex, ref castAbilityServerDebugEvent, ref fromCharacter);
  }

  /// <summary>
  /// Replaces an ability in a specific slot for an entity.
  /// </summary>
  /// <param name="entity">The entity whose ability slot will be modified</param>
  /// <param name="newAbilityGuid">The GUID of the new ability to assign</param>
  /// <param name="abilitySlotIndex">The slot index where the ability will be placed (default: 0)</param>
  /// <remarks>
  /// This method modifies the ability group slot buffer of the entity to replace
  /// </remarks>
  public static void ReplaceNpcAbilityOnSlot(Entity entity, PrefabGUID newAbilityGuid, int abilitySlotIndex = 0, int priority = 99) {
    // Verify that the Entity has the required ability slot buffer component
    if (!entity.Has<AbilityGroupSlotBuffer>()) {
      Log.Warning("Entity doesn't have AbilityGroupSlotBuffer component");
      return;
    }

    // Use the server game manager to modify the ability group slot for the entity
    GameSystems.ServerGameManager.ModifyAbilityGroupOnSlot(entity, entity, abilitySlotIndex, newAbilityGuid, priority);
  }
}