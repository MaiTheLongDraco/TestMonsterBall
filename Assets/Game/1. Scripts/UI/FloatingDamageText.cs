using UnityEngine;
using UnityEngine.UI;

namespace Aiara.UI
{
    public sealed class FloatingDamageText : MonoBehaviour
    {
        private const float Lifetime = 0.9f;
        private const float RiseSpeed = 1.45f;

        private Text _label;
        private Outline _outline;
        private float _elapsed;
        private Vector3 _velocity;
        private Color _baseColor;
        private Camera _mainCamera;

        public static int ActiveCount { get; private set; }
        public static int TotalSpawnedCount { get; private set; }

        public static FloatingDamageText Spawn(Vector3 worldPos, int damageAmount, Color? color = null)
        {
            var go = new GameObject($"BurnDamageText_{damageAmount}");
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one * 0.0085f;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 150;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 80f);

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            txtGo.transform.SetParent(go.transform, false);
            var tRt = (RectTransform)txtGo.transform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            var txt = txtGo.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = $"-{Mathf.Abs(damageAmount)}";
            txt.fontSize = 36;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            Color c = color ?? new Color(1f, 0.36f, 0.08f, 1f);
            txt.color = c;

            var outline = txtGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.01f, 0f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            var popup = go.AddComponent<FloatingDamageText>();
            popup._label = txt;
            popup._outline = outline;
            popup._baseColor = c;
            popup._velocity = new Vector3(Random.Range(-0.18f, 0.18f), RiseSpeed, 0f);
            popup._mainCamera = Camera.main;

            ActiveCount++;
            TotalSpawnedCount++;
            return popup;
        }

        private void LateUpdate()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Lifetime);

            // Bay lên mượt mà và chậm dần về cuối
            float easeMul = 1f - 0.55f * t;
            transform.position += _velocity * (easeMul * Time.deltaTime);

            // Hiệu ứng nảy nhẹ lúc mới xuất hiện
            float pop = t < 0.15f ? Mathf.Lerp(1.35f, 1.0f, t / 0.15f) : 1.0f;
            transform.localScale = Vector3.one * (0.0085f * pop);

            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward, _mainCamera.transform.up);
            }

            // Mờ dần ở nửa sau vòng đời
            float alpha = t < 0.45f ? 1f : Mathf.Clamp01(1f - (t - 0.45f) / 0.55f);
            if (_label != null)
            {
                Color c = _baseColor;
                c.a = alpha;
                _label.color = c;
            }
            if (_outline != null)
            {
                Color oc = _outline.effectColor;
                oc.a = alpha * 0.95f;
                _outline.effectColor = oc;
            }

            if (_elapsed >= Lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (ActiveCount > 0) ActiveCount--;
        }
    }
}
