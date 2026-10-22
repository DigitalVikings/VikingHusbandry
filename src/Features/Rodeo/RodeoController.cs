using UnityEngine;
using VikingHusbandry.Configuration;

namespace VikingHusbandry.Features.Rodeo
{
    public class RodeoController : MonoBehaviour
    {
        public static RodeoController? Instance { get; private set; }

        private Player? _player;
        private Character? _beast;
        private MonsterAI? _beastAI;
        private GameObject? _tempMountPoint;
        private float _rodeoTimer;
        private float _thrashTimer;
        private float _lastProgressUpdateTimer;
        private int _lastPercentShown = -1;

        public bool IsRodeoActive => _player != null && _beast != null;

        private void Awake()
        {
            Instance = this;
        }

        public static void StartRodeo(Player player, Character beast)
        {
            if (player == null || beast == null) return;
            if (Instance == null)
            {
                GameObject host = new GameObject("VH_RodeoHost");
                DontDestroyOnLoad(host);
                host.AddComponent<RodeoController>();
            }

            Instance!.Begin(player, beast);
        }

        public void Begin(Player player, Character beast)
        {
            _player = player;
            _beast = beast;
            _beastAI = beast.GetComponent<MonsterAI>();
            _rodeoTimer = 0f;
            _thrashTimer = 0f;
            _lastProgressUpdateTimer = 0f;
            _lastPercentShown = -1;

            // Clean any previous mount point
            if (_tempMountPoint != null)
            {
                Destroy(_tempMountPoint);
            }

            // Create an elevated mount point seated on the beast's back
            _tempMountPoint = new GameObject("VH_MountPoint");
            _tempMountPoint.transform.SetParent(beast.transform);

            float mountHeight = 0.7f;
            CapsuleCollider col = beast.GetCollider();
            if (col != null)
            {
                mountHeight = Mathf.Max(0.55f, col.height * 0.65f);
            }
            _tempMountPoint.transform.localPosition = new Vector3(0f, mountHeight, -0.1f);
            _tempMountPoint.transform.localRotation = Quaternion.identity;

            Vector3 detachOffset = new Vector3(0f, 0.5f, -1.5f);

            // Attach player using native Valheim seated saddle animation ("attach_chair")
            player.AttachStart(
                _tempMountPoint.transform,
                beast.gameObject,
                hideWeapons: true,
                isBed: false,
                onShip: false,
                attachAnimation: "attach_chair",
                detachOffset: detachOffset,
                cameraPos: null
            );
        }

        private void Update()
        {
            if (!IsRodeoActive || _player == null || _beast == null) return;

            // Check if player or beast died
            if (_player.IsDead() || _beast.IsDead())
            {
                EndRodeo(bucked: false);
                return;
            }

            float dt = Time.deltaTime;
            _rodeoTimer += dt;

            // Drain player stamina
            float drain = ModConfig.RodeoStaminaDrain.Value * dt;
            _player.UseStamina(drain);

            // Accumulate subdual progress
            float progress = DownedTamingManager.GetSubdualProgress(_beast);
            progress += dt / Mathf.Max(1f, ModConfig.RodeoDuration.Value);
            DownedTamingManager.SetSubdualProgress(_beast, progress);

            // Update subdual progress message at clean intervals (every 0.5s or on 10% steps) to prevent screen flicker
            _lastProgressUpdateTimer += dt;
            int currentPercent = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100);
            if (_lastProgressUpdateTimer >= 0.5f || (currentPercent % 10 == 0 && currentPercent != _lastPercentShown))
            {
                _lastProgressUpdateTimer = 0f;
                _lastPercentShown = currentPercent;
                _player.Message(MessageHud.MessageType.TopLeft, $"Subduing {currentPercent}% (Space: Jump off)");
            }

            // Make beast thrash erratically
            _thrashTimer += dt;
            if (_beastAI != null)
            {
                Vector3 moveTarget = _beast.transform.position + (_beast.transform.forward * 3f);
                AIHelper.MoveTo(_beastAI, dt, moveTarget, 1f, true);

                if (_thrashTimer >= 2.5f)
                {
                    _thrashTimer = 0f;
                    ZSyncAnimation zanim = _beast.GetZAnim();
                    if (zanim != null)
                    {
                        zanim.SetTrigger("attack");
                    }
                }
            }

            // Exit Check 1: Beast successfully subdued!
            if (progress >= 1.0f)
            {
                // Send notification BEFORE resetting _player in EndRodeo to prevent NullReferenceException
                _player.Message(MessageHud.MessageType.TopLeft, "Exhausted! Feed to tame.");
                DownedTamingManager.SetSubdualProgress(_beast, 1.0f);
                DownedTamingManager.SetDowned(_beast, ModConfig.DownedDuration.Value);
                EndRodeo(bucked: false);
                return;
            }

            // Exit Check 2: Player stamina exhausted or Jump key (Spacebar) pressed to voluntarily dismount
            // Require 0.5s grace period so initiating interaction doesn't bleed into release check
            if (_rodeoTimer > 0.5f)
            {
                if (_player.GetStamina() <= 1.0f || ZInput.GetButtonDown("Jump"))
                {
                    EndRodeo(bucked: true);
                    return;
                }
            }
        }

        public void EndRodeo(bool bucked)
        {
            if (_player != null)
            {
                _player.AttachStop();

                if (bucked)
                {
                    HitData hit = new HitData
                    {
                        m_damage = new HitData.DamageTypes { m_blunt = ModConfig.BuckDamage.Value },
                        m_point = _player.transform.position,
                        m_dir = -_player.transform.forward
                    };
                    _player.Damage(hit);
                    _player.Message(MessageHud.MessageType.TopLeft, "Bucked off!");
                }
            }

            if (_tempMountPoint != null)
            {
                Destroy(_tempMountPoint);
                _tempMountPoint = null;
            }

            _player = null;
            _beast = null;
            _beastAI = null;
            _rodeoTimer = 0f;
            _thrashTimer = 0f;
            _lastProgressUpdateTimer = 0f;
            _lastPercentShown = -1;
        }

        private void OnDestroy()
        {
            if (IsRodeoActive)
            {
                EndRodeo(bucked: false);
            }
        }
    }
}
