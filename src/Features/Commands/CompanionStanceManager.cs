using UnityEngine;
using VikingHusbandry.Configuration;

namespace VikingHusbandry.Features.Commands
{
    public static class CompanionStanceManager
    {
        private static readonly int s_stanceHash = "VH_Stance".GetStableHashCode();

        public static StanceType GetStance(Character character)
        {
            if (character == null) return StanceType.StayGuard;

            ZNetView nview = character.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return StanceType.StayGuard;
            }

            int val = nview.GetZDO().GetInt(s_stanceHash, -1);
            if (val >= 0 && val <= 3)
            {
                return (StanceType)val;
            }

            // No explicit stance set yet: determine from native Valheim follow state
            string follow = nview.GetZDO().GetString(ZDOVars.s_follow, "");
            if (!string.IsNullOrEmpty(follow))
            {
                return StanceType.FollowAggressive;
            }

            // By default, pen-bound tamed livestock stay at their post / pen
            return StanceType.StayGuard;
        }

        public static void SetStance(Character character, StanceType stance)
        {
            if (character == null) return;

            ZNetView nview = character.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            nview.GetZDO().Set(s_stanceHash, (int)stance);
        }

        public static StanceType CycleStance(Character character, Player player, MonsterAI monsterAI)
        {
            StanceType current = GetStance(character);
            StanceType next = (StanceType)(((int)current + 1) % 4);
            SetStance(character, next);

            ApplyStanceToAI(character, monsterAI, player, next);
            ShowStanceNotification(character, player, next);

            return next;
        }

        public static void ApplyStanceToAI(Character character, MonsterAI monsterAI, Player? player, StanceType stance)
        {
            if (monsterAI == null || character == null) return;

            ZNetView nview = character.GetComponent<ZNetView>();

            switch (stance)
            {
                case StanceType.FollowAggressive:
                case StanceType.FollowPassive:
                    monsterAI.ResetPatrolPoint();
                    monsterAI.SetFollowTarget(player != null ? player.gameObject : null);
                    if (nview != null && nview.IsOwner())
                    {
                        nview.GetZDO().Set(ZDOVars.s_follow, player != null ? player.GetPlayerName() : "");
                    }
                    break;

                case StanceType.StayGuard:
                case StanceType.StayPassive:
                    monsterAI.SetFollowTarget(null);
                    monsterAI.SetPatrolPoint();
                    if (nview != null && nview.IsOwner())
                    {
                        nview.GetZDO().Set(ZDOVars.s_follow, "");
                    }
                    break;
            }
        }

        public static string GetStanceDisplayName(StanceType stance)
        {
            return stance switch
            {
                StanceType.FollowAggressive => "Following (Aggressive)",
                StanceType.FollowPassive => "Following (Passive)",
                StanceType.StayGuard => "Staying (Guard)",
                StanceType.StayPassive => "Staying (Passive)",
                _ => "Following (Aggressive)"
            };
        }

        private static void ShowStanceNotification(Character character, Player player, StanceType stance)
        {
            if (player == null) return;

            string creatureName = character.GetHoverName();
            string status = GetStanceDisplayName(stance);
            player.Message(MessageHud.MessageType.TopLeft, $"{creatureName}: {status}");
        }

        public static bool ShouldRetaliateOnly(StanceType stance)
        {
            return stance == StanceType.FollowPassive || stance == StanceType.StayPassive;
        }
    }
}
