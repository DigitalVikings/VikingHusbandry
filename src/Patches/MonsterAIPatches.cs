using HarmonyLib;
using UnityEngine;
using VikingHusbandry.Configuration;
using VikingHusbandry.Features;
using VikingHusbandry.Features.Commands;
using VikingHusbandry.Features.Rodeo;
using VikingHusbandry.Features.Trough;

namespace VikingHusbandry.Patches
{
    [HarmonyPatch(typeof(MonsterAI))]
    public static class MonsterAIPatches
    {
        [HarmonyPatch(nameof(MonsterAI.UpdateAI))]
        [HarmonyPrefix]
        public static bool UpdateAI_Prefix(MonsterAI __instance, float dt, ref bool __result)
        {
            if (!ModConfig.ModEnabled.Value) return true;

            Character character = __instance.GetComponent<Character>();
            if (character == null) return true;

            // 1. Immobilize downed creatures
            if (DownedTamingManager.IsDowned(character))
            {
                __instance.StopMoving();
                ZSyncAnimation zanim = character.GetZAnim();
                if (zanim != null)
                {
                    zanim.SetTrigger("stagger");
                }
                __result = true;
                return false;
            }

            // 2. Trough scent pathing for hungry tamed livestock
            Tameable tameable = __instance.GetComponent<Tameable>();
            if (ModConfig.EnableTroughs.Value &&
                character.IsTamed() &&
                tameable != null &&
                tameable.IsHungry() &&
                __instance.m_consumeItems != null &&
                __instance.m_consumeItems.Count > 0)
            {
                Container? trough = TroughRegistry.FindClosestTroughWithFood(
                    character.transform.position,
                    __instance.m_consumeItems,
                    ModConfig.ScentRadius.Value
                );

                if (trough != null)
                {
                    float distToTrough = Vector3.Distance(character.transform.position, trough.transform.position);

                    if (distToTrough > 2.5f)
                    {
                        // Path toward the feeding trough
                        AIHelper.MoveTo(__instance, dt, trough.transform.position, 2.0f, false);
                        __result = true;
                        return false;
                    }
                    else
                    {
                        // In eating range of trough: consume 1 food item
                        __instance.StopMoving();
                        AIHelper.LookAt(__instance, trough.transform.position);

                        ItemDrop.ItemData? consumed = TroughRegistry.ConsumeOneFromTrough(trough, __instance.m_consumeItems);
                        if (consumed != null)
                        {
                            ZSyncAnimation zanim = character.GetZAnim();
                            if (zanim != null)
                            {
                                zanim.SetTrigger("consume");
                            }

                            if (tameable.m_sootheEffect != null)
                            {
                                tameable.m_sootheEffect.Create(character.GetCenterPoint(), Quaternion.identity);
                            }

                            // Reset hunger timer
                            AIHelper.ResetFeedingTimer(tameable);
                        }

                        __result = true;
                        return false;
                    }
                }
            }

            return true;
        }

        [HarmonyPatch("UpdateTarget")]
        [HarmonyPostfix]
        public static void UpdateTarget_Postfix(MonsterAI __instance)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableStances.Value) return;

            Character character = __instance.GetComponent<Character>();
            if (character == null || !character.IsTamed()) return;

            StanceType stance = CompanionStanceManager.GetStance(character);

            // Stance: FollowPassive & StayPassive (Retaliate only if damaged recently)
            if (CompanionStanceManager.ShouldRetaliateOnly(stance))
            {
                if (AIHelper.GetTimeSinceHurt(__instance) > 3.0f)
                {
                    // Suppress targeting enemies unprovoked
                    AIHelper.SetTargetCreature(__instance, null);
                }
            }
            // Stance: StayGuard (Leash back to guard post if pursued too far)
            else if (stance == StanceType.StayGuard)
            {
                if (AIHelper.GetPatrolPoint(__instance, out Vector3 patrolPoint))
                {
                    float dist = Vector3.Distance(character.transform.position, patrolPoint);
                    if (dist > ModConfig.GuardLeashRadius.Value)
                    {
                        // Exceeded leash boundary: drop target and leash back
                        AIHelper.SetTargetCreature(__instance, null);
                        AIHelper.MoveTo(__instance, Time.deltaTime, patrolPoint, 1.0f, true);
                    }
                }
            }
        }
    }
}
