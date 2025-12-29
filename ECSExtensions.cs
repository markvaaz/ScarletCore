using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ScarletCore.Systems;
using ScarletCore.Localization;

namespace ScarletCore;

/// <summary>
/// Provides extension methods for working with Unity ECS entities and components.
/// </summary>
public static class ECSExtensions {
  private static EntityManager EntityManager => GameSystems.Server.EntityManager;
  /// <summary>
  /// Delegate for handling a reference to a struct item.
  /// </summary>
  /// <typeparam name="T">The struct type.</typeparam>
  /// <param name="item">The item passed by reference.</param>
  public delegate void WithRefHandler<T>(ref T item);
  /// <summary>
  /// Reads a component from the entity, allows modification via a delegate, and writes it back.
  /// </summary>
  /// <typeparam name="T">The component type.</typeparam>
  /// <param name="entity">The entity to operate on.</param>
  /// <param name="action">The delegate to modify the component.</param>
  public static void With<T>(this Entity entity, WithRefHandler<T> action) where T : struct {
    T item = entity.Read<T>();
    action(ref item);

    EntityManager.SetComponentData(entity, item);
  }
  /// <summary>
  /// Adds a component to the entity if it does not exist, then allows modification via a delegate.
  /// </summary>
  /// <typeparam name="T">The component type.</typeparam>
  /// <param name="entity">The entity to operate on.</param>
  /// <param name="action">The delegate to modify the component.</param>
  public static void AddWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct {
    if (!entity.Has<T>()) {
      entity.Add<T>();
    }

    entity.With(action);
  }
  /// <summary>
  /// If the entity has the component, allows modification via a delegate.
  /// </summary>
  /// <typeparam name="T">The component type.</typeparam>
  /// <param name="entity">The entity to operate on.</param>
  /// <param name="action">The delegate to modify the component.</param>
  public static void HasWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct {
    if (entity.Has<T>()) {
      entity.With(action);
    }
  }
  /// <summary>
  /// Writes the specified component data to the entity.
  /// </summary>
  /// <typeparam name="T">The component type.</typeparam>
  /// <param name="entity">The entity to write to.</param>
  /// <param name="componentData">The component data to set.</param>
  public unsafe static void Write<T>(this Entity entity, T componentData) where T : struct {
    EntityManager.SetComponentData(entity, componentData);
  }
  /// <summary>
  /// Reads the specified component data from the entity.
  /// </summary>
  /// <typeparam name="T">The component type.</typeparam>
  /// <param name="entity">The entity to read from.</param>
  /// <returns>The component data.</returns>
  public static T Read<T>(this Entity entity) where T : struct {
    return EntityManager.GetComponentData<T>(entity);
  }
  /// <summary>
  /// Reads a dynamic buffer of the specified type from the entity.
  /// </summary>
  /// <typeparam name="T">The buffer element type.</typeparam>
  /// <param name="entity">The entity to read from.</param>
  /// <returns>The dynamic buffer.</returns>
  public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct {
    return EntityManager.GetBuffer<T>(entity);
  }
  /// <summary>
  /// Adds a dynamic buffer of the specified type to the entity.
  /// </summary>
  /// <typeparam name="T">The buffer element type.</typeparam>
  /// <param name="entity">The entity to add the buffer to.</param>
  /// <returns>The added dynamic buffer.</returns>
  public static DynamicBuffer<T> AddBuffer<T>(this Entity entity) where T : struct {
    return EntityManager.AddBuffer<T>(entity);
  }
  /// <summary>
  /// Tries to get the specified component data from the entity.
  /// </summary>
  /// <typeparam name="T">The component type.</typeparam>
  /// <param name="entity">The entity to query.</param>
  /// <param name="componentData">The output component data if found.</param>
  /// <returns>True if the component exists; otherwise, false.</returns>
  public static bool TryGetComponent<T>(this Entity entity, out T componentData) where T : struct {
    componentData = default;

    if (entity.Has<T>()) {
      componentData = entity.Read<T>();
      return true;
    }

    return false;
  }
  /// <summary>
  /// Determines whether the entity has the specified component type.
  /// </summary>
  /// <typeparam name="T">The component type.</typeparam>
  /// <param name="entity">The entity to check.</param>
  /// <returns>True if the entity has the component; otherwise, false.</returns>
  public static bool Has<T>(this Entity entity) where T : struct {
    return EntityManager.HasComponent(entity, new(Il2CppType.Of<T>()));
  }
  /// <summary>
  /// Adds the specified component type to the entity if it does not already exist.
  /// </summary>
  /// <typeparam name="T">The component type.</typeparam>
  /// <param name="entity">The entity to add the component to.</param>
  public static void Add<T>(this Entity entity) where T : struct {
    if (!entity.Has<T>()) EntityManager.AddComponent(entity, new(Il2CppType.Of<T>()));
  }
  /// <summary>
  /// Removes the specified component type from the entity if it exists.
  /// </summary>
  /// <typeparam name="T">The component type.</typeparam>
  /// <param name="entity">The entity to remove the component from.</param>
  public static void Remove<T>(this Entity entity) where T : struct {
    if (entity.Has<T>()) EntityManager.RemoveComponent(entity, new(Il2CppType.Of<T>()));
  }
  /// <summary>
  /// Tries to get the player entity if the entity represents a player character.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <param name="player">The output player entity if found.</param>
  /// <returns>True if the entity is a player; otherwise, false.</returns>
  public static bool TryGetPlayer(this Entity entity, out Entity player) {
    player = Entity.Null;

    if (entity.Has<PlayerCharacter>()) {
      player = entity;

      return true;
    }

    return false;
  }
  /// <summary>
  /// Determines whether the entity represents a player character.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>True if the entity is a player; otherwise, false.</returns>
  public static bool IsPlayer(this Entity entity) {
    if (entity.Has<PlayerCharacter>()) {
      return true;
    }

    return false;
  }

