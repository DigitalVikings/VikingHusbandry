using System.Collections.Generic;
using UnityEngine;
using VikingHusbandry.Configuration;

namespace VikingHusbandry.Features.Trough
{
    public static class PaddockDensityEnforcer
    {
        public static int GetNrOfInstancesHorizontal(
            GameObject prefab,
            Vector3 center,
            float maxRange,
            bool procreationOnly)
        {
            if (prefab == null) return 0;

            string cloneName = prefab.name + "(Clone)";
            int count = 0;
            float maxRangeSqr = maxRange * maxRange;

            List<BaseAI> instances = BaseAI.BaseAIInstances;
            if (instances != null)
            {
                foreach (BaseAI baseAI in instances)
                {
                    if (baseAI == null || baseAI.gameObject == null) continue;
                    if (baseAI.gameObject.name != cloneName) continue;

                    if (maxRange > 0f)
                    {
                        Vector3 pos = baseAI.transform.position;
                        float dx = pos.x - center.x;
                        float dz = pos.z - center.z;
                        float distH2 = (dx * dx) + (dz * dz);

                        // Horizontal check: ignores vertical elevation delta (dy)
                        if (distH2 > maxRangeSqr) continue;
                    }

                    if (procreationOnly)
                    {
                        Procreation procreation = baseAI.GetComponent<Procreation>();
                        if (procreation == null || !procreation.ReadyForProcreation())
                        {
                            continue;
                        }
                    }

                    count++;
                }
            }

            return count;
        }

        public static bool IsPaddockOvercrowded(
            GameObject myPrefab,
            GameObject? offspringPrefab,
            Vector3 position,
            float range,
            int maxCreatures)
        {
            if (myPrefab == null) return false;

            float horizontalRadius = ModConfig.PaddockRadius.Value > 0f
                ? ModConfig.PaddockRadius.Value
                : range;

            int count = GetNrOfInstancesHorizontal(myPrefab, position, horizontalRadius, false);

            if (offspringPrefab != null && offspringPrefab != myPrefab)
            {
                count += GetNrOfInstancesHorizontal(offspringPrefab, position, horizontalRadius, false);
            }

            return count >= maxCreatures;
        }
    }
}
