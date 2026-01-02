using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ScarletCore.Services;
using ScarletCore.Systems;
using ScarletCore.Utils;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal class AdminAuthSystem_Patch {
  [HarmonyPatch(typeof(AdminAuthSystem), nameof(AdminAuthSystem.OnUpdate))]
  [HarmonyPrefix]
  private static void AdminAuthSystem_OnUpdate_Prefix(AdminAuthSystem __instance) {
    if (!GameSystems.Initialized) return;
    var query = __instance._Query.ToEntityArray(Unity.Collections.Allocator.Temp);

    foreach (var entity in query) {
      if (!entity.Has<AdminAuthEvent>()) continue;
      var user = entity.Read<FromCharacter>().User;
      var player = user.GetPlayerData();

      if (player == null) {
        Log.Fatal($"AdminAuthSystem: PlayerData is null for user entity {user}.");
        continue;
      }

      RoleService.AddRoleToPlayer(player, DefaultRoles.Admin);
      Log.Message($"[AdminAuth] Granted admin role to player {player.Name} (ID: {player.PlatformId}).");
    }
  }

  [HarmonyPatch(typeof(NoAdminSystem), nameof(NoAdminSystem.OnUpdate))]
  [HarmonyPrefix]
  private static void NoAdminSystem_OnUpdate_Prefix(NoAdminSystem __instance) {
    if (!GameSystems.Initialized) return;
    var query = __instance._Query.ToEntityArray(Unity.Collections.Allocator.Temp);

    foreach (var entity in query) {
      if (!entity.Has<DeauthAdminEvent>()) continue;
      var user = entity.Read<FromCharacter>().User;
      var player = user.GetPlayerData();

      if (player == null) {
        Log.Fatal($"NoAdminSystem: PlayerData is null for user entity {user}.");
        continue;
      }

      RoleService.RemoveRoleFromPlayer(player, DefaultRoles.Admin);
      Log.Message($"[AdminDeAuth] Revoked admin role from player {player.Name} (ID: {player.PlatformId}).");
    }
  }
}