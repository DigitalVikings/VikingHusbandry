using System;
using UnityEngine;
using VikingHusbandry.Configuration;

namespace VikingHusbandry.Features.Rodeo
{
    public static class DownedTamingManager
    {
        private static readonly int s_downedUntilHash = "VH_DownedUntil".GetStableHashCode();
        private static readonly int s_subdualProgressHash = "VH_Subdual".GetStableHashCode();
        private static readonly int s_sleepingAnimHash = "sleeping".GetStableHashCode();

        private static readonly HarmonyLib.AccessTools.FieldRef<Character, ZNetView>? s_charNViewRef =
            HarmonyLib.AccessTools.FieldRefAccess<Character, ZNetView>("m_nview");

        private static ZNetView? GetNView(Character character)
        {
            if (character == null || !character) return null;
            if (s_charNViewRef != null)
            {
                ZNetView n = s_charNViewRef(character);
                if (n != null && n) return n;
            }
            return character.GetComponent<ZNetView>();
        }

        public static float GetSubdualProgress(Character character)
        {
            if (character == null || !character) return 0f;

            CheckDownedRecovery(character);

            ZNetView? nview = GetNView(character);
            if (nview == null || !nview.IsValid())
            {
                return 0f;
            }

            return nview.GetZDO().GetFloat(s_subdualProgressHash, 0f);
        }

        public static void SetSubdualProgress(Character character, float progress)
        {
            if (character == null || !character) return;

            ZNetView? nview = GetNView(character);
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            nview.GetZDO().Set(s_subdualProgressHash, Mathf.Clamp01(progress));
        }

        public static bool IsDowned(Character character)
        {
            CheckDownedRecovery(character);
            return GetDownedSecondsRemaining(character) > 0f;
        }

        public static float GetDownedSecondsRemaining(Character character)
        {
            if (character == null || !character || ZNet.instance == null) return 0f;

            CheckDownedRecovery(character);

            ZNetView? nview = GetNView(character);
            if (nview == null || !nview.IsValid()) return 0f;

            long downedTicks = nview.GetZDO().GetLong(s_downedUntilHash, 0L);
            if (downedTicks == 0L) return 0f;

            TimeSpan span = new DateTime(downedTicks) - ZNet.instance.GetTime();
            return Mathf.Max(0f, (float)span.TotalSeconds);
        }

        public static void SetDowned(Character character, float durationSeconds)
        {
            if (character == null || !character || ZNet.instance == null) return;

            ZNetView? nview = GetNView(character);
            if (nview == null || !nview.IsValid()) return;

            DateTime until = ZNet.instance.GetTime().AddSeconds(durationSeconds);
            nview.GetZDO().Set(s_downedUntilHash, until.Ticks);
            nview.GetZDO().Set(ZDOVars.s_sleeping, true);

            // Create limp physics ragdoll for incapacitated pose
            DownedRagdollManager.CreateDownedRagdoll(character);
        }

        public static void CheckDownedRecovery(Character character)
        {
            if (character == null || !character || ZNet.instance == null) return;

            ZNetView? nview = GetNView(character);
            if (nview == null || !nview.IsValid()) return;

            long downedTicks = nview.GetZDO().GetLong(s_downedUntilHash, 0L);
            if (downedTicks == 0L) return;

            if (ZNet.instance.GetTime().Ticks >= downedTicks)
            {
                // Downed timer expired without full taming: reset progress to 0 and wake up
                nview.GetZDO().Set(s_downedUntilHash, 0L);
                nview.GetZDO().Set(s_subdualProgressHash, 0f);
                nview.GetZDO().Set(ZDOVars.s_sleeping, false);

                // Remove ragdoll and restore living character visual
                DownedRagdollManager.RemoveDownedRagdoll(character);

                ZSyncAnimation zanim = character.GetZAnim();
                if (zanim != null)
                {
                    zanim.SetTrigger("stagger");
                }
            }
        }

        public static void ClearDowned(Character character)
        {
            if (character == null || !character) return;

            ZNetView? nview = GetNView(character);
            if (nview == null || !nview.IsValid()) return;

            nview.GetZDO().Set(s_downedUntilHash, 0L);
            nview.GetZDO().Set(s_subdualProgressHash, 0f);
            nview.GetZDO().Set(ZDOVars.s_sleeping, false);

            // Remove ragdoll and restore living character visual
            DownedRagdollManager.RemoveDownedRagdoll(character);
        }

        public static bool CanBeSubdued(Character character)
        {
            if (character == null || !character || character.IsTamed()) return false;
            CheckDownedRecovery(character);
            if (IsDowned(character)) return false;

            // Opening 1: Character is staggering from a parry
            if (character.IsStaggering()) return true;

            // Opening 2: Character is unalerted / player is in stealth
            BaseAI baseAI = character.GetBaseAI();
            if (baseAI != null && !baseAI.IsAlerted()) return true;

            return false;
        }

        public static bool TryForceFeed(Humanoid user, ItemDrop.ItemData item, Tameable tameable, MonsterAI monsterAI)
        {
            if (user == null || item == null || tameable == null || monsterAI == null) return false;
            Character character = tameable.GetComponent<Character>();
            if (character == null || !character || !IsDowned(character)) return false;

            // Verify item is acceptable food
            if (!IsAcceptableFood(monsterAI, item))
            {
                user.Message(MessageHud.MessageType.TopLeft, "Food refused.");
                return false;
            }

            float remainingTime = AIHelper.GetRemainingTime(tameable);
            float fedDuration = tameable.m_fedDuration > 0f ? tameable.m_fedDuration : 300f;
            int neededCount = Mathf.Max(1, Mathf.CeilToInt(remainingTime / fedDuration));

            int availableCount = item.m_stack;
            int countToConsume = Mathf.Min(neededCount, availableCount);

            // Remove consumed food from inventory
            user.GetInventory().RemoveItem(item, countToConsume);

            // Decrease remaining tame time
            float timeToDeduct = countToConsume * fedDuration;
            AIHelper.DecreaseRemainingTime(tameable, timeToDeduct);

            // Trigger eat particle / soothe effect
            if (tameable.m_sootheEffect != null)
            {
                tameable.m_sootheEffect.Create(character.GetCenterPoint(), Quaternion.identity);
            }

            if (AIHelper.GetRemainingTime(tameable) <= 0f)
            {
                // Full tame completed!
                AIHelper.Tame(tameable, monsterAI);
                ClearDowned(character);
                user.Message(MessageHud.MessageType.TopLeft, $"{character.GetHoverName()} tamed!");

                if (tameable.m_tamedEffect != null)
                {
                    tameable.m_tamedEffect.Create(character.GetCenterPoint(), Quaternion.identity);
                }
            }
            else
            {
                int percent = Mathf.RoundToInt((1f - (AIHelper.GetRemainingTime(tameable) / tameable.m_tamingTime)) * 100f);
                user.Message(MessageHud.MessageType.TopLeft, $"Tamed {percent}%");
            }

            return true;
        }

        public static bool IsAcceptableFood(MonsterAI monsterAI, ItemDrop.ItemData item)
        {
            if (monsterAI == null || monsterAI.m_consumeItems == null || item == null) return false;

            foreach (ItemDrop consumable in monsterAI.m_consumeItems)
            {
                if (consumable != null && consumable.m_itemData != null &&
                    consumable.m_itemData.m_shared.m_name == item.m_shared.m_name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
