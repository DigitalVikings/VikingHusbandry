using HarmonyLib;
using UnityEngine;
using VikingHusbandry.Configuration;
using VikingHusbandry.Features.Trough;

namespace VikingHusbandry.Patches
{
    [HarmonyPatch(typeof(Procreation))]
    public static class ProcreationPatches
    {
        [HarmonyPatch("Procreate")]
        [HarmonyPrefix]
        public static bool Procreate_Prefix(Procreation __instance)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.AntiChuteHorizontalOnly.Value)
            {
                return true;
            }

            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return true;

            // Allow pregnant mothers to give birth regardless of density
            long pregnantTime = nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L);
            if (pregnantTime != 0L)
            {
                return true;
            }

            int prefabHash = nview.GetZDO().GetPrefab();
            GameObject myPrefab = ZNetScene.instance.GetPrefab(prefabHash);
            if (myPrefab == null) return true;

            // Enforce horizontal 2D cylinder density capping (ignores vertical drop in breeding chutes)
            bool overcrowded = PaddockDensityEnforcer.IsPaddockOvercrowded(
                myPrefab,
                __instance.m_offspring,
                __instance.transform.position,
                __instance.m_totalCheckRange,
                __instance.m_maxCreatures
            );

            if (overcrowded)
            {
                // Herd cap reached within paddock cylinder; suppress procreation attempt
                return false;
            }

            return true;
        }
    }
}
