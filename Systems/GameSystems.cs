using System;
using System.Linq;
using Unity.Entities;
using ProjectM.Scripting;
using ProjectM;
using ProjectM.CastleBuilding;
using ScarletCore.Services;
using ProjectM.Network;
using ScarletCore.Events;

namespace ScarletCore.Systems;

/// <summary>
/// Provides a centralized static access point for core game systems and managers.
/// </summary>
public static class GameSystems {
  private static World _server;
  /// <summary>
  /// Provides centralized access to core game systems and managers, with initialization and caching for performance and convenience.
  /// </summary>
  private static EntityManager _entityManager;
  private static ServerGameManager _serverGameManager;
  private static ServerBootstrapSystem _serverBootstrapSystem;
  private static AdminAuthSystem _adminAuthSystem;
  private static PrefabCollectionSystem _prefabCollectionSystem;
  private static KickBanSystem_Server _kickBanSystem;
  private static UnitSpawnerUpdateSystem _unitSpawnerUpdateSystem;
  private static EntityCommandBufferSystem _entityCommandBufferSystem;
  private static DebugEventsSystem _debugEventsSystem;
  private static TriggerPersistenceSaveSystem _triggerPersistenceSaveSystem;
  private static EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;
  private static InstantiateMapIconsSystem_Spawn _instantiateMapIconsSystem_Spawn;
  private static ServerScriptMapper _serverScriptMapper;
  private static NetworkIdSystem.Singleton _networkIdSystem_Singleton;

  // Exception for when systems are not initialized
  private static Exception NotInitializedException => new("GameSystems not initialized. Call Initialize() first.");

  // Public properties that return the cached references
  /// <summary>
  /// Provides access to the server world system instance.
  /// </summary>
  public static World Server {
    get {
      if (!Initialized) throw NotInitializedException;
      return _server;
    }
  }

  /// <summary>
  /// Provides access to the current server elapsed time.
  /// </summary>
  public static double ServerTime => _server.Time.ElapsedTime;

  /// <summary>
  /// Provides access to the entity manager system instance.
  /// </summary>
  public static EntityManager EntityManager {
    get {
      if (!Initialized) throw NotInitializedException;
      return _entityManager;
    }
  }

  /// <summary>
  /// Provides access to the server game manager system instance.
  /// </summary>
  public static ServerGameManager ServerGameManager {
    get {
      if (!Initialized) throw NotInitializedException;
      return _serverGameManager;
    }
  }

  /// <summary>
  /// Provides access to the server bootstrap system instance.
  /// </summary>
  public static ServerBootstrapSystem ServerBootstrapSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _serverBootstrapSystem;
    }
  }

  /// <summary>
  /// Provides access to the admin auth system instance.
  /// </summary>
  public static AdminAuthSystem AdminAuthSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _adminAuthSystem;
    }
  }

  /// <summary>
  /// Provides access to the prefab collection system instance.
  /// </summary>
  public static PrefabCollectionSystem PrefabCollectionSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _prefabCollectionSystem;
    }
  }

  /// <summary>
  /// Provides access to the kick/ban system instance.
  /// </summary>
  public static KickBanSystem_Server KickBanSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _kickBanSystem;
    }
  }

  /// <summary>
  /// Provides access to the unit spawner update system instance.
  /// </summary>
  public static UnitSpawnerUpdateSystem UnitSpawnerUpdateSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _unitSpawnerUpdateSystem;
    }
  }

  /// <summary>
  /// Provides access to the entity command buffer system instance.
  /// </summary>
  public static EntityCommandBufferSystem EntityCommandBufferSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _entityCommandBufferSystem;
    }
  }

  /// <summary>
  /// Provides access to the debug events system instance.
  /// </summary>
  public static DebugEventsSystem DebugEventsSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _debugEventsSystem;
    }
  }

  /// <summary>
  /// Provides access to the trigger persistence save system instance.
  /// </summary>
  public static TriggerPersistenceSaveSystem TriggerPersistenceSaveSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _triggerPersistenceSaveSystem;
    }
  }

  /// <summary>
  /// Provides access to the end simulation entity command buffer system instance.
  /// </summary>
  public static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _endSimulationEntityCommandBufferSystem;
    }
  }

  /// <summary>
  /// Provides access to the instantiate map icons system instance.
  /// </summary>
  public static InstantiateMapIconsSystem_Spawn InstantiateMapIconsSystem_Spawn {
    get {
      if (!Initialized) throw NotInitializedException;
      return _instantiateMapIconsSystem_Spawn;
    }
  }

  /// <summary>
  /// Provides access to the server script mapper system instance.
  /// </summary>
  public static ServerScriptMapper ServerScriptMapper {
    get {
      if (!Initialized) throw NotInitializedException;
      return _serverScriptMapper;
    }
  }

  /// <summary>
  /// Provides access to the network ID system singleton instance.
  /// </summary>
  public static NetworkIdSystem.Singleton NetworkIdSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _networkIdSystem_Singleton;
    }
  }

  /// <summary>
  /// Provides access to the generate castle system instance.
  /// </summary>
  public static GenerateCastleSystem GenerateCastleSystem => _server.GetExistingSystemManaged<GenerateCastleSystem>();

  /// <summary>
  /// Gets a value indicating whether the game systems have been initialized.
  /// </summary>
  public static bool Initialized { get; private set; } = false;

  internal static void Initialize() {
    if (Initialized) return;

    // Cache the server world
    _server = GetServerWorld() ?? throw new Exception("There is no Server world (yet)...");

    // Cache the EntityManager
    _entityManager = _server.EntityManager;

    // Cache all system references
    _serverScriptMapper = _server.GetExistingSystemManaged<ServerScriptMapper>();
    _serverGameManager = _serverScriptMapper.GetServerGameManager();
    _serverBootstrapSystem = _server.GetExistingSystemManaged<ServerBootstrapSystem>();
    _adminAuthSystem = _server.GetExistingSystemManaged<AdminAuthSystem>();
    _prefabCollectionSystem = _server.GetExistingSystemManaged<PrefabCollectionSystem>();
    _kickBanSystem = _server.GetExistingSystemManaged<KickBanSystem_Server>();
    _unitSpawnerUpdateSystem = _server.GetExistingSystemManaged<UnitSpawnerUpdateSystem>();
    _entityCommandBufferSystem = _server.GetExistingSystemManaged<EntityCommandBufferSystem>();
    _debugEventsSystem = _server.GetExistingSystemManaged<DebugEventsSystem>();
    _triggerPersistenceSaveSystem = _server.GetExistingSystemManaged<TriggerPersistenceSaveSystem>();
    _endSimulationEntityCommandBufferSystem = _server.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
    _instantiateMapIconsSystem_Spawn = _server.GetExistingSystemManaged<InstantiateMapIconsSystem_Spawn>();
    _networkIdSystem_Singleton = _serverScriptMapper.GetSingleton<NetworkIdSystem.Singleton>();

    Initialized = true;

    PlayerService.Initialize();
  }

  /// <summary>
  /// Registers an action to be called when the game systems are initialized, or invokes it immediately if already initialized.
  /// </summary>
  /// <param name="action">The action to invoke on initialization.</param>
  public static void OnInitialize(Action action) {
    if (Initialized) {
      action.DynamicInvoke();
    } else {
      EventManager.On(ServerEvents.OnInitialize, action);
    }
  }

  static World GetServerWorld() {
    return World.s_AllWorlds.ToArray().FirstOrDefault(world => world.Name == "Server");
  }
}