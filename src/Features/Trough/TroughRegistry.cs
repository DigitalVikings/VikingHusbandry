using System.Collections.Generic;
using UnityEngine;
using VikingHusbandry.Configuration;

namespace VikingHusbandry.Features.Trough
{
    public static class TroughRegistry
    {
        private static readonly int s_isTroughHash = "VH_IsTrough".GetStableHashCode();
        private static readonly List<Container> s_activeTroughs = new();

        public static bool IsValidChest(Container container)
        {
            if (container == null) return false;
            if (container.m_wagon != null) return false;

            // Must be a player-buildable piece (excludes tombstones, drops, companions)
            if (container.GetComponentInParent<Piece>() == null) return false;

            // Exclude creatures and incinerator
            if (container.GetComponentInParent<Character>() != null) return false;
            if (container.GetComponentInParent<Incinerator>() != null) return false;

            // Exclude ship holds
            if (container.GetComponentInParent<Ship>() != null) return false;

            // Exclude barrels, carts, coffins
            string name = container.gameObject.name.ToLower();
            if (name.Contains("barrel") || name.Contains("wagon") || name.Contains("cart") || name.Contains("ship") || name.Contains("karve"))
            {
                return false;
            }

            // Must be a chest piece
            return name.Contains("chest");
        }

        public static void Register(Container container)
        {
            if (container != null && IsValidChest(container) && !s_activeTroughs.Contains(container))
            {
                s_activeTroughs.Add(container);
            }
        }

        public static void Unregister(Container container)
        {
            if (container != null)
            {
                s_activeTroughs.Remove(container);
            }
        }

        public static bool IsTrough(Container container)
        {
            if (container == null) return false;

            ZNetView nview = container.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            return nview.GetZDO().GetInt(s_isTroughHash, 0) == 1;
        }

        public static void ToggleTrough(Container container, Player? player)
        {
            if (container == null) return;

            ZNetView nview = container.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            bool currentlyTrough = IsTrough(container);
            int nextState = currentlyTrough ? 0 : 1;
            nview.GetZDO().Set(s_isTroughHash, nextState);
            TroughVisualizer.Get(container)?.SetVisualState(nextState == 1);

            if (player != null)
            {
                string msg = nextState == 1 ? "Feeding Trough" : "Standard Chest";
                player.Message(MessageHud.MessageType.TopLeft, msg);
            }
        }

        public static int CountAcceptableFood(Container container, List<ItemDrop>? consumeItems)
        {
            if (container == null || container.GetInventory() == null) return 0;

            int total = 0;
            List<ItemDrop.ItemData> items = container.GetInventory().GetAllItems();

            foreach (var item in items)
            {
                if (item == null) continue;
                if (consumeItems == null || consumeItems.Count == 0)
                {
                    total += item.m_stack;
                    continue;
                }

                foreach (var consumable in consumeItems)
                {
                    if (consumable != null && consumable.m_itemData != null &&
                        consumable.m_itemData.m_shared.m_name == item.m_shared.m_name)
                    {
                        total += item.m_stack;
                        break;
                    }
                }
            }

            return total;
        }

        public static Container? FindClosestTroughWithFood(Vector3 position, List<ItemDrop> consumeItems, float maxRadius)
        {
            Container? closest = null;
            float closestDist = maxRadius;

            // Clean destroyed references
            s_activeTroughs.RemoveAll(c => c == null || c.gameObject == null);

            foreach (Container container in s_activeTroughs)
            {
                if (container == null || !IsTrough(container)) continue;

                float dist = Vector3.Distance(position, container.transform.position);
                if (dist > maxRadius || dist >= closestDist) continue;

                if (CountAcceptableFood(container, consumeItems) > 0)
                {
                    closest = container;
                    closestDist = dist;
                }
            }

            return closest;
        }

        public static ItemDrop.ItemData? ConsumeOneFromTrough(Container container, List<ItemDrop> consumeItems)
        {
            if (container == null || container.GetInventory() == null) return null;

            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
            {
                if (item == null) continue;

                foreach (ItemDrop consumable in consumeItems)
                {
                    if (consumable != null && consumable.m_itemData != null &&
                        consumable.m_itemData.m_shared.m_name == item.m_shared.m_name)
                    {
                        container.GetInventory().RemoveOneItem(item);
                        return item;
                    }
                }
            }

            return null;
        }
    }
}
