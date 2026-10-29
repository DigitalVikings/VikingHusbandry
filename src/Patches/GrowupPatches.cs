using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using VikingHusbandry.Configuration;
using VikingHusbandry.Features.Commands;

namespace VikingHusbandry.Patches
{
    [HarmonyPatch(typeof(Growup))]
    public static class GrowupPatches
    {
        [HarmonyPatch("GrowUpdate")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> GrowUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Character), nameof(Character.SetLevel))));

            if (matcher.IsInvalid)
            {
                VikingHusbandryPlugin.Log.LogError("Failed to match Character.SetLevel in Growup.GrowUpdate transpiler");
                return instructions;
            }

            // In Growup.GrowUpdate:
            // IL_0089: ldloc.2 (adult)         [-3]
            // IL_008A: ldloc.1 (young)         [-2]
            // IL_008B: callvirt GetLevel()     [-1]
            // IL_0090: callvirt SetLevel(...)  [0]
            var loadAdultInst = matcher.Clone().Advance(-3).Instruction;
            var loadYoungInst = matcher.Clone().Advance(-2).Instruction;

            if (!loadAdultInst.IsLdloc() || !loadYoungInst.IsLdloc())
            {
                VikingHusbandryPlugin.Log.LogError($"Expected ldloc instructions before SetLevel in Growup.GrowUpdate, but found: adult={loadAdultInst.opcode}, young={loadYoungInst.opcode}");
                return instructions;
            }

            matcher.Advance(1).Insert(
                new CodeInstruction(loadYoungInst.opcode, loadYoungInst.operand),
                new CodeInstruction(loadAdultInst.opcode, loadAdultInst.operand),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GrowupPatches), nameof(InheritStanceOnGrowup)))
            );

            return matcher.InstructionEnumeration();
        }

        public static void InheritStanceOnGrowup(Character young, Character adult)
        {
            if (young == null || adult == null) return;
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableStances.Value) return;

            StanceType stance = CompanionStanceManager.GetStance(young);
            CompanionStanceManager.SetStance(adult, stance);

            MonsterAI adultAI = adult.GetComponent<MonsterAI>();
            if (adultAI != null)
            {
                MonsterAI youngAI = young.GetComponent<MonsterAI>();
                GameObject? followTarget = youngAI != null ? youngAI.GetFollowTarget() : null;
                Player? followPlayer = followTarget != null ? followTarget.GetComponent<Player>() : null;

                CompanionStanceManager.ApplyStanceToAI(adult, adultAI, followPlayer, stance);
            }
        }
    }
}
