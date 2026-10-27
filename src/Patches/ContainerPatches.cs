using HarmonyLib;
using UnityEngine;
using VikingHusbandry.Configuration;
using VikingHusbandry.Features.Trough;

namespace VikingHusbandry.Patches
{
    [HarmonyPatch(typeof(Container))]
    public static class ContainerPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix(Container __instance)
        {
            if (__instance != null && __instance && __instance.GetComponentInParent<Piece>() != null && TroughRegistry.IsValidChest(__instance))
            {
                TroughRegistry.Register(__instance);
                TroughVisualizer.EnsureAttached(__instance);
            }
        }

        [HarmonyPatch("UpdateUseVisual")]
        [HarmonyPostfix]
        public static void UpdateUseVisual_Postfix(Container __instance)
        {
            if (__instance != null && __instance && TroughRegistry.IsTrough(__instance))
            {
                TroughVisualizer.Get(__instance)?.EnsureChestRenderersHidden();
            }
        }

        [HarmonyPatch(nameof(Container.Interact))]
        [HarmonyPrefix]
        public static bool Interact_Prefix(Container __instance, Humanoid character, bool hold, bool alt, ref bool __result)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableTroughs.Value || __instance == null || !__instance) return true;
            if (hold) return true;

            // Only allow toggling on valid storage chests (not barrels, carts, or ships)
            if (!TroughRegistry.IsValidChest(__instance)) return true;

            // Shift + E (InteractAlt) toggles Feeding Trough designation
            if (alt)
            {
                Player? player = character as Player;
                TroughRegistry.ToggleTrough(__instance, player);
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(Container.GetHoverText))]
        [HarmonyPostfix]
        public static void GetHoverText_Postfix(Container __instance, ref string __result)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableTroughs.Value || __instance == null || !__instance) return;

            // Only show prompt on chests, not barrels or other containers
            if (!TroughRegistry.IsValidChest(__instance)) return;

            string keyUse = Localization.instance.Localize("$KEY_Use");

            if (TroughRegistry.IsTrough(__instance))
            {
                int foodCount = TroughRegistry.CountAcceptableFood(__instance, null);
                string header = $"<color=green><b>Feeding Trough</b></color> ({foodCount} items)";
                string revertPrompt = $"[<color=yellow><b>Shift+{keyUse}</b></color>] Revert to Chest";
                __result = $"{header}\n{__result}\n{revertPrompt}";
            }
            else
            {
                string togglePrompt = $"[<color=yellow><b>Shift+{keyUse}</b></color>] Convert to Feeding Trough";
                __result += $"\n{togglePrompt}";
            }
        }
    }
}
