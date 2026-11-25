using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aiara.Combat
{
    /// <summary>
    /// Quản lý Volume Post-Processing chiến đấu (ACES Tonemapping, Bloom, Vignette, ChromaticAberration)
    /// và hiệu ứng xung kích sáng/méo khung hình khi MonsterBall nổ bom hoặc va chạm mạnh.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatPostProcessController : MonoBehaviour
    {
        [Header("Baseline Combat Look")]
        [SerializeField] private float _baseBloomIntensity = 0.75f;
        [SerializeField] private float _baseBloomThreshold = 0.85f;
        [SerializeField] private float _baseVignetteIntensity = 0.24f;
        [SerializeField] private float _baseChromaticIntensity = 0.04f;
        [SerializeField] private float _baseExposure = 0.05f;
        [SerializeField] private float _baseContrast = 12f;
        [SerializeField] private float _baseSaturation = 12f;

        private Volume _volume;
        private VolumeProfile _runtimeProfile;
        private Bloom _bloom;
        private ChromaticAberration _chromatic;
        private Vignette _vignette;
        private LensDistortion _lensDistortion;
        private ColorAdjustments _colorAdjustments;
        private Tonemapping _tonemapping;

        private float _pulseLevel;

        public static CombatPostProcessController Instance { get; private set; }

        public float CurrentBloomIntensity => _bloom != null ? _bloom.intensity.value : 0f;
        public float CurrentPulseLevel => _pulseLevel;

        public static void TriggerExplosion(float strength = 1.0f)
        {
            if (Instance != null)
            {
                Instance.TriggerExplosionPulse(strength);
            }
        }

        public static void TriggerHit(float strength = 0.45f)
        {
            if (Instance != null)
            {
                Instance.TriggerHitPulse(strength);
            }
        }

        private void Awake()
        {
            Instance = this;
            EnsureVolumeConfigured();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsureVolumeConfigured()
        {
            _volume = GetComponent<Volume>();
            if (_volume == null) _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 20f;
            _volume.weight = 1f;

            if (_runtimeProfile == null)
            {
                _runtimeProfile = _volume.sharedProfile != null
                    ? Instantiate(_volume.sharedProfile)
                    : ScriptableObject.CreateInstance<VolumeProfile>();
                _runtimeProfile.name = "CombatRuntimeVolumeProfile";
                _volume.profile = _runtimeProfile;
            }

            _tonemapping = GetOrAdd<Tonemapping>(_runtimeProfile);
            _tonemapping.active = true;
            _tonemapping.mode.overrideState = true;
            _tonemapping.mode.value = TonemappingMode.ACES;

            _bloom = GetOrAdd<Bloom>(_runtimeProfile);
            _bloom.active = true;
            _bloom.threshold.overrideState = true;
            _bloom.threshold.value = _baseBloomThreshold;
            _bloom.intensity.overrideState = true;
            _bloom.intensity.value = _baseBloomIntensity;
            _bloom.scatter.overrideState = true;
            _bloom.scatter.value = 0.65f;

            _vignette = GetOrAdd<Vignette>(_runtimeProfile);
            _vignette.active = true;
            _vignette.intensity.overrideState = true;
            _vignette.intensity.value = _baseVignetteIntensity;
            _vignette.smoothness.overrideState = true;
            _vignette.smoothness.value = 0.45f;

            _chromatic = GetOrAdd<ChromaticAberration>(_runtimeProfile);
            _chromatic.active = true;
            _chromatic.intensity.overrideState = true;
            _chromatic.intensity.value = _baseChromaticIntensity;

            _lensDistortion = GetOrAdd<LensDistortion>(_runtimeProfile);
            _lensDistortion.active = true;
            _lensDistortion.intensity.overrideState = true;
            _lensDistortion.intensity.value = 0f;

            _colorAdjustments = GetOrAdd<ColorAdjustments>(_runtimeProfile);
            _colorAdjustments.active = true;
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.postExposure.value = _baseExposure;
            _colorAdjustments.contrast.overrideState = true;
            _colorAdjustments.contrast.value = _baseContrast;
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.saturation.value = _baseSaturation;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing)) return existing;
            return profile.Add<T>(true);
        }

        public void TriggerExplosionPulse(float strength = 1.0f)
        {
            EnsureVolumeConfigured();
            _pulseLevel = Mathf.Max(_pulseLevel, Mathf.Clamp(strength, 0f, 1.5f));
            ApplyCurrentOverrides();
        }

        public void TriggerHitPulse(float strength = 0.45f)
        {
            EnsureVolumeConfigured();
            _pulseLevel = Mathf.Max(_pulseLevel, Mathf.Clamp(strength, 0f, 1.0f));
            ApplyCurrentOverrides();
        }

        private void Update()
        {
            if (_pulseLevel <= 0f) return;

            _pulseLevel = Mathf.MoveTowards(_pulseLevel, 0f, Time.deltaTime * 2.2f);
            ApplyCurrentOverrides();
        }

        private void ApplyCurrentOverrides()
        {
            float p = _pulseLevel;
            if (_bloom != null) _bloom.intensity.value = _baseBloomIntensity + p * 1.45f;
            if (_chromatic != null) _chromatic.intensity.value = _baseChromaticIntensity + p * 0.35f;
            if (_vignette != null) _vignette.intensity.value = _baseVignetteIntensity + p * 0.16f;
            if (_lensDistortion != null) _lensDistortion.intensity.value = -0.14f * p;
            if (_colorAdjustments != null) _colorAdjustments.postExposure.value = _baseExposure + p * 0.45f;
        }
    }
}
