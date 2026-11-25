using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Aiara.Combat;

namespace Aiara.UI
{
    [ExecuteAlways]
    public class CombatUIManager : MonoBehaviour
    {
        [Header("Target References")]
        [SerializeField] private JangHyuBurnReceiver _playerReceiver;
        [SerializeField] private MonsterBallHealth _monsterHealth;
        [SerializeField] private MonsterBallAIController _monsterAI;

        [Header("Player HUD Colors")]
        [SerializeField] private Color _playerHpColor = new Color(0.18f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color _playerMpColor = new Color(0.2f, 0.65f, 0.95f, 1f);
        [SerializeField] private Color _playerExpColor = new Color(1.0f, 0.8f, 0.15f, 1f);

        [Header("Monster Health Colors")]
        [SerializeField] private Color _monsterHpColor = new Color(0.95f, 0.15f, 0.15f, 1f);

        [Header("Player HUD UI Elements")]
        [SerializeField] public Canvas ScreenCanvas;
        [SerializeField] public Image PlayerHpFill;
        [SerializeField] public Image PlayerHpFillLerp;
        [SerializeField] public Text PlayerHpText;
        [SerializeField] public Image PlayerMpFill;
        [SerializeField] public Text PlayerMpText;

        [Header("Monster World Bar UI Elements")]
        [SerializeField] public Canvas MonsterWorldCanvas;
        [SerializeField] public Transform MonsterBarRoot;
        [SerializeField] public Image MonsterHpFill;
        [SerializeField] public Image MonsterHpFillLerp;
        [SerializeField] public Text MonsterHpText;
        [SerializeField] public Text MonsterNameText;

        [Header("Top-Right Control Panel Elements")]
        [SerializeField] public Image AiCheckmark;
        [SerializeField] public Text AiStatusLabel;
        [SerializeField] public Text AiStatusText;

        private float _targetPlayerHpRatio = 1f;
        private float _targetMonsterHpRatio = 1f;
        private Camera _mainCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeOnPlay()
        {
            var existing = Object.FindFirstObjectByType<CombatUIManager>();
            if (existing == null)
            {
                var go = new GameObject("Combat_UI_Manager");
                go.AddComponent<CombatUIManager>();
                Debug.Log("[CombatUIManager] Auto-created Combat_UI_Manager on play!");
            }
        }

        public static CombatUIManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            _mainCamera = Camera.main;
            FindReferences();
            EnsureUIBuilt();
        }

        public void SetMonsterHpBarVisible(bool visible)
        {
            if (MonsterBarRoot != null)
            {
                MonsterBarRoot.gameObject.SetActive(visible);
            }
        }

        private void OnEnable()
        {
            FindReferences();
            EnsureUIBuilt();
            if (_playerReceiver != null)
            {
                _playerReceiver.OnHealthChanged += HandlePlayerHealthChanged;
            }
            if (_monsterHealth != null)
            {
                _monsterHealth.OnHealthChanged += HandleMonsterHealthChanged;
            }
        }

        private void OnDisable()
        {
            if (_playerReceiver != null)
            {
                _playerReceiver.OnHealthChanged -= HandlePlayerHealthChanged;
            }
            if (_monsterHealth != null)
            {
                _monsterHealth.OnHealthChanged -= HandleMonsterHealthChanged;
            }
        }

        public void FindReferences()
        {
            if (_playerReceiver == null)
            {
                var p = GameObject.Find("JangHyu_AttackCombo2") ?? GameObject.FindWithTag("Player");
                if (p != null) _playerReceiver = p.GetComponent<JangHyuBurnReceiver>();
            }

            if (_monsterHealth == null || _monsterAI == null)
            {
                var m = GameObject.Find("MonsterBall_red") ?? GameObject.FindWithTag("Enemy");
                if (m != null)
                {
                    if (_monsterHealth == null) _monsterHealth = m.GetComponent<MonsterBallHealth>();
                    if (_monsterAI == null) _monsterAI = m.GetComponent<MonsterBallAIController>();
                }
            }
        }

