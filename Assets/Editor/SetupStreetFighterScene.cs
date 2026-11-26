using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.UI;
using System.IO;
using Aiara.Combat;
using Aiara.UI;
using Aiara.EditorTools;

namespace Aiara.Editor
{
    public static class SetupStreetFighterScene
    {
        private static bool _hasBaked = false;

        [MenuItem("Tools/StreetFighter/Bake And Clean Full Setup")]
        public static void BakeAndClean()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            Debug.Log("[SetupStreetFighterScene] Bắt đầu tự động cấu hình và làm sạch Scene...");

            // 1. Dọn dẹp triệt để các GameObject rác: Ellen, Key, Simple, Quad lỗi tím
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                string gn = go.name.ToLower();
                if (gn.Contains("ellen") || gn.Contains("vfx_groundcrack") || gn.Contains("vfx_grounderupt") || 
                    gn.Contains("simple(clone)") || gn.Contains("flame simple") || gn == "key" || gn.Contains("artifact"))
                {
                    Debug.Log($"[SetupStreetFighterScene] Đang gỡ bỏ GameObject rác: {go.name}");
                    Undo.DestroyObjectImmediate(go);
                }
            }

            // Đảm bảo Test Bot không tự động chạy làm chết người chơi khi Play
            var testBot = Object.FindFirstObjectByType<Aiara.Test.GameplayTestBotAndRecorder>();
            if (testBot != null)
            {
                var botSo = new SerializedObject(testBot);
                var autoProp = botSo.FindProperty("_autoRunTest");
                if (autoProp != null)
                {
                    autoProp.boolValue = false;
                    botSo.ApplyModifiedProperties();
                }
            }

            // 2. Cấu hình Animator Controller cho JangHyu
            SetupJangHyuAnimator();

            // 3. Cấu hình Player GameObject
            SetupPlayer();

            // 4. Cấu hình MonsterBall GameObject
            SetupMonsterBall();

            // 5. Cấu hình Sàn đấu & Camera
            SetupArenaAndCamera();

            // 6. Xây dựng và Bake toàn bộ UI tĩnh (HUD) & UI World Space vào Scene
            SetupFullUI();

