using System.Collections;
using UnityEngine;

namespace Aiara.Combat
{
    public enum AmbushState
    {
        Hidden,
        Emerge,
        LandIgnite,
        RollAttack,
        ImpactRecover,
        Idle
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class MonsterBallAmbushController : MonoBehaviour
    {
        [Header("Target & Detection")]
        [SerializeField] private Transform _targetPlayer;
        [SerializeField] private float _triggerDistance = 6.0f;
        [SerializeField] private float _autoEmergeDelay = 1.0f;
        private float _hiddenTimer = 0f;

        [Header("Emerge & Launch")]
        [SerializeField] private float _submergeDepth = 1.5f;
        [SerializeField] private float _jumpForce = 6.5f;
        [SerializeField] private float _launchForwardMultiplier = 1.5f;
        [SerializeField] private Vector3 _groundSpawnPos = new Vector3(0f, 0f, 4.5f);

        [Header("Roll Attack Parameters")]
        [SerializeField] private float _rollSpeed = 10.0f;
        [SerializeField] private float _maxRollDistance = 12.0f;
        [SerializeField] private float _rollTimeout = 2.0f;
        [SerializeField] private float _ballRadius = 0.8f;
        [SerializeField] private Transform _meshVisualRoot;

        [Header("Hit & Burn Reaction")]
        [SerializeField] private float _knockbackForce = 8.0f;
        [SerializeField] private float _knockbackLift = 4.0f;
        [SerializeField] private float _burnDuration = 3.0f;
        [SerializeField] private float _burnTickInterval = 0.3f;
        [SerializeField] private int _burnDamagePerTick = 1;

        [Header("VFX References")]
        [SerializeField] private GameObject _groundCrackVfx;
        [SerializeField] private GameObject _groundEruptVfx;
        [SerializeField] private GameObject _bodyFlameVfx;
        [SerializeField] private GameObject _rollTrailVfx;
        [SerializeField] private GameObject _playerBurnVfxPrefab;

        private Rigidbody _rigidbody;
        private SphereCollider _collider;
        private Animator _animator;
        private AmbushState _currentState = AmbushState.Hidden;
        private Vector3 _rollStartPos;
        private Vector3 _rollDirection;
        private float _stateTimer;
        private bool _hasHitTargetInCurrentRoll;

        public AmbushState CurrentState => _currentState;

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            if (_collider == null) _collider = GetComponent<SphereCollider>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            if (_meshVisualRoot == null)
            {
                var group = transform.Find("Group");
                _meshVisualRoot = group != null ? group : transform;
            }
        }

        private void Start()
        {
            EnsureComponents();
            if (_targetPlayer == null)
            {
                var playerObj = GameObject.Find("JangHyu_AttackCombo2") ?? GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    _targetPlayer = playerObj.transform;
                }
            }

            if (_groundSpawnPos.y < -0.1f || _groundSpawnPos == Vector3.zero)
            {
                _groundSpawnPos = new Vector3(transform.position.x, 0f, transform.position.z);
            }

            InitializeHiddenState();
        }

        public void InitializeHiddenState()
        {
            EnsureComponents();
            _currentState = AmbushState.Hidden;
            _hiddenTimer = 0f;
            if (_groundSpawnPos.y < -0.1f || _groundSpawnPos == Vector3.zero)
            {
                _groundSpawnPos = new Vector3(transform.position.x, 0f, transform.position.z);
            }
            transform.position = _groundSpawnPos - Vector3.up * _submergeDepth;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }
            if (_collider != null) _collider.isTrigger = true;

