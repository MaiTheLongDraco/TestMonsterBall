using System.Collections;
using Aiara.UI;
using UnityEngine;

namespace Aiara.Combat
{
    [RequireComponent(typeof(Collider))]
    public class JangHyuBurnReceiver : MonoBehaviour
    {
        [Header("Damage & Health")]
        [SerializeField] private int _currentHp = 100;
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private bool _isImmortal = false;

        [Header("Animation")]
        [SerializeField] private Animator _animator;

        [Header("Burn VFX Socket")]
        [SerializeField] private Transform _vfxSocket;

        private Coroutine _burnCoroutine;
        private GameObject _activeBurnVfx;
        private Rigidbody _rigidbody;
        private float _stunnedUntil;
        private float _knockbackFlyUntil;
        private bool _deathTriggered;

        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public bool IsDead => !_isImmortal && _currentHp <= 0;
        public bool IsImmortal
        {
            get => _isImmortal;
            set
            {
                _isImmortal = value;
                if (_isImmortal && _currentHp <= 0)
                {
                    ResetHealth();
                }
            }
        }
        public bool IsBurning => _burnCoroutine != null;
        public bool IsStunned => Time.time < _stunnedUntil;
        public bool IsKnockbackFlying => Time.time < _knockbackFlyUntil;

        public event System.Action<float, float> OnHealthChanged;

        private void Awake()
        {
            _currentHp = _maxHp = 100;
            _rigidbody = GetComponent<Rigidbody>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_vfxSocket == null)
            {
                var chest = transform.Find("grp_joints/jnt_root/jnt_hip/jnt_spine_01/jnt_spine_02");
                _vfxSocket = chest != null ? chest : transform;
            }
        }

        private void Start()
        {
            _currentHp = _maxHp = 100;
            OnHealthChanged?.Invoke(_currentHp, _maxHp);
        }

        public void ApplyBurn(float duration, float tickInterval, int damagePerTick, GameObject burnVfxPrefab = null)
        {
            if (_burnCoroutine != null)
            {
                StopCoroutine(_burnCoroutine);
            }

            _burnCoroutine = StartCoroutine(BurnRoutine(duration, tickInterval, damagePerTick, burnVfxPrefab));
        }

        public void TakeDirectDamage(int damage)
        {
            if (_isImmortal) return;
            if (_currentHp <= 0) return;

            _currentHp = Mathf.Max(0, _currentHp - damage);
            Debug.Log($"[JangHyuBurnReceiver] Nhận trực tiếp {damage} sát thương! Còn: {_currentHp}/{_maxHp}");
            OnHealthChanged?.Invoke(_currentHp, _maxHp);
            if (_currentHp <= 0)
            {
                TriggerDeathState();
            }
        }

        private void TriggerDeathState()
        {
            if (_deathTriggered) return;
            _deathTriggered = true;
            _stunnedUntil = float.MaxValue;
            if (_animator != null)
            {
                _animator.Play("Dead", 0, 0f);
            }
            Debug.Log("[JangHyuBurnReceiver] Player đã chết (Dead) - khóa toàn bộ input!");
        }

        public void ResetHealth()
        {
            _currentHp = _maxHp;
            _stunnedUntil = 0f;
            _knockbackFlyUntil = 0f;
            _deathTriggered = false;
            if (_burnCoroutine != null)
            {
                StopCoroutine(_burnCoroutine);
                _burnCoroutine = null;
            }
            if (_activeBurnVfx != null)
            {
                var binder = _activeBurnVfx.GetComponent<SkinnedMeshVfxBinder>();
                if (binder != null) binder.StopEffect();
                Destroy(_activeBurnVfx);
                _activeBurnVfx = null;
            }
            if (_animator != null)
            {
                _animator.Play("BattleIdle", 0, 0f);
            }
            OnHealthChanged?.Invoke(_currentHp, _maxHp);
        }

        public void ApplyKnockback(Vector3 velocity, float stunDuration = 3.0f, bool playKnockdown = true)
        {
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + stunDuration);
            _knockbackFlyUntil = Time.time + 0.55f;
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.AddForce(velocity, ForceMode.VelocityChange);
            }

            if (_animator != null)
            {
                string stateName = IsDead ? "Dead" : (playKnockdown ? "Knockdown" : "DamagedBackward");
                _animator.Play(stateName, 0, 0f);
            }
        }

        private IEnumerator BurnRoutine(float duration, float tickInterval, int damagePerTick, GameObject burnVfxPrefab)
        {
            if (burnVfxPrefab == null)
            {
#if UNITY_EDITOR
                burnVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/Clean_Flame_VFX.prefab");
#endif
            }

            if (burnVfxPrefab != null && _activeBurnVfx == null)
            {
                _activeBurnVfx = Instantiate(burnVfxPrefab, transform);
                _activeBurnVfx.name = "Player_Burn_VFX";
                _activeBurnVfx.transform.localPosition = Vector3.zero;
                _activeBurnVfx.transform.localRotation = Quaternion.identity;
                _activeBurnVfx.transform.localScale = Vector3.one;

                var bodyTransform = transform.Find("Body_JangHyu_mesh");
                var bodySmr = bodyTransform != null ? bodyTransform.GetComponent<SkinnedMeshRenderer>() : null;
                if (bodySmr == null) bodySmr = GetComponentInChildren<SkinnedMeshRenderer>();
                var binder = _activeBurnVfx.GetComponent<SkinnedMeshVfxBinder>();
                if (binder == null) binder = _activeBurnVfx.AddComponent<SkinnedMeshVfxBinder>();
                binder.Bind(bodySmr, 1.8f, 1000f);
                binder.PlayEffect();
            }

            float elapsed = 0f;
            float nextTick = 0f;

            while (elapsed < duration)
            {
                if (elapsed >= nextTick)
                {
                    if (!_isImmortal && _currentHp > 0)
                    {
                        _currentHp = Mathf.Max(0, _currentHp - damagePerTick);
                        float heightOffset = IsStunned ? 0.95f : 1.65f;
                        Vector3 popupPos = transform.position + new Vector3(UnityEngine.Random.Range(-0.22f, 0.22f), heightOffset, -0.25f);
                        FloatingDamageText.Spawn(popupPos, damagePerTick);
                        Debug.Log($"[JangHyuBurnReceiver] Burn Tick! HP: {_currentHp}/{_maxHp} (-{damagePerTick})");
                        OnHealthChanged?.Invoke(_currentHp, _maxHp);
                        if (_currentHp <= 0)
                        {
                            TriggerDeathState();
                        }
                    }
                    nextTick += tickInterval;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_activeBurnVfx != null)
            {
                Destroy(_activeBurnVfx);
                _activeBurnVfx = null;
            }

            _burnCoroutine = null;
            Debug.Log("[JangHyuBurnReceiver] Burn expired.");
        }

        private void OnDestroy()
        {
            if (_activeBurnVfx != null)
            {
                Destroy(_activeBurnVfx);
            }
        }
    }
}