            // Đánh dấu Scene Dirty và lưu lại
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SetupStreetFighterScene] === HOÀN TẤT THÀNH CÔNG 100%! ĐÃ BAKE ĐẦY ĐỦ UI VÀO SCENE ===");
        }

        private static void SetupJangHyuAnimator()
        {
            string controllerPath = "Assets/Game/3. Resources/Characters/Player/JangHyu/Anmation/JangHyu_CombatController.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogError($"[SetupStreetFighterScene] Không tìm thấy controller tại {controllerPath}");
                return;
            }

            var rootStateMachine = controller.layers[0].stateMachine;

            EnsureBoolParameter(controller, "IsWalkingBackward", false);

            AnimationClip clipWalkFwd = LoadNamedClipFromFbx(
                "Assets/Game/3. Resources/Characters/Shared/_LegacyClipSources/Backup/Characters/AnimationsTemp/BattleMovement/JangHyu_WalkFoward.fbx",
                "WalkFoward");
            AnimationClip clipWalkBwd = LoadNamedClipFromFbx(
                "Assets/Game/3. Resources/Characters/Shared/_LegacyClipSources/Backup/Characters/AnimationsTemp/BattleMovement/JangHyu_WalkBackward.fbx",
                "WalkBackward");
            AnimationClip clipDead = LoadNamedClipFromFbx(
                "Assets/Game/3. Resources/Characters/Player/JangHyu/Anmation/Die/JangHyu_Dead.fbx",
                "Dead");
            AnimationClip clipRevival = LoadNamedClipFromFbx(
                "Assets/Game/3. Resources/Characters/Player/JangHyu/Anmation/Die/JangHyu_revival.fbx",
                "Revival");

            AnimationClip clipCombo1 = LoadClipFromFbx("Assets/Game/3. Resources/Characters/Player/JangHyu/Anmation/Attack/Normal/New/New2/JangHyu_AttackCombo2.fbx");
            AnimationClip clipCombo2 = LoadClipFromFbx("Assets/Game/3. Resources/Characters/Player/JangHyu/Anmation/Attack/Normal/New/New2/JangHyu_AttackCombo3.fbx");
            AnimationClip clipCombo3 = LoadClipFromFbx("Assets/Game/3. Resources/Characters/Player/JangHyu/Anmation/Attack/Normal/New/New2/JangHyu_AttackCombo4.fbx");
            AnimationClip clipLeg = LoadClipFromFbx("Assets/Game/3. Resources/Characters/Player/JangHyu/Anmation/Jump/JangHyu_Attack_Leg.fbx");

            var idleState = FindState(rootStateMachine, "BattleIdle");
            var walkFwdState = FindOrCreateState(rootStateMachine, "Walk", clipWalkFwd);
            var walkBwdState = FindOrCreateState(rootStateMachine, "WalkBackward", clipWalkBwd);
            var knockdownState = FindOrCreateState(rootStateMachine, "Knockdown", clipDead);
            knockdownState.speed = 1.6f;
            var standUpState = FindOrCreateState(rootStateMachine, "StandUp", clipRevival);
            standUpState.speed = 2.6f;
            var deadState = FindOrCreateState(rootStateMachine, "Dead", clipDead);
            deadState.speed = 1.0f;
            ClearTransitions(deadState);

            // Chỉnh lại chuyển trạng thái Idle <-> Walk (góc nghiêng combat) <-> WalkBackward
            if (idleState != null)
            {
                ClearTransitionsTo(idleState, walkFwdState);
                ClearTransitionsTo(idleState, walkBwdState);

                var toFwd = idleState.AddTransition(walkFwdState);
                toFwd.hasExitTime = false;
                toFwd.duration = 0.12f;
                toFwd.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                toFwd.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsWalkingBackward");

                var toBwd = idleState.AddTransition(walkBwdState);
                toBwd.hasExitTime = false;
                toBwd.duration = 0.12f;
                toBwd.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                toBwd.AddCondition(AnimatorConditionMode.If, 0f, "IsWalkingBackward");

                ClearTransitions(walkFwdState);
                var fwdToIdle = walkFwdState.AddTransition(idleState);
                fwdToIdle.hasExitTime = false;
                fwdToIdle.duration = 0.12f;
                fwdToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

                var fwdToBwd = walkFwdState.AddTransition(walkBwdState);
                fwdToBwd.hasExitTime = false;
                fwdToBwd.duration = 0.12f;
                fwdToBwd.AddCondition(AnimatorConditionMode.If, 0f, "IsWalkingBackward");

                ClearTransitions(walkBwdState);
                var bwdToIdle = walkBwdState.AddTransition(idleState);
                bwdToIdle.hasExitTime = false;
                bwdToIdle.duration = 0.12f;
                bwdToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

                var bwdToFwd = walkBwdState.AddTransition(walkFwdState);
                bwdToFwd.hasExitTime = false;
                bwdToFwd.duration = 0.12f;
                bwdToFwd.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsWalkingBackward");

                // Knockdown -> StandUp -> BattleIdle để nhân vật tự đứng dậy sau khi ngã
                ClearTransitions(knockdownState);
                var kdToStand = knockdownState.AddTransition(standUpState);
                kdToStand.hasExitTime = true;
                kdToStand.exitTime = 0.88f;
                kdToStand.duration = 0.12f;

                ClearTransitions(standUpState);
                var standToIdle = standUpState.AddTransition(idleState);
                standToIdle.hasExitTime = true;
                standToIdle.exitTime = 0.90f;
                standToIdle.duration = 0.15f;
            }

            AddOrUpdateState(rootStateMachine, "Combo1", clipCombo1);
            AddOrUpdateState(rootStateMachine, "Combo2", clipCombo2);
            AddOrUpdateState(rootStateMachine, "Combo3", clipCombo3);
            AddOrUpdateState(rootStateMachine, "LegAttack", clipLeg);

            EditorUtility.SetDirty(controller);
        }

        private static void EnsureBoolParameter(AnimatorController controller, string paramName, bool defaultValue)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == paramName) return;
            }
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = paramName,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = defaultValue
            });
        }

        private static AnimationClip LoadNamedClipFromFbx(string fbxPath, string clipName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            AnimationClip fallback = null;
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    if (clip.name == clipName) return clip;
                    if (fallback == null) fallback = clip;
                }
            }
            return fallback;
        }

        private static AnimatorState FindState(AnimatorStateMachine sm, string stateName)
        {
            foreach (var cs in sm.states)
            {
                if (cs.state.name == stateName) return cs.state;
            }
            return null;
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string stateName, AnimationClip clip)
        {
            var st = FindState(sm, stateName) ?? sm.AddState(stateName);
            if (clip != null) st.motion = clip;
            return st;
        }

        private static void ClearTransitions(AnimatorState state)
        {
            foreach (var t in state.transitions)
            {
                state.RemoveTransition(t);
            }
        }

        private static void ClearTransitionsTo(AnimatorState from, AnimatorState to)
        {
            foreach (var t in from.transitions)
            {
                if (t.destinationState == to)
                {
                    from.RemoveTransition(t);
                }
            }
        }

        private static AnimationClip LoadClipFromFbx(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static void AddOrUpdateState(AnimatorStateMachine sm, string stateName, AnimationClip clip)
        {
            if (clip == null) return;

            AnimatorState targetState = null;
            foreach (var childState in sm.states)
            {
                if (childState.state.name == stateName)
                {
                    targetState = childState.state;
                    break;
                }
            }

            if (targetState == null)
            {
                targetState = sm.AddState(stateName);
            }

            targetState.motion = clip;

            AnimatorState idleState = null;
            foreach (var cs in sm.states)
            {
                if (cs.state.name == "BattleIdle")
                {
                    idleState = cs.state;
                    break;
                }
            }

            if (idleState != null)
            {
                bool hasExitTransition = false;
                foreach (var t in targetState.transitions)
                {
                    if (t.destinationState == idleState)
                    {
                        hasExitTransition = true;
                        break;
                    }
                }

                if (!hasExitTransition)
                {
                    var trans = targetState.AddTransition(idleState);
                    trans.hasExitTime = true;
                    trans.exitTime = 0.85f;
                    trans.duration = 0.15f;
                }
            }
        }

        private static void SetupPlayer()
        {
            GameObject playerObj = GameObject.Find("JangHyu_AttackCombo2");
            if (playerObj == null)
            {
                var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var go in allGos)
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
                playerObj.transform.position = new Vector3(-3.2f, 0f, 0f);
                playerObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                var wrongAmbush = playerObj.GetComponent<MonsterBallAmbushController>();
                if (wrongAmbush != null) Undo.DestroyObjectImmediate(wrongAmbush);

                var wrongAI = playerObj.GetComponent<MonsterBallAIController>();
                if (wrongAI != null) Undo.DestroyObjectImmediate(wrongAI);

                var dupMove = playerObj.GetComponent<JangHyuMovementController>();
                if (dupMove != null) Undo.DestroyObjectImmediate(dupMove);

                EnsureAnimationEventHandler(playerObj);

                if (playerObj.GetComponent<JangHyuBurnReceiver>() == null)
                {
                    Undo.AddComponent<JangHyuBurnReceiver>(playerObj);
                }

                var fightCtrl = playerObj.GetComponent<JangHyuFightingController>();
                if (fightCtrl == null)
                {
                    fightCtrl = Undo.AddComponent<JangHyuFightingController>(playerObj);
                }

                var anim = playerObj.GetComponent<Animator>();
                if (anim == null) anim = playerObj.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    var c = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        "Assets/Game/3. Resources/Characters/Player/JangHyu/Anmation/JangHyu_CombatController.controller");
                    if (c != null) anim.runtimeAnimatorController = c;
                }
            }
        }

        private static void SetupMonsterBall()
        {
            GameObject monsterObj = GameObject.Find("MonsterBall_red");
            if (monsterObj == null)
            {
                var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var go in allGos)
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
                try { monsterObj.tag = "Enemy"; } catch { }
                monsterObj.transform.position = new Vector3(3.5f, 0.08f, 0f);
                monsterObj.transform.rotation = Quaternion.Euler(0f, 205f, 0f);
                monsterObj.transform.localScale = Vector3.one * 0.7f;

                // Bật các SkinnedMeshRenderer thân chính (Group/Geometry) và ẩn mảnh vỡ chết (group1)
                var groupTr = monsterObj.transform.Find("Group");
                if (groupTr != null)
                {
                    groupTr.gameObject.SetActive(true);
                    foreach (var smr in groupTr.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        smr.enabled = true;
                    }
                }
                var group1Tr = monsterObj.transform.Find("group1");
                if (group1Tr != null)
                {
                    group1Tr.gameObject.SetActive(false);
                }

                var wrongBurn = monsterObj.GetComponent<JangHyuBurnReceiver>();
                if (wrongBurn != null) Undo.DestroyObjectImmediate(wrongBurn);

                var wrongFight = monsterObj.GetComponent<JangHyuFightingController>();
                if (wrongFight != null) Undo.DestroyObjectImmediate(wrongFight);

                var oldAmbush = monsterObj.GetComponent<MonsterBallAmbushController>();
                if (oldAmbush != null) Undo.DestroyObjectImmediate(oldAmbush);

                // Gỡ Animator bị đặt nhầm trên root MonsterBall_red; đặt đúng lên node con Group (chứa DeformationSystem/Root_M)
                var rootAnim = monsterObj.GetComponent<Animator>();
                if (rootAnim != null)
                {
                    Undo.DestroyObjectImmediate(rootAnim);
                }

                var monsterCtrl = SetupMonsterBallAnimator();
                if (groupTr != null)
                {
                    var groupAnim = groupTr.GetComponent<Animator>();
                    if (groupAnim == null) groupAnim = Undo.AddComponent<Animator>(groupTr.gameObject);
                    if (monsterCtrl != null) groupAnim.runtimeAnimatorController = monsterCtrl;
                    groupAnim.applyRootMotion = false;
                }

                EnsureAnimationEventHandler(monsterObj);

                if (monsterObj.GetComponent<MonsterBallHealth>() == null)
                {
                    Undo.AddComponent<MonsterBallHealth>(monsterObj);
                }

                var ai = monsterObj.GetComponent<MonsterBallAIController>();
                if (ai == null)
                {
                    ai = Undo.AddComponent<MonsterBallAIController>(monsterObj);
                }

                // Tái tạo bộ VFX Prefab & Sound Effect chuẩn hóa
                CreateMonsterBallVfxPrefabs.BuildAllVfxPrefabs();
                CombatSfxGenerator.GenerateAllSfx();
                string cleanVfxPath = "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/Clean_Flame_VFX.prefab";
                var cleanVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cleanVfxPath);

                var so = new SerializedObject(ai);
                var vfxProp = so.FindProperty("_flameVfxPrefab");
                if (vfxProp != null && cleanVfxPrefab != null)
                {
                    vfxProp.objectReferenceValue = cleanVfxPrefab;
                }
                var crackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_GroundCrack_Complete.prefab");
                var crackProp = so.FindProperty("_groundCrackPrefab");
                if (crackProp != null && crackPrefab != null) crackProp.objectReferenceValue = crackPrefab;

                var eruptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_GroundErupt_Complete.prefab");
                var eruptProp = so.FindProperty("_groundEruptPrefab");
                if (eruptProp != null && eruptPrefab != null) eruptProp.objectReferenceValue = eruptPrefab;

                var rollPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_RollTrail_Complete.prefab");
                var rollProp = so.FindProperty("_rollTrailPrefab");
                if (rollProp != null && rollPrefab != null) rollProp.objectReferenceValue = rollPrefab;

                var jumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_JumpTrail_Complete.prefab");
                var jumpProp = so.FindProperty("_jumpTrailPrefab");
                if (jumpProp != null && jumpPrefab != null) jumpProp.objectReferenceValue = jumpPrefab;

                var bombPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Prefabs/VFX_BombExplosion_Complete.prefab");
                var bombProp = so.FindProperty("_bombExplosionPrefab");
                if (bombProp != null && bombPrefab != null) bombProp.objectReferenceValue = bombPrefab;

                BindAudioProp(so, "_sfxEmerge", CombatSfxGenerator.SfxDir + "/sfx_monster_emerge.wav");
                BindAudioProp(so, "_sfxJump", CombatSfxGenerator.SfxDir + "/sfx_monster_jump.wav");
                BindAudioProp(so, "_sfxRoll", CombatSfxGenerator.SfxDir + "/sfx_monster_roll.wav");
                BindAudioProp(so, "_sfxBombExplosion", CombatSfxGenerator.SfxDir + "/sfx_bomb_explosion.wav");
                BindAudioProp(so, "_sfxHitImpact", CombatSfxGenerator.SfxDir + "/sfx_hit_impact.wav");

                var gProp = so.FindProperty("_groundSpawnPos");
                if (gProp != null) gProp.vector3Value = new Vector3(3.5f, 0.0f, 0f);
                var yProp = so.FindProperty("_groundY");
                if (yProp != null) yProp.floatValue = 0.0f;
                var subProp = so.FindProperty("_submergeDepth");
                if (subProp != null) subProp.floatValue = 0.0f;
                so.ApplyModifiedProperties();

                monsterObj.transform.position = new Vector3(3.5f, 0.0f, 0f);
            }
        }

        private static void BindAudioProp(SerializedObject so, string propName, string clipPath)
        {
            var prop = so.FindProperty(propName);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (prop != null && clip != null) prop.objectReferenceValue = clip;
        }

        private static RuntimeAnimatorController SetupMonsterBallAnimator()
        {
            string ctrlPath = "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Animator/MonsterBall_CombatController.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            }

            var sm = controller.layers[0].stateMachine;
            const string animBase = "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Animations";
            const string tempBase = "Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Temp";

            var burrowClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Idle/MonsterBall_Burrow.anim");
            var unburrowClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Idle/MonsterBall_Unburrow.anim");
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Idle/MonsterBall_Idle.anim");
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Walk/MonsterBall_walk.anim");
            var backWalkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{tempBase}/MonsterBall_backwalk.anim");
            var atk1Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Attacks/MonsterRedBall_Attack1.anim");
            var atk2Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Attacks/MonsterRedball_Attack2.anim");
            var dmgFwdClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Damaged/MonsterBall_DamagedForward.anim");
            var dmgBwdClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Damaged/MonsterBall_DamagedBackward.anim");
            // Lưu ý: MonsterBall_GroggyStart.anim có lỗi scale 100x (bay lên Y=146m), dùng GroggyLoop.anim chuẩn Y=0.43m
            var groggyLoopClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Meze/MonsterBall_GroggyLoop.anim");
            var groggyStartClip = groggyLoopClip;
            var deadClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{tempBase}/dead_red.anim");

            AnimatorState EnsureState(string stateName, AnimationClip clip)
            {
                AnimatorState st = null;
                foreach (var cs in sm.states)
                {
                    if (cs.state.name == stateName) { st = cs.state; break; }
                }
                if (st == null) st = sm.AddState(stateName);
                if (clip != null) st.motion = clip;
                return st;
            }

            var burrowState = EnsureState("Burrow", burrowClip);
            sm.defaultState = burrowState;
            EnsureState("Unburrow", unburrowClip);
            EnsureState("BattleIdle", idleClip);
            EnsureState("BattleWalk", walkClip);
            EnsureState("WalkBackward", backWalkClip);
            EnsureState("Attack1", atk1Clip);
            EnsureState("Attack2", atk2Clip);
            EnsureState("DamagedForward", dmgFwdClip);
            EnsureState("DamagedBackward", dmgBwdClip);
            var groggyStart = EnsureState("GroggyStart", groggyStartClip);
            var groggyLoop = EnsureState("GroggyLoop", groggyLoopClip);
            if (groggyStart.transitions.Length == 0)
            {
                var tr = groggyStart.AddTransition(groggyLoop);
                tr.hasExitTime = true;
                tr.exitTime = 0.9f;
                tr.duration = 0.1f;
            }
            EnsureState("Dead", deadClip);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void EnsureAnimationEventHandler(GameObject root)
        {
            var animators = root.GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                if (anim.GetComponent<AnimationEventHandler>() == null)
                {
                    Undo.AddComponent<AnimationEventHandler>(anim.gameObject);
                }
            }
            if (root.GetComponent<AnimationEventHandler>() == null)
            {
                Undo.AddComponent<AnimationEventHandler>(root);
            }
        }

        private static void SetupArenaAndCamera()
        {
            var grounds = new System.Collections.Generic.List<GameObject>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name == "Arena_Ground") grounds.Add(go);
            }
            for (int i = 1; i < grounds.Count; i++)
            {
                Undo.DestroyObjectImmediate(grounds[i]);
            }
            var ground = grounds.Count > 0 ? grounds[0] : GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (grounds.Count == 0)
            {
                ground.name = "Arena_Ground";
                Undo.RegisterCreatedObjectUndo(ground, "Create Arena Ground");
            }
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(35f, 0.5f, 12f);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.55f, -5.6f);
                cam.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
                cam.fieldOfView = 42f;
                if (cam.GetComponent<StreetFighterCamera>() == null)
                {
                    Undo.AddComponent<StreetFighterCamera>(cam.gameObject);
                }
                var uacd = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (uacd != null) uacd.renderPostProcessing = true;
            }

            // Bật Global Volume + Post-Processing chiến đấu (ACES + Bloom + ChromaticAberration + LensDistortion pulse)
            GameObject volGo = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name == "Global Volume") { volGo = go; break; }
            }
            if (volGo == null)
            {
                volGo = new GameObject("Global Volume");
                Undo.RegisterCreatedObjectUndo(volGo, "Create Global Volume");
            }
            volGo.SetActive(true);
            var vol = volGo.GetComponent<UnityEngine.Rendering.Volume>() ?? Undo.AddComponent<UnityEngine.Rendering.Volume>(volGo);
            vol.isGlobal = true;
            vol.priority = 20f;
            if (vol.sharedProfile == null)
            {
                vol.sharedProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            }
            var ppCtrl = volGo.GetComponent<CombatPostProcessController>() ?? Undo.AddComponent<CombatPostProcessController>(volGo);
            ppCtrl.EnsureVolumeConfigured();
        }

        private static void SetupFullUI()
        {
            // Tạo bộ Sprite UI thư pháp (Avatar JangHyu, viền mực đen, vệt mực tên) trước khi dựng UI
            CreateUiBrushSprites.GenerateAllSprites();

            // 1. Quản lý UI Manager
            var uiMgrGo = GameObject.Find("Combat_UI_Manager");
            if (uiMgrGo == null)
            {
                uiMgrGo = new GameObject("Combat_UI_Manager");
                Undo.RegisterCreatedObjectUndo(uiMgrGo, "Create Combat UI Manager");
            }
            var uiMgr = uiMgrGo.GetComponent<CombatUIManager>();
            if (uiMgr == null) uiMgr = Undo.AddComponent<CombatUIManager>(uiMgrGo);

            // Đảm bảo EventSystem tồn tại để nhận click UI
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            }

            // 2. Dựng lại toàn bộ UI theo 1 nguồn chuẩn duy nhất (Single source of truth)
            uiMgr.RebuildAllUI();
            uiMgr.FindReferences();

            EditorUtility.SetDirty(uiMgr);
            if (uiMgr.ScreenCanvas != null) EditorUtility.SetDirty(uiMgr.ScreenCanvas.gameObject);
            if (uiMgr.MonsterWorldCanvas != null) EditorUtility.SetDirty(uiMgr.MonsterWorldCanvas.gameObject);
        }
    }
}
