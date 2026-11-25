using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Aiara.Combat;
using Aiara.UI;

namespace Aiara.EditorTools
{
    public static class FixPinkAndSetupUI
    {
        [MenuItem("Tools/MonsterBall/Fix Pink and Setup UI Now")]
        public static void ExecuteFix()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            Debug.Log("[FixPinkAndSetupUI] Starting automated repair and UI integration...");

            // 1. Tắt Play Mode nếu đang chạy để lưu Scene chuẩn
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[FixPinkAndSetupUI] Exiting Play Mode to safely save scene...");
                EditorApplication.isPlaying = false;
            }

            // 2. Sửa các Materials bị lỗi Shader
            FixVfxMaterials();

            // 3. Setup Scene hiện tại
            SetupCurrentScene();

            Debug.Log("[FixPinkAndSetupUI] Repair and UI integration completed successfully!");
        }

        private static void FixVfxMaterials()
        {
            // Tìm Shader hợp lệ
            var toonLitShader = Shader.Find("Shader Graphs/Toon Lit");
            var urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            var particleUnlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            var targetShader = toonLitShader ?? urpLitShader ?? urpUnlitShader ?? Shader.Find("Standard");

            // Fix VFX_CrackMat
            string crackMatPath = "Assets/Game/4. Rendering/Materials/VFX_CrackMat.mat";
            var crackMat = AssetDatabase.LoadAssetAtPath<Material>(crackMatPath);
            if (crackMat == null)
            {
                crackMat = new Material(targetShader);
                AssetDatabase.CreateAsset(crackMat, crackMatPath);
            }
            else
            {
                crackMat.shader = targetShader;
            }
            crackMat.color = new Color(0.12f, 0.08f, 0.05f, 0.9f);
            EditorUtility.SetDirty(crackMat);

            // Fix VFX_DebrisMat
            string debrisMatPath = "Assets/Game/4. Rendering/Materials/VFX_DebrisMat.mat";
            var debrisMat = AssetDatabase.LoadAssetAtPath<Material>(debrisMatPath);
            if (debrisMat == null)
            {
                debrisMat = new Material(particleUnlitShader ?? targetShader);
                AssetDatabase.CreateAsset(debrisMat, debrisMatPath);
            }
            else
            {
                debrisMat.shader = particleUnlitShader ?? targetShader;
            }
            debrisMat.color = new Color(0.4f, 0.28f, 0.15f, 1f);
            EditorUtility.SetDirty(debrisMat);

            // Fix VFX_FlameMat
            string flameMatPath = "Assets/Game/4. Rendering/Materials/VFX_FlameMat.mat";
            var flameMat = AssetDatabase.LoadAssetAtPath<Material>(flameMatPath);
            if (flameMat == null)
            {
                flameMat = new Material(particleUnlitShader ?? targetShader);
                AssetDatabase.CreateAsset(flameMat, flameMatPath);
            }
            else
            {
                flameMat.shader = particleUnlitShader ?? targetShader;
            }
            flameMat.color = new Color(1.0f, 0.45f, 0.1f, 1f);
            EditorUtility.SetDirty(flameMat);

            // Fix VFX_SmokeMat
            string smokeMatPath = "Assets/Game/4. Rendering/Materials/VFX_SmokeMat.mat";
            var smokeMat = AssetDatabase.LoadAssetAtPath<Material>(smokeMatPath);
            if (smokeMat == null)
            {
                smokeMat = new Material(particleUnlitShader ?? targetShader);
                AssetDatabase.CreateAsset(smokeMat, smokeMatPath);
            }
            else
            {
                smokeMat.shader = particleUnlitShader ?? targetShader;
            }
            smokeMat.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            EditorUtility.SetDirty(smokeMat);

            AssetDatabase.SaveAssets();
            Debug.Log("[FixPinkAndSetupUI] VFX Materials successfully updated with valid shaders.");
        }

        private static void SetupCurrentScene()
        {
            var activeScene = SceneManager.GetActiveScene();

            // 1. Kiểm tra và dọn dẹp hoặc sửa VFX_GroundCrack Quad bị tím
            var groundCrack = GameObject.Find("VFX_GroundCrack");
            if (groundCrack != null)
            {
                var mr = groundCrack.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var crackMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/4. Rendering/Materials/VFX_CrackMat.mat");
                    mr.sharedMaterial = crackMat;
                }
                // Tắt active mặc định để không hiện khối chữ nhật thô
                groundCrack.SetActive(false);
                EditorUtility.SetDirty(groundCrack);
            }

            // 2. Căn chỉnh vị trí & Gắn MonsterBallHealth vào MonsterBall_red chuẩn Street Fighter 2.5D
            var monster = GameObject.Find("MonsterBall_red");
            if (monster != null)
            {
                monster.transform.position = new Vector3(3.5f, 0f, 0f);
                monster.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

                var health = monster.GetComponent<MonsterBallHealth>();
                if (health == null)
                {
                    health = monster.AddComponent<MonsterBallHealth>();
                    Debug.Log("[FixPinkAndSetupUI] Attached MonsterBallHealth to MonsterBall_red.");
                }

                var ambush = monster.GetComponent<MonsterBallAmbushController>();
                if (ambush != null)
                {
                    var soAmbush = new SerializedObject(ambush);
                    soAmbush.FindProperty("_triggerDistance").floatValue = 6.0f;
                    soAmbush.FindProperty("_autoEmergeDelay").floatValue = 1.0f;
                    soAmbush.FindProperty("_groundSpawnPos").vector3Value = new Vector3(3.5f, 0f, 0f);
                    soAmbush.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(monster);
            }

            // 3. Tạo Combat_UI_Manager
            var uiMgrGo = GameObject.Find("Combat_UI_Manager");
            if (uiMgrGo == null)
            {
                uiMgrGo = new GameObject("Combat_UI_Manager");
                uiMgrGo.AddComponent<CombatUIManager>();
                Debug.Log("[FixPinkAndSetupUI] Created Combat_UI_Manager in scene.");
            }
            else
            {
                if (uiMgrGo.GetComponent<CombatUIManager>() == null)
                {
                    uiMgrGo.AddComponent<CombatUIManager>();
                }
            }
            EditorUtility.SetDirty(uiMgrGo);

            // 4. Căn chỉnh vị trí & Gắn Fighting Controller cho JangHyu
            var player = GameObject.Find("JangHyu_AttackCombo2");
            if (player != null)
            {
                player.transform.position = new Vector3(-3.2f, 0f, 0f);
                player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                if (player.GetComponent<AnimationEventHandler>() == null)
                {
                    player.AddComponent<AnimationEventHandler>();
                }
                if (player.GetComponent<JangHyuFightingController>() == null)
                {
                    player.AddComponent<JangHyuFightingController>();
                }
                if (player.GetComponent<JangHyuBurnReceiver>() == null)
                {
                    player.AddComponent<JangHyuBurnReceiver>();
                }
                EditorUtility.SetDirty(player);
            }

            // 5. Căn chỉnh Sàn Đấu Arena_Ground
            var ground = GameObject.Find("Arena_Ground");
            if (ground != null)
            {
                ground.transform.position = new Vector3(0f, -0.25f, 0f);
                ground.transform.localScale = new Vector3(35f, 0.5f, 12f);
                EditorUtility.SetDirty(ground);
            }

            // 6. Căn chỉnh Camera 2.5D Street Fighter
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.55f, -5.6f);
                cam.transform.rotation = Quaternion.Euler(5.0f, 0f, 0f);
                cam.fieldOfView = 42.0f;
                if (cam.GetComponent<StreetFighterCamera>() == null)
                {
                    cam.gameObject.AddComponent<StreetFighterCamera>();
                }
                EditorUtility.SetDirty(cam);
            }

            // 7. Gắn Gameplay Test Bot & Recorder
            var recorder = GameObject.Find("Gameplay_Test_Bot_And_Recorder");
            if (recorder == null)
            {
                recorder = new GameObject("Gameplay_Test_Bot_And_Recorder");
                recorder.AddComponent<Aiara.Test.GameplayTestBotAndRecorder>();
                EditorUtility.SetDirty(recorder);
            }

            // Lưu Scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[FixPinkAndSetupUI] Scene '{activeScene.name}' saved with all repairs and UI manager!");
        }
    }
}