  /// <summary>
  /// Tries to get the parent entity attached to this entity via the Attach component.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <param name="attached">The output attached entity if found.</param>
  /// <returns>True if an attached entity exists; otherwise, false.</returns>
  public static bool TryGetAttached(this Entity entity, out Entity attached) {
    attached = Entity.Null;

    if (entity.TryGetComponent(out Attach attach) && attach.Parent.Exists()) {
      attached = attach.Parent;
      return true;
    }

    return false;
  }
  /// <summary>
  /// Tries to get the team entity associated with this entity.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <param name="teamEntity">The output team entity if found.</param>
  /// <returns>True if a team entity exists; otherwise, false.</returns>
  public static bool TryGetTeamEntity(this Entity entity, out Entity teamEntity) {
    teamEntity = Entity.Null;

    if (entity.TryGetComponent(out TeamReference teamReference)) {
      Entity teamReferenceEntity = teamReference.Value._Value;

      if (teamReferenceEntity.Exists()) {
        teamEntity = teamReferenceEntity;
        return true;
      }
    }

    return false;
  }
  /// <summary>
  /// Gets the user entity associated with this entity, if available.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>The user entity, or Entity.Null if not found.</returns>
  public static Entity GetUserEntity(this Entity entity) {
    if (entity.TryGetComponent(out PlayerCharacter playerCharacter)) return playerCharacter.UserEntity;
    else if (entity.Has<User>()) return entity;

    return Entity.Null;
  }
  /// <summary>
  /// Determines whether the entity exists in the EntityManager and is not null.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>True if the entity exists; otherwise, false.</returns>
  public static bool Exists(this Entity entity) {
    return !entity.IsNull() && EntityManager.Exists(entity);
  }
  /// <summary>
  /// Determines whether the entity is Entity.Null.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>True if the entity is null; otherwise, false.</returns>
  public static bool IsNull(this Entity entity) {
    return Entity.Null.Equals(entity);
  }
  /// <summary>
  /// Determines whether the entity is disabled (has the Disabled component).
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>True if the entity is disabled; otherwise, false.</returns>
  public static bool IsDisabled(this Entity entity) {
    return entity.Has<Disabled>();
  }

  /// <summary>
  /// Gets the PrefabGUID component from the entity, or PrefabGUID.Empty if not found.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>The PrefabGUID value.</returns>
  public static PrefabGUID GetPrefabGuid(this Entity entity) {
    if (entity.TryGetComponent(out PrefabGUID prefabGuid)) return prefabGuid;

    return PrefabGUID.Empty;
  }
  /// <summary>
  /// Gets the GuidHash from the PrefabGUID component, or the empty hash if not found.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>The GuidHash value.</returns>
  public static int GetGuidHash(this Entity entity) {
    if (entity.TryGetComponent(out PrefabGUID prefabGUID)) return prefabGUID.GuidHash;

    return PrefabGUID.Empty.GuidHash;
  }
  /// <summary>
  /// Gets the owner entity from the EntityOwner component, or Entity.Null if not found.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>The owner entity.</returns>
  public static Entity GetOwner(this Entity entity) {
    if (entity.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.Exists()) return entityOwner.Owner;

    return Entity.Null;
  }

