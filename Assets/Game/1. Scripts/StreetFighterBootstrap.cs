using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using Aiara.Combat;
using Aiara.UI;
using Aiara.Test;

namespace Aiara
{
    public static class StreetFighterBootstrap
    {
        private const string OutputDir = @"C:\Users\Admin\.gemini\antigravity\brain\65c98726-979a-4b32-944b-88da764e48fb\gameplay_frames";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void OnSceneLoaded()
        {
            Debug.Log("[StreetFighterBootstrap] Auto-normalizing scene for 2.5D Street Fighter Gameplay...");
            ApplySetup(isPlayMode: true);
        }

#if UNITY_EDITOR
        [MenuItem("Tools/StreetFighter/Apply Full Setup")]
        public static void MenuApplySetup()
        {
            ApplySetup(isPlayMode: Application.isPlaying);
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[StreetFighterBootstrap] Setup successfully baked and saved to scene!");
        }
#endif

        public static void ApplySetup(bool isPlayMode)
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            // Dọn dẹp tất cả rác Ellen, Key, Simple, Quad lỗi tím
            var allSceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allSceneObjects)
            {
                if (go == null) continue;
                string gn = go.name.ToLower();
                if (gn.Contains("ellen") || gn.Contains("vfx_groundcrack") || gn.Contains("vfx_grounderupt") || 
                    gn.Contains("simple(clone)") || gn.Contains("flame simple") || gn == "key")
                {
                    if (isPlayMode) Object.Destroy(go);
                    else Object.DestroyImmediate(go);
                }
            }

