using HarmonyLib;
using VikingHusbandry.Features.Rodeo;

namespace VikingHusbandry.Patches
{
    [HarmonyPatch]
    public static class CharacterDeathPatches
    {
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        [HarmonyPrefix]
        public static void Character_OnDeath_Prefix(Character __instance)
        {
            RodeoController.HandleCharacterRemovedOrDead(__instance);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        [HarmonyPrefix]
        public static void Player_OnDeath_Prefix(Player __instance)
        {
            RodeoController.HandleCharacterRemovedOrDead(__instance);
        }

        [HarmonyPatch(typeof(Character), "OnDestroy")]
        [HarmonyPrefix]
        public static void Character_OnDestroy_Prefix(Character __instance)
        {
            RodeoController.HandleCharacterRemovedOrDead(__instance);
        }
    }
}
