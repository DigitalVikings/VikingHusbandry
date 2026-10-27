using System.Collections.Generic;
using UnityEngine;

namespace VikingHusbandry.Features.Rodeo
{
    public static class DownedRagdollManager
    {
        private class DownedState
        {
            public GameObject RagdollObj = null!;
            public Vector3 OriginalColCenter;
            public float OriginalColHeight;
            public float OriginalColRadius;
            public int OriginalColDirection;
            public RigidbodyConstraints OriginalConstraints;
        }

        private static readonly Dictionary<Character, DownedState> s_activeStates = new Dictionary<Character, DownedState>();

        public static bool HasRagdoll(Character character)
        {
            if (character == null) return false;
            return s_activeStates.TryGetValue(character, out DownedState state) && state.RagdollObj != null;
        }

        public static void CreateDownedRagdoll(Character character)
        {
            if (character == null || !character) return;
            if (HasRagdoll(character)) return;

            GameObject? prefab = FindRagdollPrefab(character);
            if (prefab == null) return;

            // Instantiate ragdoll at character's exact world position and orientation
            GameObject ragdollObj = Object.Instantiate(prefab, character.transform.position, character.transform.rotation);
            ragdollObj.transform.localScale = character.transform.localScale;

            // Zero out all initial velocities so ragdoll collapses downward in place rather than flying away
            foreach (Rigidbody rb in ragdollObj.GetComponentsInChildren<Rigidbody>())
            {
                if (rb != null && !rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // Cancel and destroy native Ragdoll script so it never invokes DestroyNow() and never drops loot
            Ragdoll nativeRagdoll = ragdollObj.GetComponent<Ragdoll>();
            if (nativeRagdoll != null)
            {
                nativeRagdoll.CancelInvoke();
                Object.Destroy(nativeRagdoll);
            }

            // Remove ZNetView so this remains a purely local cosmetic representation
            ZNetView netView = ragdollObj.GetComponent<ZNetView>();
            if (netView != null)
            {
                Object.Destroy(netView);
            }

            // Prevent physics collision between living character's capsule collider and ragdoll bones
            Collider charCol = character.GetCollider();
            if (charCol != null)
            {
                foreach (Collider c in ragdollObj.GetComponentsInChildren<Collider>())
                {
                    if (c != null)
                    {
                        Physics.IgnoreCollision(charCol, c, true);
                    }
                }
            }

            // Save original character collider and constraints state
            CapsuleCollider? col = character.GetCollider();
            Rigidbody? charRb = character.GetComponent<Rigidbody>();
            DownedState state = new DownedState
            {
                RagdollObj = ragdollObj,
                OriginalColCenter = col != null ? col.center : Vector3.zero,
                OriginalColHeight = col != null ? col.height : 0.8f,
                OriginalColRadius = col != null ? col.radius : 0.4f,
                OriginalColDirection = col != null ? col.direction : 1,
                OriginalConstraints = charRb != null ? charRb.constraints : RigidbodyConstraints.None
            };

            // Reshape capsule collider into a low horizontal ground cylinder matching the collapsed animal
            if (col != null)
            {
                col.direction = 2; // Z-axis lengthwise
                col.center = new Vector3(0f, 0.22f, 0f);
                col.height = 1.0f;
                col.radius = 0.35f;
            }

            // Lock character position with constraints rather than kinematic flag to avoid Unity kinematic velocity warnings
            if (charRb != null)
            {
                if (!charRb.isKinematic)
                {
                    charRb.linearVelocity = Vector3.zero;
                    charRb.angularVelocity = Vector3.zero;
                }
                charRb.constraints = RigidbodyConstraints.FreezeAll;
            }

            // Ignore physical collision between local player and downed character so walking near it never nudges it
            if (Player.m_localPlayer != null && Player.m_localPlayer.GetCollider() != null && charCol != null)
            {
                Physics.IgnoreCollision(Player.m_localPlayer.GetCollider(), charCol, true);
            }

            // Match star/level visual tints if LevelEffects is present
            LevelEffects charLevels = character.GetComponentInChildren<LevelEffects>();
            LevelEffects ragdollLevels = ragdollObj.GetComponentInChildren<LevelEffects>();
            if (charLevels != null && ragdollLevels != null)
            {
                charLevels.GetColorChanges(out float hue, out float sat, out float val);
                if (ragdollLevels.m_mainRender != null && ragdollLevels.m_mainRender.material != null)
                {
                    ragdollLevels.m_mainRender.material.SetFloat("_Hue", hue);
                    ragdollLevels.m_mainRender.material.SetFloat("_Saturation", sat);
                    ragdollLevels.m_mainRender.material.SetFloat("_Value", val);
                }
            }

            // Hide living character's standing animated visual armature
            Transform visual = character.transform.Find("Visual");
            if (visual != null)
            {
                visual.gameObject.SetActive(false);
            }

            // Attach controller to sync character position to ragdoll pelvis every frame
            DownedRagdollController ctrl = ragdollObj.AddComponent<DownedRagdollController>();
            ctrl.Setup(character);

            s_activeStates[character] = state;
        }

        public static void RemoveDownedRagdoll(Character character)
        {
            if (character == null) return;

            if (s_activeStates.TryGetValue(character, out DownedState state))
            {
                if (state.RagdollObj != null)
                {
                    // Snap character transform to ragdoll settled position so animal stands up where it fell
                    character.transform.position = state.RagdollObj.transform.position;
                    Object.Destroy(state.RagdollObj);
                }

                // Restore original collider dimensions
                CapsuleCollider? col = character.GetCollider();
                if (col != null)
                {
                    col.direction = state.OriginalColDirection;
                    col.center = state.OriginalColCenter;
                    col.height = state.OriginalColHeight;
                    col.radius = state.OriginalColRadius;
                }

                // Restore original constraints
                Rigidbody? charRb = character.GetComponent<Rigidbody>();
                if (charRb != null)
                {
                    charRb.constraints = state.OriginalConstraints;
                }

                // Restore collision with local player
                Collider? charCol = character.GetCollider();
                if (Player.m_localPlayer != null && Player.m_localPlayer.GetCollider() != null && charCol != null)
                {
                    Physics.IgnoreCollision(Player.m_localPlayer.GetCollider(), charCol, false);
                }

                s_activeStates.Remove(character);
            }

            // Restore character visual armature
            if (character)
            {
                Transform visual = character.transform.Find("Visual");
                if (visual != null)
                {
                    visual.gameObject.SetActive(true);
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
                GameObject p = ZNetScene.instance.GetPrefab(cleanName + "_ragdoll");
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