        private void Update()
        {
            FindReferences();

            // Mượt mà thanh máu (Smooth Lerp)
            if (PlayerHpFill != null)
            {
                PlayerHpFill.fillAmount = Mathf.MoveTowards(PlayerHpFill.fillAmount, _targetPlayerHpRatio, Time.deltaTime * 3.5f);
            }
            if (PlayerHpFillLerp != null)
            {
                PlayerHpFillLerp.fillAmount = Mathf.MoveTowards(PlayerHpFillLerp.fillAmount, PlayerHpFill != null ? PlayerHpFill.fillAmount : _targetPlayerHpRatio, Time.deltaTime * 0.9f);
            }

            if (MonsterHpFill != null)
            {
                MonsterHpFill.fillAmount = Mathf.MoveTowards(MonsterHpFill.fillAmount, _targetMonsterHpRatio, Time.deltaTime * 3.5f);
            }
            if (MonsterHpFillLerp != null)
            {
                MonsterHpFillLerp.fillAmount = Mathf.MoveTowards(MonsterHpFillLerp.fillAmount, MonsterHpFill != null ? MonsterHpFill.fillAmount : _targetMonsterHpRatio, Time.deltaTime * 0.9f);
            }

            // Quản lý ẩn hiện thanh máu quái vật
            if (MonsterBarRoot != null)
            {
                bool isHidden = (_monsterAI != null && _monsterAI.CurrentState == MonsterAIState.Hidden);
                bool isDead = (_monsterHealth != null && _monsterHealth.IsDead);
                bool shouldShow = !isHidden && !isDead;
                if (MonsterBarRoot.gameObject.activeSelf != shouldShow)
                {
                    MonsterBarRoot.gameObject.SetActive(shouldShow);
                }
            }

            // Đồng bộ trạng thái Checkbox AI
            UpdateAICheckboxVisual();
        }

        private void UpdateAICheckboxVisual()
        {
            if (_monsterAI != null && AiCheckmark != null)
            {
                AiCheckmark.gameObject.SetActive(_monsterAI.IsAIEnabled);
            }
        }