            // 1. Tìm Player (JangHyu)
            GameObject playerObj = GameObject.Find("JangHyu_AttackCombo2");
            if (playerObj == null)
            {
                var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var go in allGos)
                {
                    if (go.name.Contains("JangHyu") && !go.name.Contains("VFX"))
                    {
                        playerObj = go;
                        break;
                    }
                }
            }

            // 2. Tìm MonsterBall
            GameObject monsterObj = GameObject.Find("MonsterBall_red");
            if (monsterObj == null)
            {
                var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var go in allGos)
                {
                    if (go.name.Contains("MonsterBall") && !go.name.Contains("VFX"))
                    {
                        monsterObj = go;
                        break;
                    }
                }
            }

            // 3. Khắc phục Player (JangHyu)
            if (playerObj != null)
            {
                playerObj.tag = "Player";
                playerObj.transform.position = new Vector3(-3.2f, 0f, 0f);
                playerObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                // Gỡ bỏ MonsterBallAmbushController nếu bị gán nhầm
                var wrongAmbush = playerObj.GetComponent<MonsterBallAmbushController>();
                if (wrongAmbush != null)
                {
                    if (isPlayMode) Object.Destroy(wrongAmbush);
                    else Object.DestroyImmediate(wrongAmbush);
                }

                // Gắn AnimationEventHandler trên Player và tất cả Animator con
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

                // Gắn JangHyuBurnReceiver
                if (playerObj.GetComponent<JangHyuBurnReceiver>() == null)
                {
                    playerObj.AddComponent<JangHyuBurnReceiver>();
                }

                // Gắn JangHyuFightingController
                var fightCtrl = playerObj.GetComponent<JangHyuFightingController>();
                if (fightCtrl == null)
                {
                    fightCtrl = playerObj.AddComponent<JangHyuFightingController>();
                }

                Debug.Log("[StreetFighterBootstrap] Player (JangHyu) configured at X = -3.2");
            }

            // 4. Khắc phục MonsterBall
            if (monsterObj != null)
            {
                try { monsterObj.tag = "Enemy"; } catch { }
                monsterObj.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
                monsterObj.transform.localScale = Vector3.one * 0.7f;

                // Bật SkinnedMeshRenderer thân chính (Group) và ẩn mảnh vỡ (group1)
                var groupTr = monsterObj.transform.Find("Group");
                if (groupTr != null)
                {
                    groupTr.gameObject.SetActive(true);
                    foreach (var smr in groupTr.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        smr.enabled = true;
                    }
                    var rootAnim = monsterObj.GetComponent<Animator>();
                    if (rootAnim != null)
                    {
                        rootAnim.enabled = false;
                    }
                }
                var group1Tr = monsterObj.transform.Find("group1");
                if (group1Tr != null)
                {
                    group1Tr.gameObject.SetActive(false);
                }

                // Gỡ bỏ script Player nếu bị gán nhầm
                var wrongBurn = monsterObj.GetComponent<JangHyuBurnReceiver>();
                if (wrongBurn != null)
                {
                    if (isPlayMode) Object.Destroy(wrongBurn);
                    else Object.DestroyImmediate(wrongBurn);
                }
                var wrongFight = monsterObj.GetComponent<JangHyuFightingController>();
                if (wrongFight != null)
                {
                    if (isPlayMode) Object.Destroy(wrongFight);
                    else Object.DestroyImmediate(wrongFight);
                }

                // Gắn AnimationEventHandler
                if (monsterObj.GetComponent<AnimationEventHandler>() == null)
                {
                    monsterObj.AddComponent<AnimationEventHandler>();
                }

                // Gắn MonsterBallHealth
                if (monsterObj.GetComponent<MonsterBallHealth>() == null)
                {
                    monsterObj.AddComponent<MonsterBallHealth>();
                }

                // Gỡ bỏ Ambush cũ nếu có
                var oldAmbush = monsterObj.GetComponent<MonsterBallAmbushController>();
                if (oldAmbush != null)
                {
                    if (isPlayMode) Object.Destroy(oldAmbush);
                    else Object.DestroyImmediate(oldAmbush);
                }

                // Gắn MonsterBallAIController
                var ai = monsterObj.GetComponent<MonsterBallAIController>();
                if (ai == null)
                {
                    ai = monsterObj.AddComponent<MonsterBallAIController>();
                }
                if (ai != null)
                {
                    ai.IsAIEnabled = true;
                    ai.SpawnAtDefaultPosition();
                }

                Debug.Log("[StreetFighterBootstrap] MonsterBall configured with AI Controller at X = 3.5 (Hidden state)");
            }

            // 5. Setup Arena Ground (xóa trùng lặp nếu có)
            var grounds = new System.Collections.Generic.List<GameObject>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name == "Arena_Ground") grounds.Add(go);
            }
            for (int i = 1; i < grounds.Count; i++)
            {
                if (isPlayMode) Object.Destroy(grounds[i]);
                else Object.DestroyImmediate(grounds[i]);
            }
            var ground = grounds.Count > 0 ? grounds[0] : GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (grounds.Count == 0)
            {
                ground.name = "Arena_Ground";
            }
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(35f, 0.5f, 12f);

            // 6. Xóa vĩnh viễn các GameObject rác gây lỗi tím và cọc rác Key
            string[] trashNames = { "VFX_GroundCrack", "VFX_GroundErupt", "VFX_BodyFlameAura", "VFX_RollTrail", "VFX_RollTrail_AI", "VFX_GroundErupt_AI", "Key", "Destructive Elements Flame Simple(Clone)", "Destructive Elements Flame VFX Simple", "Destructive Elements Flame Simple" };
            foreach (var trash in trashNames)
            {
                var go = GameObject.Find(trash);
                while (go != null)
                {
                    if (isPlayMode) Object.Destroy(go);
                    else Object.DestroyImmediate(go);
                    go = GameObject.Find(trash);
                }
            }

            // 7. Setup Camera 2.5D
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.55f, -5.6f);
                cam.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
                cam.fieldOfView = 42f;
                if (cam.GetComponent<StreetFighterCamera>() == null)
                {
                    cam.gameObject.AddComponent<StreetFighterCamera>();
                }
            }

            // 8. Đảm bảo UI Manager tồn tại
            if (Object.FindFirstObjectByType<CombatUIManager>() == null)
            {
                var uiGo = new GameObject("Combat_UI_Manager");
                uiGo.AddComponent<CombatUIManager>();
            }

            Debug.Log("[StreetFighterBootstrap] === ĐÃ HOÀN TẤT CHUẨN HÓA 2.5D STREET FIGHTER ===");
        }
    }
}
