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
        private float _rollWindupDuration = 0.22f;
        private bool _rollChargeStarted = false;
        private float _jumpWindupDuration = 0.28f;
        private bool _jumpLaunchStarted = false;
        private float _jumpWindupDir = 1f;
        private Vector3 _jumpStartPos;
        private Vector3 _jumpTargetPos;
        private string _currentAnimState;
        private JangHyuBurnReceiver _playerBurnReceiver;

        // VFX instances
        private GameObject _bodyFlameInstance;
        private GameObject _groundCrackInstance;
        private GameObject _rollTrailInstance;
        private GameObject _jumpTrailInstance;
        private Coroutine _deathExplosionCoroutine;
        private System.Collections.Generic.List<(Transform t, Vector3 pos, Quaternion rot)> _chunkRestPose;

        public bool IsAIEnabled
        {
            get => _aiEnabled;
            set => _aiEnabled = value;
        }

        public MonsterAIState CurrentState => _currentState;
        public int ImpactExplosionsTriggered { get; private set; }
        public bool IsRollTrailPlaying => _rollTrailInstance != null && _rollTrailInstance.activeSelf && _rollTrailInstance.GetComponentInChildren<ParticleSystem>() is ParticleSystem ps && ps.isEmitting;
        public bool IsJumpTrailPlaying => _jumpTrailInstance != null && _jumpTrailInstance.activeSelf && _jumpTrailInstance.GetComponentInChildren<ParticleSystem>() is ParticleSystem ps && ps.isEmitting;

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
                if (_chunkRestPose == null)
                {
                    _chunkRestPose = new System.Collections.Generic.List<(Transform, Vector3, Quaternion)>();
                    foreach (Transform child in g1.GetComponentsInChildren<Transform>(true))
                    {
                        if (child == g1) continue;
                        _chunkRestPose.Add((child, child.localPosition, child.localRotation));
                    }
                }
                if (!active && _chunkRestPose != null)
                {
                    foreach (var item in _chunkRestPose)
                    {
                        if (item.t != null)
                        {
                            item.t.localPosition = item.pos;
                            item.t.localRotation = item.rot;
                        }
                    }
                }
                g1.gameObject.SetActive(active);
            }
        }

        private void RestoreAliveVisuals()
        {
            if (_deathExplosionCoroutine != null)
            {
                StopCoroutine(_deathExplosionCoroutine);
                _deathExplosionCoroutine = null;
            }
            if (_meshVisualRoot != null)
            {
                _meshVisualRoot.gameObject.SetActive(true);
                _meshVisualRoot.localRotation = Quaternion.identity;
            }
            if (_collider != null) _collider.enabled = true;
            SetFractureChunksActive(false);
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
            RestoreAliveVisuals();
            _currentAnimState = null;
            _attackCooldownTimer = 0f;
            _rollCooldownTimer = 0f;
            _jumpSlamCooldownTimer = 1.5f;

            // Khởi tạo trạng thái ẨN DƯỚI LÒNG ĐẤT:
            // Y = 0.0m - (-0.08m) = +0.08m kết hợp anim Burrow trên Group -> mặt hướng ra màn hình (-Z) nghiêng nhẹ về phía Player
            transform.position = new Vector3(_groundSpawnPos.x, _groundSpawnPos.y - _submergeDepth, 0f);
            transform.rotation = Quaternion.Euler(0f, 205f, 0f);

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

        public void SpawnAndEmerge(bool startActiveImmediately = false)
        {
            if (_health != null && _health.IsDead)
            {
                _health.ResetHealth();
            }

            if (_currentState == MonsterAIState.Hidden || startActiveImmediately)
            {
                CleanupUnwantedArtifacts();
                RestoreAliveVisuals();
                _currentAnimState = null;
                transform.position = new Vector3(_groundSpawnPos.x, _groundSpawnPos.y - _submergeDepth, 0f);
                if (_meshVisualRoot != null) _meshVisualRoot.localRotation = Quaternion.identity;
                TransitionTo(MonsterAIState.Emerge);
                return;
            }

            SpawnAtDefaultPosition();
        }

        public void TriggerDemoAction(MonsterAIState actionState)
        {
            FindPlayerTarget();

            if (_health != null && _health.IsDead)
            {
                _health.ResetHealth();
            }

            CleanupUnwantedArtifacts();
            RestoreAliveVisuals();
            if (_meshVisualRoot != null)
            {
                _meshVisualRoot.localRotation = Quaternion.identity;
            }

            if (actionState == MonsterAIState.Hidden || actionState == MonsterAIState.Emerge)
            {
                transform.position = new Vector3(_groundSpawnPos.x, _groundSpawnPos.y - _submergeDepth, 0f);
                TransitionTo(actionState);
                return;
            }

            if (transform.position.y < _groundY || _currentState == MonsterAIState.Hidden)
            {
                transform.position = new Vector3(transform.position.x, _groundY, 0f);
            }

            SetBodyFlameActive(true);
            SetWorldHpBarVisible(true);

            if ((actionState == MonsterAIState.Attack1 || actionState == MonsterAIState.Attack2) && _targetPlayer != null)
            {
                float dist = Mathf.Abs(transform.position.x - _targetPlayer.position.x);
                if (dist > _meleeAttackRange + 0.5f)
                {
                    float side = Mathf.Sign(transform.position.x - _targetPlayer.position.x);
                    if (Mathf.Abs(side) < 0.1f) side = 1f;
                    transform.position = new Vector3(Mathf.Clamp(_targetPlayer.position.x + side * 1.75f, -9.5f, 9.5f), _groundY, 0f);
                }
            }

            AutoFaceTarget();
            if (actionState == MonsterAIState.Dead && _health != null)
            {
                _health.TakeDamage(_health.MaxHp);
                return;
            }
            TransitionTo(actionState);
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

        private void SetRollTrailActive(bool active, bool immediateClear = false)
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
            else if (_rollTrailInstance != null)
            {
                var psList = _rollTrailInstance.GetComponentsInChildren<ParticleSystem>(true);
                var behavior = immediateClear
                    ? ParticleSystemStopBehavior.StopEmittingAndClear
                    : ParticleSystemStopBehavior.StopEmitting;
                foreach (var ps in psList) ps.Stop(true, behavior);
                if (immediateClear) _rollTrailInstance.SetActive(false);
            }
        }

        private void SetJumpTrailActive(bool active, bool immediateClear = false)
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
            else if (_jumpTrailInstance != null)
            {
                var behavior = immediateClear
                    ? ParticleSystemStopBehavior.StopEmittingAndClear
                    : ParticleSystemStopBehavior.StopEmitting;
                foreach (var ps in _jumpTrailInstance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps.Stop(true, behavior);
                }
                if (immediateClear) _jumpTrailInstance.SetActive(false);
            }
        }

        private Vector3 GetCollisionPointWithPlayer(float defaultY = 0.65f)
        {
            if (_targetPlayer == null) return new Vector3(transform.position.x, defaultY, 0f);
            float midX = Mathf.Lerp(transform.position.x, _targetPlayer.position.x, 0.52f);
            float hitY = Mathf.Max(defaultY, transform.position.y * 0.45f + 0.55f);
            return new Vector3(midX, hitY, 0f);
        }

        private void SpawnImpactFireExplosion(Vector3 impactPoint, float scale, float sfxVolume, float postProcessIntensity)
        {
            if (_bombExplosionPrefab != null)
            {
                var boom = Instantiate(_bombExplosionPrefab, impactPoint, Quaternion.identity);
                boom.transform.localScale = Vector3.one * scale;
                Destroy(boom, 3.2f);
            }
            ImpactExplosionsTriggered++;
            CombatPostProcessController.TriggerExplosion(postProcessIntensity);
            PlaySfx(_sfxBombExplosion, sfxVolume);
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

            // Chờ 0.4s để hiệu ứng bốc cháy và thanh máu hiển thị trọn vẹn; nếu AI đang tắt thì đứng CombatIdle chờ demo
            if (_stateTimer >= 0.4f)
            {
                TransitionTo(_aiEnabled ? MonsterAIState.RollAttack : MonsterAIState.CombatIdle);
            }
        }

        private void ProcessRollAttackState()
        {
            if (_targetPlayer == null) FindPlayerTarget();

            // Pha 1: Lấy đà / bật lùi tạo khoảng cách nếu đang đứng sát Player
            if (_stateTimer < _rollWindupDuration)
            {
                float curY = _groundY;
                if (_targetPlayer != null)
                {
                    float distToPlayer = Mathf.Abs(transform.position.x - _targetPlayer.position.x);
                    if (distToPlayer < 3.8f)
                    {
                        float backDir = Mathf.Sign(transform.position.x - _targetPlayer.position.x);
                        if (Mathf.Abs(backDir) < 0.1f) backDir = 1f;
                        float nextX = Mathf.Clamp(transform.position.x + backDir * 11.5f * Time.deltaTime, -9.5f, 9.5f);
                        float hopT = Mathf.Clamp01(_stateTimer / _rollWindupDuration);
                        curY = _groundY + Mathf.Sin(hopT * Mathf.PI) * 0.45f;
                        transform.position = new Vector3(nextX, curY, 0f);
                    }
                }
                AutoFaceTarget();
                return;
            }

            if (!_rollChargeStarted)
            {
                _rollChargeStarted = true;
                if (_targetPlayer != null)
                {
                    _currentRollDir = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
                    if (Mathf.Abs(_currentRollDir) < 0.1f) _currentRollDir = -1f;
                }
                AutoFaceTarget();
                if (_animator != null) _animator.speed = 3.2f;
                PlaySfx(_sfxRoll, 0.9f);
            }

            // Pha 2: Xoay tít mù (3.2x vòng/s quanh Root_M) + phóng vòng cung thấp lao tốc độ cao vào Player
            float chargeElapsed = _stateTimer - _rollWindupDuration;
            float arcT = Mathf.Clamp01(chargeElapsed / 0.52f);
            float chargeY = _groundY + Mathf.Sin(arcT * Mathf.PI) * 1.15f;
            float speed = Mathf.Max(_rollSpeed, 13.5f);
            float movedX = Mathf.Clamp(transform.position.x + _currentRollDir * speed * Time.deltaTime, -10f, 10f);
            transform.position = new Vector3(movedX, chargeY, 0f);

            // Kiểm tra va chạm với JangHyu
            if (_targetPlayer != null)
            {
                float dist = Mathf.Abs(transform.position.x - _targetPlayer.position.x);
                if (dist <= 1.5f)
                {
                    transform.position = new Vector3(transform.position.x, _groundY, 0f);
                    ApplyRollHitToPlayer();
                    TransitionTo(MonsterAIState.Recover);
                    return;
                }
            }

            if (chargeElapsed >= 1.4f)
            {
                transform.position = new Vector3(transform.position.x, _groundY, 0f);
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

            // Vụ nổ lửa bùng phát ngay tại điểm va chạm giữa MonsterBall và JangHyu
            SpawnImpactFireExplosion(GetCollisionPointWithPlayer(0.62f), 0.9f, 0.85f, 0.72f);

            // Húc văng mạnh Player lùi về sau 3.5-4.5m và khóa di chuyển suốt thời gian Knockdown -> StandUp
            receiver.ApplyKnockback(new Vector3(_currentRollDir * 10.5f, 4.4f, 0f), 3.0f, true);
            _rollCooldownTimer = 4.5f;

            StreetFighterCamera.TriggerShake(0.35f, 0.32f, 3.0f);
            PlaySfx(_sfxHitImpact, 0.95f);

            Debug.Log("[MonsterBallAI] ROLL ATTACK ĐÂM TRÚNG JANGHYU! Húc văng lùi về sau + Gây 25 sát thương + Thiêu đốt!");
        }

        private void ProcessJumpSlamState()
        {
            if (_targetPlayer == null) FindPlayerTarget();

            // Pha 1: Lùi về sau lấy đà trước khi nhảy bổng lên không
            if (_stateTimer < _jumpWindupDuration)
            {
                float backSpeed = 7.0f;
                if (_targetPlayer != null && Mathf.Abs(transform.position.x - _targetPlayer.position.x) < 3.0f)
                {
                    backSpeed = 9.0f;
                }
                float backX = Mathf.Clamp(transform.position.x + _jumpWindupDir * backSpeed * Time.deltaTime, -9.5f, 9.5f);
                transform.position = new Vector3(backX, _groundY, 0f);
                AutoFaceTarget();
                return;
            }

            // Pha 2: Khởi động cú bật nhảy bổng lên không trung
            if (!_jumpLaunchStarted)
            {
                _jumpLaunchStarted = true;
                SetRollTrailActive(false);
                SetJumpTrailActive(true);
                _jumpStartPos = new Vector3(transform.position.x, _groundY, 0f);
                if (_targetPlayer != null)
                {
                    float dx = Mathf.Clamp(_targetPlayer.position.x - _jumpStartPos.x, -7.5f, 7.5f);
                    _jumpTargetPos = new Vector3(Mathf.Clamp(_jumpStartPos.x + dx, -9.5f, 9.5f), _groundY, 0f);
                }
                else
                {
                    _jumpTargetPos = _jumpStartPos + new Vector3(-2.5f, 0f, 0f);
                }
                AutoFaceTarget();
                PlaySfx(_sfxJump, 0.95f);
                PlayAnimation("Unburrow", true);
            }

            const float jumpDuration = 0.78f;
            float flyElapsed = _stateTimer - _jumpWindupDuration;
            float t = Mathf.Clamp01(flyElapsed / jumpDuration);

            // Quỹ đạo parabol nhảy bổng lên 2.45m (gọn trong khung hình camera) rồi lao thẳng xuống đập trúng người chơi
            float curX = Mathf.Lerp(_jumpStartPos.x, _jumpTargetPos.x, t);
            float curY = _groundY + Mathf.Sin(t * Mathf.PI) * 2.45f;
            transform.position = new Vector3(curX, curY, 0f);
            AutoFaceTarget();

            if (flyElapsed >= jumpDuration)
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
            float distToPlayer = _targetPlayer != null ? Mathf.Abs(transform.position.x - _targetPlayer.position.x) : 999f;
            bool hitsPlayer = _targetPlayer != null && distToPlayer <= 2.9f;
            Vector3 impactPos = hitsPlayer
                ? GetCollisionPointWithPlayer(0.22f)
                : new Vector3(transform.position.x, 0.08f, 0f);

            // Nổ bom lửa + khói đen + Post-Processing + âm thanh tại điểm va chạm
            SpawnImpactFireExplosion(impactPos, 1.1f, 1.0f, 1.05f);
            StreetFighterCamera.TriggerShake(0.45f, 0.42f, 4.5f);

            if (hitsPlayer)
            {
                var receiver = _playerBurnReceiver != null ? _playerBurnReceiver : _targetPlayer.GetComponent<JangHyuBurnReceiver>();
                if (receiver != null && !receiver.IsDead)
                {
                    float hitDir = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
                    if (Mathf.Abs(hitDir) < 0.1f) hitDir = -1f;

                    receiver.TakeDirectDamage(30);
                    receiver.ApplyBurn(3.5f, 0.4f, 1, _flameVfxPrefab);
                    receiver.ApplyKnockback(new Vector3(hitDir * 9.5f, 4.8f, 0f), 3.0f, true);
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
                // Quay mặt (+Z local) hướng ra ngoài màn hình (-Z) và nghiêng 25° về phía Player để luôn nhìn rõ mặt & đồng bộ animation
                float targetY = dirX > 0 ? 155f : 205f;
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
                    SpawnImpactFireExplosion(GetCollisionPointWithPlayer(0.65f), 0.72f, 0.65f, 0.45f);

                    float hitDir = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
                    if (Mathf.Abs(hitDir) < 0.1f) hitDir = -1f;
                    bool shouldKnockdown = _currentState == MonsterAIState.Attack1;
                    receiver.ApplyKnockback(
                        new Vector3(hitDir * (shouldKnockdown ? 7.5f : 3.8f), shouldKnockdown ? 3.2f : 1.2f, 0f),
                        shouldKnockdown ? 3.0f : 0.6f,
                        shouldKnockdown);
                    StreetFighterCamera.TriggerShake(0.18f, 0.16f, 1.5f);
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
            if (_animator != null) _animator.speed = 1f;

            switch (newState)
            {
                case MonsterAIState.Hidden:
                    SetFractureChunksActive(false);
                    SetBodyFlameActive(false);
                    SetRollTrailActive(false, immediateClear: true);
                    SetJumpTrailActive(false, immediateClear: true);
                    SetGroundCrackActive(true);
                    SetWorldHpBarVisible(false);
                    PlayAnimation("Burrow");
                    break;

                case MonsterAIState.Emerge:
                    SetFractureChunksActive(false);
                    SetGroundCrackActive(false);
                    SetBodyFlameActive(true);
                    SetRollTrailActive(true);
                    SetJumpTrailActive(true);
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
                    SetRollTrailActive(false);
                    SetJumpTrailActive(false);
                    SetWorldHpBarVisible(true);
                    AutoFaceTarget();
                    PlayAnimation("BattleIdle");
                    break;

                case MonsterAIState.RollAttack:
                    SetRollTrailActive(true);
                    SetJumpTrailActive(true);
                    _rollChargeStarted = false;
                    float curDist = _targetPlayer != null ? Mathf.Abs(transform.position.x - _targetPlayer.position.x) : 4.5f;
                    _rollWindupDuration = curDist < 3.6f ? 0.28f : 0.16f;
                    if (_targetPlayer != null)
                    {
                        _currentRollDir = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
                        if (Mathf.Abs(_currentRollDir) < 0.1f) _currentRollDir = -1f;
                    }
                    else
                    {
                        _currentRollDir = -1f;
                    }
                    AutoFaceTarget();
                    if (_animator != null) _animator.speed = 2.6f;
                    PlaySfx(_sfxRoll, 0.85f);
                    PlayAnimation("Attack1", true);
                    break;

                case MonsterAIState.JumpSlam:
                    SetRollTrailActive(true);
                    SetBodyFlameActive(true);
                    SetJumpTrailActive(true);
                    _jumpLaunchStarted = false;
                    _jumpWindupDuration = 0.28f;
                    _jumpWindupDir = _targetPlayer != null ? Mathf.Sign(transform.position.x - _targetPlayer.position.x) : 1f;
                    if (Mathf.Abs(_jumpWindupDir) < 0.1f) _jumpWindupDir = 1f;
                    AutoFaceTarget();
                    PlayAnimation("WalkBackward", true);
                    break;

                case MonsterAIState.Recover:
                    SetRollTrailActive(false);
                    SetJumpTrailActive(false);
                    if (_meshVisualRoot != null)
                    {
                        _meshVisualRoot.localRotation = Quaternion.identity;
                    }
                    PlayAnimation("BattleIdle");
                    break;

                case MonsterAIState.CombatIdle:
                    SetRollTrailActive(false);
                    SetJumpTrailActive(false);
                    if (_meshVisualRoot != null)
                    {
                        _meshVisualRoot.localRotation = Quaternion.identity;
                    }
                    PlayAnimation("BattleIdle");
                    break;

                case MonsterAIState.Approach:
                    SetRollTrailActive(true);
                    SetJumpTrailActive(false);
                    PlayAnimation("BattleWalk");
                    break;

                case MonsterAIState.KeepDistance:
                    SetRollTrailActive(true);
                    SetJumpTrailActive(false);
                    PlayAnimation("WalkBackward");
                    break;

                case MonsterAIState.Attack1:
                    SetRollTrailActive(false);
                    SetJumpTrailActive(false);
                    AutoFaceTarget();
                    PlayAnimation("Attack1", true);
                    break;

                case MonsterAIState.Attack2:
                    SetRollTrailActive(false);
                    SetJumpTrailActive(false);
                    AutoFaceTarget();
                    PlayAnimation("Attack2", true);
                    break;

                case MonsterAIState.Damaged:
                    SetRollTrailActive(false);
                    SetJumpTrailActive(false);
                    PlayAnimation("DamagedForward", true);
                    break;

                case MonsterAIState.Groggy:
                    SetRollTrailActive(false);
                    SetJumpTrailActive(false);
                    PlayAnimation("GroggyStart", true);
                    break;

                case MonsterAIState.Dead:
                    TriggerDeathExplosionSequence();
                    break;
            }
        }

        private void TriggerDeathExplosionSequence()
        {
            SetBodyFlameActive(false);
            SetRollTrailActive(false, immediateClear: true);
            SetJumpTrailActive(false, immediateClear: true);
            SetGroundCrackActive(false);
            SetWorldHpBarVisible(false);
            if (_collider != null) _collider.enabled = false;

            Vector3 boomPos = new Vector3(transform.position.x, Mathf.Max(0.15f, transform.position.y + 0.25f), 0f);
            if (_bombExplosionPrefab != null)
            {
                var boom = Instantiate(_bombExplosionPrefab, boomPos, Quaternion.identity);
                boom.transform.localScale = Vector3.one * 1.45f;
                Destroy(boom, 4.0f);
            }
            if (_groundEruptPrefab != null)
            {
                var erupt = Instantiate(_groundEruptPrefab, new Vector3(transform.position.x, 0.05f, 0f), Quaternion.identity);
                erupt.transform.localScale = Vector3.one * 1.25f;
                Destroy(erupt, 3.2f);
            }

            StreetFighterCamera.TriggerShake(0.55f, 0.45f, 5.5f);
            CombatPostProcessController.TriggerExplosion(0.95f);
            PlaySfx(_sfxBombExplosion, 1.0f);

            if (_meshVisualRoot != null)
            {
                _meshVisualRoot.gameObject.SetActive(false);
            }
            // Không bật group1 vì group1 là mesh thân nguyên vẹn; dùng mảnh vỡ debris rời bắn ra từ vùng lửa trung tâm
            SetFractureChunksActive(false);

            if (_deathExplosionCoroutine != null) StopCoroutine(_deathExplosionCoroutine);
            _deathExplosionCoroutine = StartCoroutine(ExplodeFractureChunksRoutine());
        }

        private IEnumerator ExplodeFractureChunksRoutine()
        {
            var tempPieces = new System.Collections.Generic.List<(GameObject go, Vector3 vel, Vector3 ang)>();
            Color[] colors =
            {
                new Color(0.58f, 0.07f, 0.07f, 1f),
                new Color(0.22f, 0.09f, 0.08f, 1f),
                new Color(0.88f, 0.84f, 0.74f, 1f),
                new Color(0.12f, 0.42f, 0.52f, 1f)
            };

            Vector3 origin = transform.position + Vector3.up * 0.55f;
            for (int i = 0; i < 24; i++)
            {
                var piece = GameObject.CreatePrimitive(i % 4 == 2 ? PrimitiveType.Capsule : PrimitiveType.Cube);
                piece.name = "DeadDebrisChunk";
                var col = piece.GetComponent<Collider>();
                if (col != null) Destroy(col);
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.85f + 0.35f;
                dir.Normalize();
                piece.transform.position = origin + dir * Random.Range(0.1f, 0.35f);
                piece.transform.localScale = i % 4 == 2
                    ? new Vector3(0.09f, 0.18f, 0.09f)
                    : new Vector3(Random.Range(0.10f, 0.24f), Random.Range(0.08f, 0.18f), Random.Range(0.10f, 0.22f));
                var mr = piece.GetComponent<MeshRenderer>();
                if (mr != null && mr.material != null) mr.material.color = colors[i % colors.Length];
                Vector3 vel = new Vector3(dir.x * Random.Range(6.5f, 12.0f), dir.y * Random.Range(5.0f, 11.0f), dir.z * Random.Range(2.5f, 6.0f));
                tempPieces.Add((piece, vel, Random.insideUnitSphere * 680f));
            }

            float elapsed = 0f;
            while (elapsed < 1.8f)
            {
                float dt = Time.deltaTime;
                for (int i = 0; i < tempPieces.Count; i++)
                {
                    var tp = tempPieces[i];
                    if (tp.go == null) continue;
                    tp.vel.y -= 18f * dt;
                    tp.go.transform.position += tp.vel * dt;
                    if (tp.go.transform.position.y < 0.06f)
                    {
                        var p = tp.go.transform.position; p.y = 0.06f; tp.go.transform.position = p;
                        tp.vel.x *= 0.82f; tp.vel.y = 0f;
                    }
                    tp.go.transform.Rotate(tp.ang * dt, Space.Self);
                    tempPieces[i] = tp;
                }
                elapsed += dt;
                yield return null;
            }

            foreach (var tp in tempPieces)
            {
                if (tp.go != null) Destroy(tp.go);
            }
            _deathExplosionCoroutine = null;
        }

        private void HandleTakeDamage(int damage)
        {
            if (_currentState == MonsterAIState.Dead) return;
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
