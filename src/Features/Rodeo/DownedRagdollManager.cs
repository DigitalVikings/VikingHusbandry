using System.Collections.Generic;
using UnityEngine;

namespace VikingHusbandry.Features.Rodeo
{
    public static class DownedRagdollManager
    {
        private class DownedState
        {
            public GameObject? RagdollObj { get; set; }
        }

        private static readonly Dictionary<Character, DownedState> s_activeStates = new Dictionary<Character, DownedState>();

        public static bool HasRagdoll(Character character)
        {
            if (character == null) return false;
            return s_activeStates.TryGetValue(character, out DownedState state) && state.RagdollObj != null;
        }

        public static void CreateDownedRagdoll(Character character)
        {
            // Downed state is rendered natively on the living character using Valheim's synchronized
            // sleep animation pose (ZDOVars.s_sleeping).
            // Do NOT instantiate a duplicate ragdoll prefab:
            // 1) Prevents duplicate overlapping meshes (living character + ragdoll).
            // 2) Prevents conflicts with loot mods (like InstantMonsterLootDrop) that hook Ragdoll.Awake.
            // 3) Prevents persistent ZDO leaks in ZDOMan from prefab instantiation.
            if (character == null || !character) return;

            // Ensure character's native visual is active and all renderers are enabled
            GameObject? vis = character.GetVisual();
            if (vis != null && !vis.activeSelf)
            {
                vis.SetActive(true);
            }
            Transform visual = character.transform.Find("Visual");
            if (visual != null && !visual.gameObject.activeSelf)
            {
                visual.gameObject.SetActive(true);
            }

            // Ensure sleeping animation parameter is set on living character
            ZSyncAnimation zanim = character.GetZAnim();
            if (zanim != null)
            {
                zanim.SetBool("sleeping", true);
            }
        }

        public static void RemoveDownedRagdoll(Character character)
        {
            if (character == null) return;

            if (s_activeStates.TryGetValue(character, out DownedState state))
            {
                if (state.RagdollObj != null)
                {
                    character.transform.position = state.RagdollObj.transform.position;
                    Object.Destroy(state.RagdollObj);
                }

                s_activeStates.Remove(character);
            }

            // Restore character visual armature and wake up from sleeping pose
            if (character)
            {
                GameObject? vis = character.GetVisual();
                if (vis != null && !vis.activeSelf) vis.SetActive(true);

                Transform visual = character.transform.Find("Visual");
                if (visual != null && !visual.gameObject.activeSelf)
                {
                    visual.gameObject.SetActive(true);
                }

                foreach (Renderer r in character.GetComponentsInChildren<Renderer>(true))
                {
                    if (r != null) r.enabled = true;
                }

                ZSyncAnimation zanim = character.GetZAnim();
                if (zanim != null)
                {
                    zanim.SetBool("sleeping", false);
                }
            }
        }

        private static GameObject? FindRagdollPrefab(Character character)
        {
            if (character.m_deathEffects != null && character.m_deathEffects.m_effectPrefabs != null)
            {
                foreach (var effect in character.m_deathEffects.m_effectPrefabs)
                {
                    if (effect.m_prefab != null && effect.m_prefab.GetComponent<Ragdoll>() != null)
                    {
                        return effect.m_prefab;
                    }
                }
            }

            string cleanName = character.name.Replace("(Clone)", "").Trim();
            if (ZNetScene.instance != null)
            {
                // Try lower case suffix (e.g. boar_ragdoll)
                GameObject p = ZNetScene.instance.GetPrefab(cleanName + "_ragdoll");
                if (p != null) return p;

                // Try title case suffix (e.g. Wolf_Ragdoll, Moose_Ragdoll, Ulv_Ragdoll)
                p = ZNetScene.instance.GetPrefab(cleanName + "_Ragdoll");
                if (p != null) return p;

                // Try prefix convention (e.g. Ragdoll_Asksvin)
                p = ZNetScene.instance.GetPrefab("Ragdoll_" + cleanName);
                if (p != null) return p;

                p = ZNetScene.instance.GetPrefab("ragdoll_" + cleanName);
                if (p != null) return p;
            }

            return null;
        }
    }

    public class DownedRagdollController : MonoBehaviour
    {
        private Character? _character;
        private Transform? _pelvis;
        private Rigidbody[]? _bodies;
        private float _timer;
        private bool _frozen;

        public void Setup(Character character)
        {
            _character = character;
            _pelvis = Utils.FindChild(transform, "Pelvis");
            _bodies = GetComponentsInChildren<Rigidbody>();
        }

        private void Update()
        {
            // Continuously lock the living character's transform to the ragdoll pelvis position
            // This guarantees the healthbar, hover text, and interactable collider remain 100% unified with the visible body
            if (_character != null && _character)
            {
                Vector3 pos = _pelvis != null ? _pelvis.position : transform.position;
                _character.transform.position = pos;
            }

            if (_frozen) return;
            _timer += Time.deltaTime;

            // After 0.8s of physics tumble, freeze all bone rigidbodies kinematically so it rests stably
            if (_timer >= 0.8f)
            {
                _frozen = true;
                if (_bodies != null)
                {
                    foreach (Rigidbody rb in _bodies)
                    {
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                        }
                    }
                }
            }
        }
    }
}
