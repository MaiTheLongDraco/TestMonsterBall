using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Aiara.Combat;
using Aiara.UI;

namespace Aiara.EditorTools
{
    public static class BakeStreetFighterDirectly
    {
        private static bool _hasExecuted = false;

        [MenuItem("Tools/StreetFighter/Bake Complete Scene Now")]
        public static void BakeFullScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            Debug.Log("[BakeStreetFighterDirectly] >>> BẮT ĐẦU BAKE TOÀN BỘ SCENE 2.5D STREET FIGHTER <<<");

            // 1. Tắt Play mode nếu đang bật để lưu Scene an toàn
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            // 2. Đảm bảo mở đúng SampleScene
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "SampleScene")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            // 3. Xóa bỏ triệt để và vĩnh viễn tất cả các GameObject rác gây khối màu tím
            string[] trashNames = { "VFX_GroundCrack", "VFX_GroundErupt", "VFX_BodyFlameAura", "VFX_RollTrail", "VFX_RollTrail_AI", "VFX_GroundErupt_AI" };
            foreach (var trash in trashNames)
            {
                var gos = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var go in gos)
                {
                    if (go != null && go.name == trash)
                    {
                        Debug.Log($"[BakeStreetFighterDirectly] Xóa GameObject rác gây tím: {go.name}");
                        UnityEngine.Object.DestroyImmediate(go);
                    }
                }
            }

            // 4. Tìm Player (JangHyu) và cấu hình chuẩn 2.5D
            GameObject playerObj = GameObject.Find("JangHyu_AttackCombo2");
            if (playerObj == null)
            {
                var gos = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var go in gos)
                {
                    if (go.name.Contains("JangHyu") && !go.name.Contains("VFX") && !go.name.Contains("Canvas"))
                    {
                        playerObj = go;
                        break;
                    }
                }
            }

            if (playerObj != null)
            {
                playerObj.tag = "Player";
                playerObj.transform.position = new Vector3(-2.8f, 0f, 0f);
                playerObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                // Gỡ bỏ script quái vật nếu bị gán nhầm
                var wrongAmbush = playerObj.GetComponent<MonsterBallAmbushController>();
                if (wrongAmbush != null) UnityEngine.Object.DestroyImmediate(wrongAmbush);

                // Gắn AnimationEventHandler cho tất cả Animator
                var animators = playerObj.GetComponentsInChildren<Animator>(true);
                foreach (var anim in animators)
                {
                    if (anim.GetComponent<AnimationEventHandler>() == null)
                    {
                        anim.gameObject.AddComponent<AnimationEventHandler>();
                    }
                }
                if (playerObj.GetComponent<AnimationEventHandler>() == null)
                {
                    playerObj.AddComponent<AnimationEventHandler>();
                }

                if (playerObj.GetComponent<JangHyuBurnReceiver>() == null)
                {
                    playerObj.AddComponent<JangHyuBurnReceiver>();
                }

                if (playerObj.GetComponent<JangHyuFightingController>() == null)
                {
                    playerObj.AddComponent<JangHyuFightingController>();
                }

                EditorUtility.SetDirty(playerObj);
                Debug.Log("[BakeStreetFighterDirectly] Player (JangHyu) đã được thiết lập tại X = -2.8 nhìn sang phải!");
            }

            // 5. Tìm MonsterBall_red và cấu hình chuẩn AI
            GameObject monsterObj = GameObject.Find("MonsterBall_red");
            if (monsterObj == null)
            {
                var gos = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var go in gos)
                {
                    if (go.name.Contains("MonsterBall") && !go.name.Contains("VFX") && !go.name.Contains("Canvas"))
                    {
                        monsterObj = go;
                        break;
                    }
                }
            }

            if (monsterObj != null)
            {
                monsterObj.tag = "Enemy";
                monsterObj.transform.position = new Vector3(2.8f, 0f, 0f);
                monsterObj.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

                // Gỡ bỏ Ambush cũ khiến quái bị lặn dưới đất
                var oldAmbush = monsterObj.GetComponent<MonsterBallAmbushController>();
                if (oldAmbush != null) UnityEngine.Object.DestroyImmediate(oldAmbush);

                // Gỡ bỏ Player scripts nếu bị gán nhầm
                var wrongBurn = monsterObj.GetComponent<JangHyuBurnReceiver>();
                if (wrongBurn != null) UnityEngine.Object.DestroyImmediate(wrongBurn);
                var wrongFight = monsterObj.GetComponent<JangHyuFightingController>();
                if (wrongFight != null) UnityEngine.Object.DestroyImmediate(wrongFight);

                if (monsterObj.GetComponent<AnimationEventHandler>() == null)
                {
                    monsterObj.AddComponent<AnimationEventHandler>();
                }

                if (monsterObj.GetComponent<MonsterBallHealth>() == null)
                {
                    monsterObj.AddComponent<MonsterBallHealth>();
                }

                if (monsterObj.GetComponent<MonsterBallAIController>() == null)
                {
                    monsterObj.AddComponent<MonsterBallAIController>();
                }

                EditorUtility.SetDirty(monsterObj);
                Debug.Log("[BakeStreetFighterDirectly] MonsterBall Red đã được thiết lập tại X = 2.8 nhìn sang trái với AI!");
            }

            // 6. Cấu hình Sàn đấu Arena_Ground
            var ground = GameObject.Find("Arena_Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Arena_Ground";
            }
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(35f, 0.5f, 12f);
            EditorUtility.SetDirty(ground);

            // 7. Cấu hình Main Camera
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.55f, -5.6f);
                cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
                cam.fieldOfView = 42f;
                if (cam.GetComponent<StreetFighterCamera>() == null)
                {
                    cam.gameObject.AddComponent<StreetFighterCamera>();
                }
                EditorUtility.SetDirty(cam);
            }

            // 8. Đảm bảo EventSystem tồn tại
            if (GameObject.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                EditorUtility.SetDirty(esGo);
            }

            // 9. Bake toàn bộ hệ thống UI vào Scene
            BakeUIHierarchy(playerObj, monsterObj);

            // 10. Lưu Scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[BakeStreetFighterDirectly] >>> BAKE VÀ LƯU SCENE THÀNH CÔNG 100%! <<<");
        }

        private static void BakeUIHierarchy(GameObject playerObj, GameObject monsterObj)
        {
            var defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") 
                           ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // --- A. Combat_UI_Manager ---
            var uiMgrGo = GameObject.Find("Combat_UI_Manager");
            if (uiMgrGo == null)
            {
                uiMgrGo = new GameObject("Combat_UI_Manager");
            }
            var uiMgr = uiMgrGo.GetComponent<CombatUIManager>();
            if (uiMgr == null)
            {
                uiMgr = uiMgrGo.AddComponent<CombatUIManager>();
            }

            // --- B. Player_HUD_Canvas (Screen Space Overlay) ---
            var pHudGo = GameObject.Find("Player_HUD_Canvas");
            if (pHudGo != null) UnityEngine.Object.DestroyImmediate(pHudGo);

            pHudGo = new GameObject("Player_HUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var pCanvas = pHudGo.GetComponent<Canvas>();
            pCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pCanvas.sortingOrder = 100;

            var scaler = pHudGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Root Frame góc trên trái
            var hudRoot = CreateRect("Player_HUD_Root", pHudGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(520f, 130f));

            // Avatar Ring & Face
            var ring = CreateImage("Avatar_Ring", hudRoot.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(96f, 96f), new Color(0.12f, 0.14f, 0.18f, 0.95f));
            var face = CreateImage("Avatar_Face", ring.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.2f, 0.65f, 0.6f, 1f));
            face.offsetMin = new Vector2(6f, 6f);
            face.offsetMax = new Vector2(-6f, -6f);

            var faceTxt = CreateText("Avatar_Initials", face.transform, "JH", 32, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, defaultFont);
            faceTxt.rectTransform.anchorMin = Vector2.zero;
            faceTxt.rectTransform.anchorMax = Vector2.one;
            faceTxt.rectTransform.offsetMin = Vector2.zero;
            faceTxt.rectTransform.offsetMax = Vector2.zero;

            // Player Name
            var nameTxt = CreateText("Player_Name", hudRoot.transform, "장휴 (JangHyu)", 24, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, defaultFont);
            nameTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
            nameTxt.rectTransform.anchorMax = new Vector2(0f, 1f);
            nameTxt.rectTransform.pivot = new Vector2(0f, 1f);
            nameTxt.rectTransform.anchoredPosition = new Vector2(120f, -6f);
            nameTxt.rectTransform.sizeDelta = new Vector2(280f, 28f);

            // HP Bar (Xanh lá)
            var (hpFill, hpLerp, hpTxt) = CreateBarWithLerp(hudRoot.transform, "HP_Bar", new Vector2(120f, -40f), new Vector2(380f, 26f),
                new Color(0.18f, 0.85f, 0.3f, 1f), new Color(1f, 0.82f, 0.2f, 0.85f), "100 / 100", defaultFont);

            // MP Bar (Xanh dương)
            var (mpFill, _, mpTxt) = CreateBarWithLerp(hudRoot.transform, "MP_Bar", new Vector2(120f, -72f), new Vector2(330f, 16f),
                new Color(0.2f, 0.65f, 0.95f, 1f), new Color(0.5f, 0.8f, 1f, 0.6f), "50 / 50", defaultFont);

            // EXP Bar (Vàng cam)
            CreateBarWithLerp(hudRoot.transform, "EXP_Bar", new Vector2(120f, -94f), new Vector2(280f, 8f),
                new Color(1f, 0.75f, 0.15f, 1f), Color.clear, "", defaultFont);

            // Debug Control Panel (Góc trên phải)
            var ctrlPanel = CreateRect("Debug_Control_Panel", pHudGo.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(320f, 140f));
            var ctrlBg = ctrlPanel.gameObject.AddComponent<Image>();
            ctrlBg.color = new Color(0.08f, 0.1f, 0.12f, 0.85f);

            var aiStatusTxt = CreateText("AI_Status_Text", ctrlPanel.transform, "AI [F1]: ON", 20, FontStyle.Bold, new Color(0.2f, 0.95f, 0.3f), TextAnchor.MiddleLeft, defaultFont);
            aiStatusTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
            aiStatusTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            aiStatusTxt.rectTransform.pivot = new Vector2(0f, 1f);
            aiStatusTxt.rectTransform.anchoredPosition = new Vector2(16f, -12f);
            aiStatusTxt.rectTransform.sizeDelta = new Vector2(-32f, 28f);

            var guideTxt = CreateText("Guide_Text", ctrlPanel.transform, "A / D : Tiến / Lùi (2.5D)\nShift : Dash né đòn\nJ / K : Combo đòn đánh\nF1 : Bật/Tắt AI Quái", 14, FontStyle.Normal, new Color(0.85f, 0.88f, 0.9f), TextAnchor.UpperLeft, defaultFont);
            guideTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
            guideTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            guideTxt.rectTransform.offsetMin = new Vector2(16f, 12f);
            guideTxt.rectTransform.offsetMax = new Vector2(-16f, -44f);

            // --- C. Monster_World_Canvas (World Space) ---
            var mHudGo = GameObject.Find("Monster_World_Canvas");
            if (mHudGo != null) UnityEngine.Object.DestroyImmediate(mHudGo);

            mHudGo = new GameObject("Monster_World_Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var mCanvas = mHudGo.GetComponent<Canvas>();
            mCanvas.renderMode = RenderMode.WorldSpace;
            mCanvas.sortingOrder = 95;
            mHudGo.transform.localScale = Vector3.one * 0.01f;
            mHudGo.transform.position = new Vector3(2.8f, 2.15f, 0f);

            var mRoot = CreateRect("Bar_Root", mHudGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 55f));

            var mNameTxt = CreateText("Monster_Name", mRoot.transform, "MonsterBall Red (적구)", 16, FontStyle.Bold, new Color(1f, 0.9f, 0.8f), TextAnchor.MiddleCenter, defaultFont);
            mNameTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
            mNameTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            mNameTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
            mNameTxt.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            mNameTxt.rectTransform.sizeDelta = new Vector2(0f, 20f);

            var (mHpFill, mHpLerp, mHpTxt) = CreateBarWithLerp(mRoot.transform, "Monster_HP_Bar", new Vector2(0f, -28f), new Vector2(210f, 20f),
                new Color(0.95f, 0.15f, 0.15f, 1f), new Color(1f, 0.85f, 0.2f, 0.9f), "200 / 200", defaultFont, isCentered: true);

            // --- D. Gán references vào CombatUIManager ---
            uiMgr.ScreenCanvas = pCanvas;
            uiMgr.PlayerHpFill = hpFill;
            uiMgr.PlayerHpFillLerp = hpLerp;
            uiMgr.PlayerHpText = hpTxt;
            uiMgr.PlayerMpFill = mpFill;
            uiMgr.PlayerMpText = mpTxt;

            uiMgr.MonsterWorldCanvas = mCanvas;
            uiMgr.MonsterBarRoot = mRoot.transform;
            uiMgr.MonsterHpFill = mHpFill;
            uiMgr.MonsterHpFillLerp = mHpLerp;
            uiMgr.MonsterHpText = mHpTxt;
            uiMgr.MonsterNameText = mNameTxt;

            uiMgr.AiStatusText = aiStatusTxt;

            if (playerObj != null)
            {
                var burn = playerObj.GetComponent<JangHyuBurnReceiver>();
                var so = new SerializedObject(uiMgr);
                so.FindProperty("_playerReceiver").objectReferenceValue = burn;
                if (monsterObj != null)
                {
                    so.FindProperty("_monsterHealth").objectReferenceValue = monsterObj.GetComponent<MonsterBallHealth>();
                    so.FindProperty("_monsterAI").objectReferenceValue = monsterObj.GetComponent<MonsterBallAIController>();
                }
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(pHudGo);
            EditorUtility.SetDirty(mHudGo);
            EditorUtility.SetDirty(uiMgrGo);
            Debug.Log("[BakeStreetFighterDirectly] Hierarchy UI đã được dựng và liên kết đầy đủ vào Scene!");
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var rt = CreateRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor align, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.font = font;
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = align;
            return txt;
        }

        private static (Image fill, Image lerp, Text label) CreateBarWithLerp(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor, Color lerpColor, string initialText, Font font, bool isCentered = false)
        {
            var barRoot = CreateRect(name, parent, isCentered ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f),
                                                 isCentered ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f),
                                                 isCentered ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f),
                                                 pos, size);

            // Border viền đen
            var bgImg = barRoot.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);

            // Lerp Bar (vệt trễ vàng)
            Image lerpImg = null;
            if (lerpColor != Color.clear)
            {
                var lerpGo = CreateRect("LerpFill", barRoot.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                lerpGo.offsetMin = new Vector2(2f, 2f);
                lerpGo.offsetMax = new Vector2(-2f, -2f);
                lerpImg = lerpGo.gameObject.AddComponent<Image>();
                lerpImg.color = lerpColor;
                lerpImg.type = Image.Type.Filled;
                lerpImg.fillMethod = Image.FillMethod.Horizontal;
                lerpImg.fillOrigin = 0;
                lerpImg.fillAmount = 1f;
            }

            // Main Fill Bar
            var fillGo = CreateRect("MainFill", barRoot.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fillGo.offsetMin = new Vector2(2f, 2f);
            fillGo.offsetMax = new Vector2(-2f, -2f);
            var fillImg = fillGo.gameObject.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 1f;

            // Label text
            Text txtComp = null;
            if (!string.IsNullOrEmpty(initialText))
            {
                txtComp = CreateText("ValueText", barRoot.transform, initialText, Mathf.Max(11, (int)(size.y * 0.65f)), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, font);
                txtComp.rectTransform.anchorMin = Vector2.zero;
                txtComp.rectTransform.anchorMax = Vector2.one;
                txtComp.rectTransform.offsetMin = Vector2.zero;
                txtComp.rectTransform.offsetMax = Vector2.zero;
            }

            return (fillImg, lerpImg, txtComp);
        }
    }
}
