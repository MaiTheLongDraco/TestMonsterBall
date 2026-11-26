using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        [SerializeField] public Dropdown MonsterTypeDropdown;
        [SerializeField] public Button SpawnButton;
        [SerializeField] public Button ResetButton;
        [SerializeField] public Button AiToggleButton;
        [SerializeField] public Image ImmortalCheckmark;
        [SerializeField] public Button ImmortalToggleButton;

        private float _targetPlayerHpRatio = 1f;
        private float _targetMonsterHpRatio = 1f;
        private int _selectedSpawnModeIndex = 0;
        private bool _meleeToggle;
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
            if (Application.isPlaying)
            {
                EnsureEventSystem();
            }
            FindReferences();
            EnsureUIBuilt();
        }

        private void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es = esGo.GetComponent<EventSystem>();
            }
            else if (es.GetComponent<BaseInputModule>() == null)
            {
                es.gameObject.AddComponent<StandaloneInputModule>();
            }
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

            // Đồng bộ trạng thái Checkbox AI & Immortal
            UpdateAICheckboxVisual();
            UpdateImmortalCheckboxVisual();
        }

        private void UpdateAICheckboxVisual()
        {
            if (_monsterAI != null && AiCheckmark != null)
            {
                AiCheckmark.gameObject.SetActive(_monsterAI.IsAIEnabled);
            }
        }

        private void UpdateImmortalCheckboxVisual()
        {
            if (_playerReceiver != null && ImmortalCheckmark != null)
            {
                ImmortalCheckmark.gameObject.SetActive(_playerReceiver.IsImmortal);
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
            // 1. Kiểm tra / Tự dựng Player_HUD_Canvas (UI tĩnh chuẩn Sprite brush như video)
            var pCanvasGo = GameObject.Find("Player_HUD_Canvas");
            if (pCanvasGo != null && pCanvasGo.transform.Find("Player_HUD_Root/Name_Splash") == null)
            {
                if (Application.isPlaying) Destroy(pCanvasGo);
                else DestroyImmediate(pCanvasGo);
                pCanvasGo = null;
            }
            if (pCanvasGo == null)
            {
                pCanvasGo = BuildPlayerHUDCanvas();
            }
            if (pCanvasGo != null)
            {
                ScreenCanvas = pCanvasGo.GetComponent<Canvas>();
                BindPlayerHUDElements(pCanvasGo);
            }

            // 2. Kiểm tra / Tự dựng Monster_World_Canvas (UI World Space chuẩn Sprite brush)
            var mCanvasGo = GameObject.Find("Monster_World_Canvas");
            if (mCanvasGo != null && mCanvasGo.transform.Find("Bar_Root/Monster_Name_Splash") == null)
            {
                if (Application.isPlaying) Destroy(mCanvasGo);
                else DestroyImmediate(mCanvasGo);
                mCanvasGo = null;
            }
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

        public void RebuildAllUI()
        {
            var oldP = GameObject.Find("Player_HUD_Canvas");
            if (oldP != null)
            {
                if (Application.isPlaying) Destroy(oldP);
                else DestroyImmediate(oldP);
            }
            var oldM = GameObject.Find("Monster_World_Canvas");
            if (oldM != null)
            {
                if (Application.isPlaying) Destroy(oldM);
                else DestroyImmediate(oldM);
            }
            EnsureUIBuilt();
        }

        private static Sprite LoadUiSprite(string spriteName)
        {
            return Resources.Load<Sprite>($"UI/Sprites/{spriteName}");
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
            rootRt.anchoredPosition = new Vector2(28f, -24f);
            rootRt.sizeDelta = new Vector2(640f, 140f);

            var portraitSprite = LoadUiSprite("UI_Avatar_Portrait");
            var ringSprite = LoadUiSprite("UI_Avatar_Ring");
            var splashSprite = LoadUiSprite("UI_Brush_NameSplash");

            // Avatar Portrait (lớp dưới) + Brush Ring (lớp trên) giống y video
            var av = new GameObject("Avatar_Ring", typeof(RectTransform));
            av.transform.SetParent(root.transform, false);
            var avRt = (RectTransform)av.transform;
            avRt.anchorMin = new Vector2(0f, 1f);
            avRt.anchorMax = new Vector2(0f, 1f);
            avRt.pivot = new Vector2(0f, 1f);
            avRt.anchoredPosition = new Vector2(0f, -2f);
            avRt.sizeDelta = new Vector2(118f, 118f);

            var face = new GameObject("Avatar_Face", typeof(RectTransform), typeof(Image));
            face.transform.SetParent(av.transform, false);
            var fRt = (RectTransform)face.transform;
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(8f, 8f);
            fRt.offsetMax = new Vector2(-8f, -8f);
            var faceImg = face.GetComponent<Image>();
            faceImg.sprite = portraitSprite;
            faceImg.color = portraitSprite != null ? Color.white : new Color(0.15f, 0.42f, 0.38f, 1f);

            var ringImgGo = new GameObject("Avatar_BrushBorder", typeof(RectTransform), typeof(Image));
            ringImgGo.transform.SetParent(av.transform, false);
            var rbRt = (RectTransform)ringImgGo.transform;
            rbRt.anchorMin = Vector2.zero;
            rbRt.anchorMax = Vector2.one;
            rbRt.sizeDelta = Vector2.zero;
            var ringImg = ringImgGo.GetComponent<Image>();
            ringImg.sprite = ringSprite;
            ringImg.color = ringSprite != null ? Color.white : new Color(0.06f, 0.06f, 0.07f, 0.95f);

            // Vệt mực nền tên Nhân vật
            var nameSplashGo = new GameObject("Name_Splash", typeof(RectTransform), typeof(Image));
            nameSplashGo.transform.SetParent(root.transform, false);
            var nsRt = (RectTransform)nameSplashGo.transform;
            nsRt.anchorMin = new Vector2(0f, 1f);
            nsRt.anchorMax = new Vector2(0f, 1f);
            nsRt.pivot = new Vector2(0f, 1f);
            nsRt.anchoredPosition = new Vector2(104f, 4f);
            nsRt.sizeDelta = new Vector2(360f, 42f);
            var nsImg = nameSplashGo.GetComponent<Image>();
            nsImg.sprite = splashSprite;
            nsImg.color = splashSprite != null ? Color.white : new Color(0.06f, 0.06f, 0.07f, 0.88f);

            // Player Name
            var nameGo = new GameObject("Player_Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(root.transform, false);
            var nRt = (RectTransform)nameGo.transform;
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(0f, 1f);
            nRt.pivot = new Vector2(0f, 1f);
            nRt.anchoredPosition = new Vector2(124f, -2f);
            nRt.sizeDelta = new Vector2(280f, 32f);
            var nameTxt = nameGo.GetComponent<Text>();
            nameTxt.font = GetUIFont();
            nameTxt.text = "JangHyu";
            nameTxt.fontSize = 24;
            nameTxt.fontStyle = FontStyle.BoldAndItalic;
            nameTxt.color = Color.white;
            nameTxt.alignment = TextAnchor.MiddleLeft;

            // HP Bar (Xanh lá rực #1CC838, viền cọ mực)
            BuildBar(root.transform, "HP_Bar", new Vector2(470f, 22f), new Vector2(104f, -38f),
                new Color(0.11f, 0.78f, 0.22f), new Color(1f, 0.82f, 0.2f, 0.85f), "");

            // MP Bar (Xanh dương #1992E6, viền cọ mực)
            BuildBar(root.transform, "MP_Bar", new Vector2(420f, 17f), new Vector2(104f, -64f),
                new Color(0.10f, 0.57f, 0.90f), new Color(0.5f, 0.8f, 1f, 0.6f), "");

            // LV.1 Badge + EXP Bar (Vàng cam #F5A614)
            var lvSplash = new GameObject("LV_Splash", typeof(RectTransform), typeof(Image));
            lvSplash.transform.SetParent(root.transform, false);
            var lvRt = (RectTransform)lvSplash.transform;
            lvRt.anchorMin = new Vector2(0f, 1f);
            lvRt.anchorMax = new Vector2(0f, 1f);
            lvRt.pivot = new Vector2(0f, 1f);
            lvRt.anchoredPosition = new Vector2(102f, -94f);
            lvRt.sizeDelta = new Vector2(68f, 24f);
            var lvImg = lvSplash.GetComponent<Image>();
            lvImg.sprite = splashSprite;
            lvImg.color = splashSprite != null ? Color.white : new Color(0.05f, 0.05f, 0.06f, 0.9f);

            var lvTextGo = new GameObject("LV_Text", typeof(RectTransform), typeof(Text));
            lvTextGo.transform.SetParent(lvSplash.transform, false);
            var lvtRt = (RectTransform)lvTextGo.transform;
            lvtRt.anchorMin = Vector2.zero; lvtRt.anchorMax = Vector2.one;
            lvtRt.offsetMin = new Vector2(6f, 0f); lvtRt.offsetMax = Vector2.zero;
            var lvTxt = lvTextGo.GetComponent<Text>();
            lvTxt.font = GetUIFont();
            lvTxt.text = "LV.1";
            lvTxt.fontSize = 13;
            lvTxt.fontStyle = FontStyle.Bold;
            lvTxt.color = new Color(1f, 0.72f, 0.12f);
            lvTxt.alignment = TextAnchor.MiddleLeft;

            BuildBar(root.transform, "EXP_Bar", new Vector2(180f, 11f), new Vector2(166f, -101f),
                new Color(0.96f, 0.65f, 0.08f), new Color(1f, 0.8f, 0.15f), "");

            // Đưa Avatar lên trên đầu thanh máu giống y video
            av.transform.SetAsLastSibling();

            // Top-Right Control Panel + Demo Actions
            BuildTopRightControlPanel(go.transform);

            return go;
        }

        private void BuildBar(Transform parent, string name, Vector2 size, Vector2 pos, Color mainColor, Color lerpColor, string defaultText)
        {
            var barBgSprite = LoadUiSprite("UI_Brush_BarBg");
            var barFillSprite = LoadUiSprite("UI_Brush_BarFill");

            var bar = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var barBgImg = bar.GetComponent<Image>();
            barBgImg.sprite = barBgSprite;
            barBgImg.type = barBgSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            barBgImg.color = barBgSprite != null ? Color.white : new Color(0.08f, 0.08f, 0.08f, 0.85f);

            var lerp = new GameObject("LerpFill", typeof(RectTransform), typeof(Image));
            lerp.transform.SetParent(bar.transform, false);
            var lRt = (RectTransform)lerp.transform;
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(4f, 3f); lRt.offsetMax = new Vector2(-4f, -3f);
            var lImg = lerp.GetComponent<Image>();
            lImg.sprite = barFillSprite;
            lImg.color = lerpColor;
            lImg.type = Image.Type.Filled;
            lImg.fillMethod = Image.FillMethod.Horizontal;
            lImg.fillAmount = 1f;

            var main = new GameObject("MainFill", typeof(RectTransform), typeof(Image));
            main.transform.SetParent(bar.transform, false);
            var mRt = (RectTransform)main.transform;
            mRt.anchorMin = Vector2.zero; mRt.anchorMax = Vector2.one;
            mRt.offsetMin = new Vector2(4f, 3f); mRt.offsetMax = new Vector2(-4f, -3f);
            var mImg = main.GetComponent<Image>();
            mImg.sprite = barFillSprite;
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
                txt.font = GetUIFont();
                txt.text = defaultText;
                txt.fontSize = Mathf.Max(10, (int)(size.y * 0.65f));
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
            }
        }

        public void BuildTopRightControlPanel(Transform canvasT)
        {
            var old = canvasT.Find("TopRight_Control_Panel");
            if (old != null)
            {
                if (Application.isPlaying) Destroy(old.gameObject);
                else DestroyImmediate(old.gameObject);
            }

            var panel = new GameObject("TopRight_Control_Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasT, false);
            var pRt = (RectTransform)panel.transform;
            pRt.anchorMin = new Vector2(1f, 1f);
            pRt.anchorMax = new Vector2(1f, 1f);
            pRt.pivot = new Vector2(1f, 1f);
            pRt.anchoredPosition = new Vector2(-20f, -20f);
            pRt.sizeDelta = new Vector2(300f, 260f);

            // 1. Dropdown chọn chế độ spawn MonsterBallRed
            MonsterTypeDropdown = BuildDropdown(panel.transform, "Dropdown_MonsterBallRed", new Vector2(210f, 32f), new Vector2(0f, 0f));

            // 2. Nút "Spawn"
            SpawnButton = CreateNativeBtn(panel.transform, "Btn_Spawn", new Vector2(82f, 32f), new Vector2(218f, 0f), "Spawn", OnClickSpawn);

            // 3. Nút "Reset"
            ResetButton = CreateNativeBtn(panel.transform, "Btn_Reset", new Vector2(82f, 32f), new Vector2(218f, -38f), "Reset", OnClickReset);

            // 4. Checkbox "Immortal" (Player bất tử) & Checkbox "AI"
            var (immBtn, immCheck) = BuildCheckbox(
                panel.transform, "Immortal_Checkbox_Container", "Immortal",
                new Vector2(0f, -76f), new Vector2(150f, 28f), new Color(1f, 0.72f, 0.12f), OnClickToggleImmortal);
            ImmortalToggleButton = immBtn;
            ImmortalCheckmark = immCheck;

            var (aiBtn, aiCheck) = BuildCheckbox(
                panel.transform, "AI_Checkbox_Container", "AI",
                new Vector2(180f, -76f), new Vector2(120f, 28f), new Color(0.15f, 0.8f, 0.25f), OnClickToggleAI);
            AiToggleButton = aiBtn;
            AiCheckmark = aiCheck;

            // 5. Demo AI Actions Panel
            var demoPanel = new GameObject("Demo_Actions_Panel", typeof(RectTransform), typeof(Image));
            demoPanel.transform.SetParent(panel.transform, false);
            var dpRt = (RectTransform)demoPanel.transform;
            dpRt.anchorMin = new Vector2(0f, 1f);
            dpRt.anchorMax = new Vector2(0f, 1f);
            dpRt.pivot = new Vector2(0f, 1f);
            dpRt.anchoredPosition = new Vector2(0f, -112f);
            dpRt.sizeDelta = new Vector2(300f, 138f);
            demoPanel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.78f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(demoPanel.transform, false);
            var tRt = (RectTransform)titleGo.transform;
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -4f);
            tRt.sizeDelta = new Vector2(0f, 18f);
            var tTxt = titleGo.GetComponent<Text>();
            tTxt.font = GetUIFont();
            tTxt.text = "DEMO AI ACTIONS";
            tTxt.fontSize = 11;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.color = new Color(0.8f, 0.85f, 0.92f);
            tTxt.alignment = TextAnchor.MiddleCenter;

            CreateNativeBtn(demoPanel.transform, "Btn_Action_Emerge", new Vector2(90f, 30f), new Vector2(8f, -26f), "Emerge", OnClickDemoEmerge, 13);
            CreateNativeBtn(demoPanel.transform, "Btn_Action_Roll", new Vector2(90f, 30f), new Vector2(105f, -26f), "Roll", OnClickDemoRoll, 13);
            CreateNativeBtn(demoPanel.transform, "Btn_Action_JumpSlam", new Vector2(90f, 30f), new Vector2(202f, -26f), "Jump Slam", OnClickDemoJumpSlam, 13);
            CreateNativeBtn(demoPanel.transform, "Btn_Action_Melee", new Vector2(90f, 30f), new Vector2(8f, -62f), "Melee", OnClickDemoMelee, 13);
            CreateNativeBtn(demoPanel.transform, "Btn_Action_Groggy", new Vector2(90f, 30f), new Vector2(105f, -62f), "Groggy", OnClickDemoGroggy, 13);
            CreateNativeBtn(demoPanel.transform, "Btn_Action_Burrow", new Vector2(90f, 30f), new Vector2(202f, -62f), "Burrow", OnClickDemoBurrow, 13);
            CreateNativeBtn(demoPanel.transform, "Btn_Action_Explode", new Vector2(284f, 30f), new Vector2(8f, -98f), "Explode (Death)", OnClickDemoExplode, 13);

            // Đưa Dropdown lên cuối hierarchy để danh sách xổ xuống không bị nút bên dưới che
            dropPanelBringToFront(panel.transform);

            if (AiCheckmark != null) AiCheckmark.gameObject.SetActive(_monsterAI != null ? _monsterAI.IsAIEnabled : true);
            if (ImmortalCheckmark != null) ImmortalCheckmark.gameObject.SetActive(_playerReceiver != null && _playerReceiver.IsImmortal);
        }

        private (Button btn, Image check) BuildCheckbox(
            Transform parent, string containerName, string labelText, Vector2 pos, Vector2 size, Color checkColor, UnityEngine.Events.UnityAction action)
        {
            var container = new GameObject(containerName, typeof(RectTransform), typeof(Image), typeof(Button));
            container.transform.SetParent(parent, false);
            var cRt = (RectTransform)container.transform;
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(0f, 1f);
            cRt.pivot = new Vector2(0f, 1f);
            cRt.anchoredPosition = pos;
            cRt.sizeDelta = size;
            container.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var btn = container.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(container.transform, false);
            var lRt = (RectTransform)labelGo.transform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = new Vector2(-30f, 0f);
            var lTxt = labelGo.GetComponent<Text>();
            lTxt.font = GetUIFont();
            lTxt.text = labelText;
            lTxt.fontSize = 15;
            lTxt.fontStyle = FontStyle.Bold;
            lTxt.color = new Color(0.92f, 0.92f, 0.94f);
            lTxt.alignment = TextAnchor.MiddleRight;

            var boxGo = new GameObject("Checkbox_Box", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(container.transform, false);
            var bRt = (RectTransform)boxGo.transform;
            bRt.anchorMin = new Vector2(1f, 0.5f);
            bRt.anchorMax = new Vector2(1f, 0.5f);
            bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = Vector2.zero;
            bRt.sizeDelta = new Vector2(24f, 24f);
            boxGo.GetComponent<Image>().color = Color.white;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(boxGo.transform, false);
            var chRt = (RectTransform)checkGo.transform;
            chRt.anchorMin = Vector2.zero;
            chRt.anchorMax = Vector2.one;
            chRt.offsetMin = new Vector2(4f, 4f);
            chRt.offsetMax = new Vector2(-4f, -4f);
            var chImg = checkGo.GetComponent<Image>();
            chImg.color = checkColor;
            return (btn, chImg);
        }

        private static void dropPanelBringToFront(Transform panel)
        {
            var drop = panel.Find("Dropdown_MonsterBallRed");
            if (drop != null) drop.SetAsLastSibling();
        }

        private static Font GetUIFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private Dropdown BuildDropdown(Transform parent, string name, Vector2 size, Vector2 pos)
        {
            var font = GetUIFont();
            var dropGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
            dropGo.transform.SetParent(parent, false);
            var dRt = (RectTransform)dropGo.transform;
            dRt.anchorMin = new Vector2(0f, 1f);
            dRt.anchorMax = new Vector2(0f, 1f);
            dRt.pivot = new Vector2(0f, 1f);
            dRt.anchoredPosition = pos;
            dRt.sizeDelta = size;
            dropGo.GetComponent<Image>().color = Color.white;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(dropGo.transform, false);
            var lRt = (RectTransform)labelGo.transform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(10f, 0f);
            lRt.offsetMax = new Vector2(-26f, 0f);
            var labelTxt = labelGo.GetComponent<Text>();
            labelTxt.font = font;
            labelTxt.fontSize = 14;
            labelTxt.color = Color.black;
            labelTxt.alignment = TextAnchor.MiddleLeft;

            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Text));
            arrowGo.transform.SetParent(dropGo.transform, false);
            var aRt = (RectTransform)arrowGo.transform;
            aRt.anchorMin = new Vector2(1f, 0f);
            aRt.anchorMax = new Vector2(1f, 1f);
            aRt.pivot = new Vector2(1f, 0.5f);
            aRt.anchoredPosition = new Vector2(-6f, 0f);
            aRt.sizeDelta = new Vector2(20f, 0f);
            var arrowTxt = arrowGo.GetComponent<Text>();
            arrowTxt.font = font;
            arrowTxt.text = "v";
            arrowTxt.fontSize = 13;
            arrowTxt.fontStyle = FontStyle.Bold;
            arrowTxt.color = new Color(0.25f, 0.25f, 0.28f);
            arrowTxt.alignment = TextAnchor.MiddleCenter;

            // Template cho Dropdown list
            var templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGo.transform.SetParent(dropGo.transform, false);
            var tplRt = (RectTransform)templateGo.transform;
            tplRt.anchorMin = new Vector2(0f, 0f);
            tplRt.anchorMax = new Vector2(1f, 0f);
            tplRt.pivot = new Vector2(0.5f, 1f);
            tplRt.anchoredPosition = new Vector2(0f, -2f);
            tplRt.sizeDelta = new Vector2(0f, 96f);
            templateGo.GetComponent<Image>().color = new Color(0.96f, 0.96f, 0.98f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(templateGo.transform, false);
            var vpRt = (RectTransform)viewportGo.transform;
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var cRt = (RectTransform)contentGo.transform;
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 28f);

            var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemGo.transform.SetParent(contentGo.transform, false);
            var iRt = (RectTransform)itemGo.transform;
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(1f, 0.5f);
            iRt.sizeDelta = new Vector2(0f, 28f);

            var itemBgGo = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBgGo.transform.SetParent(itemGo.transform, false);
            var ibRt = (RectTransform)itemBgGo.transform;
            ibRt.anchorMin = Vector2.zero;
            ibRt.anchorMax = Vector2.one;
            ibRt.sizeDelta = Vector2.zero;
            itemBgGo.GetComponent<Image>().color = new Color(0.92f, 0.94f, 0.98f);

            var itemCheckGo = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            itemCheckGo.transform.SetParent(itemGo.transform, false);
            var icRt = (RectTransform)itemCheckGo.transform;
            icRt.anchorMin = new Vector2(0f, 0.5f);
            icRt.anchorMax = new Vector2(0f, 0.5f);
            icRt.anchoredPosition = new Vector2(10f, 0f);
            icRt.sizeDelta = new Vector2(10f, 10f);
            var icImg = itemCheckGo.GetComponent<Image>();
            icImg.color = new Color(0.15f, 0.7f, 0.25f);

            var itemLabelGo = new GameObject("Item Label", typeof(RectTransform), typeof(Text));
            itemLabelGo.transform.SetParent(itemGo.transform, false);
            var ilRt = (RectTransform)itemLabelGo.transform;
            ilRt.anchorMin = Vector2.zero;
            ilRt.anchorMax = Vector2.one;
            ilRt.offsetMin = new Vector2(22f, 0f);
            ilRt.offsetMax = new Vector2(-6f, 0f);
            var itemLabelTxt = itemLabelGo.GetComponent<Text>();
            itemLabelTxt.font = font;
            itemLabelTxt.fontSize = 13;
            itemLabelTxt.color = Color.black;
            itemLabelTxt.alignment = TextAnchor.MiddleLeft;

            var itemToggle = itemGo.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemBgGo.GetComponent<Image>();
            itemToggle.graphic = icImg;
            itemToggle.isOn = true;

            var scroll = templateGo.GetComponent<ScrollRect>();
            scroll.content = cRt;
            scroll.viewport = vpRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            templateGo.SetActive(false);

            var dropdown = dropGo.GetComponent<Dropdown>();
            dropdown.targetGraphic = dropGo.GetComponent<Image>();
            dropdown.template = tplRt;
            dropdown.captionText = labelTxt;
            dropdown.itemText = itemLabelTxt;
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "MonsterBallRed (Burrowed)",
                "MonsterBallRed (Active)"
            });
            dropdown.value = _selectedSpawnModeIndex;
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(OnDropdownModeChanged);
            return dropdown;
        }

        private Button CreateNativeBtn(Transform parent, string name, Vector2 size, Vector2 pos, string label, UnityEngine.Events.UnityAction action, int fontSize = 15)
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
            txt.font = GetUIFont();
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.black;
            txt.alignment = TextAnchor.MiddleCenter;
            return btn;
        }

        private GameObject BuildMonsterWorldCanvas()
        {
            var splashSprite = LoadUiSprite("UI_Brush_NameSplash");
            var barBgSprite = LoadUiSprite("UI_Brush_BarBg");
            var barFillSprite = LoadUiSprite("UI_Brush_BarFill");

            var go = new GameObject("Monster_World_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 95;

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(240f, 44f);
            rt.localScale = Vector3.one * 0.0085f;

            var root = new GameObject("Bar_Root", typeof(RectTransform));
            root.transform.SetParent(go.transform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            // Vệt mực nền tên quái giống video
            var splashGo = new GameObject("Monster_Name_Splash", typeof(RectTransform), typeof(Image));
            splashGo.transform.SetParent(root.transform, false);
            var spRt = (RectTransform)splashGo.transform;
            spRt.anchorMin = new Vector2(0.5f, 1f); spRt.anchorMax = new Vector2(0.5f, 1f);
            spRt.pivot = new Vector2(0.5f, 1f);
            spRt.anchoredPosition = new Vector2(0f, 0f);
            spRt.sizeDelta = new Vector2(180f, 24f);
            var spImg = splashGo.GetComponent<Image>();
            spImg.sprite = splashSprite;
            spImg.color = splashSprite != null ? Color.white : new Color(0.05f, 0.05f, 0.06f, 0.85f);

            // Monster Name
            var nameGo = new GameObject("Monster_Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(root.transform, false);
            var nRt = (RectTransform)nameGo.transform;
            nRt.anchorMin = new Vector2(0f, 1f); nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0.5f, 1f);
            nRt.anchoredPosition = new Vector2(0f, -1f);
            nRt.sizeDelta = new Vector2(0f, 22f);
            var nTxt = nameGo.GetComponent<Text>();
            nTxt.font = GetUIFont();
            nTxt.text = "MonsterBall Red";
            nTxt.fontSize = 13;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.color = Color.white;
            nTxt.alignment = TextAnchor.MiddleCenter;

            // Health Bar Background (Đỏ #EB1D1D, viền cọ mực)
            var barGo = new GameObject("Monster_HP_Bar", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(root.transform, false);
            var bRt = (RectTransform)barGo.transform;
            bRt.anchorMin = new Vector2(0f, 0f); bRt.anchorMax = new Vector2(1f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 2f);
            bRt.sizeDelta = new Vector2(0f, 15f);
            var bgImg = barGo.GetComponent<Image>();
            bgImg.sprite = barBgSprite;
            bgImg.type = barBgSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bgImg.color = barBgSprite != null ? Color.white : new Color(0.08f, 0.08f, 0.08f, 0.9f);

            var lerp = new GameObject("LerpFill", typeof(RectTransform), typeof(Image));
            lerp.transform.SetParent(barGo.transform, false);
            var lRt = (RectTransform)lerp.transform;
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(3f, 2f); lRt.offsetMax = new Vector2(-3f, -2f);
            var lImg = lerp.GetComponent<Image>();
            lImg.sprite = barFillSprite;
            lImg.color = new Color(1f, 0.95f, 0.6f, 0.9f);
            lImg.type = Image.Type.Filled;
            lImg.fillMethod = Image.FillMethod.Horizontal;
            lImg.fillAmount = 1f;

            var main = new GameObject("MainFill", typeof(RectTransform), typeof(Image));
            main.transform.SetParent(barGo.transform, false);
            var mRt = (RectTransform)main.transform;
            mRt.anchorMin = Vector2.zero; mRt.anchorMax = Vector2.one;
            mRt.offsetMin = new Vector2(3f, 2f); mRt.offsetMax = new Vector2(-3f, -2f);
            var mImg = main.GetComponent<Image>();
            mImg.sprite = barFillSprite;
            mImg.color = new Color(0.92f, 0.12f, 0.12f, 1f);
            mImg.type = Image.Type.Filled;
            mImg.fillMethod = Image.FillMethod.Horizontal;
            mImg.fillAmount = 1f;

            return go;
        }

        private void BindPlayerHUDElements(GameObject canvasGo)
        {
            var pNameT = canvasGo.transform.Find("Player_HUD_Root/Player_Name");
            if (pNameT != null && pNameT.TryGetComponent<Text>(out var pNameTxt))
            {
                pNameTxt.text = "JangHyu";
            }

            var hpMain = canvasGo.transform.Find("Player_HUD_Root/HP_Bar/MainFill");
            if (hpMain != null) PlayerHpFill = hpMain.GetComponent<Image>();

            var hpLerp = canvasGo.transform.Find("Player_HUD_Root/HP_Bar/LerpFill");
            if (hpLerp != null) PlayerHpFillLerp = hpLerp.GetComponent<Image>();

            var hpTxt = canvasGo.transform.Find("Player_HUD_Root/HP_Bar/ValueText");
            if (hpTxt != null) PlayerHpText = hpTxt.GetComponent<Text>();

            var mpMain = canvasGo.transform.Find("Player_HUD_Root/MP_Bar/MainFill");
            if (mpMain != null) PlayerMpFill = mpMain.GetComponent<Image>();

            var mpTxt = canvasGo.transform.Find("Player_HUD_Root/MP_Bar/ValueText");
            if (mpTxt != null) PlayerMpText = mpTxt.GetComponent<Text>();

            EnsureTopRightControlBindings(canvasGo.transform);
        }

        private void EnsureTopRightControlBindings(Transform canvasT)
        {
            var topRight = canvasT.Find("TopRight_Control_Panel");
            bool needsRebuild = topRight == null
                || topRight.Find("Demo_Actions_Panel") == null
                || topRight.Find("Demo_Actions_Panel/Btn_Action_Explode") == null
                || topRight.Find("Immortal_Checkbox_Container") == null
                || topRight.Find("Dropdown_MonsterBallRed")?.GetComponent<Dropdown>() == null;

            if (needsRebuild)
            {
                BuildTopRightControlPanel(canvasT);
                return;
            }

            MonsterTypeDropdown = topRight.Find("Dropdown_MonsterBallRed")?.GetComponent<Dropdown>();
            if (MonsterTypeDropdown != null)
            {
                MonsterTypeDropdown.onValueChanged.RemoveAllListeners();
                MonsterTypeDropdown.onValueChanged.AddListener(OnDropdownModeChanged);
            }

            BindButton(topRight, "Btn_Spawn", "Spawn", OnClickSpawn, ref SpawnButton);
            BindButton(topRight, "Btn_Reset", "Reset", OnClickReset, ref ResetButton);

            var aiContainer = topRight.Find("AI_Checkbox_Container");
            if (aiContainer != null && aiContainer.TryGetComponent<Button>(out var aiBtn))
            {
                aiBtn.onClick.RemoveAllListeners();
                aiBtn.onClick.AddListener(OnClickToggleAI);
                AiToggleButton = aiBtn;
            }

            var chk = topRight.Find("AI_Checkbox_Container/Checkbox_Box/Checkmark");
            if (chk != null) AiCheckmark = chk.GetComponent<Image>();

            var immContainer = topRight.Find("Immortal_Checkbox_Container");
            if (immContainer != null && immContainer.TryGetComponent<Button>(out var immBtn))
            {
                immBtn.onClick.RemoveAllListeners();
                immBtn.onClick.AddListener(OnClickToggleImmortal);
                ImmortalToggleButton = immBtn;
            }
            var immChk = topRight.Find("Immortal_Checkbox_Container/Checkbox_Box/Checkmark");
            if (immChk != null) ImmortalCheckmark = immChk.GetComponent<Image>();

            var demoPanel = topRight.Find("Demo_Actions_Panel");
            if (demoPanel != null)
            {
                Button unused = null;
                BindButton(demoPanel, "Btn_Action_Emerge", "Emerge", OnClickDemoEmerge, ref unused);
                BindButton(demoPanel, "Btn_Action_Roll", "Roll", OnClickDemoRoll, ref unused);
                BindButton(demoPanel, "Btn_Action_JumpSlam", "Jump Slam", OnClickDemoJumpSlam, ref unused);
                BindButton(demoPanel, "Btn_Action_Melee", "Melee", OnClickDemoMelee, ref unused);
                BindButton(demoPanel, "Btn_Action_Groggy", "Groggy", OnClickDemoGroggy, ref unused);
                BindButton(demoPanel, "Btn_Action_Burrow", "Burrow", OnClickDemoBurrow, ref unused);
                BindButton(demoPanel, "Btn_Action_Explode", "Explode (Death)", OnClickDemoExplode, ref unused);
            }
        }

        private static void BindButton(Transform parent, string childName, string labelText, UnityEngine.Events.UnityAction action, ref Button targetField)
        {
            var tr = parent.Find(childName);
            if (tr == null) return;
            var labelTxt = tr.Find("Label")?.GetComponent<Text>();
            if (labelTxt != null) labelTxt.text = labelText;

            if (tr.TryGetComponent<Button>(out var btn))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
                targetField = btn;
            }
        }

        private void BindMonsterWorldElements(GameObject canvasGo)
        {
            MonsterBarRoot = canvasGo.transform.Find("Bar_Root");
            var hpMain = canvasGo.transform.Find("Bar_Root/Monster_HP_Bar/MainFill");
            if (hpMain != null) MonsterHpFill = hpMain.GetComponent<Image>();

            var hpLerp = canvasGo.transform.Find("Bar_Root/Monster_HP_Bar/LerpFill");
            if (hpLerp != null) MonsterHpFillLerp = hpLerp.GetComponent<Image>();

            var nTxt = canvasGo.transform.Find("Bar_Root/Monster_Name");
            if (nTxt != null)
            {
                MonsterNameText = nTxt.GetComponent<Text>();
                if (MonsterNameText != null) MonsterNameText.text = "MonsterBall Red";
            }
        }

        public void OnDropdownModeChanged(int index)
        {
            _selectedSpawnModeIndex = index;
        }

        public void OnClickSpawn()
        {
            FindReferences();
            if (_monsterAI != null)
            {
                _monsterAI.SpawnAndEmerge(_selectedSpawnModeIndex == 1);
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
            Debug.Log("[CombatUIManager] Match Reset!");
        }

        public void OnClickToggleAI()
        {
            FindReferences();
            if (_monsterAI != null)
            {
                _monsterAI.IsAIEnabled = !_monsterAI.IsAIEnabled;
                UpdateAICheckboxVisual();
                Debug.Log($"[CombatUIManager] AI Toggled: {_monsterAI.IsAIEnabled}");
            }
        }

        public void OnClickToggleImmortal()
        {
            FindReferences();
            if (_playerReceiver != null)
            {
                _playerReceiver.IsImmortal = !_playerReceiver.IsImmortal;
                UpdateImmortalCheckboxVisual();
                Debug.Log($"[CombatUIManager] Player Immortal Toggled: {_playerReceiver.IsImmortal}");
            }
        }

        public void OnClickDemoEmerge() => TriggerMonsterAction(MonsterAIState.Emerge);
        public void OnClickDemoRoll() => TriggerMonsterAction(MonsterAIState.RollAttack);
        public void OnClickDemoJumpSlam() => TriggerMonsterAction(MonsterAIState.JumpSlam);
        public void OnClickDemoGroggy() => TriggerMonsterAction(MonsterAIState.Groggy);
        public void OnClickDemoBurrow() => TriggerMonsterAction(MonsterAIState.Hidden);
        public void OnClickDemoExplode() => TriggerMonsterAction(MonsterAIState.Dead);

        public void OnClickDemoMelee()
        {
            _meleeToggle = !_meleeToggle;
            TriggerMonsterAction(_meleeToggle ? MonsterAIState.Attack2 : MonsterAIState.Attack1);
        }

        private void TriggerMonsterAction(MonsterAIState actionState)
        {
            FindReferences();
            if (_monsterAI == null) return;
            _monsterAI.TriggerDemoAction(actionState);
            Debug.Log($"[CombatUIManager] Demo Action triggered: {actionState}");
        }
    }
}
