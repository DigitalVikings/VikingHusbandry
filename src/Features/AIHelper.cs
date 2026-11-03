using System;
using HarmonyLib;
using UnityEngine;

namespace VikingHusbandry.Features
{
    public static class AIHelper
    {
        public delegate bool MoveToFn(BaseAI ai, float dt, Vector3 point, float dist, bool run);
        public delegate void LookAtFn(BaseAI ai, Vector3 point);
        public delegate bool FleeFn(BaseAI ai, float dt, Vector3 point);
        public delegate bool GetPatrolPointFn(BaseAI ai, out Vector3 point);

        private static readonly MoveToFn? s_moveTo =
            BindMethod<MoveToFn>(typeof(BaseAI), "MoveTo", new[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) });

        private static readonly LookAtFn? s_lookAt =
            BindMethod<LookAtFn>(typeof(BaseAI), "LookAt", new[] { typeof(Vector3) });

        private static readonly FleeFn? s_flee =
            BindMethod<FleeFn>(typeof(BaseAI), "Flee", new[] { typeof(float), typeof(Vector3) });

        private static readonly GetPatrolPointFn? s_getPatrolPoint =
            BindMethod<GetPatrolPointFn>(typeof(BaseAI), "GetPatrolPoint", new[] { typeof(Vector3).MakeByRefType() });

        private static readonly AccessTools.FieldRef<MonsterAI, Character>? s_targetCreature =
            BindField<MonsterAI, Character>("m_targetCreature");

        private static readonly AccessTools.FieldRef<BaseAI, float>? s_timeSinceHurt =
            BindField<BaseAI, float>("m_timeSinceHurt");

        private static TDelegate? BindMethod<TDelegate>(Type type, string name, Type[] parameters) where TDelegate : Delegate
        {
            try
            {
                var method = AccessTools.Method(type, name, parameters);
                if (method != null)
                {
                    return AccessTools.MethodDelegate<TDelegate>(method);
                }
                VikingHusbandryPlugin.Log?.LogWarning($"[AIHelper] Method {type.Name}.{name} not found.");
            }
            catch (Exception ex)
            {
                VikingHusbandryPlugin.Log?.LogWarning($"[AIHelper] Failed to bind delegate for {type.Name}.{name}: {ex.Message}");
            }
            return null;
        }

        private static AccessTools.FieldRef<TTarget, TField>? BindField<TTarget, TField>(string fieldName)
        {
            try
            {
                if (AccessTools.Field(typeof(TTarget), fieldName) != null)
                {
                    return AccessTools.FieldRefAccess<TTarget, TField>(fieldName);
                }
                VikingHusbandryPlugin.Log?.LogWarning($"[AIHelper] Field {typeof(TTarget).Name}.{fieldName} not found.");
            }
            catch (Exception ex)
            {
                VikingHusbandryPlugin.Log?.LogWarning($"[AIHelper] Failed to bind field ref for {typeof(TTarget).Name}.{fieldName}: {ex.Message}");
            }
            return null;
        }

        public static bool Flee(BaseAI ai, float dt, Vector3 point)
        {
            if (ai == null || s_flee == null) return false;
            return s_flee(ai, dt, point);
        }

        public static bool MoveTo(BaseAI ai, float dt, Vector3 point, float dist, bool run)
        {
            if (ai == null || s_moveTo == null) return false;
            return s_moveTo(ai, dt, point, dist, run);
        }

        public static void LookAt(BaseAI ai, Vector3 point)
        {
            if (ai == null) return;
            s_lookAt?.Invoke(ai, point);
        }

        public static bool GetPatrolPoint(BaseAI ai, out Vector3 point)
        {
            point = Vector3.zero;
            if (ai == null || s_getPatrolPoint == null) return false;
            return s_getPatrolPoint(ai, out point);
        }

        public static void SetTargetCreature(MonsterAI ai, Character? target)
        {
            if (ai == null || s_targetCreature == null) return;
            s_targetCreature(ai) = target!;
        }

        public static Character? GetTargetCreature(MonsterAI ai)
        {
            if (ai == null || s_targetCreature == null) return null;
            return s_targetCreature(ai);
        }

        public static float GetTimeSinceHurt(BaseAI ai)
        {
            if (ai == null || s_timeSinceHurt == null) return 999f;
            return s_timeSinceHurt(ai);
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