            SetVfxActive(_groundCrackVfx, false);
            SetVfxActive(_groundEruptVfx, false);
            SetVfxActive(_bodyFlameVfx, false);
            SetVfxActive(_rollTrailVfx, false);
            SetVisualsVisible(false);
        }

        private void SetVisualsVisible(bool visible)
        {
            if (_meshVisualRoot != null)
            {
                _meshVisualRoot.gameObject.SetActive(visible);
            }
            var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                smr.enabled = visible;
            }
        }

        private void Update()
        {
            var p = transform.position;
            if (Mathf.Abs(p.z) > 0.001f)
            {
                p.z = 0f;
                transform.position = p;
            }

            switch (_currentState)
            {
                case AmbushState.Hidden:
                    CheckPlayerProximity();
                    break;

                case AmbushState.Emerge:
                    CheckLanding();
                    break;

                case AmbushState.LandIgnite:
                    UpdateIgniteWindup();
                    break;

                case AmbushState.RollAttack:
                    UpdateRollAttack();
                    break;
            }
        }

        private void CheckPlayerProximity()
        {
            _hiddenTimer += Time.deltaTime;

            if (_targetPlayer != null)
            {
                float distance = Vector3.Distance(_groundSpawnPos, _targetPlayer.position);
                if (distance <= _triggerDistance)
                {
                    StartEmerge();
                    return;
                }
            }

            if (_hiddenTimer >= _autoEmergeDelay)
            {
                StartEmerge();
            }
        }

        public void StartEmerge()
        {
            _currentState = AmbushState.Emerge;
            SetVisualsVisible(true);
            SetVfxActive(_groundCrackVfx, false);

            if (_groundEruptVfx != null)
            {
                _groundEruptVfx.transform.position = _groundSpawnPos;
                _groundEruptVfx.SetActive(true);
            }

            transform.position = _groundSpawnPos;
            _rigidbody.isKinematic = false;
            _collider.isTrigger = false;

            Vector3 toPlayer = _targetPlayer != null ? (_targetPlayer.position - transform.position) : transform.forward;
            toPlayer.y = 0f;
            Vector3 forwardImpulse = toPlayer.normalized * _launchForwardMultiplier;
            _rigidbody.linearVelocity = Vector3.up * _jumpForce + forwardImpulse;

            if (_animator != null)
            {
                _animator.Play("Unburrow", 0, 0f);
            }

            Debug.Log("[MonsterBall] Emerge! Erupting from underground.");
        }

        private void CheckLanding()
        {
            if (_rigidbody.linearVelocity.y <= 0.1f && transform.position.y <= _groundSpawnPos.y + 0.15f)
            {
                OnLanded();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_currentState == AmbushState.Emerge && collision.gameObject.CompareTag("Untagged"))
            {
                OnLanded();
            }
            else if (_currentState == AmbushState.RollAttack)
            {
                HandleRollCollision(collision.collider);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_currentState == AmbushState.RollAttack)
            {
                HandleRollCollision(other);
            }
        }

        private void OnLanded()
        {
            _currentState = AmbushState.LandIgnite;
            _stateTimer = 0.35f;

            Vector3 pos = transform.position;
            pos.y = _groundSpawnPos.y;
            transform.position = pos;
            _rigidbody.linearVelocity = Vector3.zero;

            SetVfxActive(_bodyFlameVfx, true);

            if (_targetPlayer != null)
            {
                Vector3 lookDir = _targetPlayer.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            Debug.Log("[MonsterBall] Landed and ignited body flame!");
        }

        private void UpdateIgniteWindup()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                StartRollAttack();
            }
        }

        public void StartRollAttack()
        {
            _currentState = AmbushState.RollAttack;
            _rollStartPos = transform.position;
            _hasHitTargetInCurrentRoll = false;
            _stateTimer = _rollTimeout;

            if (_targetPlayer != null)
            {
                float dirX = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
                if (Mathf.Abs(dirX) < 0.1f) dirX = -1f;
                _rollDirection = new Vector3(dirX, 0f, 0f);
            }
            else
            {
                _rollDirection = new Vector3(-1f, 0f, 0f);
            }

            transform.rotation = Quaternion.LookRotation(_rollDirection);

            SetVfxActive(_rollTrailVfx, true);

            if (_animator != null)
            {
                _animator.Play("NormalAttack1", 0, 0f);
            }

            Debug.Log("[MonsterBall] Roll Attack started!");
        }

        private void UpdateRollAttack()
        {
            _stateTimer -= Time.deltaTime;
            Vector3 vel = _rollDirection * _rollSpeed;
            vel.y = _rigidbody.linearVelocity.y;
            _rigidbody.linearVelocity = vel;

            if (transform.position.y < _groundSpawnPos.y)
            {
                Vector3 pos = transform.position;
                pos.y = _groundSpawnPos.y;
                transform.position = pos;
            }

            if (_meshVisualRoot != null)
            {
                float angularSpeed = (_rollSpeed / Mathf.Max(0.1f, _ballRadius)) * Mathf.Rad2Deg;
                _meshVisualRoot.Rotate(Vector3.right, angularSpeed * Time.deltaTime, Space.Self);
            }

            float traveled = Vector3.Distance(_rollStartPos, transform.position);
            if (traveled >= _maxRollDistance || _stateTimer <= 0f)
            {
                EndRollAttack();
            }
        }

        private void HandleRollCollision(Collider hitCollider)
        {
            if (_hasHitTargetInCurrentRoll) return;

            var burnReceiver = hitCollider.GetComponentInParent<JangHyuBurnReceiver>();
            if (burnReceiver != null)
            {
                _hasHitTargetInCurrentRoll = true;
                Vector3 knockbackDir = _rollDirection + Vector3.up * (_knockbackLift / _knockbackForce);
                knockbackDir.Normalize();

                burnReceiver.ApplyKnockback(knockbackDir * _knockbackForce);
                burnReceiver.ApplyBurn(_burnDuration, _burnTickInterval, _burnDamagePerTick, _playerBurnVfxPrefab);

                Debug.Log("[MonsterBall] Hit JangHyu! Applied Knockback & Burn DoT.");
                EndRollAttack();
            }
        }

        private void EndRollAttack()
        {
            _currentState = AmbushState.ImpactRecover;
            SetVfxActive(_rollTrailVfx, false);
            _rigidbody.linearVelocity = Vector3.zero;

            if (_animator != null)
            {
                _animator.Play("BattleIdle", 0, 0f);
            }

            StartCoroutine(RecoverRoutine());
        }

        private IEnumerator RecoverRoutine()
        {
            yield return new WaitForSeconds(1.0f);
            _currentState = AmbushState.Idle;

            // Quay mặt về phía người chơi
            if (_targetPlayer != null)
            {
                float dirX = Mathf.Sign(_targetPlayer.position.x - transform.position.x);
                transform.rotation = Quaternion.LookRotation(new Vector3(dirX, 0f, 0f));
            }

            Debug.Log("[MonsterBall] Recovered and returned to Idle. Preparing next attack loop...");

            yield return new WaitForSeconds(1.8f);
            if (_currentState == AmbushState.Idle)
            {
                var health = GetComponent<MonsterBallHealth>();
                if (health == null || !health.IsDead)
                {
                    StartRollAttack();
                }
            }
        }

        private void SetVfxActive(GameObject vfx, bool active)
        {
            if (vfx != null)
            {
                vfx.SetActive(active);
                if (active)
                {
                    var particles = vfx.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in particles)
                    {
                        ps.Play();
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _triggerDistance);
        }
    }
}

