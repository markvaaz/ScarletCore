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

    // Get the user entity associated with the character
    var userEntity = entity.Read<PlayerCharacter>().UserEntity;

    // Set up the character context for the ability cast
    FromCharacter fromCharacter = new() {
      Character = entity,
      User = isPlayer ? userEntity : entity // Use userEntity for players, entity for NPCs
    };

    // Get the user index for the debug system (0 for NPCs)
    int userIndex = isPlayer ? userEntity.Read<User>().Index : 0;

    // Execute the ability cast through the debug events system
    GameSystems.DebugEventsSystem.CastAbilityServerDebugEvent(userIndex, ref castAbilityServerDebugEvent, ref fromCharacter);
  }

  /// <summary>
  /// Replaces an ability in a specific slot for an NPC entity.
  /// </summary>
  /// <param name="npc">The NPC entity whose ability will be replaced</param>
  /// <param name="newAbilityGuid">The GUID of the new ability to assign</param>
  /// <param name="abilitySlotIndex">The slot index where the ability will be placed (default: 0)</param>
  /// <remarks>
  /// This method modifies the NPC's ability buffer directly and ensures the ability
  /// is visible on the action bar. The slot index must be valid for the operation to succeed.
  /// </remarks>
  public static void ReplaceNpcAbilityOnSlot(Entity npc, PrefabGUID newAbilityGuid, int abilitySlotIndex = 0) {
    // Verify that the NPC has the required ability slot buffer component
    if (!npc.Has<AbilityGroupSlotBuffer>()) {
      Log.Warning("Entity doesn't have AbilityGroupSlotBuffer component");
      return;
    }

    // Get the ability buffer from the NPC entity
    var abilityBuffer = npc.ReadBuffer<AbilityGroupSlotBuffer>();

    // Validate the slot index to prevent out-of-bounds access
    if (abilitySlotIndex <= 0 || abilitySlotIndex > abilityBuffer.Length) {
      Log.Warning($"Invalid slot index: {abilitySlotIndex}. Buffer length: {abilityBuffer.Length}");
      return;
    }

    // Get the current ability slot data
    var abilitySlot = abilityBuffer[abilitySlotIndex];

    // Configure the new ability slot
    abilitySlot.BaseAbilityGroupOnSlot = new ModifiablePrefabGUID(newAbilityGuid);

    // Update the buffer with the modified slot
    abilityBuffer[abilitySlotIndex] = abilitySlot;

    Log.Info($"Successfully replaced ability in slot {abilitySlotIndex}");
  }
}