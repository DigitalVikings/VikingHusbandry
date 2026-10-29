using System.Collections.Generic;
using System.Reflection.Emit;
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
        [HarmonyPatch("Procreate")]
        [HarmonyPrefix]
        public static bool Procreate_Prefix(Procreation __instance)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.AntiChuteHorizontalOnly.Value || __instance == null || !__instance)
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
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Procreate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Character), nameof(Character.SetTamed))));

            if (matcher.IsInvalid)
            {
                VikingHusbandryPlugin.Log.LogError("Failed to match Character.SetTamed in Procreation.Procreate transpiler");
                return instructions;
            }

            // Scan backwards from Character.SetTamed to find the ldloc instruction that pushed the offspring Character instance (IL_01B9: ldloc.s V_6)
            var scan = matcher.Clone();
            CodeInstruction? loadCharInst = null;
            for (int i = 0; i < 6; i++)
            {
                scan.Advance(-1);
                if (scan.Instruction.IsLdloc())
                {
                    loadCharInst = scan.Instruction;
                    break;
                }
            }

            if (loadCharInst == null)
            {
                VikingHusbandryPlugin.Log.LogError("Failed to locate offspring Character ldloc instruction before Character.SetTamed in Procreation.Procreate");
                return instructions;
            }

            matcher.Advance(1).Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(loadCharInst.opcode, loadCharInst.operand),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ProcreationPatches), nameof(InheritParentStance)))
            );

            return matcher.InstructionEnumeration();
        }

        public static void InheritParentStance(Procreation mother, Character offspring)
        {
            if (mother == null || offspring == null) return;
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableStances.Value) return;

            Character parent = mother.GetComponent<Character>();
            if (parent == null || !parent) return;

            StanceType stance = CompanionStanceManager.GetStance(parent);
            CompanionStanceManager.SetStance(offspring, stance);

            MonsterAI offspringAI = offspring.GetComponent<MonsterAI>();
            if (offspringAI != null)
            {
                MonsterAI parentAI = parent.GetComponent<MonsterAI>();
                GameObject? followTarget = parentAI != null ? parentAI.GetFollowTarget() : null;
                Player? followPlayer = followTarget != null ? followTarget.GetComponent<Player>() : null;

                CompanionStanceManager.ApplyStanceToAI(offspring, offspringAI, followPlayer, stance);
            }
        }
    }
}
