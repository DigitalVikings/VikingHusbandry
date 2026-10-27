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
        private GameObject? _lineObj;
        private bool _isTetherMode;
        private float _rodeoTimer;
        private float _thrashTimer;
        private float _lastProgressUpdateTimer;
        private int _lastPercentShown = -1;
        private Vector3 _baseLocalPos;
        private float _basePitch;
        private float _buckImpulse;

        public bool IsRodeoActive => _player != null && _beast != null;
        public Player? ActivePlayer => _player;

        public static bool IsBeastInTether(Character character)
        {
            return Instance != null && Instance.IsRodeoActive && Instance._isTetherMode && Instance._beast == character;
        }

        public static bool IsBeastMounted(Character character)
        {
            return Instance != null && Instance.IsRodeoActive && !Instance._isTetherMode && Instance._beast == character;
        }

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
            _buckImpulse = 0f;

            // Clean any previous artifacts
            if (_tempMountPoint != null)
            {
                Destroy(_tempMountPoint);
                _tempMountPoint = null;
            }
            if (_lineObj != null)
            {
                if (ZNetScene.instance != null) ZNetScene.instance.Destroy(_lineObj);
                else Destroy(_lineObj);
                _lineObj = null;
            }

            bool isBoar = beast.name.StartsWith("Boar") || (beast.m_name != null && beast.m_name.ToLower().Contains("boar"));
            _isTetherMode = isBoar;

            if (_isTetherMode)
            {
                // Tether Leash Subdual: player stays on foot, attaching harpoon line to the fleeing wild beast
                player.HideHandItems(true, false);

                // Beast enters fully alerted panic state
                if (_beastAI != null)
                {
                    _beastAI.Alert();
                }

                GameObject? harpoonPrefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("vfx_Harpooned") : null;
                if (harpoonPrefab != null)
                {
                    _lineObj = Object.Instantiate(harpoonPrefab, beast.transform.position, Quaternion.identity);

                    // Strictly enforce 2 positions (index 0 = start/beast, index 1 = end/player)
                    // This eliminates extra vertices from the prefab that shoot off sharply into the distance
                    foreach (LineRenderer lr in _lineObj.GetComponentsInChildren<LineRenderer>())
                    {
                        if (lr != null)
                        {
                            lr.positionCount = 2;
                        }
                    }

                    LineConnect line = _lineObj.GetComponent<LineConnect>();
                    if (line != null)
                    {
                        line.SetPeer(player.GetComponent<ZNetView>());
                        line.m_dynamicSlack = false;
                        line.SetSlack(0.01f);
                        line.m_minThickness = 0.04f;
                        line.m_maxThickness = 0.05f;
                    }
                }

                player.Message(MessageHud.MessageType.TopLeft, "Leashed! Hold the line...");
            }
            else
            {
                // Elevated saddle mount on back of large rideable beast (e.g. Lox)
                _tempMountPoint = new GameObject("VH_MountPoint");
                _tempMountPoint.transform.SetParent(beast.transform);

                string attachAnim = beast.name.Contains("Lox") ? "attach_lox" : (beast.name.Contains("Asksvin") ? "attach_asksvin" : "attach_chair");
                _basePitch = 0f;

                float mountHeight = 0.7f;
                float mountZ = -0.1f;
                CapsuleCollider col = beast.GetCollider();
                if (col != null)
                {
                    mountHeight = Mathf.Max(0.55f, col.height * 0.65f);
                    mountZ = -0.15f;
                }

                _baseLocalPos = new Vector3(0f, mountHeight, mountZ);
                _tempMountPoint.transform.localPosition = _baseLocalPos;
                _tempMountPoint.transform.localRotation = Quaternion.identity;
                Vector3 detachOffset = new Vector3(0f, 0.5f, -1.5f);
                player.Message(MessageHud.MessageType.TopLeft, "Mounted! Hold on...");

                player.AttachStart(
                    _tempMountPoint.transform,
                    beast.gameObject,
                    hideWeapons: true,
                    isBed: false,
                    onShip: false,
                    attachAnimation: attachAnim,
                    detachOffset: detachOffset,
                    cameraPos: null
                );
            }
        }

        private void Update()
        {
            if (!IsRodeoActive || _player == null || _beast == null) return;

            // Check if player or beast died
            if (_player.IsDead() || _beast.IsDead())
            {
                EndRodeo(bucked: false, released: false);
                return;
            }

            float dt = Time.deltaTime;
            _rodeoTimer += dt;

            // Accumulate subdual progress
            float progress = DownedTamingManager.GetSubdualProgress(_beast);
            progress += dt / Mathf.Max(1f, ModConfig.RodeoDuration.Value);
            DownedTamingManager.SetSubdualProgress(_beast, progress);

            // Update subdual progress message at clean intervals
            _lastProgressUpdateTimer += dt;
            int currentPercent = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100);
            if (_lastProgressUpdateTimer >= 0.5f || (currentPercent % 10 == 0 && currentPercent != _lastPercentShown))
            {
                _lastProgressUpdateTimer = 0f;
                _lastPercentShown = currentPercent;
                _player.Message(MessageHud.MessageType.TopLeft, $"Subduing {currentPercent}% (Space: Release)");
            }

            if (_isTetherMode)
            {
                // Update harpoon line anchor to beast center and ensure strictly 2 vertices
                if (_lineObj != null)
                {
                    _lineObj.transform.position = _beast.GetCenterPoint();

                    foreach (LineRenderer lr in _lineObj.GetComponentsInChildren<LineRenderer>())
                    {
                        if (lr != null && lr.positionCount != 2)
                        {
                            lr.positionCount = 2;
                        }
                    }

                    LineConnect line = _lineObj.GetComponent<LineConnect>();
                    if (line != null)
                    {
                        line.m_dynamicSlack = false;
                        line.SetSlack(0.01f);
                    }
                }

                float dist = Vector3.Distance(_player.transform.position, _beast.transform.position);
                const float restLength = 3.6f;
                const float maxTether = 5.0f;

                Vector3 toPlayer = _player.transform.position - _beast.transform.position;
                toPlayer.y = 0f;
                float horizDist = toPlayer.magnitude;
                if (horizDist < 0.1f) toPlayer = -_beast.transform.forward;
                else toPlayer /= horizDist;

                Vector3 toBeast = -toPlayer;

                // Check if the player is actively moving away from the beast to drag the tether back
                Vector3 playerMoveDir = _player.GetMoveDir();
                float playerPullBack = 0f;
                if (playerMoveDir.sqrMagnitude > 0.01f)
                {
                    playerPullBack = Mathf.Clamp01(Vector3.Dot(playerMoveDir.normalized, toPlayer));
                }

                // Drive panicked thrashing run: player's counter-pull actively slows the beast down
                if (_beastAI != null)
                {
                    float veerAngle = Mathf.Sin(_rodeoTimer * 3.0f) * 35f;
                    Vector3 runDir = Quaternion.Euler(0f, veerAngle, 0f) * toBeast;

                    // When player pulls back, shorten target step and drag speed
                    float stepDist = Mathf.Lerp(4.5f, 2.0f, playerPullBack);
                    Vector3 targetPoint = _beast.transform.position + (runDir * stepDist);

                    _beastAI.StopMoving();
                    AIHelper.MoveTo(_beastAI, dt, targetPoint, 0.5f, true);
                    _beast.SetRun(true);
                }

                // When line pulls taut beyond rest length, apply bidirectional physical tug-of-war
                if (dist > restLength)
                {
                    float stretch = Mathf.Clamp01((dist - restLength) / (maxTether - restLength));

                    // 1. Beast drags player forward (stronger, but resisted by player stance)
                    float beastPullForce = Mathf.Lerp(7f, 14f, stretch);
                    Rigidbody playerRb = _player.GetComponent<Rigidbody>();
                    if (playerRb != null && !playerRb.isKinematic)
                    {
                        playerRb.AddForce(toBeast * beastPullForce, ForceMode.Acceleration);
                    }

                    // 2. Player drags tether back (less force than animal, but actively slows and tugs beast)
                    // Scales up significantly when player walks backwards away from the beast
                    float playerCounterForce = Mathf.Lerp(4f, 9.5f, playerPullBack) * Mathf.Max(0.4f, stretch);
                    Rigidbody beastRb = _beast.GetComponent<Rigidbody>();
                    if (beastRb != null && !beastRb.isKinematic)
                    {
                        beastRb.AddForce(toPlayer * playerCounterForce, ForceMode.Acceleration);
                    }

                    // Soft position clamp only at max tether extension so beast cannot escape
                    if (dist > maxTether)
                    {
                        Vector3 clampTarget = _beast.transform.position + (toPlayer * maxTether);
                        _player.transform.position = Vector3.Lerp(_player.transform.position, clampTarget, dt * 8f);
                    }

                    // Stamina drains continuously; pulling back actively increases stamina exertion (+40%)
                    float drainRate = ModConfig.RodeoStaminaDrain.Value * (1f + playerPullBack * 0.4f);
                    _player.UseStamina(drainRate * dt);
                }

                // Check 1: Beast successfully subdued!
                if (progress >= 1.0f)
                {
                    _player.Message(MessageHud.MessageType.TopLeft, "Exhausted! Feed to tame.");
                    DownedTamingManager.SetSubdualProgress(_beast, 1.0f);
                    DownedTamingManager.SetDowned(_beast, ModConfig.DownedDuration.Value);
                    EndRodeo(bucked: false, released: false);
                    return;
                }

                // Check 2: Player stamina depleted -> Line snaps!
                if (_rodeoTimer > 0.5f)
                {
                    if (_player.GetStamina() <= 1.0f)
                    {
                        EndRodeo(bucked: true, released: false);
                        return;
                    }

                    if (ZInput.GetButtonDown("Jump"))
                    {
                        EndRodeo(bucked: false, released: true);
                        return;
                    }
                }
            }
            else
            {
                // Mount rodeo for large beasts
                float drain = ModConfig.RodeoStaminaDrain.Value * dt;
                _player.UseStamina(drain);

                _buckImpulse = Mathf.Lerp(_buckImpulse, 0f, dt * 5f);
                float thrashPitch = (Mathf.Sin(_rodeoTimer * 12f) * 4f) + _buckImpulse;
                float thrashRoll = Mathf.Cos(_rodeoTimer * 10f) * 6f;
                float thrashYaw = Mathf.Sin(_rodeoTimer * 8f) * 4f;

                if (_tempMountPoint != null)
                {
                    _tempMountPoint.transform.localRotation = Quaternion.Euler(_basePitch - thrashPitch, thrashYaw, thrashRoll);
                    _tempMountPoint.transform.localPosition = _baseLocalPos + new Vector3(
                        Mathf.Sin(_rodeoTimer * 10f) * 0.015f,
                        Mathf.Abs(Mathf.Sin(_rodeoTimer * 16f)) * 0.02f,
                        -(_buckImpulse / 25f) * 0.04f
                    );
                }

                _thrashTimer += dt;
                if (_beastAI != null)
                {
                    float weaveAngle = Mathf.Sin(_rodeoTimer * 3.5f) * 35f;
                    Vector3 weaveDir = Quaternion.Euler(0f, weaveAngle, 0f) * _beast.transform.forward;
                    Vector3 moveTarget = _beast.transform.position + (weaveDir * 4f);
                    AIHelper.MoveTo(_beastAI, dt, moveTarget, 1f, true);

                    if (_thrashTimer >= 2.2f)
                    {
                        _thrashTimer = 0f;
                        _buckImpulse = 24f;
                        ZSyncAnimation zanim = _beast.GetZAnim();
                        if (zanim != null)
                        {
                            zanim.SetTrigger("attack");
                        }
                    }
                }

                if (progress >= 1.0f)
                {
                    _player.Message(MessageHud.MessageType.TopLeft, "Subdued! Feed to tame.");
                    DownedTamingManager.SetSubdualProgress(_beast, 1.0f);
                    DownedTamingManager.SetDowned(_beast, ModConfig.DownedDuration.Value);
                    EndRodeo(bucked: false, released: false);
                    return;
                }

                if (_rodeoTimer > 0.5f)
                {
                    if (_player.GetStamina() <= 1.0f || ZInput.GetButtonDown("Jump"))
                    {
                        EndRodeo(bucked: true, released: false);
                        return;
                    }
                }
            }
        }

        public void EndRodeo(bool bucked, bool released)
        {
            if (_player != null)
            {
                if (!_isTetherMode)
                {
                    _player.AttachStop();
                }

                if (bucked)
                {
                    if (!_isTetherMode)
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
                    else
                    {
                        _player.Message(MessageHud.MessageType.TopLeft, "Line snapped! The beast broke free.");
                    }
                }
                else if (released)
                {
                    _player.Message(MessageHud.MessageType.TopLeft, "Line released.");
                }
            }

            if (_lineObj != null)
            {
                if (ZNetScene.instance != null) ZNetScene.instance.Destroy(_lineObj);
                else Destroy(_lineObj);
                _lineObj = null;
            }

            if (_tempMountPoint != null)
            {
                Destroy(_tempMountPoint);
                _tempMountPoint = null;
            }

            _player = null;
            _beast = null;
            _beastAI = null;
            _isTetherMode = false;
            _rodeoTimer = 0f;
            _thrashTimer = 0f;
            _lastProgressUpdateTimer = 0f;
            _lastPercentShown = -1;
            _baseLocalPos = Vector3.zero;
            _basePitch = 0f;
            _buckImpulse = 0f;
        }

        private void OnDestroy()
        {
            if (IsRodeoActive)
            {
                EndRodeo(bucked: false, released: false);
            }
        }
    }
}
