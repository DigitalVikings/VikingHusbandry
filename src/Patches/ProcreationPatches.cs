using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using VikingHusbandry.Configuration;
using VikingHusbandry.Features.Commands;
using VikingHusbandry.Features.Trough;

namespace VikingHusbandry.Patches
{
    [HarmonyPatch(typeof(Procreation))]
    public static class ProcreationPatches
    {
        private static readonly AccessTools.FieldRef<Procreation, ZNetView> _procreationNView =
            AccessTools.FieldRefAccess<Procreation, ZNetView>("m_nview");
        private static readonly AccessTools.FieldRef<Procreation, Character> _procreationChar =
            AccessTools.FieldRefAccess<Procreation, Character>("m_character");
        private static readonly int s_partnerLevelHash = "VH_PartnerLevel".GetStableHashCode();

        [ThreadStatic]
        internal static Procreation? s_activeMother;

        [ThreadStatic]
        internal static Character? s_newbornOffspring;

        private static bool? s_isStarLevelSystemInstalled;
        private static bool IsStarLevelSystemInstalled
        {
            get
            {
                if (!s_isStarLevelSystemInstalled.HasValue)
                {
                    s_isStarLevelSystemInstalled = BepInEx.Bootstrap.Chainloader.PluginInfos != null &&
                                                  BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("MidnightsFX.StarLevelSystem");
                }
                return s_isStarLevelSystemInstalled.Value;
            }
        }

        [HarmonyPatch("MakePregnant")]
        [HarmonyPrefix]
        public static void MakePregnant_Prefix(Procreation __instance)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnablePartnerStarInheritance.Value || IsStarLevelSystemInstalled || __instance == null) return;

            ZNetView nview = _procreationNView(__instance);
            if (nview == null || !nview.IsValid()) return;

            Character mother = _procreationChar(__instance);
            if (mother == null) return;

            Vector3 pos = __instance.transform.position;
            float checkRange = __instance.m_partnerCheckRange;
            float rangeSq = checkRange * checkRange;

            int bestPartnerLevel = 1;
            List<Character> allChars = Character.GetAllCharacters();
            for (int i = 0; i < allChars.Count; i++)
            {
                Character other = allChars[i];
                if (other == null || other == mother || other.IsDead() || !other.IsTamed()) continue;

                if (other.m_name != mother.m_name) continue;

                Vector3 diff = other.transform.position - pos;
                diff.y = 0f;
                if (diff.sqrMagnitude <= rangeSq)
                {
                    int lvl = other.GetLevel();
                    if (lvl > bestPartnerLevel)
                    {
                        bestPartnerLevel = lvl;
                    }
                }
            }

            if (bestPartnerLevel > 1)
            {
                nview.GetZDO().Set(s_partnerLevelHash, bestPartnerLevel);
            }
        }

        [HarmonyPatch("Procreate")]
        [HarmonyPrefix]
        public static bool Procreate_Prefix(Procreation __instance, bool __runOriginal)
        {
            if (!__runOriginal || __instance == null || !__instance)
            {
                s_activeMother = null;
                s_newbornOffspring = null;
                return false;
            }

            s_activeMother = __instance;
            s_newbornOffspring = null;

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

            if (ZNetScene.instance == null) return true;

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

        [HarmonyPatch("Procreate")]
        [HarmonyPostfix]
        public static void Procreate_Postfix(Procreation __instance)
        {
            if (s_activeMother != null && s_newbornOffspring != null)
            {
                InheritParentStance(s_activeMother, s_newbornOffspring);
            }
        }

        [HarmonyPatch("Procreate")]
        [HarmonyFinalizer]
        public static void Procreate_Finalizer()
        {
            s_activeMother = null;
            s_newbornOffspring = null;
        }

        public static void InheritParentStance(Procreation mother, Character offspring)
        {
            if (mother == null || offspring == null) return;
            if (!ModConfig.ModEnabled.Value) return;

            Character parent = mother.GetComponent<Character>();
            if (parent == null || !parent) return;

            // 50/50 Star Inheritance between mother and nearby partner (yield to StarLevelSystem if installed)
            if (ModConfig.EnablePartnerStarInheritance.Value)
            {
                ZNetView nview = _procreationNView(mother);
                if (nview != null && nview.IsValid())
                {
                    int partnerLevel = nview.GetZDO().GetInt(s_partnerLevelHash, 0);
                    if (partnerLevel > 1)
                    {
                        if (!IsStarLevelSystemInstalled)
                        {
                            int motherLevel = parent.GetLevel();
                            int chosenLevel = UnityEngine.Random.value < 0.5f ? motherLevel : partnerLevel;
                            chosenLevel = Mathf.Max(mother.m_minOffspringLevel, chosenLevel);
                            offspring.SetLevel(chosenLevel);
                        }
                        nview.GetZDO().Set(s_partnerLevelHash, 0);
                    }
                }
            }

            if (!ModConfig.EnableStances.Value) return;

            StanceType stance = CompanionStanceManager.GetStance(parent);
            CompanionStanceManager.SetStance(offspring, stance);

            MonsterAI offspringAI = offspring.GetComponent<MonsterAI>();
            if (offspringAI != null)
            {
                MonsterAI? parentAI = parent.GetComponent<MonsterAI>();
                GameObject? followTarget = parentAI != null ? parentAI.GetFollowTarget() : null;
                Player? followPlayer = followTarget != null ? followTarget.GetComponent<Player>() : null;

                CompanionStanceManager.ApplyStanceToAI(offspring, offspringAI, followPlayer, stance);
            }
            else
            {
                BaseAI baseAI = offspring.GetComponent<BaseAI>();
                if (baseAI != null && (stance == StanceType.StayGuard || stance == StanceType.StayPassive))
                {
                    baseAI.ResetPatrolPoint();
                    baseAI.SetPatrolPoint();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetTamed))]
    public static class CharacterSetTamedProcreationPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Character __instance)
        {
            if (ProcreationPatches.s_activeMother != null)
            {
                ProcreationPatches.s_newbornOffspring = __instance;
            }
        }
    }
}
