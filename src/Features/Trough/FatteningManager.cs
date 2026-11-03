using System.Collections.Generic;
using UnityEngine;
using VikingHusbandry.Configuration;

namespace VikingHusbandry.Features.Trough
{
    public static class FatteningManager
    {
        public static readonly int FeedCountHash = "VH_FeedCount".GetStableHashCode();

        public static void RecordTroughFeed(Character character)
        {
            if (character == null || !ModConfig.ModEnabled.Value || !ModConfig.EnableFattening.Value) return;

            ZNetView nview = character.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            int current = nview.GetZDO().GetInt(FeedCountHash, 0);
            nview.GetZDO().Set(FeedCountHash, current + 1);
        }

        public static int GetFeedCount(Character character)
        {
            if (character == null) return 0;

            ZNetView nview = character.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return 0;

            return nview.GetZDO().GetInt(FeedCountHash, 0);
        }

        public static float GetDropMultiplier(Character character)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableFattening.Value) return 1.0f;

            int feedCount = GetFeedCount(character);
            if (feedCount <= 0) return 1.0f;

            float maxBonus = Mathf.Max(0f, ModConfig.MaxFattenBonus.Value);
            float halfLife = Mathf.Max(1f, ModConfig.FatteningHalfLife.Value);

            // Asymptotic curve: bonus = MaxBonus * (1 - e^(-feedCount / halfLife))
            float bonus = maxBonus * (1.0f - Mathf.Exp(-feedCount / halfLife));
            return 1.0f + bonus;
        }

        public static void ApplyDropBonus(CharacterDrop dropTable, float multiplier, ref List<KeyValuePair<GameObject, int>> result)
        {
            if (result == null || multiplier <= 1.001f) return;

            // 1. Scale existing generated drops (guaranteed drops like Raw Meat, Pelts, etc.)
            List<KeyValuePair<GameObject, int>> scaledResult = new();
            HashSet<GameObject> droppedPrefabs = new();

            for (int i = 0; i < result.Count; i++)
            {
                var kvp = result[i];
                GameObject prefab = kvp.Key;
                int originalCount = kvp.Value;

                if (prefab != null)
                {
                    droppedPrefabs.Add(prefab);
                }

                if (prefab == null) continue;

                if (originalCount > 0)
                {
                    float raw = originalCount * multiplier;
                    int baseCount = Mathf.FloorToInt(raw);
                    float remainder = raw - baseCount;

                    if (remainder > 0.001f && Random.value < remainder)
                    {
                        baseCount++;
                    }

                    scaledResult.Add(new KeyValuePair<GameObject, int>(prefab, Mathf.Max(1, baseCount)));
                }
                else
                {
                    scaledResult.Add(kvp);
                }
            }

            // 2. Increase drop chance for rare drops (like Trophies with chance < 1.0f) that did not drop
            if (dropTable != null && dropTable.m_drops != null)
            {
                Character dropChar = dropTable.GetComponent<Character>();

                foreach (CharacterDrop.Drop drop in dropTable.m_drops)
                {
                    if (drop.m_prefab == null || drop.m_chance >= 1.0f) continue;

                    // If vanilla didn't roll this drop, roll extra chance granted by fattening
                    if (!droppedPrefabs.Contains(drop.m_prefab))
                    {
                        float baseChance = Mathf.Clamp01(drop.m_chance);
                        float boostedChance = Mathf.Clamp01(baseChance * multiplier);
                        float extraChance = (boostedChance - baseChance) / Mathf.Max(0.001f, 1.0f - baseChance);

                        if (Random.value < extraChance)
                        {
                            int count = Random.Range(drop.m_amountMin, drop.m_amountMax + 1);
                            if (drop.m_levelMultiplier && dropChar != null)
                            {
                                int levelFactor = Mathf.Max(1, (int)Mathf.Pow(2f, dropChar.GetLevel() - 1));
                                count *= levelFactor;
                            }

                            if (count > 0)
                            {
                                scaledResult.Add(new KeyValuePair<GameObject, int>(drop.m_prefab, count));
                                droppedPrefabs.Add(drop.m_prefab);
                            }
                        }
                    }
                }
            }

            result = scaledResult;
        }

        public static string? GetFattenStatusText(Character character)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableFattening.Value || character == null) return null;

            int feedCount = GetFeedCount(character);
            if (feedCount <= 0) return null;

            float multiplier = GetDropMultiplier(character);
            float bonusPercent = (multiplier - 1.0f) * 100f;

            if (bonusPercent < 2.0f) return null;

            int pct = Mathf.RoundToInt(bonusPercent);
            return $"<color=#e6c66e>[Fattened: +{pct}% yield ({feedCount} fed)]</color>";
        }
    }
}