  /// <summary>
  /// Determines whether the entity has the specified buff.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <param name="buffPrefabGuid">The buff PrefabGUID to check for.</param>
  /// <returns>True if the entity has the buff; otherwise, false.</returns>
  public static bool HasBuff(this Entity entity, PrefabGUID buffPrefabGuid) {
    return GameSystems.ServerGameManager.HasBuff(entity, buffPrefabGuid.ToIdentifier());
  }
  /// <summary>
  /// Tries to get a dynamic buffer from the entity using the ServerGameManager.
  /// </summary>
  /// <typeparam name="T">The buffer element type.</typeparam>
  /// <param name="entity">The entity to check.</param>
  /// <param name="dynamicBuffer">The output dynamic buffer if found.</param>
  /// <returns>True if the buffer exists; otherwise, false.</returns>
  public static unsafe bool TryGetBuffer<T>(this Entity entity, out DynamicBuffer<T> dynamicBuffer) where T : struct {
    if (GameSystems.ServerGameManager.TryGetBuffer(entity, out dynamicBuffer)) {
      return true;
    }

    dynamicBuffer = default;
    return false;
  }
  /// <summary>
  /// Gets the aim position from the EntityInput component, or float3.zero if not found.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>The aim position.</returns>
  public static float3 AimPosition(this Entity entity) {
    if (entity.TryGetComponent(out EntityInput entityInput)) {
      return entityInput.AimPosition;
    }

    return float3.zero;
  }
  /// <summary>
  /// Gets the position from the Translation component, or float3.zero if not found.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>The position value.</returns>
  public static float3 Position(this Entity entity) {
    if (entity.TryGetComponent(out Translation translation)) {
      return translation.Value;
    }

    return float3.zero;
  }
  /// <summary>
  /// Gets the tile coordinates from the TilePosition component, or int2.zero if not found.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>The tile coordinates.</returns>
  public static int2 GetTileCoord(this Entity entity) {
    if (entity.TryGetComponent(out TilePosition tilePosition)) {
      return tilePosition.Tile;
    }

    return int2.zero;
  }
  /// <summary>
  /// Gets the unit level from the UnitLevel component, or 0 if not found.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>The unit level.</returns>
  public static int GetUnitLevel(this Entity entity) {
    if (entity.TryGetComponent(out UnitLevel unitLevel)) {
      return unitLevel.Level._Value;
    }

    return 0;
  }
  /// <summary>
  /// Destroys the entity, either immediately or using DestroyUtility.
  /// </summary>
  /// <param name="entity">The entity to destroy.</param>
  /// <param name="immediate">If true, destroys immediately; otherwise, uses DestroyUtility.</param>
  public static void Destroy(this Entity entity, bool immediate = false) {
    if (!entity.Exists()) return;

    if (immediate) {
      EntityManager.DestroyEntity(entity);
    } else {
      DestroyUtility.Destroy(EntityManager, entity);
    }
  }
  /// <summary>
  /// Sets the team and team reference of the entity based on another team source entity.
  /// </summary>
  /// <param name="entity">The entity to set the team for.</param>
  /// <param name="teamSource">The source entity to copy team data from.</param>
  public static void SetTeam(this Entity entity, Entity teamSource) {
    if (entity.Has<Team>() && entity.Has<TeamReference>() && teamSource.TryGetComponent(out Team sourceTeam) && teamSource.TryGetComponent(out TeamReference sourceTeamReference)) {
      Entity teamRefEntity = sourceTeamReference.Value._Value;
      int teamId = sourceTeam.Value;

      entity.With((ref TeamReference teamReference) => {
        teamReference.Value._Value = teamRefEntity;
      });

      entity.With((ref Team team) => {
        team.Value = teamId;
      });
    }
  }
  /// <summary>
  /// Sets the position of the entity in Translation and LastTranslation components.
  /// </summary>
  /// <param name="entity">The entity to set the position for.</param>
  /// <param name="position">The new position value.</param>
  public static void SetPosition(this Entity entity, float3 position) {
    if (entity.Has<Translation>()) {
      entity.With((ref Translation translation) => {
        translation.Value = position;
      });
    }

    if (entity.Has<LastTranslation>()) {
      entity.With((ref LastTranslation lastTranslation) => {
        lastTranslation.Value = position;
      });
    }
  }
  /// <summary>
  /// Sets the faction reference of the entity to the specified PrefabGUID.
  /// </summary>
  /// <param name="entity">The entity to set the faction for.</param>
  /// <param name="factionPrefabGuid">The faction PrefabGUID to assign.</param>
  public static void SetFaction(this Entity entity, PrefabGUID factionPrefabGuid) {
    if (entity.Has<FactionReference>()) {
      entity.With((ref FactionReference factionReference) => {
        factionReference.FactionGuid._Value = factionPrefabGuid;
      });
    }
  }
  /// <summary>
  /// Determines whether the entity is allied with the specified player entity.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <param name="player">The player entity to compare with.</param>
  /// <returns>True if allied; otherwise, false.</returns>
  public static bool IsAllies(this Entity entity, Entity player) {
    return GameSystems.ServerGameManager.IsAllies(entity, player);
  }
  /// <summary>
  /// Determines whether the entity is owned by a player.
  /// </summary>
  /// <param name="entity">The entity to check.</param>
  /// <returns>True if owned by a player; otherwise, false.</returns>
  public static bool IsPlayerOwned(this Entity entity) {
    if (entity.TryGetComponent(out EntityOwner entityOwner)) {
      return entityOwner.Owner.IsPlayer();
    }

    return false;
  }
  /// <summary>
  /// Gets the buff target entity for the specified entity.
  /// </summary>
  /// <param name="entity">The entity to get the buff target for.</param>
  /// <returns>The buff target entity.</returns>
  public static Entity GetBuffTarget(this Entity entity) {
    return CreateGameplayEventServerUtility.GetBuffTarget(EntityManager, entity);
  }


  /// <summary>
  /// Gets the localized name for a PrefabGUID.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to localize.</param>
  /// <returns>The localized name string.</returns>
  public static string LocalizedName(this PrefabGUID prefabGuid) {
    return Localizer.GetText(prefabGuid);
  }
}
