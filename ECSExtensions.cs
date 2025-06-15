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

namespace ScarletCore;

public static class ECSExtensions {
  public static EntityManager EntityManager => GameSystems.Server.EntityManager;
  public delegate void WithRefHandler<T>(ref T item);
  public static void With<T>(this Entity entity, WithRefHandler<T> action) where T : struct {
    T item = entity.Read<T>();
    action(ref item);

    EntityManager.SetComponentData(entity, item);
  }
  public static void AddWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct {
    if (!entity.Has<T>()) {
      entity.Add<T>();
    }

    entity.With(action);
  }
  public static void HasWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct {
    if (entity.Has<T>()) {
      entity.With(action);
    }
  }
  public unsafe static void Write<T>(this Entity entity, T componentData) where T : struct {
    EntityManager.SetComponentData(entity, componentData);
  }
  public static T Read<T>(this Entity entity) where T : struct {
    return EntityManager.GetComponentData<T>(entity);
  }
  public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct {
    return EntityManager.GetBuffer<T>(entity);
  }
  public static DynamicBuffer<T> AddBuffer<T>(this Entity entity) where T : struct {
    return EntityManager.AddBuffer<T>(entity);
  }
  public static bool TryGetComponent<T>(this Entity entity, out T componentData) where T : struct {
    componentData = default;

    if (entity.Has<T>()) {
      componentData = entity.Read<T>();
      return true;
    }

    return false;
  }
  public static bool Has<T>(this Entity entity) where T : struct {
    return EntityManager.HasComponent(entity, new(Il2CppType.Of<T>()));
  }
  public static void Add<T>(this Entity entity) where T : struct {
    if (!entity.Has<T>()) EntityManager.AddComponent(entity, new(Il2CppType.Of<T>()));
  }
  public static void Remove<T>(this Entity entity) where T : struct {
    if (entity.Has<T>()) EntityManager.RemoveComponent(entity, new(Il2CppType.Of<T>()));
  }
  public static bool TryGetPlayer(this Entity entity, out Entity player) {
    player = Entity.Null;

    if (entity.Has<PlayerCharacter>()) {
      player = entity;

      return true;
    }

    return false;
  }
  public static bool IsPlayer(this Entity entity) {
    if (entity.Has<PlayerCharacter>()) {
      return true;
    }

    return false;
  }

  public static bool TryGetAttached(this Entity entity, out Entity attached) {
    attached = Entity.Null;

    if (entity.TryGetComponent(out Attach attach) && attach.Parent.Exists()) {
      attached = attach.Parent;
      return true;
    }

    return false;
  }
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
  public static Entity GetUserEntity(this Entity entity) {
    if (entity.TryGetComponent(out PlayerCharacter playerCharacter)) return playerCharacter.UserEntity;
    else if (entity.Has<User>()) return entity;

    return Entity.Null;
  }
  public static bool Exists(this Entity entity) {
    return !entity.IsNull() && EntityManager.Exists(entity);
  }
  public static bool IsNull(this Entity entity) {
    return Entity.Null.Equals(entity);
  }
  public static bool IsDisabled(this Entity entity) {
    return entity.Has<Disabled>();
  }

  public static PrefabGUID GetPrefabGuid(this Entity entity) {
    if (entity.TryGetComponent(out PrefabGUID prefabGuid)) return prefabGuid;

    return PrefabGUID.Empty;
  }
  public static int GetGuidHash(this Entity entity) {
    if (entity.TryGetComponent(out PrefabGUID prefabGUID)) return prefabGUID.GuidHash;

    return PrefabGUID.Empty.GuidHash;
  }
  public static Entity GetOwner(this Entity entity) {
    if (entity.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.Exists()) return entityOwner.Owner;

    return Entity.Null;
  }

  public static bool HasBuff(this Entity entity, PrefabGUID buffPrefabGuid) {
    return GameSystems.ServerGameManager.HasBuff(entity, buffPrefabGuid.ToIdentifier());
  }
  public static unsafe bool TryGetBuffer<T>(this Entity entity, out DynamicBuffer<T> dynamicBuffer) where T : struct {
    if (GameSystems.ServerGameManager.TryGetBuffer(entity, out dynamicBuffer)) {
      return true;
    }

    dynamicBuffer = default;
    return false;
  }
  public static float3 AimPosition(this Entity entity) {
    if (entity.TryGetComponent(out EntityInput entityInput)) {
      return entityInput.AimPosition;
    }

    return float3.zero;
  }
  public static float3 Position(this Entity entity) {
    if (entity.TryGetComponent(out Translation translation)) {
      return translation.Value;
    }

    return float3.zero;
  }
  public static int2 GetTileCoord(this Entity entity) {
    if (entity.TryGetComponent(out TilePosition tilePosition)) {
      return tilePosition.Tile;
    }

    return int2.zero;
  }
  public static int GetUnitLevel(this Entity entity) {
    if (entity.TryGetComponent(out UnitLevel unitLevel)) {
      return unitLevel.Level._Value;
    }

    return 0;
  }
  public static void Destroy(this Entity entity, bool immediate = false) {
    if (!entity.Exists()) return;

    if (immediate) {
      EntityManager.DestroyEntity(entity);
    } else {
      DestroyUtility.Destroy(EntityManager, entity);
    }
  }
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
  public static void SetFaction(this Entity entity, PrefabGUID factionPrefabGuid) {
    if (entity.Has<FactionReference>()) {
      entity.With((ref FactionReference factionReference) => {
        factionReference.FactionGuid._Value = factionPrefabGuid;
      });
    }
  }
  public static bool IsAllies(this Entity entity, Entity player) {
    return GameSystems.ServerGameManager.IsAllies(entity, player);
  }
  public static bool IsPlayerOwned(this Entity entity) {
    if (entity.TryGetComponent(out EntityOwner entityOwner)) {
      return entityOwner.Owner.IsPlayer();
    }

    return false;
  }
  public static Entity GetBuffTarget(this Entity entity) {
    return CreateGameplayEventServerUtility.GetBuffTarget(EntityManager, entity);
  }
}
