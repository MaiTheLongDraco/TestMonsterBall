using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Aiara.Combat;
using Aiara.UI;
using Aiara.Test;

namespace Aiara.EditorTools
{
    public static class RunStreetFighterSceneAuto
    {
        private const string OutputDir = @"C:\Users\Admin\.gemini\antigravity\brain\65c98726-979a-4b32-944b-88da764e48fb\gameplay_frames";

        [MenuItem("Tools/StreetFighter/Setup And Capture Now")]
        public static void ExecuteFullSetupAndCapture()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            Debug.Log("[StreetFighterAuto] === BẮT ĐẦU SETUP SCENE 2.5D STREET FIGHTER ===");

            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            var scene = SceneManager.GetActiveScene();

            // 1. Sàn đấu Arena_Ground (Trải dài 35m theo trục X)
            var ground = GameObject.Find("Arena_Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Arena_Ground";
            }
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(35f, 0.5f, 12f);
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UNI VFX/Common/Materials/UNI-Ground.mat");
            if (groundMat != null)
            {
                ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            }
            EditorUtility.SetDirty(ground);

            // 2. Người chơi JangHyu (Đứng bên trái tại X = -3.2, mặt quay sang phải Y = 90)
            var player = GameObject.Find("JangHyu_AttackCombo2") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.tag = "Player";
                player.transform.position = new Vector3(-3.2f, 0f, 0f);
                player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                if (player.GetComponent<AnimationEventHandler>() == null) player.AddComponent<AnimationEventHandler>();
                if (player.GetComponent<JangHyuBurnReceiver>() == null) player.AddComponent<JangHyuBurnReceiver>();

                var fightCtrl = player.GetComponent<JangHyuFightingController>();
                if (fightCtrl == null) fightCtrl = player.AddComponent<JangHyuFightingController>();

                EditorUtility.SetDirty(player);
            }

            // 3. Quái vật MonsterBall_red (Đứng bên phải tại X = 3.5, mặt quay sang trái Y = 270)
            var monster = GameObject.Find("MonsterBall_red") ?? GameObject.FindWithTag("Enemy");
            if (monster != null)
            {
                monster.transform.position = new Vector3(3.5f, 0f, 0f);
                monster.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

                if (monster.GetComponent<AnimationEventHandler>() == null) monster.AddComponent<AnimationEventHandler>();

                var health = monster.GetComponent<MonsterBallHealth>();
                if (health == null) health = monster.AddComponent<MonsterBallHealth>();

                // Gỡ bỏ Ambush cũ nếu có
                var ambush = monster.GetComponent<MonsterBallAmbushController>();
                if (ambush != null) Object.DestroyImmediate(ambush);

                // Gắn AI Controller
                var ai = monster.GetComponent<MonsterBallAIController>();
                if (ai == null) ai = monster.AddComponent<MonsterBallAIController>();
                ai.IsAIEnabled = true;

                EditorUtility.SetDirty(monster);
            }

            // 4. Ẩn Quad lỗi màu tím VFX_GroundCrack và các GameObject rác
            string[] trashNames = { "VFX_GroundCrack", "VFX_GroundErupt", "VFX_BodyFlameAura", "VFX_RollTrail", "VFX_RollTrail_AI", "VFX_GroundErupt_AI" };
            foreach (var trash in trashNames)
            {
                var go = GameObject.Find(trash);
                if (go != null)
                {
                    go.SetActive(false);
                    EditorUtility.SetDirty(go);
                }
            }

            // 5. Main Camera 2.5D Street Fighter
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.55f, -5.6f);
                cam.transform.rotation = Quaternion.Euler(5.0f, 0f, 0f);
                cam.fieldOfView = 42.0f;

                var sfCam = cam.GetComponent<StreetFighterCamera>();
                if (sfCam == null) sfCam = cam.gameObject.AddComponent<StreetFighterCamera>();

                EditorUtility.SetDirty(cam);
            }

            // 6. Combat UI Manager
            var uiMgr = GameObject.Find("Combat_UI_Manager");
            if (uiMgr == null) uiMgr = new GameObject("Combat_UI_Manager");
            if (uiMgr.GetComponent<CombatUIManager>() == null) uiMgr.AddComponent<CombatUIManager>();
            EditorUtility.SetDirty(uiMgr);

            // 7. Gameplay Test Bot And Recorder
            var testBot = GameObject.Find("Gameplay_Test_Bot_And_Recorder");
            if (testBot == null) testBot = new GameObject("Gameplay_Test_Bot_And_Recorder");
            if (testBot.GetComponent<GameplayTestBotAndRecorder>() == null) testBot.AddComponent<GameplayTestBotAndRecorder>();
            EditorUtility.SetDirty(testBot);

            // 8. Lưu Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[StreetFighterAuto] Scene '{scene.name}' đã được lưu hoàn chỉnh!");

            // 9. Chụp ảnh Render Preview Scene View 2.5D ngay lập tức
            if (cam != null)
            {
                RenderCameraToDisk(cam, Path.Combine(OutputDir, "frame_00_scene_overview_2d.jpg"));
            }

            Debug.Log("[StreetFighterAuto] === HOÀN THÀNH SETUP SCENE VÀ CHỤP PREVIEW ===");
        }

        [MenuItem("Tools/StreetFighter/Trigger Test Bot Scenario (In Play Mode)")]
        public static void TriggerTestBotScenario()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[StreetFighterAuto] Cần phải ở Play Mode để kích hoạt Test Bot!");
                return;
            }

            var bot = Object.FindFirstObjectByType<GameplayTestBotAndRecorder>();
            if (bot != null)
            {
                bot.StartAutoTest();
                Debug.Log("[StreetFighterAuto] Đã kích hoạt diễn tập 9 pha Test Bot!");
            }
        }

        private static void RenderCameraToDisk(Camera cam, string filePath, int width = 1920, int height = 1080)
        {
            try
            {
                var rt = new RenderTexture(width, height, 24);
                var prevTarget = cam.targetTexture;
                var prevActive = RenderTexture.active;

                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                Object.DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToJPG(90);
                File.WriteAllBytes(filePath, bytes);
                Object.DestroyImmediate(tex);

                Debug.Log($"[StreetFighterAuto] Đã chụp ảnh render thành công tới: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[StreetFighterAuto] Lỗi khi render camera: {ex.Message}");
            }
        }
    }
}
