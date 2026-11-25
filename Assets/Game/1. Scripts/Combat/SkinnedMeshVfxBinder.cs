using UnityEngine;
using UnityEngine.VFX;

namespace Aiara.Combat
{
    public class SkinnedMeshVfxBinder : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer _targetRenderer;
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _vfxHeight = 1.4f;
        [SerializeField] private float _swarmSpawnRate = 1200f;

        private VisualEffect _vfx;
        private VFXRenderer _vfxRenderer;
        private bool _shouldLoop;
        private float _lastReassertTime = -10f;

        public bool IsPlaying => _shouldLoop && _vfx != null && _vfx.enabled;

        private void Awake()
        {
            _vfx = GetComponentInChildren<VisualEffect>(true);
            _vfxRenderer = GetComponentInChildren<VFXRenderer>(true);
        }

        public void Bind(SkinnedMeshRenderer smr, float vfxHeight = 1.4f, float swarmSpawnRate = 1200f, float swarmSizeMax = 0.55f)
        {
            _targetRenderer = smr;
            _targetTransform = smr != null ? (smr.rootBone != null ? smr.rootBone : smr.transform) : null;
            _vfxHeight = vfxHeight;
            _swarmSpawnRate = swarmSpawnRate;

            if (_vfx == null) _vfx = GetComponentInChildren<VisualEffect>(true);
            if (_vfx == null || _targetRenderer == null) return;

            if (_vfx.HasSkinnedMeshRenderer("SkinnedMesh"))
            {
                _vfx.SetSkinnedMeshRenderer("SkinnedMesh", _targetRenderer);
            }
            if (_vfx.HasTexture("VFXMeshMask")) _vfx.SetTexture("VFXMeshMask", Texture2D.whiteTexture);
            if (_vfx.HasFloat("VFXHeight")) _vfx.SetFloat("VFXHeight", _vfxHeight);
            if (_vfx.HasFloat("SwarmSpawnRate")) _vfx.SetFloat("SwarmSpawnRate", _swarmSpawnRate);
            if (_vfx.HasFloat("SwarmSizeMax")) _vfx.SetFloat("SwarmSizeMax", swarmSizeMax);
            if (_vfx.HasVector2("SwarmSizeMinMax")) _vfx.SetVector2("SwarmSizeMinMax", new Vector2(swarmSizeMax * 0.45f, swarmSizeMax));
            if (_vfx.HasFloat("SmokeSpawnRate")) _vfx.SetFloat("SmokeSpawnRate", 48f);
            if (_vfx.HasVector3("VFXOffset")) _vfx.SetVector3("VFXOffset", Vector3.zero);
            if (_vfx.HasVector3("VFXRotationOffset")) _vfx.SetVector3("VFXRotationOffset", Vector3.zero);
            if (_vfx.HasVector3("BoundsSizeXYZ")) _vfx.SetVector3("BoundsSizeXYZ", _targetRenderer.bounds.size);

            SyncTransform();
        }

        public void PlayEffect()
        {
            if (_vfx == null) _vfx = GetComponentInChildren<VisualEffect>(true);
            if (_vfxRenderer == null) _vfxRenderer = GetComponentInChildren<VFXRenderer>(true);

            _shouldLoop = true;
            _lastReassertTime = Time.unscaledTime;

            if (_vfxRenderer != null)
            {
                _vfxRenderer.allowOcclusionWhenDynamic = false;
            }

            if (_vfx != null)
            {
                _vfx.enabled = true;
                _vfx.initialEventName = "loop";
                _vfx.Reinit();
                _vfx.Play();
                _vfx.SendEvent("create");
                _vfx.SendEvent("loop");
            }

            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Play();
            }
        }

        public void StopEffect()
        {
            _shouldLoop = false;
            if (_vfx == null) _vfx = GetComponentInChildren<VisualEffect>(true);
            if (_vfx != null)
            {
                _vfx.SendEvent("stop");
                _vfx.SendEvent("end");
                _vfx.Stop();
            }

            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop();
            }
        }

        private void LateUpdate()
        {
            SyncTransform();

            if (_shouldLoop && _vfx != null && _vfx.enabled)
            {
                // Tự kích hoạt lại event "loop" nếu VFX Graph bị reset hoặc tắt hạt giữa chừng
                if (_vfx.aliveParticleCount == 0 && Time.unscaledTime - _lastReassertTime >= 0.4f)
                {
                    _lastReassertTime = Time.unscaledTime;
                    _vfx.SendEvent("loop");
                }
            }

            if (_shouldLoop)
            {
                foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (ps.main.loop && !ps.isPlaying)
                    {
                        ps.Play();
                    }
                }
            }
        }

        private void SyncTransform()
        {
            if (_vfx == null || _targetTransform == null) return;

            Vector3 localPos = _vfx.transform.InverseTransformPoint(_targetTransform.position);
            Quaternion localRot = Quaternion.Inverse(_vfx.transform.rotation) * _targetTransform.rotation;
            Vector3 vScale = _vfx.transform.lossyScale;
            Vector3 tScale = _targetTransform.lossyScale;
            Vector3 localScale = new Vector3(
                Mathf.Abs(vScale.x) > 1e-5f ? tScale.x / vScale.x : 1f,
                Mathf.Abs(vScale.y) > 1e-5f ? tScale.y / vScale.y : 1f,
                Mathf.Abs(vScale.z) > 1e-5f ? tScale.z / vScale.z : 1f);

            if (_vfx.HasVector3("SkinnedMeshTransform_position"))
                _vfx.SetVector3("SkinnedMeshTransform_position", localPos);
            if (_vfx.HasVector3("SkinnedMeshTransform_angles"))
                _vfx.SetVector3("SkinnedMeshTransform_angles", localRot.eulerAngles);
            if (_vfx.HasVector3("SkinnedMeshTransform_scale"))
                _vfx.SetVector3("SkinnedMeshTransform_scale", localScale);
        }
    }
}
