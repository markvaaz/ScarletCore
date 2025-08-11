using System;
using System.Linq;
using Unity.Entities;
using ProjectM.Scripting;
using ProjectM;
using ProjectM.CastleBuilding;
using ScarletCore.Services;
using ProjectM.Network;

namespace ScarletCore.Systems;

public static class GameSystems {
  // Cached system references - initialized once during startup
  private static World _server;
  private static EntityManager _entityManager;
  private static ServerGameManager _serverGameManager;
  private static ServerBootstrapSystem _serverBootstrapSystem;
  private static AdminAuthSystem _adminAuthSystem;
  private static PrefabCollectionSystem _prefabCollectionSystem;
  private static KickBanSystem_Server _kickBanSystem;
  private static UnitSpawnerUpdateSystem _unitSpawnerUpdateSystem;
  private static EntityCommandBufferSystem _entityCommandBufferSystem;
  private static DebugEventsSystem _debugEventsSystem;
  private static TriggerPersistenceSaveSystem _triggerPersistenceSaveSystem; private static EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;
  private static InstantiateMapIconsSystem_Spawn _instantiateMapIconsSystem_Spawn;
  private static ServerScriptMapper _serverScriptMapper;
  private static NetworkIdSystem.Singleton _networkIdSystem_Singleton;

  // Exception for when systems are not initialized
  private static Exception NotInitializedException => new("GameSystems not initialized. Call Initialize() first.");

  // Public properties that return the cached references
  public static World Server {
    get {
      if (!Initialized) throw NotInitializedException;
      return _server;
    }
  }

  public static EntityManager EntityManager {
    get {
      if (!Initialized) throw NotInitializedException;
      return _entityManager;
    }
  }

  public static ServerGameManager ServerGameManager {
    get {
      if (!Initialized) throw NotInitializedException;
      return _serverGameManager;
    }
  }

  public static ServerBootstrapSystem ServerBootstrapSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _serverBootstrapSystem;
    }
  }

  public static AdminAuthSystem AdminAuthSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _adminAuthSystem;
    }
  }

  public static PrefabCollectionSystem PrefabCollectionSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _prefabCollectionSystem;
    }
  }

  public static KickBanSystem_Server KickBanSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _kickBanSystem;
    }
  }

  public static UnitSpawnerUpdateSystem UnitSpawnerUpdateSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _unitSpawnerUpdateSystem;
    }
  }

  public static EntityCommandBufferSystem EntityCommandBufferSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _entityCommandBufferSystem;
    }
  }

  public static DebugEventsSystem DebugEventsSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _debugEventsSystem;
    }
  }

  public static TriggerPersistenceSaveSystem TriggerPersistenceSaveSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _triggerPersistenceSaveSystem;
    }
  }

  public static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _endSimulationEntityCommandBufferSystem;
    }
  }

  public static InstantiateMapIconsSystem_Spawn InstantiateMapIconsSystem_Spawn {
    get {
      if (!Initialized) throw NotInitializedException;
      return _instantiateMapIconsSystem_Spawn;
    }
  }

  public static ServerScriptMapper ServerScriptMapper {
    get {
      if (!Initialized) throw NotInitializedException;
      return _serverScriptMapper;
    }
  }

  public static NetworkIdSystem.Singleton NetworkIdSystem {
    get {
      if (!Initialized) throw NotInitializedException;
      return _networkIdSystem_Singleton;
    }
  }

  public static GenerateCastleSystem GenerateCastleSystem => _server.GetExistingSystemManaged<GenerateCastleSystem>();

  public static bool Initialized { get; private set; } = false;

  public static void Initialize() {
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

    Events.EventManager.InvokeInitialize();
  }

  static World GetServerWorld() {
    return World.s_AllWorlds.ToArray().FirstOrDefault(world => world.Name == "Server");
  }
}