        private void LateUpdate()
        {
            // World Space Health Bar luôn bám theo đỉnh đầu con quái vật và quay mặt về phía Camera
            if (MonsterWorldCanvas != null)
            {
                Transform targetTransform = _monsterHealth != null ? _monsterHealth.transform : null;
                if (targetTransform == null && _monsterAI != null) targetTransform = _monsterAI.transform;
                if (targetTransform == null)
                {
                    var m = GameObject.Find("MonsterBall_red");
                    if (m != null) targetTransform = m.transform;
                }

                if (targetTransform != null)
                {
                    // Lơ lửng ngay trên đỉnh đầu quái vật (chiều cao MonsterBall ~1.34m)
                    MonsterWorldCanvas.transform.position = targetTransform.position + Vector3.up * 1.65f;
                    MonsterWorldCanvas.transform.localScale = Vector3.one * 0.008f;
                }

                if (_mainCamera == null) _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    MonsterWorldCanvas.transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward, _mainCamera.transform.up);
                }
            }
        }

        private void HandlePlayerHealthChanged(float current, float max)
        {
            _targetPlayerHpRatio = Mathf.Clamp01(current / max);
            string txt = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            if (PlayerHpText != null) PlayerHpText.text = txt;
        }

        private void HandleMonsterHealthChanged(float current, float max)
        {
            _targetMonsterHpRatio = Mathf.Clamp01(current / max);
            string txt = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            if (MonsterHpText != null) MonsterHpText.text = txt;
        }

        public void EnsureUIBuilt()
        {
            // 1. Kiểm tra / Tự dựng Player_HUD_Canvas (UI tĩnh)
            var pCanvasGo = GameObject.Find("Player_HUD_Canvas");
            if (pCanvasGo == null)
            {
                pCanvasGo = BuildPlayerHUDCanvas();
            }
            if (pCanvasGo != null)
            {
                ScreenCanvas = pCanvasGo.GetComponent<Canvas>();
                BindPlayerHUDElements(pCanvasGo);
            }

            // 2. Kiểm tra / Tự dựng Monster_World_Canvas (UI World Space)
            var mCanvasGo = GameObject.Find("Monster_World_Canvas");
            if (mCanvasGo == null)
            {
                mCanvasGo = BuildMonsterWorldCanvas();
            }
            if (mCanvasGo != null)
            {
                MonsterWorldCanvas = mCanvasGo.GetComponent<Canvas>();
                BindMonsterWorldElements(mCanvasGo);
            }
        }

        private GameObject BuildPlayerHUDCanvas()
        {
            var go = new GameObject("Player_HUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 100;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Root HUD
            var root = new GameObject("Player_HUD_Root", typeof(RectTransform));
            root.transform.SetParent(go.transform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0f, 1f);
            rootRt.anchoredPosition = new Vector2(40f, -40f);
            rootRt.sizeDelta = new Vector2(500f, 120f);

            // Avatar Ring & Face
            var av = new GameObject("Avatar_Ring", typeof(RectTransform), typeof(Image));
            av.transform.SetParent(root.transform, false);
            var avRt = (RectTransform)av.transform;
            avRt.anchorMin = new Vector2(0f, 0.5f);
            avRt.anchorMax = new Vector2(0f, 0.5f);
            avRt.pivot = new Vector2(0f, 0.5f);
            avRt.anchoredPosition = Vector2.zero;
            avRt.sizeDelta = new Vector2(96f, 96f);
            av.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.15f, 0.95f);

            var face = new GameObject("Avatar_Face", typeof(RectTransform), typeof(Image));
            face.transform.SetParent(av.transform, false);
            var fRt = (RectTransform)face.transform;
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(6f, 6f);
            fRt.offsetMax = new Vector2(-6f, -6f);
            face.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.55f, 1f);

            // Player Name (Thư pháp 장휴 (長休))
            var nameGo = new GameObject("Player_Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(root.transform, false);
            var nRt = (RectTransform)nameGo.transform;
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(0f, 1f);
            nRt.pivot = new Vector2(0f, 1f);
            nRt.anchoredPosition = new Vector2(120f, -6f);
            nRt.sizeDelta = new Vector2(250f, 28f);
            var nameTxt = nameGo.GetComponent<Text>();
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameTxt.text = "장휴 (長休)";
            nameTxt.fontSize = 22;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.color = Color.white;
            nameTxt.alignment = TextAnchor.MiddleLeft;

            // HP Bar (Xanh lá #2ED94D)
            BuildBar(root.transform, "HP_Bar", new Vector2(360f, 24f), new Vector2(120f, -38f),
                new Color(0.18f, 0.85f, 0.3f), new Color(1f, 0.82f, 0.2f, 0.85f), "100 / 100");

            // MP Bar (Xanh dương #33A6F2)
            BuildBar(root.transform, "MP_Bar", new Vector2(320f, 14f), new Vector2(120f, -68f),
                new Color(0.2f, 0.65f, 0.95f), new Color(0.5f, 0.8f, 1f, 0.6f), "50 / 50");

            // EXP Bar (Vàng cam)
            BuildBar(root.transform, "EXP_Bar", new Vector2(160f, 8f), new Vector2(120f, -88f),
                new Color(1f, 0.8f, 0.15f), new Color(1f, 0.8f, 0.15f), "");

            // Top-Right Control Panel (Khớp 100% video gốc)
            BuildTopRightControlPanel(go.transform);

            return go;
        }

        private void BuildBar(Transform parent, string name, Vector2 size, Vector2 pos, Color mainColor, Color lerpColor, string defaultText)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            bar.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            var lerp = new GameObject("LerpFill", typeof(RectTransform), typeof(Image));
            lerp.transform.SetParent(bar.transform, false);
            var lRt = (RectTransform)lerp.transform;
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(2f, 2f); lRt.offsetMax = new Vector2(-2f, -2f);
            var lImg = lerp.GetComponent<Image>();
            lImg.color = lerpColor;
            lImg.type = Image.Type.Filled;
            lImg.fillMethod = Image.FillMethod.Horizontal;
            lImg.fillAmount = 1f;

            var main = new GameObject("MainFill", typeof(RectTransform), typeof(Image));
            main.transform.SetParent(bar.transform, false);
            var mRt = (RectTransform)main.transform;
            mRt.anchorMin = Vector2.zero; mRt.anchorMax = Vector2.one;
            mRt.offsetMin = new Vector2(2f, 2f); mRt.offsetMax = new Vector2(-2f, -2f);
            var mImg = main.GetComponent<Image>();
            mImg.color = mainColor;
            mImg.type = Image.Type.Filled;
            mImg.fillMethod = Image.FillMethod.Horizontal;
            mImg.fillAmount = 1f;

            if (!string.IsNullOrEmpty(defaultText))
            {
                var txtGo = new GameObject("ValueText", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(bar.transform, false);
                var tRt = (RectTransform)txtGo.transform;
                tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
                tRt.sizeDelta = Vector2.zero;
                var txt = txtGo.GetComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.text = defaultText;
                txt.fontSize = Mathf.Max(10, (int)(size.y * 0.65f));
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
            }
        }

        private void BuildTopRightControlPanel(Transform canvasT)
        {
            var panel = new GameObject("TopRight_Control_Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasT, false);
            var pRt = (RectTransform)panel.transform;
            pRt.anchorMin = new Vector2(1f, 1f);
            pRt.anchorMax = new Vector2(1f, 1f);
            pRt.pivot = new Vector2(1f, 1f);
            pRt.anchoredPosition = new Vector2(-20f, -20f);
            pRt.sizeDelta = new Vector2(280f, 120f);

            // 1. Dropdown Box hiển thị "MonsterBallRed"
            var dropGo = new GameObject("Dropdown_MonsterBallRed", typeof(RectTransform), typeof(Image));
            dropGo.transform.SetParent(panel.transform, false);
            var dRt = (RectTransform)dropGo.transform;
            dRt.anchorMin = new Vector2(0f, 1f);
            dRt.anchorMax = new Vector2(0f, 1f);
            dRt.pivot = new Vector2(0f, 1f);
            dRt.anchoredPosition = new Vector2(0f, 0f);
            dRt.sizeDelta = new Vector2(200f, 32f);
            dropGo.GetComponent<Image>().color = Color.white;

            var dropTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            dropTxtGo.transform.SetParent(dropGo.transform, false);
            var dtRt = (RectTransform)dropTxtGo.transform;
            dtRt.anchorMin = Vector2.zero;
            dtRt.anchorMax = Vector2.one;
            dtRt.offsetMin = new Vector2(10f, 0f);
            dtRt.offsetMax = new Vector2(-10f, 0f);
            var dtTxt = dropTxtGo.GetComponent<Text>();
            dtTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            dtTxt.text = "MonsterBallRed";
            dtTxt.fontSize = 15;
            dtTxt.color = Color.black;
            dtTxt.alignment = TextAnchor.MiddleLeft;

            // 2. Nút "생성" (Spawn)
            CreateNativeBtn(panel.transform, "Btn_Spawn", new Vector2(65f, 32f), new Vector2(210f, 0f), "생성", OnClickSpawn);

            // 3. Nút "리셋" (Reset)
            CreateNativeBtn(panel.transform, "Btn_Reset", new Vector2(65f, 32f), new Vector2(210f, -38f), "리셋", OnClickReset);

            // 4. Checkbox "AI" (Nhãn AI và ô vuông trắng)
            var aiContainer = new GameObject("AI_Checkbox_Container", typeof(RectTransform), typeof(Button));
            aiContainer.transform.SetParent(panel.transform, false);
            var acRt = (RectTransform)aiContainer.transform;
            acRt.anchorMin = new Vector2(0f, 1f);
            acRt.anchorMax = new Vector2(0f, 1f);
            acRt.pivot = new Vector2(0f, 1f);
            acRt.anchoredPosition = new Vector2(170f, -76f);
            acRt.sizeDelta = new Vector2(105f, 30f);

            var aiBtn = aiContainer.GetComponent<Button>();
            aiBtn.onClick.RemoveAllListeners();
            aiBtn.onClick.AddListener(OnClickToggleAI);

            // Nhãn chữ "AI"
            var aiLabelGo = new GameObject("AI_Label", typeof(RectTransform), typeof(Text));
            aiLabelGo.transform.SetParent(aiContainer.transform, false);
            var alRt = (RectTransform)aiLabelGo.transform;
            alRt.anchorMin = new Vector2(0f, 0f);
            alRt.anchorMax = new Vector2(0f, 1f);
            alRt.pivot = new Vector2(0f, 0.5f);
            alRt.anchoredPosition = Vector2.zero;
            alRt.sizeDelta = new Vector2(30f, 0f);
            var alTxt = aiLabelGo.GetComponent<Text>();
            alTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            alTxt.text = "AI";
            alTxt.fontSize = 16;
            alTxt.fontStyle = FontStyle.Bold;
            alTxt.color = new Color(0.85f, 0.85f, 0.88f);
            alTxt.alignment = TextAnchor.MiddleRight;

            // Ô vuông trắng Checkbox
            var boxGo = new GameObject("Checkbox_Box", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(aiContainer.transform, false);
            var bRt = (RectTransform)boxGo.transform;
            bRt.anchorMin = new Vector2(1f, 0.5f);
            bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = new Vector2(0f, 0f);
            bRt.sizeDelta = new Vector2(24f, 24f);
            boxGo.GetComponent<Image>().color = Color.white;

            // Dấu tích xanh bên trong
            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(boxGo.transform, false);
            var chRt = (RectTransform)checkGo.transform;
            chRt.anchorMin = Vector2.zero;
            chRt.anchorMax = Vector2.one;
            chRt.offsetMin = new Vector2(4f, 4f);
            chRt.offsetMax = new Vector2(-4f, -4f);
            var chImg = checkGo.GetComponent<Image>();
            chImg.color = new Color(0.15f, 0.8f, 0.25f);
            AiCheckmark = chImg;
        }

        private void CreateNativeBtn(Transform parent, string name, Vector2 size, Vector2 pos, string label, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            btnGo.GetComponent<Image>().color = new Color(0.92f, 0.92f, 0.94f);

            var btn = btnGo.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(btnGo.transform, false);
            var tRt = (RectTransform)txtGo.transform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;
            var txt = txtGo.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.black;
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private GameObject BuildMonsterWorldCanvas()
        {
            var go = new GameObject("Monster_World_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 95;

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(240f, 50f);
            rt.localScale = Vector3.one * 0.01f;

            var root = new GameObject("Bar_Root", typeof(RectTransform));
            root.transform.SetParent(go.transform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            // Monster Name (Thư pháp 적구)
            var nameGo = new GameObject("Monster_Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(root.transform, false);
            var nRt = (RectTransform)nameGo.transform;
            nRt.anchorMin = new Vector2(0f, 1f); nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0.5f, 1f);
            nRt.anchoredPosition = new Vector2(0f, 0f);
            nRt.sizeDelta = new Vector2(0f, 22f);
            var nTxt = nameGo.GetComponent<Text>();
            nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            nTxt.text = "적구";
            nTxt.fontSize = 16;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.color = Color.white;
            nTxt.alignment = TextAnchor.MiddleCenter;

            // Health Bar Background (Đỏ #EB2626)
            var barGo = new GameObject("Monster_HP_Bar", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(root.transform, false);
            var bRt = (RectTransform)barGo.transform;
            bRt.anchorMin = new Vector2(0f, 0f); bRt.anchorMax = new Vector2(1f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 2f);
            bRt.sizeDelta = new Vector2(0f, 18f);
            barGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

            var lerp = new GameObject("LerpFill", typeof(RectTransform), typeof(Image));
            lerp.transform.SetParent(barGo.transform, false);
            var lRt = (RectTransform)lerp.transform;
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(2f, 2f); lRt.offsetMax = new Vector2(-2f, -2f);
            var lImg = lerp.GetComponent<Image>();
            lImg.color = new Color(1f, 0.95f, 0.6f, 0.9f);
            lImg.type = Image.Type.Filled;
            lImg.fillMethod = Image.FillMethod.Horizontal;
            lImg.fillAmount = 1f;

            var main = new GameObject("MainFill", typeof(RectTransform), typeof(Image));
            main.transform.SetParent(barGo.transform, false);
            var mRt = (RectTransform)main.transform;
            mRt.anchorMin = Vector2.zero; mRt.anchorMax = Vector2.one;
            mRt.offsetMin = new Vector2(2f, 2f); mRt.offsetMax = new Vector2(-2f, -2f);
            var mImg = main.GetComponent<Image>();
            mImg.color = new Color(0.92f, 0.15f, 0.15f, 1f);
            mImg.type = Image.Type.Filled;
            mImg.fillMethod = Image.FillMethod.Horizontal;
            mImg.fillAmount = 1f;

            return go;
        }

        private void BindPlayerHUDElements(GameObject canvasGo)
        {
            var hpMain = canvasGo.transform.Find("Player_HUD_Root/HP_Bar/MainFill");
            if (hpMain != null) PlayerHpFill = hpMain.GetComponent<Image>();

            var hpLerp = canvasGo.transform.Find("Player_HUD_Root/HP_Bar/LerpFill");
            if (hpLerp != null) PlayerHpFillLerp = hpLerp.GetComponent<Image>();

            var hpTxt = canvasGo.transform.Find("Player_HUD_Root/HP_Bar/ValueText");
            if (hpTxt != null) PlayerHpText = hpTxt.GetComponent<Text>();

            var mpMain = canvasGo.transform.Find("Player_HUD_Root/MP_Bar/MainFill");
            if (mpMain != null) PlayerMpFill = mpMain.GetComponent<Image>();

            var chk = canvasGo.transform.Find("TopRight_Control_Panel/AI_Checkbox_Container/Checkbox_Box/Checkmark");
            if (chk != null) AiCheckmark = chk.GetComponent<Image>();
        }

        private void BindMonsterWorldElements(GameObject canvasGo)
        {
            MonsterBarRoot = canvasGo.transform.Find("Bar_Root");
            var hpMain = canvasGo.transform.Find("Bar_Root/Monster_HP_Bar/MainFill");
            if (hpMain != null) MonsterHpFill = hpMain.GetComponent<Image>();

            var hpLerp = canvasGo.transform.Find("Bar_Root/Monster_HP_Bar/LerpFill");
            if (hpLerp != null) MonsterHpFillLerp = hpLerp.GetComponent<Image>();

            var nTxt = canvasGo.transform.Find("Bar_Root/Monster_Name");
            if (nTxt != null) MonsterNameText = nTxt.GetComponent<Text>();
        }

        public void OnClickSpawn()
        {
            FindReferences();
            if (_monsterAI != null)
            {
                _monsterAI.SpawnAtDefaultPosition();
            }
            if (_monsterHealth != null)
            {
                _monsterHealth.ResetHealth();
            }
            Debug.Log("[CombatUIManager] Spawn MonsterBallRed!");
        }

        public void OnClickReset()
        {
            FindReferences();
            if (_monsterAI != null)
            {
                _monsterAI.ResetMonster();
            }
            if (_monsterHealth != null)
            {
                _monsterHealth.ResetHealth();
            }
            if (_playerReceiver != null)
            {
                _playerReceiver.ResetHealth();
                _playerReceiver.transform.position = new Vector3(-3.2f, 0f, 0f);
                _playerReceiver.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            Debug.Log("[CombatUIManager] Đã Reset trận đấu!");
        }

        public void OnClickToggleAI()
        {
            FindReferences();
            if (_monsterAI != null)
            {
                _monsterAI.IsAIEnabled = !_monsterAI.IsAIEnabled;
                UpdateAICheckboxVisual();
                Debug.Log($"[CombatUIManager] Đã chuyển đổi AI: {_monsterAI.IsAIEnabled}");
            }
        }
    }
}
