using HarmonyLib;
using UnityEngine;
using VikingHusbandry.Configuration;
using VikingHusbandry.Features;
using VikingHusbandry.Features.Commands;
using VikingHusbandry.Features.Rodeo;

namespace VikingHusbandry.Patches
{
    [HarmonyPatch(typeof(Tameable))]
    public static class TameablePatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix(Tameable __instance)
        {
            if (__instance == null || !__instance || !ModConfig.ModEnabled.Value) return;

            // Enable boar commandability so boars can be commanded like wolves
            if (ModConfig.AllowBoarCommands.Value && !__instance.m_commandable)
            {
                __instance.m_commandable = true;
            }
        }

        [HarmonyPatch(nameof(Tameable.Interact))]
        [HarmonyPrefix]
        public static bool Interact_Prefix(Tameable __instance, Humanoid user, bool hold, bool alt, ref bool __result)
        {
            if (__instance == null || !__instance || !ModConfig.ModEnabled.Value) return true;
            if (hold) return true;

            Character character = __instance.GetComponent<Character>();
            if (character == null || !character) return true;

            Player? player = user as Player;
            MonsterAI? monsterAI = __instance.GetComponent<MonsterAI>();

            // Case 1: Untamed creature interactions (Rodeo subdual & Downed state)
            if (!__instance.IsTamed())
            {
                if (ModConfig.EnableRodeoSubdual.Value)
                {
                    if (DownedTamingManager.IsDowned(character))
                    {
                        if (player != null)
                        {
                            player.Message(MessageHud.MessageType.TopLeft, "Exhausted. Use hotbar food (1-8).");
                        }
                        __result = true;
                        return false;
                    }

                    if (DownedTamingManager.CanBeSubdued(character))
                    {
                        if (player != null)
                        {
                            // Tapping E initiates rodeo; no holding required
                            RodeoController.StartRodeo(player, character);
                        }
                        __result = true;
                        return false;
                    }
                }

                // Fall back to vanilla taming interaction (e.g. petting check)
                return true;
            }

            // Case 2: Tamed creature interactions (Shift+E renames, standard E cycles 4-state stance)
            if (alt)
            {
                // Shift+E renames pet (vanilla behavior)
                return true;
            }

            if (ModConfig.EnableStances.Value && player != null && monsterAI != null)
            {
                CompanionStanceManager.CycleStance(character, player, monsterAI);
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(Tameable.GetHoverText))]
        [HarmonyPostfix]
        public static void GetHoverText_Postfix(Tameable __instance, ref string __result)
        {
            if (__instance == null || !__instance || !ModConfig.ModEnabled.Value) return;

            Character character = __instance.GetComponent<Character>();
            if (character == null || !character) return;

            string keyUse = Localization.instance.Localize("$KEY_Use");

            if (!__instance.IsTamed())
            {
                if (ModConfig.EnableRodeoSubdual.Value)
                {
                    if (DownedTamingManager.IsDowned(character))
                    {
                        float remainingSeconds = DownedTamingManager.GetDownedSecondsRemaining(character);
                        float tameRemaining = AIHelper.GetRemainingTime(__instance);
                        int tamePercent = Mathf.Clamp(Mathf.RoundToInt((1f - (tameRemaining / __instance.m_tamingTime)) * 100f), 0, 100);

                        string tameStatus = tamePercent > 0 ? $" (Tamed {tamePercent}%)" : "";
                        __result += $"\n<color=orange><b>Downed ({Mathf.CeilToInt(remainingSeconds)}s)</b></color>{tameStatus} - [<color=yellow><b>1-8</b></color>] Feed to tame";
                        return;
                    }

                    float progress = DownedTamingManager.GetSubdualProgress(character);
                    int subdualPercent = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100);
                    bool isBoar = character.name.StartsWith("Boar") || (character.m_name != null && character.m_name.ToLower().Contains("boar"));
                    string actionName = isBoar ? "Leash" : "Subdue";

                    if (DownedTamingManager.CanBeSubdued(character))
                    {
                        string progressText = subdualPercent > 0 ? $" ({subdualPercent}%)" : "";
                        __result += $"\n[<color=yellow><b>{keyUse}</b></color>] {actionName}{progressText}";
                    }
                    else if (subdualPercent > 0)
                    {
                        // Show saved subdual progress even while active/alerted so player sees their progress
                        __result += $"\n<color=yellow>[Subdual: {subdualPercent}% - Parry or Sneak to {actionName.ToLower()}]</color>";
                    }
                }
                return;
            }

            // If tamed, append current companion stance with valid TextMeshPro hex color
            if (ModConfig.EnableStances.Value)
            {
                StanceType stance = CompanionStanceManager.GetStance(character);
                string stanceName = CompanionStanceManager.GetStanceDisplayName(stance);
                __result += $"\n<color=#55ffff>[{stanceName}]</color>";
            }
        }

        [HarmonyPatch(nameof(Tameable.UseItem))]
        [HarmonyPrefix]
        public static bool UseItem_Prefix(Tameable __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            if (__instance == null || !__instance || !ModConfig.ModEnabled.Value || !ModConfig.EnableRodeoSubdual.Value) return true;

            Character character = __instance.GetComponent<Character>();
            if (character == null || !character || !DownedTamingManager.IsDowned(character)) return true;

            MonsterAI? monsterAI = __instance.GetComponent<MonsterAI>();
            if (monsterAI == null || !monsterAI) return true;

            if (DownedTamingManager.TryForceFeed(user, item, __instance, monsterAI))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
