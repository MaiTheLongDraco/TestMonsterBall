using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Aiara.UI;

namespace Aiara.Combat
{
    public enum MonsterAIState
    {
        Hidden,         // Giai đoạn 1: Ẩn sâu dưới lòng đất, chỉ nhô gai nhọn trắng lên mặt sàn, nứt đất, tắt lửa, ẩn thanh máu
        Emerge,         // Giai đoạn 2: Nhân vật tiến lại gần (<= 5.5m), quái lao vọt lên, nổ tung cột khói đen và đất đá vụn
        LandIgnite,     // Giai đoạn 3: Tiếp đất vững vàng (Y = 0.0m), bùng cháy toàn thân ngọn lửa cam vàng, hiện thanh máu "적구"
        RollAttack,     // Giai đoạn 4: Cuộn tròn lăn tít mù (11.5m/s) về phía JangHyu, vệt khói bụi và tia lửa ma sát kéo dài
        JumpSlam,       // Kỹ năng đặc biệt: Nhảy bổng lên cao (3.5m) kéo vệt khói đen rồi đập xuống nổ bom lửa + khói đen + rung màn hình
        Recover,        // Hồi phục tư thế sau khi đâm trúng và thiêu đốt JangHyu
        CombatIdle,     // Giai đoạn 5: Vòng lặp chiến đấu cận chiến
        Approach,
        KeepDistance,
        Attack1,
        Attack2,
        Damaged,
        Groggy,
        Dead
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class MonsterBallAIController : MonoBehaviour
    {
        [Header("AI Master Switch")]
        [Tooltip("Bật/tắt AI quái vật (Có thể ấn phím F1 hoặc bấm Checkbox AI trên màn hình)")]
        [SerializeField] private bool _aiEnabled = true;

        [Header("Target & Detection")]
        [SerializeField] private Transform _targetPlayer;
        [SerializeField] private float _triggerDistance = 5.5f;
        [SerializeField] private float _detectionRange = 12.0f;
        [SerializeField] private float _meleeAttackRange = 2.2f;
        [SerializeField] private float _keepDistanceThreshold = 1.1f;

        [Header("Movement & Speeds")]
        [SerializeField] private float _walkSpeed = 3.6f;
        [SerializeField] private float _backstepSpeed = 2.2f;
        [SerializeField] private float _rollSpeed = 11.5f;

        [Header("Combat Timers & Cooldowns")]
        [SerializeField] private float _attackCooldown = 1.5f;
        [SerializeField] private float _groggyDuration = 2.0f;

        [Header("Ground & Spawn Settings")]
        [SerializeField] private float _groundY = 0.0f;
        [SerializeField] private float _submergeDepth = 0.0f; // Với anim Burrow trên Group ở Y = 0.0m, thân đỏ chìm hẳn dưới sàn và chỉ nhô chỏm sừng trắng lên ~3-5cm
        [SerializeField] private Vector3 _groundSpawnPos = new Vector3(3.5f, 0.0f, 0f);

        [Header("VFX Prefab References")]
        [SerializeField] private GameObject _flameVfxPrefab;
        [SerializeField] private GameObject _groundCrackPrefab;
        [SerializeField] private GameObject _groundEruptPrefab;
        [SerializeField] private GameObject _rollTrailPrefab;
        [SerializeField] private GameObject _jumpTrailPrefab;
        [SerializeField] private GameObject _bombExplosionPrefab;

        [Header("Sound Effects (SFX)")]
        [SerializeField] private AudioClip _sfxEmerge;
        [SerializeField] private AudioClip _sfxJump;
        [SerializeField] private AudioClip _sfxRoll;
        [SerializeField] private AudioClip _sfxBombExplosion;
        [SerializeField] private AudioClip _sfxHitImpact;

        private Rigidbody _rigidbody;
        private SphereCollider _collider;
        private Animator _animator;
        private MonsterBallHealth _health;
        private AudioSource _audioSource;
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        private Transform _meshVisualRoot;

        private MonsterAIState _currentState = MonsterAIState.Hidden;
        private float _stateTimer = 0f;
        private float _attackCooldownTimer = 0f;
        private float _rollCooldownTimer = 0f;
        private float _jumpSlamCooldownTimer = 1.5f;
        private int _consecutiveHitCount = 0;
        private float _lastHitTime = 0f;
        private bool _hasAttackedInCurrentState = false;
        private float _currentRollDir = -1f;
        private Vector3 _jumpStartPos;
        private Vector3 _jumpTargetPos;
        private string _currentAnimState;
        private JangHyuBurnReceiver _playerBurnReceiver;

        // VFX instances
        private GameObject _bodyFlameInstance;
        private GameObject _groundCrackInstance;
        private GameObject _rollTrailInstance;
        private GameObject _jumpTrailInstance;

        public bool IsAIEnabled
        {
            get => _aiEnabled;
            set => _aiEnabled = value;
        }

        public MonsterAIState CurrentState => _currentState;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();
            _health = GetComponent<MonsterBallHealth>();
            if (_health == null) _health = gameObject.AddComponent<MonsterBallHealth>();

            // Animator BẮT BUỘC phải đặt trên Group (nơi chứa DeformationSystem/Root_M) để các anim clip bind đúng bone
            var group = transform.Find("Group");
            _meshVisualRoot = group != null ? group : transform;
            if (group != null)
            {
                group.gameObject.SetActive(true);
                _animator = group.GetComponent<Animator>();
                var rootAnim = GetComponent<Animator>();
                if (rootAnim != null && _animator != null)
                {
                    rootAnim.enabled = false;
                }
            }
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            // Ưu tiên chọn đúng Blue_Mask_Ball trong Group/Geometry thay vì các mảnh vỡ trong group1
            var mainBody = transform.Find("Group/Geometry/Blue_Mask_Ball");
            if (mainBody != null)
            {
                _skinnedMeshRenderer = mainBody.GetComponent<SkinnedMeshRenderer>();
            }
            if (_skinnedMeshRenderer == null)
            {
                _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            }

            // Ẩn mảnh vỡ group1 khi quái còn sống để không bị rác đỏ lồi trên sàn
            SetFractureChunksActive(false);

            // Đặt scale vừa vặn và cân đối như video gốc
            transform.localScale = Vector3.one * 0.7f;

            SetupPhysics();
            SetupAudio();
            LoadVfxAssets();
            CleanupUnwantedArtifacts();
        }

