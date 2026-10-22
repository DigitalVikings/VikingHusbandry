using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace VikingHusbandry.Features
{
    public static class AIHelper
    {
        private static readonly MethodInfo? s_moveToMethod = AccessTools.Method(
            typeof(BaseAI),
            "MoveTo",
            new[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) }
        );

        private static readonly MethodInfo? s_lookAtMethod = AccessTools.Method(
            typeof(BaseAI),
            "LookAt",
            new[] { typeof(Vector3) }
        );

        private static readonly MethodInfo? s_getPatrolPointMethod = AccessTools.Method(
            typeof(BaseAI),
            "GetPatrolPoint",
            new[] { typeof(Vector3).MakeByRefType() }
        );

        private static readonly FieldInfo? s_targetCreatureField = AccessTools.Field(
            typeof(MonsterAI),
            "m_targetCreature"
        );

        private static readonly FieldInfo? s_timeSinceHurtField = AccessTools.Field(
            typeof(BaseAI),
            "m_timeSinceHurt"
        );

        public static bool MoveTo(BaseAI ai, float dt, Vector3 point, float dist, bool run)
        {
            if (ai == null || s_moveToMethod == null) return false;
            return (bool)s_moveToMethod.Invoke(ai, new object[] { dt, point, dist, run });
        }

        public static void LookAt(BaseAI ai, Vector3 point)
        {
            if (ai == null || s_lookAtMethod == null) return;
            s_lookAtMethod.Invoke(ai, new object[] { point });
        }

        public static bool GetPatrolPoint(BaseAI ai, out Vector3 point)
        {
            point = Vector3.zero;
            if (ai == null || s_getPatrolPointMethod == null) return false;

            object[] args = new object[] { Vector3.zero };
            bool result = (bool)s_getPatrolPointMethod.Invoke(ai, args);
            point = (Vector3)args[0];
            return result;
        }

        public static void SetTargetCreature(MonsterAI ai, Character? target)
        {
            if (ai == null || s_targetCreatureField == null) return;
            s_targetCreatureField.SetValue(ai, target);
        }

        public static Character? GetTargetCreature(MonsterAI ai)
        {
            if (ai == null || s_targetCreatureField == null) return null;
            return (Character?)s_targetCreatureField.GetValue(ai);
        }

        public static float GetTimeSinceHurt(BaseAI ai)
        {
            if (ai == null || s_timeSinceHurtField == null) return 999f;
            return (float)s_timeSinceHurtField.GetValue(ai);
        }

        public static float GetRemainingTime(Tameable tameable)
        {
            if (tameable == null) return 0f;
            ZNetView nview = tameable.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return tameable.m_tamingTime;

            return nview.GetZDO().GetFloat(ZDOVars.s_tameTimeLeft, tameable.m_tamingTime);
        }

        public static void DecreaseRemainingTime(Tameable tameable, float time)
        {
            if (tameable == null) return;
            ZNetView nview = tameable.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            float current = GetRemainingTime(tameable);
            float next = Mathf.Max(0f, current - time);
            nview.GetZDO().Set(ZDOVars.s_tameTimeLeft, next);
        }

        public static void ResetFeedingTimer(Tameable tameable)
        {
            if (tameable == null || ZNet.instance == null) return;
            ZNetView nview = tameable.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            nview.GetZDO().Set(ZDOVars.s_tameLastFeeding, ZNet.instance.GetTime().Ticks);
        }

        public static void Tame(Tameable tameable, MonsterAI monsterAI)
        {
            if (tameable == null) return;
            ZNetView nview = tameable.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid())
            {
                nview.GetZDO().Set(ZDOVars.s_tameTimeLeft, 0f);
            }

            if (monsterAI != null)
            {
                monsterAI.MakeTame();
            }
            else
            {
                Character c = tameable.GetComponent<Character>();
                if (c != null)
                {
                    c.SetTamed(true);
                }
            }
        }
    }
}
