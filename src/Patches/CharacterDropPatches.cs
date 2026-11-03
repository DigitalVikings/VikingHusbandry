using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using VikingHusbandry.Configuration;
using VikingHusbandry.Features.Trough;

namespace VikingHusbandry.Patches
{
    [HarmonyPatch(typeof(CharacterDrop))]
    public static class CharacterDropPatches
    {
        [HarmonyPatch(nameof(CharacterDrop.GenerateDropList))]
        [HarmonyPostfix]
        public static void GenerateDropList_Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            if (__result == null || __instance == null || !ModConfig.ModEnabled.Value || !ModConfig.EnableFattening.Value) return;

            Character character = __instance.GetComponent<Character>();
            if (character == null || !character || !character.IsTamed()) return;

            float bonusMult = FatteningManager.GetDropMultiplier(character);
            if (bonusMult <= 1.001f) return;

            FatteningManager.ApplyDropBonus(__instance, bonusMult, ref __result);
        }
    }
}