        private void SetupAudio()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.25f;
            _audioSource.volume = 0.9f;
        }

        private void PlaySfx(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null) return;
            if (_audioSource == null) SetupAudio();
            _audioSource.PlayOneShot(clip, volumeScale);
        }

        private void SetFractureChunksActive(bool active)
        {
            var g1 = transform.Find("group1");
            if (g1 != null)
            {
                g1.gameObject.SetActive(active);
            }
        }

        private void SetupPhysics()
        {
            _rigidbody.mass = 60f;
            _rigidbody.linearDamping = 1.2f;
            _rigidbody.angularDamping = 0.05f;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;

            // Khóa cứng trục Z và Rotation
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionZ |
                                      RigidbodyConstraints.FreezeRotation;

            _collider.radius = 0.8f;
            _collider.center = new Vector3(0f, 0.8f, 0f);
        }

        private void LoadVfxAssets()
        {
#if UNITY_EDITOR
            if (_flameVfxPrefab == null)
            {
                _flameVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/Clean_Flame_VFX.prefab");
            }
            if (_groundCrackPrefab == null)
            {
                _groundCrackPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_GroundCrack_Complete.prefab");
            }
            if (_groundEruptPrefab == null)
            {
                _groundEruptPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_GroundErupt_Complete.prefab");
            }
            if (_rollTrailPrefab == null)
            {
                _rollTrailPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_RollTrail_Complete.prefab");
            }
            if (_jumpTrailPrefab == null)
            {
                _jumpTrailPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_JumpTrail_Complete.prefab");
            }
            if (_bombExplosionPrefab == null)
            {
                _bombExplosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_BombExplosion_Complete.prefab");
            }
            if (_sfxEmerge == null)
                _sfxEmerge = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/3. Resources/Audio/SFX/sfx_monster_emerge.wav");
            if (_sfxJump == null)
                _sfxJump = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/3. Resources/Audio/SFX/sfx_monster_jump.wav");
            if (_sfxRoll == null)
                _sfxRoll = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/3. Resources/Audio/SFX/sfx_monster_roll.wav");
            if (_sfxBombExplosion == null)
                _sfxBombExplosion = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/3. Resources/Audio/SFX/sfx_bomb_explosion.wav");
            if (_sfxHitImpact == null)
                _sfxHitImpact = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/3. Resources/Audio/SFX/sfx_hit_impact.wav");
#endif
        }

        public void CleanupUnwantedArtifacts()
        {
            // Bật lại tất cả SkinnedMeshRenderer của MonsterBall
            var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                smr.enabled = true;
            }

            // Hủy bỏ bất kỳ GameObject nào tên là Ellen, Key hoặc chứa model rác làm con của quái
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                string cn = child.name.ToLower();
                if (cn.Contains("ellen") || cn.Contains("key") || cn.Contains("artifact") || cn.Contains("simple(clone)"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void Start()
        {
            FindPlayerTarget();
            if (_health != null)
            {
                _health.OnDamagedEvent += HandleTakeDamage;
                _health.OnDeathEvent += HandleDeath;
            }

            // Khởi tạo trạng thái ban đầu: Ẩn náu dưới lòng đất
            SpawnAtDefaultPosition();
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamagedEvent -= HandleTakeDamage;
                _health.OnDeathEvent -= HandleDeath;
            }
            if (_groundCrackInstance != null) Destroy(_groundCrackInstance);
        }

        public void FindPlayerTarget()
        {
            if (_targetPlayer == null)
            {
                var p = GameObject.Find("JangHyu_AttackCombo2") ?? GameObject.FindWithTag("Player");
                if (p != null)
                {
                    _targetPlayer = p.transform;
                    _playerBurnReceiver = p.GetComponent<JangHyuBurnReceiver>();
                }
            }
        }

        public void SpawnAtDefaultPosition()
        {
            CleanupUnwantedArtifacts();
            SetFractureChunksActive(false);
            _currentAnimState = null;
            _attackCooldownTimer = 0f;
            _rollCooldownTimer = 0f;
            _jumpSlamCooldownTimer = 1.5f;

            // Khởi tạo trạng thái ẨN DƯỚI LÒNG ĐẤT:
            // Y = 0.0m - (-0.08m) = +0.08m kết hợp anim Burrow trên Group -> chỉ đúng 2 chỏm gai trắng nhô lên mặt sàn ~10cm
            transform.position = new Vector3(_groundSpawnPos.x, _groundSpawnPos.y - _submergeDepth, 0f);
            transform.rotation = Quaternion.Euler(0f, 270f, 0f);

            if (_meshVisualRoot != null)
            {
                _meshVisualRoot.localRotation = Quaternion.identity;
            }

            if (_rigidbody != null && !_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            // Tắt ngọn lửa toàn thân khi đang ẩn dưới đất
            SetBodyFlameActive(false);

            // Tắt vệt lăn
            SetRollTrailActive(false);

            // Bật hiệu ứng khói rỉ quanh gai nhô lên
            SetGroundCrackActive(true);

            // Ẩn thanh máu của quái vật trên đầu khi đang trốn
            SetWorldHpBarVisible(false);

            // Chuyển sang State Hidden
            TransitionTo(MonsterAIState.Hidden);
            Debug.Log("[MonsterBallAI] MonsterBall xuất hiện ở trạng thái ẨN DƯỚI ĐẤT (Burrow / Hidden)!");
        }

        public void ResetMonster()
        {
            if (_health != null)
            {
                _health.ResetHealth();
            }

            SpawnAtDefaultPosition();
            _consecutiveHitCount = 0;
            Debug.Log("[MonsterBallAI] MonsterBall đã được Reset về trạng thái xuất phát!");
        }

        public void SetBodyFlameActive(bool active)
        {
            try
            {
                if (active)
                {
                    if (_bodyFlameInstance == null && _flameVfxPrefab != null)
                    {
                        _bodyFlameInstance = Instantiate(_flameVfxPrefab, transform);
                        _bodyFlameInstance.name = "BodyFlame_UNI_VFX";
                        _bodyFlameInstance.transform.localPosition = Vector3.zero;
                        _bodyFlameInstance.transform.localRotation = Quaternion.identity;
                        _bodyFlameInstance.transform.localScale = Vector3.one;
                    }

                    if (_bodyFlameInstance != null)
                    {
                        bool wasPlaying = _bodyFlameInstance.activeInHierarchy;
                        _bodyFlameInstance.SetActive(true);
                        var binder = _bodyFlameInstance.GetComponent<SkinnedMeshVfxBinder>();
                        if (binder == null) binder = _bodyFlameInstance.AddComponent<SkinnedMeshVfxBinder>();
                        binder.Bind(_skinnedMeshRenderer, 1.9f, 2400f, 1.05f);
                        if (!wasPlaying || !binder.IsPlaying)
                        {
                            binder.PlayEffect();
                        }
                    }
                }
                else
                {
                    if (_bodyFlameInstance != null)
                    {
                        var binder = _bodyFlameInstance.GetComponent<SkinnedMeshVfxBinder>();
                        if (binder != null) binder.StopEffect();
                        _bodyFlameInstance.SetActive(false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MonsterBallAI] SetBodyFlameActive warning: {ex.Message}");
            }
        }

        private void SetGroundCrackActive(bool active)
        {
            if (active)
            {
                if (_groundCrackInstance == null && _groundCrackPrefab != null)
                {
                    _groundCrackInstance = Instantiate(_groundCrackPrefab, new Vector3(_groundSpawnPos.x, 0.02f, 0f), Quaternion.identity);
                    _groundCrackInstance.name = "GroundCrack_Instance";
                }
                if (_groundCrackInstance != null) _groundCrackInstance.SetActive(true);
            }
            else
            {
                if (_groundCrackInstance != null) _groundCrackInstance.SetActive(false);
            }
        }

        private void SetRollTrailActive(bool active)
        {
            if (active)
            {
                if (_rollTrailInstance == null && _rollTrailPrefab != null)
                {
                    _rollTrailInstance = Instantiate(_rollTrailPrefab, transform);
                    _rollTrailInstance.name = "RollTrail_Instance";
                    _rollTrailInstance.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                }
                if (_rollTrailInstance != null)
                {
                    _rollTrailInstance.SetActive(true);
                    var psList = _rollTrailInstance.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (var ps in psList) ps.Play();
                }
            }
            else
            {
                if (_rollTrailInstance != null)
                {
                    var psList = _rollTrailInstance.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (var ps in psList) ps.Stop();
                    _rollTrailInstance.SetActive(false);
                }
            }
        }

        private void SetJumpTrailActive(bool active)
        {
            if (active)
            {
                if (_jumpTrailInstance == null && _jumpTrailPrefab != null)
                {
                    _jumpTrailInstance = Instantiate(_jumpTrailPrefab, transform);
                    _jumpTrailInstance.name = "JumpTrail_Instance";
                    _jumpTrailInstance.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                }
                if (_jumpTrailInstance != null)
                {
                    _jumpTrailInstance.SetActive(true);
                    foreach (var ps in _jumpTrailInstance.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        ps.Play();
                    }
                }
            }
            else
            {
                if (_jumpTrailInstance != null)
                {
                    foreach (var ps in _jumpTrailInstance.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        ps.Stop();
                    }
                }
            }
        }

        private void SetWorldHpBarVisible(bool visible)
        {
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SetMonsterHpBarVisible(visible);
            }
            else
            {
                var canvas = GameObject.Find("Monster_World_Canvas");
                if (canvas != null)
                {
                    var barRoot = canvas.transform.Find("Bar_Root");
                    if (barRoot != null) barRoot.gameObject.SetActive(visible);
                }
            }
        }

        private void PlayAnimation(string animName, bool forceRestart = false)
        {
            if (_animator == null || string.IsNullOrEmpty(animName)) return;
            if (!forceRestart && _currentAnimState == animName) return;
            try
            {
                _currentAnimState = animName;
                _animator.CrossFadeInFixedTime(animName, 0.12f);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MonsterBallAI] PlayAnimation error: {ex.Message}");
            }
        }

        private void Update()
        {
            // Phím F1: Bật / Tắt AI nhanh cho người chơi thử nghiệm
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _aiEnabled = !_aiEnabled;
                Debug.Log($"[MonsterBallAI] AI Enabled: {_aiEnabled}");
            }

            // Khóa cứng trục Z = 0
            var pos = transform.position;
            pos.z = 0f;
            transform.position = pos;

            // Cập nhật cooldowns
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
            if (_rollCooldownTimer > 0f) _rollCooldownTimer -= Time.deltaTime;
            if (_jumpSlamCooldownTimer > 0f) _jumpSlamCooldownTimer -= Time.deltaTime;

            // Reset chuỗi combo nếu sau 1.2s không bị đánh tiếp
            if (Time.time - _lastHitTime > 1.2f)
            {
                _consecutiveHitCount = 0;
            }

            // Vòng lặp AI State Machine
            UpdateStateMachine();
        }

        private void UpdateStateMachine()
        {
            _stateTimer += Time.deltaTime;

            switch (_currentState)
            {
                case MonsterAIState.Hidden:
                    ProcessHiddenState();
                    break;

                case MonsterAIState.Emerge:
                    ProcessEmergeState();
                    break;

                case MonsterAIState.LandIgnite:
                    ProcessLandIgniteState();
                    break;

                case MonsterAIState.RollAttack:
                    ProcessRollAttackState();
                    break;

                case MonsterAIState.JumpSlam:
                    ProcessJumpSlamState();
                    break;

                case MonsterAIState.Recover:
                    ProcessRecoverState();
                    break;

                case MonsterAIState.CombatIdle:
                    AutoFaceTarget();
                    PlayAnimation("BattleIdle");
                    if (_aiEnabled) ProcessCombatDecision();
                    break;

                case MonsterAIState.Approach:
                    AutoFaceTarget();
                    ProcessApproach();
                    break;

                case MonsterAIState.KeepDistance:
                    AutoFaceTarget();
                    ProcessKeepDistance();
                    break;

                case MonsterAIState.Attack1:
                case MonsterAIState.Attack2:
                    ProcessAttackExecution();
                    break;

                case MonsterAIState.Damaged:
                    if (_stateTimer >= 0.45f)
                    {
                        TransitionTo(MonsterAIState.CombatIdle);
                    }
                    break;

                case MonsterAIState.Groggy:
                    if (_stateTimer >= _groggyDuration)
                    {
                        TransitionTo(MonsterAIState.CombatIdle);
                    }
                    break;
            }
        }

        private void ProcessHiddenState()
        {
            // Duy trì vị trí ẩn dưới đất bằng Burrow anim
            var p = transform.position;
            p.y = _groundSpawnPos.y - _submergeDepth;
            transform.position = p;

            if (_targetPlayer == null) FindPlayerTarget();
            if (_targetPlayer == null) return;

            // Chỉ trồi lên khi AI bật và JangHyu tiến lại gần (<= 5.5m)
            if (!_aiEnabled) return;

            float dist = Mathf.Abs(transform.position.x - _targetPlayer.position.x);
            if (dist <= _triggerDistance)
            {
                Debug.Log($"[MonsterBallAI] JangHyu tiến lại gần ({dist:F2}m)! Kích hoạt LAO VỌT LÊN (Emerge)!");
                TransitionTo(MonsterAIState.Emerge);
            }
        }

        private void ProcessEmergeState()
        {
            // Thời gian phóng vọt lên và rơi xuống tiếp đất: 0.75s
            float duration = 0.75f;
            float t = Mathf.Clamp01(_stateTimer / duration);

            // Quỹ đạo Parabol: Phóng vọt từ mặt đất lên đỉnh ~2.6m rồi tiếp đất vững vàng tại Y = 0.0m
            float startY = _groundSpawnPos.y - _submergeDepth;
            float targetY = _groundY;
            float arcHeight = Mathf.Sin(t * Mathf.PI) * 2.5f;
            float curY = Mathf.Lerp(startY, targetY, t) + arcHeight;

            // Di chuyển nhẹ về phía người chơi trong lúc bay
            float startX = _groundSpawnPos.x;
            float targetX = _groundSpawnPos.x - 0.6f;
            float curX = Mathf.Lerp(startX, targetX, t);

            transform.position = new Vector3(curX, curY, 0f);

            // Khi chạm đất (t >= 1.0)
            if (_stateTimer >= duration)
            {
                var finalPos = transform.position;
                finalPos.y = _groundY;
                transform.position = finalPos;
                TransitionTo(MonsterAIState.LandIgnite);
            }
        }

        private void ProcessLandIgniteState()
        {
            // Đứng vững trên sàn Y = 0.0m
            var p = transform.position;
            p.y = _groundY;
            transform.position = p;

            AutoFaceTarget();

            // Chờ 0.4s để hiệu ứng bốc cháy và thanh máu hiển thị trọn vẹn trước khi Roll
            if (_stateTimer >= 0.4f)
            {
                TransitionTo(MonsterAIState.RollAttack);
            }
        }

        private void ProcessRollAttackState()
        {
            // Giữ vững cao độ Y = 0.0m; dùng animation Attack1 (xoay 360° quanh tâm Root_M) thay vì xoay pivot ở đáy khiến quái cắm đầu xuống đất
            var p = transform.position;
            p.y = _groundY;
            transform.position = p;

            if (_targetPlayer == null) FindPlayerTarget();

            // Di chuyển cuộn tròn tốc độ cao dọc trục X
            transform.position += new Vector3(_currentRollDir * _rollSpeed * Time.deltaTime, 0f, 0f);

            // Kiểm tra va chạm với JangHyu
            if (_targetPlayer != null)
            {
                float dist = Mathf.Abs(transform.position.x - _targetPlayer.position.x);
                if (dist <= 1.4f)
                {
                    // Đâm trúng JangHyu!
                    ApplyRollHitToPlayer();
                    TransitionTo(MonsterAIState.Recover);
                    return;
                }
            }

            // Timeout sau 1.8s hoặc đi quá xa mà không trúng
            if (_stateTimer >= 1.8f)
            {
                _rollCooldownTimer = 4.5f;
                TransitionTo(MonsterAIState.Recover);
            }
        }

        private void ApplyRollHitToPlayer()
        {
            if (_targetPlayer == null) return;

            var receiver = _playerBurnReceiver != null ? _playerBurnReceiver : _targetPlayer.GetComponent<JangHyuBurnReceiver>();
            if (receiver == null || receiver.IsDead) return;

            // Gây 25 sát thương trực tiếp (HP JangHyu giảm từ 100 xuống 75 như hình demo)
            receiver.TakeDirectDamage(25);

            // Thiêu đốt bốc cháy người chơi trong 3 giây
            receiver.ApplyBurn(3.0f, 0.4f, 1, _flameVfxPrefab);

            // Hất văng ngã ngửa và khóa di chuyển suốt thời gian Knockdown -> StandUp
            receiver.ApplyKnockback(new Vector3(_currentRollDir * 5.0f, 3.5f, 0f), 3.0f, true);
            _rollCooldownTimer = 4.5f;

            StreetFighterCamera.TriggerShake(0.32f, 0.28f, 2.5f);
            CombatPostProcessController.TriggerHit(0.55f);
            PlaySfx(_sfxHitImpact, 0.9f);

            Debug.Log("[MonsterBallAI] ROLL ATTACK ĐÂM TRÚNG JANGHYU! Gây 25 sát thương + Thiêu đốt ngọn lửa!");
        }

        private void ProcessJumpSlamState()
        {
            const float jumpDuration = 0.80f;
            float t = Mathf.Clamp01(_stateTimer / jumpDuration);

            // Quỹ đạo parabol nhảy bổng lên 2.45m (gọn trong khung hình camera) rồi lao thẳng xuống đập trúng người chơi
            float curX = Mathf.Lerp(_jumpStartPos.x, _jumpTargetPos.x, t);
            float curY = _groundY + Mathf.Sin(t * Mathf.PI) * 2.45f;
            transform.position = new Vector3(curX, curY, 0f);

            if (_meshVisualRoot != null)
            {
                _meshVisualRoot.Rotate(Vector3.right, -640f * Time.deltaTime, Space.Self);
            }

            if (_stateTimer >= jumpDuration)
            {
                transform.position = new Vector3(_jumpTargetPos.x, _groundY, 0f);
                if (_meshVisualRoot != null)
                {
                    _meshVisualRoot.localRotation = Quaternion.identity;
                }
                SetJumpTrailActive(false);

                TriggerBombExplosionImpact();
                _jumpSlamCooldownTimer = 6.0f;
                _attackCooldownTimer = 1.4f;
                TransitionTo(MonsterAIState.Recover);
            }
        }

        private void TriggerBombExplosionImpact()
        {
            if (_bombExplosionPrefab != null)
            {
                var boom = Instantiate(_bombExplosionPrefab, new Vector3(transform.position.x, 0.08f, 0f), Quaternion.identity);
                Destroy(boom, 3.5f);
            }

            // Rung lắc màn hình + chớp sáng/lệch sắc Post-Processing + âm thanh nổ bom
            StreetFighterCamera.TriggerShake(0.45f, 0.42f, 4.5f);
            CombatPostProcessController.TriggerExplosion(1.05f);
            PlaySfx(_sfxBombExplosion, 1.0f);

            if (_targetPlayer == null) return;
            float dist = Mathf.Abs(transform.position.x - _targetPlayer.position.x);
            if (dist <= 2.9f)
            {
                var receiver = _playerBurnReceiver != null ? _playerBurnReceiver : _targetPlayer.GetComponent<JangHyuBurnReceiver>();
                if (receiver != null && !receiver.IsDead)
                {
                    float hitDir = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
                    if (Mathf.Abs(hitDir) < 0.1f) hitDir = -1f;

                    receiver.TakeDirectDamage(30);
                    receiver.ApplyBurn(3.5f, 0.4f, 1, _flameVfxPrefab);
                    receiver.ApplyKnockback(new Vector3(hitDir * 6.5f, 4.5f, 0f), 3.0f, true);
                }
            }

            Debug.Log("[MonsterBallAI] JUMP SLAM NỔ BOM! Khói đen + Post-Process + Rung màn hình + SFX!");
        }

        private void ProcessRecoverState()
        {
            // Giữ vững cao độ Y = 0.0m
            var p = transform.position;
            p.y = _groundY;
            transform.position = p;

            AutoFaceTarget();

            // Lùi nhẹ lại lấy thăng bằng
            transform.position += new Vector3(-_currentRollDir * 1.2f * Time.deltaTime, 0f, 0f);

            if (_stateTimer >= 0.5f)
            {
                _attackCooldownTimer = Mathf.Max(_attackCooldownTimer, 0.9f);
                TransitionTo(MonsterAIState.CombatIdle);
            }
        }

        private void AutoFaceTarget()
        {
            if (_targetPlayer == null) return;
            float dirX = _targetPlayer.position.x - transform.position.x;
            if (Mathf.Abs(dirX) > 0.15f)
            {
                float targetY = dirX > 0 ? 90f : 270f;
                transform.rotation = Quaternion.Euler(0f, targetY, 0f);
            }
        }

        private void ProcessCombatDecision()
        {
            if (_targetPlayer == null)
            {
                FindPlayerTarget();
                return;
            }

            var pos = transform.position;
            pos.y = _groundY;
            transform.position = pos;

            // Nếu người chơi đã gục hẳn (HP = 0), giữ tư thế cảnh giác BattleIdle
            if (_playerBurnReceiver != null && _playerBurnReceiver.IsDead)
            {
                PlayAnimation("BattleIdle");
                return;
            }

            float dist = Mathf.Abs(transform.position.x - _targetPlayer.position.x);

            // 1. Kỹ năng nhảy bổng đập xuống nổ bom (ưu tiên định kỳ khi hết hồi chiêu)
            if (_jumpSlamCooldownTimer <= 0f && dist <= 6.5f)
            {
                TransitionTo(MonsterAIState.JumpSlam);
                return;
            }

            // 2. Quá gần người chơi (< _keepDistanceThreshold): Lùi lại tạo khoảng cách an toàn
            if (dist < _keepDistanceThreshold)
            {
                TransitionTo(MonsterAIState.KeepDistance);
                return;
            }

            // 3. Trong tầm cận chiến và hết cooldown: Tung đòn tấn công
            if (dist <= _meleeAttackRange && _attackCooldownTimer <= 0f)
            {
                TransitionTo(Random.value > 0.35f ? MonsterAIState.Attack2 : MonsterAIState.Attack1);
                return;
            }

            // 4. Ở xa (> _meleeAttackRange): Lao cuộn RollAttack nếu hết hồi chiêu, hoặc đi bộ tiếp cận
            if (dist > _meleeAttackRange)
            {
                if (dist >= 3.2f && _rollCooldownTimer <= 0f)
                {
                    TransitionTo(MonsterAIState.RollAttack);
                }
                else
                {
                    TransitionTo(MonsterAIState.Approach);
                }
                return;
            }

            // 4. Đang chờ hồi chiêu trong tầm đánh: Giữ BattleIdle nhịp nhàng (không bị đóng băng animation)
            PlayAnimation("BattleIdle");
        }

        private void ProcessApproach()
        {
            if (!_aiEnabled || _targetPlayer == null)
            {
                TransitionTo(MonsterAIState.CombatIdle);
                return;
            }

            var p = transform.position;
            p.y = _groundY;
            transform.position = p;

            float dist = Mathf.Abs(transform.position.x - _targetPlayer.position.x);
            if (dist <= _meleeAttackRange * 0.88f)
            {
                TransitionTo(MonsterAIState.CombatIdle);
                return;
            }

            if (dist >= 3.5f && _rollCooldownTimer <= 0f)
            {
                TransitionTo(MonsterAIState.RollAttack);
                return;
            }

            PlayAnimation("BattleWalk");

            float dirX = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
            transform.position += new Vector3(dirX * _walkSpeed * Time.deltaTime, 0f, 0f);
        }

        private void ProcessKeepDistance()
        {
            if (!_aiEnabled || _targetPlayer == null)
            {
                TransitionTo(MonsterAIState.CombatIdle);
                return;
            }

            var p = transform.position;
            p.y = _groundY;
            transform.position = p;

            float dist = Mathf.Abs(transform.position.x - _targetPlayer.position.x);
            if (dist >= _meleeAttackRange * 0.9f || _stateTimer >= 0.7f)
            {
                TransitionTo(MonsterAIState.CombatIdle);
                return;
            }

            PlayAnimation("WalkBackward");

            float retreatDirX = Mathf.Sign(transform.position.x - _targetPlayer.position.x);
            transform.position += new Vector3(retreatDirX * _backstepSpeed * Time.deltaTime, 0f, 0f);
        }

        private void ProcessAttackExecution()
        {
            var p = transform.position;
            p.y = _groundY;
            transform.position = p;

            // Đòn đánh trúng vào khoảng 0.32s của animation
            if (!_hasAttackedInCurrentState && _stateTimer >= 0.32f)
            {
                _hasAttackedInCurrentState = true;
                CheckHitPlayer();
            }

            // Animation tấn công hoàn tất sau 0.75s
            if (_stateTimer >= 0.75f)
            {
                _attackCooldownTimer = _attackCooldown;
                TransitionTo(MonsterAIState.CombatIdle);
            }
        }

        private void CheckHitPlayer()
        {
            if (_targetPlayer == null) return;

            float dist = Vector3.Distance(transform.position, _targetPlayer.position);
            if (dist <= _meleeAttackRange + 0.5f)
            {
                var receiver = _playerBurnReceiver != null ? _playerBurnReceiver : _targetPlayer.GetComponent<JangHyuBurnReceiver>();
                if (receiver != null && !receiver.IsDead)
                {
                    receiver.TakeDirectDamage(15);
                    receiver.ApplyBurn(3.0f, 0.4f, 1, _flameVfxPrefab);

                    float hitDir = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
                    if (Mathf.Abs(hitDir) < 0.1f) hitDir = -1f;
                    bool shouldKnockdown = _currentState == MonsterAIState.Attack1;
                    receiver.ApplyKnockback(
                        new Vector3(hitDir * (shouldKnockdown ? 4.2f : 2.5f), shouldKnockdown ? 2.5f : 0.8f, 0f),
                        shouldKnockdown ? 3.0f : 0.6f,
                        shouldKnockdown);
                    StreetFighterCamera.TriggerShake(0.18f, 0.16f, 1.5f);
                    CombatPostProcessController.TriggerHit(0.35f);
                    PlaySfx(_sfxHitImpact, 0.75f);
                }

                Debug.Log("[MonsterBallAI] Quái vật cắn/húc trúng JangHyu!");
            }
        }

        public void TransitionTo(MonsterAIState newState)
        {
            _currentState = newState;
            _stateTimer = 0f;
            _hasAttackedInCurrentState = false;

            switch (newState)
            {
                case MonsterAIState.Hidden:
                    SetFractureChunksActive(false);
                    SetBodyFlameActive(false);
                    SetRollTrailActive(false);
                    SetJumpTrailActive(false);
                    SetGroundCrackActive(true);
                    SetWorldHpBarVisible(false);
                    PlayAnimation("Burrow");
                    break;

                case MonsterAIState.Emerge:
                    SetFractureChunksActive(false);
                    SetGroundCrackActive(false);
                    SetBodyFlameActive(true);
                    StreetFighterCamera.TriggerShake(0.24f, 0.24f, 2.0f);
                    PlaySfx(_sfxEmerge, 0.9f);
                    // Sinh vụ nổ khói đất đá phun trào từ lòng đất
                    if (_groundEruptPrefab != null)
                    {
                        var erupt = Instantiate(_groundEruptPrefab, new Vector3(_groundSpawnPos.x, 0.05f, 0f), Quaternion.identity);
                        Destroy(erupt, 2.5f);
                    }
                    PlayAnimation("Unburrow");
                    break;

                case MonsterAIState.LandIgnite:
                    SetBodyFlameActive(true);
                    SetWorldHpBarVisible(true);
                    AutoFaceTarget();
                    PlayAnimation("BattleIdle");
                    break;

                case MonsterAIState.RollAttack:
                    SetRollTrailActive(true);
                    if (_targetPlayer != null)
                    {
                        _currentRollDir = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
                    }
                    else
                    {
                        _currentRollDir = -1f;
                    }
                    AutoFaceTarget();
                    PlaySfx(_sfxRoll, 0.8f);
                    PlayAnimation("Attack1", true);
                    break;

                case MonsterAIState.JumpSlam:
                    SetRollTrailActive(false);
                    SetBodyFlameActive(true);
                    SetJumpTrailActive(true);
                    _jumpStartPos = new Vector3(transform.position.x, _groundY, 0f);
                    if (_targetPlayer != null)
                    {
                        float dx = Mathf.Clamp(_targetPlayer.position.x - _jumpStartPos.x, -6.5f, 6.5f);
                        _jumpTargetPos = new Vector3(Mathf.Clamp(_jumpStartPos.x + dx, -9.5f, 9.5f), _groundY, 0f);
                    }
                    else
                    {
                        _jumpTargetPos = _jumpStartPos + new Vector3(-2.5f, 0f, 0f);
                    }
                    AutoFaceTarget();
                    PlaySfx(_sfxJump, 0.95f);
                    PlayAnimation("Unburrow", true);
                    break;

                case MonsterAIState.Recover:
                    SetRollTrailActive(false);
                    if (_meshVisualRoot != null)
                    {
                        _meshVisualRoot.localRotation = Quaternion.identity;
                    }
                    PlayAnimation("BattleIdle");
                    break;

                case MonsterAIState.CombatIdle:
                    SetRollTrailActive(false);
                    if (_meshVisualRoot != null)
                    {
                        _meshVisualRoot.localRotation = Quaternion.identity;
                    }
                    PlayAnimation("BattleIdle");
                    break;

                case MonsterAIState.Approach:
                    PlayAnimation("BattleWalk");
                    break;

                case MonsterAIState.KeepDistance:
                    PlayAnimation("WalkBackward");
                    break;

                case MonsterAIState.Attack1:
                    AutoFaceTarget();
                    PlayAnimation("Attack1", true);
                    break;

                case MonsterAIState.Attack2:
                    AutoFaceTarget();
                    PlayAnimation("Attack2", true);
                    break;

                case MonsterAIState.Damaged:
                    PlayAnimation("DamagedForward", true);
                    break;

                case MonsterAIState.Groggy:
                    PlayAnimation("GroggyStart", true);
                    break;

                case MonsterAIState.Dead:
                    SetBodyFlameActive(false);
                    SetRollTrailActive(false);
                    PlayAnimation("DamagedBackward", true);
                    break;
            }
        }

        private void HandleTakeDamage(int damage)
        {
            _lastHitTime = Time.time;
            _consecutiveHitCount++;

            // Khi trúng đủ chuỗi Combo 3 hit liên tiếp -> Rơi vào Groggy (Choáng)
            if (_consecutiveHitCount >= 3)
            {
                _consecutiveHitCount = 0;
                TransitionTo(MonsterAIState.Groggy);
                Debug.Log("[MonsterBallAI] Trúng trọn combo 3 hit! MonsterBall rơi vào trạng thái CHOÁNG (Groggy)!");
                return;
            }

            // Đang không tấn công hoặc không choáng thì giật lùi
            if (_currentState != MonsterAIState.Attack1 && _currentState != MonsterAIState.Attack2 && 
                _currentState != MonsterAIState.Groggy && _currentState != MonsterAIState.RollAttack && 
                _currentState != MonsterAIState.Hidden && _currentState != MonsterAIState.Emerge)
            {
                TransitionTo(MonsterAIState.Damaged);

                // Giật lùi nhẹ 0.35m
                if (_targetPlayer != null)
                {
                    float knockDirX = Mathf.Sign(transform.position.x - _targetPlayer.position.x);
                    transform.position += new Vector3(knockDirX * 0.35f, 0f, 0f);
                }
            }
        }

        private void HandleDeath()
        {
            TransitionTo(MonsterAIState.Dead);
            Debug.Log("[MonsterBallAI] MonsterBall đã bị tiêu diệt!");
        }
    }
}
