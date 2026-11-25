using UnityEngine;

namespace Aiara.Combat
{
    public class StreetFighterCamera : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Transform _player;
        [SerializeField] private Transform _opponent;

        [Header("Framing & Offset")]
        [SerializeField] private float _baseDistance = 5.2f;
        [SerializeField] private float _zoomMultiplier = 0.55f;
        [SerializeField] private float _baseHeight = 1.45f;
        [SerializeField] private float _heightMultiplier = 0.08f;
        [SerializeField] private float _lookAtHeight = 1.05f;

        [Header("Smoothing")]
        [SerializeField] private float _smoothSpeed = 8.0f;
        [SerializeField] private float _minX = -12f;
        [SerializeField] private float _maxX = 12f;

        [Header("Screen Shake & Impact Punch")]
        [SerializeField] private float _baseFov = 42f;

        private Camera _camera;
        private Vector3 _smoothedBasePosition;
        private bool _hasBasePosition;

        private float _shakeAmplitude;
        private float _shakeDuration;
        private float _shakeElapsed;
        private float _fovKick;

        public static StreetFighterCamera Instance { get; private set; }

        public static void TriggerShake(float amplitude = 0.35f, float duration = 0.32f, float fovKick = 3.0f)
        {
            if (Instance != null)
            {
                Instance.Shake(amplitude, duration, fovKick);
            }
        }

        public void Shake(float amplitude, float duration, float fovKick = 0f)
        {
            _shakeAmplitude = Mathf.Max(_shakeAmplitude, amplitude);
            _shakeDuration = Mathf.Max(_shakeDuration - _shakeElapsed, duration);
            _shakeElapsed = 0f;
            _fovKick = Mathf.Max(_fovKick, fovKick);
        }

        private void Awake()
        {
            Instance = this;
            _camera = GetComponent<Camera>();
            if (_camera != null) _baseFov = _camera.fieldOfView;
        }

        private void Start()
        {
            FindTargets();
            _smoothedBasePosition = transform.position;
            _hasBasePosition = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void FindTargets()
        {
            if (_player == null)
            {
                var p = GameObject.Find("JangHyu_AttackCombo2") ?? GameObject.FindWithTag("Player");
                if (p != null) _player = p.transform;
            }

            if (_opponent == null)
            {
                var m = GameObject.Find("MonsterBall_red") ?? GameObject.FindWithTag("Enemy");
                if (m != null) _opponent = m.transform;
            }
        }

        private void LateUpdate()
        {
            if (_player == null || _opponent == null)
            {
                FindTargets();
                if (_player == null) return;
            }

            Vector3 p1 = _player.position;
            Vector3 p2 = _opponent != null ? _opponent.position : p1 + Vector3.right * 4f;

            float midX = Mathf.Clamp((p1.x + p2.x) * 0.5f, _minX, _maxX);
            float dist = Mathf.Abs(p1.x - p2.x);

            float targetZ = -(_baseDistance + dist * _zoomMultiplier);
            float targetY = _baseHeight + dist * _heightMultiplier;

            Vector3 desiredPosition = new Vector3(midX, targetY, targetZ);
            if (!_hasBasePosition)
            {
                _smoothedBasePosition = transform.position;
                _hasBasePosition = true;
            }
            _smoothedBasePosition = Vector3.Lerp(_smoothedBasePosition, desiredPosition, Time.deltaTime * _smoothSpeed);

            Vector3 shakeOffset = Vector3.zero;
            float rollAngle = 0f;
            if (_shakeDuration > 0f && _shakeElapsed < _shakeDuration)
            {
                _shakeElapsed += Time.deltaTime;
                float t = 1f - Mathf.Clamp01(_shakeElapsed / _shakeDuration);
                float damper = t * t;
                float freq = Time.unscaledTime * 48f;
                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(freq, 0.3f) - 0.5f) * 2f * _shakeAmplitude * damper,
                    (Mathf.PerlinNoise(0.7f, freq) - 0.5f) * 2f * _shakeAmplitude * damper,
                    0f);
                rollAngle = (Mathf.PerlinNoise(freq * 0.8f, 3.1f) - 0.5f) * 2f * (_shakeAmplitude * 5.0f) * damper;
            }
            else
            {
                _shakeAmplitude = 0f;
                _shakeDuration = 0f;
            }

            if (_camera != null)
            {
                _fovKick = Mathf.MoveTowards(_fovKick, 0f, Time.deltaTime * 16f);
                _camera.fieldOfView = _baseFov + _fovKick;
            }

            transform.position = _smoothedBasePosition + shakeOffset;

            Vector3 lookTarget = new Vector3(midX, _lookAtHeight, 0f);
            Quaternion baseRot = Quaternion.LookRotation(lookTarget - _smoothedBasePosition);
            transform.rotation = baseRot * Quaternion.Euler(0f, 0f, rollAngle);
        }
    }
}

