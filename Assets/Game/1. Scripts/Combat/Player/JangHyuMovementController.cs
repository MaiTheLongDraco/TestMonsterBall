using UnityEngine;

namespace Aiara.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class JangHyuMovementController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 4.5f;
        [SerializeField] private float _dashSpeed = 9.0f;
        [SerializeField] private float _dashDuration = 0.25f;
        [SerializeField] private float _dashCooldown = 0.8f;
        [SerializeField] private float _turnSpeed = 15f;

        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private JangHyuBurnReceiver _burnReceiver;

        private Rigidbody _rigidbody;
        private Vector3 _moveInput;
        private bool _isDashing;
        private float _dashEndTime;
        private float _nextDashTime;
        private Vector3 _dashDirection;
        private float _currentFacingY = 90f;

        public bool IsMoving => _moveInput.sqrMagnitude > 0.01f;
        public bool IsDashing => _isDashing;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_burnReceiver == null) _burnReceiver = GetComponent<JangHyuBurnReceiver>();
            _currentFacingY = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (_burnReceiver != null && _burnReceiver.IsStunned)
            {
                _moveInput = Vector3.zero;
                UpdateAnimator(0f);
                return;
            }

            ReadInput();
            HandleDashInput();
            UpdateRotation();
        }

        private void FixedUpdate()
        {
            if (_burnReceiver != null && _burnReceiver.IsStunned)
            {
                return;
            }

            ApplyMovement();
        }

        private void ReadInput()
        {
            float h = 0f;
            float v = 0f;

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;

            if (Mathf.Approximately(h, 0f)) h = Input.GetAxisRaw("Horizontal");
            if (Mathf.Approximately(v, 0f)) v = Input.GetAxisRaw("Vertical");

            _moveInput = new Vector3(h, 0f, v).normalized;
        }

        private void HandleDashInput()
        {
            if (Time.time < _dashEndTime)
            {
                _isDashing = true;
            }
            else
            {
                _isDashing = false;
            }

            if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space)) && Time.time >= _nextDashTime)
            {
                _dashDirection = _moveInput.sqrMagnitude > 0.01f ? _moveInput : (transform.forward);
                _dashEndTime = Time.time + _dashDuration;
                _nextDashTime = Time.time + _dashCooldown;
                _isDashing = true;
            }
        }

        private void ApplyMovement()
        {
            float speed = _isDashing ? _dashSpeed : _walkSpeed;
            Vector3 targetVelocity;

            if (_isDashing)
            {
                targetVelocity = _dashDirection * speed;
            }
            else if (_moveInput.sqrMagnitude > 0.01f)
            {
                targetVelocity = _moveInput * speed;
            }
            else
            {
                targetVelocity = Vector3.zero;
            }

            _rigidbody.linearVelocity = new Vector3(targetVelocity.x, _rigidbody.linearVelocity.y, targetVelocity.z);

            float currentHorizontalSpeed = new Vector2(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.z).magnitude;
            UpdateAnimator(currentHorizontalSpeed);
        }

        private void UpdateRotation()
        {
            if (_moveInput.sqrMagnitude > 0.05f)
            {
                if (_moveInput.x > 0.1f)
                {
                    _currentFacingY = 90f;
                }
                else if (_moveInput.x < -0.1f)
                {
                    _currentFacingY = 270f;
                }
            }

            Quaternion targetRotation = Quaternion.Euler(0f, _currentFacingY, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _turnSpeed);
        }

        private void UpdateAnimator(float speed)
        {
            if (_animator != null)
            {
                _animator.SetFloat("Speed", speed);
                _animator.SetBool("IsMoving", speed > 0.1f);
            }
        }
    }
}

