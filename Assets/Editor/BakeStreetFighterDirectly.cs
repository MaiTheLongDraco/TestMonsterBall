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
                monsterObj.transform.rotation = Quaternion.Euler(0f, 205f, 0f);

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
            CreateUiBrushSprites.GenerateAllSprites();

            var uiMgrGo = GameObject.Find("Combat_UI_Manager");
            if (uiMgrGo == null)
            {
                uiMgrGo = new GameObject("Combat_UI_Manager");
            }
            var uiMgr = uiMgrGo.GetComponent<CombatUIManager>() ?? uiMgrGo.AddComponent<CombatUIManager>();
            uiMgr.RebuildAllUI();
            uiMgr.FindReferences();

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

            if (uiMgr.ScreenCanvas != null) EditorUtility.SetDirty(uiMgr.ScreenCanvas.gameObject);
            if (uiMgr.MonsterWorldCanvas != null) EditorUtility.SetDirty(uiMgr.MonsterWorldCanvas.gameObject);
            EditorUtility.SetDirty(uiMgrGo);
            Debug.Log("[BakeStreetFighterDirectly] Hierarchy UI đã được dựng với Sprite cọ mực và liên kết đầy đủ vào Scene!");
        }
    }
}
