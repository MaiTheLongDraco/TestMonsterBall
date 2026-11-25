using System.Collections;
using UnityEngine;

namespace Aiara.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class JangHyuFightingController : MonoBehaviour
    {
        [Header("Target & Facing")]
        [SerializeField] private Transform _opponent;
        [SerializeField] private float _turnSpeed = 15f;
        [SerializeField] private bool _autoFaceOpponent = true;

        [Header("2.5D Movement Speeds")]
        [SerializeField] private float _forwardWalkSpeed = 4.5f;
        [SerializeField] private float _backwardWalkSpeed = 3.2f;
        [SerializeField] private float _dashSpeed = 10.0f;
        [SerializeField] private float _dashDuration = 0.22f;
        [SerializeField] private float _dashCooldown = 0.6f;

        [Header("Combat & Combos")]
        [SerializeField] private float _attackRange = 2.4f;
        [SerializeField] private int _combo1Damage = 15;
        [SerializeField] private int _combo2Damage = 20;
        [SerializeField] private int _combo3Damage = 35;
        [SerializeField] private int _kickDamage = 25;
        [SerializeField] private float _comboResetWindow = 0.65f;

        // Components
        private Rigidbody _rigidbody;
        private Animator _animator;
        private JangHyuBurnReceiver _burnReceiver;

        // Movement State
        private bool _isDashing;
        private float _lastDashTime = -10f;
        private int _facingDirection = 1; // +1: quay sang phải, -1: quay sang trái

        // Attack & Combo State
        private int _comboStep = 0;
        private float _lastAttackTime = 0f;
        private bool _isAttacking = false;

        public bool IsDashing => _isDashing;
        public bool IsAttacking => _isAttacking;
        public int FacingDirection => _facingDirection;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            _burnReceiver = GetComponent<JangHyuBurnReceiver>();

            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        }

        private void Start()
        {
            FindOpponent();
            LockTo2DPlane();
        }

        private void FindOpponent()
        {
            if (_opponent == null)
            {
                var monster = GameObject.Find("MonsterBall_red") ?? GameObject.FindWithTag("Enemy");
                if (monster != null)
                {
                    _opponent = monster.transform;
                }
            }
        }

        private bool IsActionLocked()
        {
            if (_burnReceiver != null && (_burnReceiver.IsStunned || _burnReceiver.IsDead))
            {
                return true;
            }

            if (_animator == null) return false;

            var cur = _animator.GetCurrentAnimatorStateInfo(0);
            if (IsDownedOrHitState(cur)) return true;

            if (_animator.IsInTransition(0))
            {
                var next = _animator.GetNextAnimatorStateInfo(0);
                if (IsDownedOrHitState(next)) return true;
            }

            return false;
        }

        private static bool IsDownedOrHitState(AnimatorStateInfo state)
        {
            return state.IsName("Knockdown") ||
                   state.IsName("StandUp") ||
                   state.IsName("DamagedBackward") ||
                   state.IsName("Dead");
        }

        private void Update()
        {
            LockTo2DPlane();

            // Khóa hoàn toàn di chuyển, xoay người và ra đòn khi bị ngã / đứng dậy / choáng
            if (IsActionLocked())
            {
                _isAttacking = false;
                _isDashing = false;
                _comboStep = 0;

                if (_rigidbody != null && !_rigidbody.isKinematic)
                {
                    var v = _rigidbody.linearVelocity;
                    v.x = Mathf.MoveTowards(v.x, 0f, Time.deltaTime * 12f);
                    _rigidbody.linearVelocity = v;
                }

                if (_animator != null)
                {
                    _animator.SetFloat("Speed", 0f);
                    _animator.SetBool("IsMoving", false);
                    _animator.SetBool("IsWalkingBackward", false);
                }
                return;
            }

            if (_opponent == null) FindOpponent();

            HandleFacing();

            // Reset combo nếu quá thời gian
            if (_comboStep > 0 && Time.time - _lastAttackTime > _comboResetWindow && !_isAttacking)
            {
                _comboStep = 0;
            }

            // Xử lý tấn công
            if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
            {
                ExecuteComboAttack();
                return;
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                ExecuteKickAttack();
                return;
            }

            // Nếu đang trong đòn đánh, tạm ngừng di chuyển
            if (_isAttacking || _isDashing) return;

            // Xử lý di chuyển
            HandleMovementInput();
        }

        private void LockTo2DPlane()
        {
            var pos = transform.position;
            if (Mathf.Abs(pos.z) > 0.001f)
            {
                pos.z = 0f;
                transform.position = pos;
            }
            if (_rigidbody != null)
            {
                var vel = _rigidbody.linearVelocity;
                vel.z = 0f;
                _rigidbody.linearVelocity = vel;
            }
        }

        private void HandleFacing()
        {
            if (!_autoFaceOpponent || _opponent == null || _isAttacking || _isDashing) return;

            float dx = _opponent.position.x - transform.position.x;
            _facingDirection = dx >= 0f ? 1 : -1;

            float targetYAngle = _facingDirection == 1 ? 90f : 270f;
            Quaternion targetRot = Quaternion.Euler(0f, targetYAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * _turnSpeed);
        }

        private void HandleMovementInput()
        {
            float horizontal = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;

            // Kiểm tra Dash
            bool dashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space);
            if (dashPressed && Mathf.Abs(horizontal) > 0.1f && Time.time >= _lastDashTime + _dashCooldown)
            {
                StartCoroutine(DashRoutine(horizontal));
                return;
            }

            if (Mathf.Abs(horizontal) > 0.1f)
            {
                // Xác định tiến hay lùi so với đối thủ
                // Nếu horizontal cùng dấu với _facingDirection -> TIẾN (Forward)
                // Nếu horizontal ngược dấu với _facingDirection -> LÙI (Backward)
                bool isWalkingForward = (horizontal * _facingDirection) > 0f;
                float speed = isWalkingForward ? _forwardWalkSpeed : _backwardWalkSpeed;

                Vector3 move = new Vector3(horizontal * speed, _rigidbody.linearVelocity.y, 0f);
                _rigidbody.linearVelocity = move;

                if (_animator != null)
                {
                    _animator.SetFloat("Speed", speed);
                    _animator.SetBool("IsMoving", true);
                    _animator.SetBool("IsWalkingBackward", !isWalkingForward);
                }
            }
            else
            {
                _rigidbody.linearVelocity = new Vector3(0f, _rigidbody.linearVelocity.y, 0f);
                if (_animator != null)
                {
                    _animator.SetFloat("Speed", 0f);
                    _animator.SetBool("IsMoving", false);
                    _animator.SetBool("IsWalkingBackward", false);
                }
            }
        }

        private IEnumerator DashRoutine(float dirX)
        {
            _isDashing = true;
            _lastDashTime = Time.time;

            bool isForwardDash = (dirX * _facingDirection) > 0f;
            string clipName = isForwardDash ? "Dash" : "BattleIdle";

            if (_animator != null)
            {
                _animator.Play(clipName, 0, 0f);
            }

            float elapsed = 0f;
            Vector3 dashVel = new Vector3(Mathf.Sign(dirX) * _dashSpeed, 0f, 0f);

            while (elapsed < _dashDuration)
            {
                if (IsActionLocked())
                {
                    _isDashing = false;
                    yield break;
                }
                LockTo2DPlane();
                _rigidbody.linearVelocity = dashVel;
                elapsed += Time.deltaTime;
                yield return null;
            }

            _rigidbody.linearVelocity = Vector3.zero;
            _isDashing = false;
        }

        #region Attack & Combo System

        private void ExecuteComboAttack()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;
            _comboStep = (_comboStep % 3) + 1;

            _rigidbody.linearVelocity = Vector3.zero;

            // Lướt nhẹ về phía trước theo nhịp ra đòn
            _rigidbody.AddForce(new Vector3(_facingDirection * 2.5f, 0f, 0f), ForceMode.Impulse);

            string stateName = $"Combo{_comboStep}";
            if (_animator != null)
            {
                _animator.Play(stateName, 0, 0f);
            }

            int damage = _comboStep == 1 ? _combo1Damage : (_comboStep == 2 ? _combo2Damage : _combo3Damage);
            StartCoroutine(AttackHitCheckRoutine(0.15f, damage));
            StartCoroutine(AttackCooldownRoutine(0.35f));

            Debug.Log($"[JangHyuFighting] Executed Combo {_comboStep}!");
        }

        private void ExecuteKickAttack()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.AddForce(new Vector3(_facingDirection * 3.5f, 0f, 0f), ForceMode.Impulse);

            if (_animator != null)
            {
                _animator.Play("LegAttack", 0, 0f);
            }

            StartCoroutine(AttackHitCheckRoutine(0.2f, _kickDamage));
            StartCoroutine(AttackCooldownRoutine(0.45f));

            Debug.Log("[JangHyuFighting] Executed Kick Attack!");
        }

        private IEnumerator AttackHitCheckRoutine(float delay, int damage)
        {
            yield return new WaitForSeconds(delay);

            if (_opponent != null)
            {
                float dx = _opponent.position.x - transform.position.x;
                bool inFront = (dx * _facingDirection) > 0f;
                float distance = Mathf.Abs(dx);

                if (inFront && distance <= _attackRange)
                {
                    var health = _opponent.GetComponent<MonsterBallHealth>() ?? _opponent.GetComponentInChildren<MonsterBallHealth>();
                    if (health != null && !health.IsDead)
                    {
                        health.TakeDamage(damage);
                        Debug.Log($"[JangHyuFighting] Hit Opponent for {damage} damage! Remaining HP: {health.CurrentHp}/{health.MaxHp}");
                    }
                }
            }
        }

        private IEnumerator AttackCooldownRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            _isAttacking = false;
        }

        #endregion

        // Test API cho Bot tự động điều khiển
        public void BotWalk(float direction)
        {
            if (IsActionLocked() || _isAttacking || _isDashing) return;
            float horizontal = Mathf.Clamp(direction, -1f, 1f);
            bool isWalkingForward = (horizontal * _facingDirection) > 0f;
            float speed = isWalkingForward ? _forwardWalkSpeed : _backwardWalkSpeed;
            _rigidbody.linearVelocity = new Vector3(horizontal * speed, _rigidbody.linearVelocity.y, 0f);
            if (_animator != null)
            {
                _animator.SetFloat("Speed", speed);
                _animator.SetBool("IsMoving", true);
                _animator.SetBool("IsWalkingBackward", !isWalkingForward);
            }
        }

        public void BotStop()
        {
            _rigidbody.linearVelocity = new Vector3(0f, _rigidbody.linearVelocity.y, 0f);
            if (_animator != null)
            {
                _animator.SetFloat("Speed", 0f);
                _animator.SetBool("IsMoving", false);
                _animator.SetBool("IsWalkingBackward", false);
            }
        }

        public void BotDash(float direction)
        {
            if (IsActionLocked()) return;
            if (!_isDashing && Time.time >= _lastDashTime + _dashCooldown)
            {
                StartCoroutine(DashRoutine(direction));
            }
        }

        public void BotAttack()
        {
            if (IsActionLocked()) return;
            if (!_isAttacking)
            {
                ExecuteComboAttack();
            }
        }
    }
}

