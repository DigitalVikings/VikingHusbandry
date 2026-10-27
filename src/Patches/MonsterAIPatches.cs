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
        private static readonly AccessTools.FieldRef<BaseAI, Character>? s_characterRef =
            AccessTools.FieldRefAccess<BaseAI, Character>("m_character");

        private static readonly AccessTools.FieldRef<BaseAI, Tameable>? s_tamableRef =
            AccessTools.FieldRefAccess<BaseAI, Tameable>("m_tamable");

        private static readonly AccessTools.FieldRef<BaseAI, ZNetView>? s_nviewRef =
            AccessTools.FieldRefAccess<BaseAI, ZNetView>("m_nview");

        private static Character? GetCharacter(MonsterAI ai)
        {
            if (ai == null || !ai) return null;
            if (s_characterRef != null)
            {
                Character c = s_characterRef(ai);
                if (c != null && c) return c;
            }
            return ai.GetComponent<Character>();
        }

        private static Tameable? GetTameable(MonsterAI ai)
        {
            if (ai == null || !ai) return null;
            if (s_tamableRef != null)
            {
                Tameable t = s_tamableRef(ai);
                if (t != null && t) return t;
            }
            return ai.GetComponent<Tameable>();
        }

        private static ZNetView? GetNView(MonsterAI ai)
        {
            if (ai == null || !ai) return null;
            if (s_nviewRef != null)
            {
                ZNetView n = s_nviewRef(ai);
                if (n != null && n) return n;
            }
            return ai.GetComponent<ZNetView>();
        }

        [HarmonyPatch(nameof(MonsterAI.UpdateAI))]
        [HarmonyPrefix]
        public static bool UpdateAI_Prefix(MonsterAI __instance, float dt, ref bool __result)
        {
            if (!ModConfig.ModEnabled.Value) return true;
            if (__instance == null || !__instance) return true;

            ZNetView? nview = GetNView(__instance);
            if (nview == null || !nview.IsValid()) return true;

            Character? character = GetCharacter(__instance);
            if (character == null || !character) return true;

            // 0a. Suppress default AI attack loops while in Tether Subdual (locomotion is driven by RodeoController)
            if (RodeoController.IsBeastInTether(character))
            {
                __result = true;
                return false;
            }

            // 0b. Freeze locomotion if beast is currently mounted in saddle rodeo
            if (RodeoController.IsBeastMounted(character))
            {
                __instance.StopMoving();
                __result = true;
                return false;
            }

            // 1. Immobilize downed creatures
            DownedTamingManager.CheckDownedRecovery(character);
            if (DownedTamingManager.IsDowned(character))
            {
                __instance.StopMoving();
                Rigidbody body = character.GetComponent<Rigidbody>();
                if (body != null && !body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                if (!DownedRagdollManager.HasRagdoll(character))
                {
                    DownedRagdollManager.CreateDownedRagdoll(character);
                }

                __result = true;
                return false;
            }
            else
            {
                if (DownedRagdollManager.HasRagdoll(character))
                {
                    DownedRagdollManager.RemoveDownedRagdoll(character);
                }
            }

            // 2. Trough scent pathing for hungry tamed livestock
            Tameable? tameable = GetTameable(__instance);
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

        [HarmonyPatch(nameof(MonsterAI.IsSleeping))]
        [HarmonyPrefix]
        public static bool IsSleeping_Prefix(MonsterAI __instance, ref bool __result)
        {
            if (!ModConfig.ModEnabled.Value) return true;
            if (__instance == null || !__instance) return true;

            Character? character = GetCharacter(__instance);
            if (character != null && DownedTamingManager.IsDowned(character))
            {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch("UpdateTarget")]
        [HarmonyPostfix]
        public static void UpdateTarget_Postfix(MonsterAI __instance)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableStances.Value) return;
            if (__instance == null || !__instance) return;

            Character? character = GetCharacter(__instance);
            if (character == null || !character || !character.IsTamed()) return;

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
