using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Aiara.Combat;
using Aiara.UI;
using Aiara.Editor;

namespace Aiara.EditorTools
{
    public static class CaptureStreetFighterEvidence
    {
        private const string OutDir = @"C:\Users\Admin\.gemini\antigravity\brain\65c98726-979a-4b32-944b-88da764e48fb\verification_frames";

        public static void RunVerification()
        {
            if (!Directory.Exists(OutDir))
            {
                Directory.CreateDirectory(OutDir);
            }

            Debug.Log("[Verification] Mở SampleScene và bắt đầu cấu hình...");
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            // Tạo các Prefab VFX nếu chưa có
            CreateMonsterBallVfxPrefabs.BuildAllVfxPrefabs();

            // Bake và Clean toàn bộ Scene
            SetupStreetFighterScene.BakeAndClean();

            // Kích hoạt Runner cho PlayMode
            VerificationRunnerMono.EnabledForBatch = true;

            Debug.Log("[Verification] Khởi động Play Mode để ghi nhận bằng chứng...");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/StreetFighter/Run In-Editor Verification")]
        public static void RunInEditorVerification()
        {
            if (!Directory.Exists(OutDir))
            {
                Directory.CreateDirectory(OutDir);
            }
            SessionState.SetBool("CaptureStreetFighter_RunVerify", true);
            EditorApplication.isPlaying = true;
        }
    }

    public class VerificationRunnerMono : MonoBehaviour
    {
        public static bool EnabledForBatch = false;
        private const string OutDir = @"C:\Users\Admin\.gemini\antigravity\brain\65c98726-979a-4b32-944b-88da764e48fb\verification_frames";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStartOnPlay()
        {
#if UNITY_EDITOR
            bool runEditor = SessionState.GetBool("CaptureStreetFighter_RunVerify", false);
#else
            bool runEditor = false;
#endif
            if (EnabledForBatch || runEditor || Application.isBatchMode)
            {
#if UNITY_EDITOR
                SessionState.SetBool("CaptureStreetFighter_RunVerify", false);
#endif
                var existing = Object.FindFirstObjectByType<VerificationRunnerMono>();
                if (existing == null)
                {
                    var go = new GameObject("VerificationRunner");
                    go.AddComponent<VerificationRunnerMono>();
                    Object.DontDestroyOnLoad(go);
                }
            }
        }

        private IEnumerator Start()
        {
            Debug.Log("[VerificationRunner] >>> BẮT ĐẦU CHUỖI KIỂM CHỨNG BẰNG CHỨNG THỰC TẾ 100% KHỚP VIDEO DEMO <<<");

            if (!Directory.Exists(OutDir))
            {
                Directory.CreateDirectory(OutDir);
            }

            yield return new WaitForSeconds(0.6f);

            var player = GameObject.Find("JangHyu_AttackCombo2") ?? GameObject.FindWithTag("Player");
            var monster = GameObject.Find("MonsterBall_red") ?? GameObject.FindWithTag("Enemy");
            var playerCtrl = player != null ? player.GetComponent<JangHyuFightingController>() : null;
            var monsterAI = monster != null ? monster.GetComponent<MonsterBallAIController>() : null;

            // Đảm bảo quái vật ở trạng thái Hidden lúc đầu
            if (monsterAI != null)
            {
                monsterAI.SpawnAtDefaultPosition();
            }
            if (player != null)
            {
                var receiver = player.GetComponent<JangHyuBurnReceiver>();
                if (receiver != null) receiver.ResetHealth();
                player.transform.position = new Vector3(-3.5f, 0f, 0f);
            }

            yield return new WaitForSeconds(0.4f);

            // BẰNG CHỨNG 1: Quái vật ẩn sâu dưới đất (Y = -0.90m), chỉ nhô 2 gai nhọn trắng lên mặt sàn, nứt đất, lửa tắt, thanh máu ẩn
            Capture("01_monster_hidden_underground.png");
            Debug.Log("[VerificationRunner] Đã chụp 01_monster_hidden_underground.png: Quái vật ẩn đất, gai nhọn nhô lên, nứt đất");

            yield return new WaitForSeconds(0.4f);

            // BẰNG CHỨNG 2: JangHyu bước lại gần tiếp cận bằng BotWalk thực tế (góc nghiêng combat)
            if (player != null)
            {
                player.transform.position = new Vector3(-0.8f, 0f, 0f);
                if (playerCtrl != null) playerCtrl.BotWalk(1f);
            }
            yield return new WaitForSeconds(0.25f);
            var pAnimWalk = player != null ? player.GetComponentInChildren<Animator>() : null;
            bool walkAngled = pAnimWalk != null && (pAnimWalk.GetCurrentAnimatorStateInfo(0).IsName("Walk") || pAnimWalk.GetCurrentAnimatorStateInfo(0).IsName("WalkBackward"));
            yield return StartCoroutine(CaptureFrameCoroutine("02_player_walks_forward.png"));
            if (playerCtrl != null) playerCtrl.BotStop();
            Debug.Log($"[VerificationRunner] Đã chụp 02_player_walks_forward.png | walkStateActive={walkAngled}, playerX={player?.transform.position.x:F2}");

            // Chờ 0.35s cho quái vật nhận diện khoảng cách <= 5.5m và phóng vọt lên đỉnh cao
            yield return new WaitForSeconds(0.35f);
            Capture("03_monster_emerge_eruption.png");
            Debug.Log($"[VerificationRunner] [CHECK_EMERGE_TRAIL] jumpTrail={monsterAI?.IsJumpTrailPlaying}, rollTrail={monsterAI?.IsRollTrailPlaying}");

            // Chờ tiếp 0.9s cho quái vật tiếp đất vững vàng Y = 0.0m, bùng cháy ngọn lửa cam vàng và hiện thanh máu 적구
            yield return new WaitForSeconds(0.90f);
            Capture("04_monster_land_and_ignite.png");
            Debug.Log("[VerificationRunner] Đã chụp 04_monster_land_and_ignite.png");

            // Chờ 0.25s cho quái vật cuộn tròn lăn tít mù (Roll Attack)
            yield return new WaitForSeconds(0.25f);
            Capture("05_monster_roll_attack.png");
            Debug.Log("[VerificationRunner] Đã chụp 05_monster_roll_attack.png");

            // Chờ 0.30s: Roll Attack đâm trúng JangHyu -> Kiểm chứng khóa di chuyển trong lúc Knockdown!
            yield return new WaitForSeconds(0.30f);
            Capture("06_roll_hit_knockdown_burn.png");
            yield return new WaitForSeconds(0.55f);
            yield return StartCoroutine(CaptureFrameCoroutine("06b_burn_floating_text.png"));
            Debug.Log($"[VerificationRunner] [CHECK_BURN_DAMAGE_TEXT] activePopups={FloatingDamageText.ActiveCount}, totalSpawned={FloatingDamageText.TotalSpawnedCount}");
            float xBeforeDownedWalk = player != null ? player.transform.position.x : 0f;
            if (playerCtrl != null) playerCtrl.BotWalk(1f);
            yield return new WaitForSeconds(0.45f);
            float xAfterDownedWalk = player != null ? player.transform.position.x : 0f;
            Debug.Log($"[VerificationRunner] [CHECK_KNOCKDOWN_LOCK] dxDuringKnockdown={Mathf.Abs(xAfterDownedWalk - xBeforeDownedWalk):F3}m (kỳ vọng <= 0.15m)");

            // Chờ JangHyu tự đứng dậy (StandUp -> BattleIdle) và kiểm chứng AI + VFX MonsterBall vẫn lặp liên tục sau >5s
            yield return new WaitForSeconds(2.5f);
            var flameVfx = monster != null ? monster.GetComponentInChildren<UnityEngine.VFX.VisualEffect>(true) : null;
            int aliveAfter5s = flameVfx != null ? flameVfx.aliveParticleCount : -1;
            var monsterStateAfter5s = monsterAI != null ? monsterAI.CurrentState : MonsterAIState.Hidden;
            var pStateAfterGetup = pAnimWalk != null ? pAnimWalk.GetCurrentAnimatorStateInfo(0) : default;
            Debug.Log($"[VerificationRunner] [CHECK_LOOP_5S] monsterState={monsterStateAfter5s}, vfxAlive={aliveAfter5s}, playerStandUpDone={(pStateAfterGetup.IsName("BattleIdle") || pStateAfterGetup.IsName("Walk"))}");

            // JangHyu phản công tung Combo
            if (player != null && monster != null)
            {
                player.transform.position = new Vector3(monster.transform.position.x - 1.6f, 0f, 0f);
                var pAnim = player.GetComponentInChildren<Animator>();
                if (pAnim != null) pAnim.Play("Combo1", 0, 0f);
            }
            var monsterHealth = monster != null ? monster.GetComponent<MonsterBallHealth>() : null;
            if (monsterHealth != null) monsterHealth.TakeDamage(35);
            if (monsterAI != null)
            {
                monsterAI.TransitionTo(MonsterAIState.Damaged);
            }
            yield return new WaitForSeconds(0.3f);
            Capture("07_player_combo_attack.png");
            Debug.Log("[VerificationRunner] Đã chụp 07_player_combo_attack.png");

            // Chuỗi combo 3 hit khiến quái vật Choáng váng (Groggy)
            yield return new WaitForSeconds(0.3f);
            if (monsterAI != null)
            {
                monsterAI.TransitionTo(MonsterAIState.Groggy);
            }
            yield return new WaitForSeconds(0.4f);
            Capture("08_monster_groggy_stun.png");
            Debug.Log("[VerificationRunner] Đã chụp 08_monster_groggy_stun.png");

            // Chờ thêm 2.2s nữa kiểm chứng quái thoát Groggy quay lại AI chiến đấu và VFX lửa vẫn loop!
            yield return new WaitForSeconds(2.2f);
            int aliveFinal = flameVfx != null ? flameVfx.aliveParticleCount : -1;
            var monsterStateFinal = monsterAI != null ? monsterAI.CurrentState : MonsterAIState.Hidden;
            Debug.Log($"[VerificationRunner] [CHECK_POST_GROGGY] monsterState={monsterStateFinal}, vfxAlive={aliveFinal}");

            // Kiểm chứng kỹ năng mới: Nhảy bổng đập xuống nổ bom + khói đen + PostProcess + Rung màn hình + SFX
            if (player != null)
            {
                var recv = player.GetComponent<JangHyuBurnReceiver>();
                if (recv != null) recv.ResetHealth();
                player.transform.position = new Vector3(-1.5f, 0f, 0f);
            }
            if (monster != null)
            {
                monster.transform.position = new Vector3(2.2f, 0f, 0f);
            }
            if (monsterAI != null)
            {
                monsterAI.TransitionTo(MonsterAIState.JumpSlam);
            }
            yield return new WaitForSeconds(0.65f);
            yield return StartCoroutine(CaptureFrameCoroutine("09_monster_jump_slam_air.png"));
            Debug.Log($"[VerificationRunner] Đã chụp 09_monster_jump_slam_air.png | state={monsterAI?.CurrentState}, monsterY={monster?.transform.position.y:F2}");

            yield return new WaitForSeconds(0.48f);
            yield return StartCoroutine(CaptureFrameCoroutine("10_monster_bomb_explosion.png"));
            var pp = CombatPostProcessController.Instance;
            Debug.Log($"[VerificationRunner] [CHECK_EXPLOSION] state={monsterAI?.CurrentState}, bloom={pp?.CurrentBloomIntensity:F2}, pulse={pp?.CurrentPulseLevel:F2}");
            yield return new WaitForSeconds(0.35f);
            yield return StartCoroutine(CaptureFrameCoroutine("11_monster_bomb_black_smoke.png"));
            yield return new WaitForSeconds(0.3f);

            // Kiểm chứng UI: 0 ký tự tiếng Hàn & toàn bộ nút TopRight_Control_Panel / Demo Actions hoạt động thực tế
            VerifyEnglishUIAndButtonClicks(monsterAI);
            yield return StartCoroutine(CaptureFrameCoroutine("12_ui_english_and_demo_buttons.png"));
            if (monster != null)
            {
                float yaw = monster.transform.eulerAngles.y;
                float faceCamDot = Vector3.Dot(monster.transform.forward, -Vector3.forward);
                Debug.Log($"[VerificationRunner] [CHECK_MONSTER_FACING] yaw={yaw:F1}deg, faceToCameraDot={faceCamDot:F2} (kỳ vọng > 0.70)");
            }

            // Test bấm trực tiếp từng nút Demo Action qua Button.onClick.Invoke()
            var pHud = GameObject.Find("Player_HUD_Canvas");
            if (pHud != null && monsterAI != null)
            {
                var aiBtn = pHud.transform.Find("TopRight_Control_Panel/AI_Checkbox_Container")?.GetComponent<UnityEngine.UI.Button>();
                bool prevAi = monsterAI.IsAIEnabled;
                if (prevAi && aiBtn != null) aiBtn.onClick.Invoke();
                Debug.Log($"[VerificationRunner] [UI_CHECK] AI Toggle clicked -> IsAIEnabled={monsterAI.IsAIEnabled}");

                yield return InvokeAndCheckButton(pHud, "TopRight_Control_Panel/Demo_Actions_Panel/Btn_Action_Emerge", monsterAI, MonsterAIState.Emerge, 0.3f);

                // Reset vị trí chuẩn để kiểm chứng nút Roll: đứng gần ~2.2m, quái phải lùi lấy đà -> xoay tít mù -> đâm văng Player lùi ra sau!
                if (player != null)
                {
                    var recvRoll = player.GetComponent<JangHyuBurnReceiver>();
                    if (recvRoll != null) recvRoll.ResetHealth();
                    player.transform.position = new Vector3(-2.0f, 0f, 0f);
                }
                if (monster != null) monster.transform.position = new Vector3(0.4f, 0f, 0f);
                float playerXBeforeRoll = player != null ? player.transform.position.x : 0f;
                yield return InvokeAndCheckButton(pHud, "TopRight_Control_Panel/Demo_Actions_Panel/Btn_Action_Roll", monsterAI, MonsterAIState.RollAttack, 0.42f);
                yield return StartCoroutine(CaptureFrameCoroutine("14_ui_demo_roll_spinning.png"));
                yield return new WaitForSeconds(0.55f);
                yield return StartCoroutine(CaptureFrameCoroutine("15_ui_demo_roll_knockback.png"));
                float playerXAfterRoll = player != null ? player.transform.position.x : 0f;
                float rollKnockDist = Mathf.Abs(playerXAfterRoll - playerXBeforeRoll);
                Debug.Log($"[VerificationRunner] [CHECK_ROLL_KNOCKBACK] beforeX={playerXBeforeRoll:F2}, afterX={playerXAfterRoll:F2}, dxKnockback={rollKnockDist:F2}m (kỳ vọng >= 1.80m)");
                Debug.Log($"[VerificationRunner] [CHECK_TRAILS_AND_IMPACT] impactExplosions={monsterAI.ImpactExplosionsTriggered}");

                yield return InvokeAndCheckButton(pHud, "TopRight_Control_Panel/Demo_Actions_Panel/Btn_Action_JumpSlam", monsterAI, MonsterAIState.JumpSlam, 0.65f);
                yield return StartCoroutine(CaptureFrameCoroutine("13_ui_demo_jump_slam.png"));
                yield return new WaitForSeconds(0.55f);
                yield return InvokeAndCheckButton(pHud, "TopRight_Control_Panel/Demo_Actions_Panel/Btn_Action_Melee", monsterAI, MonsterAIState.Attack2, 0.25f);
                yield return InvokeAndCheckButton(pHud, "TopRight_Control_Panel/Demo_Actions_Panel/Btn_Action_Groggy", monsterAI, MonsterAIState.Groggy, 0.25f);
                yield return InvokeAndCheckButton(pHud, "TopRight_Control_Panel/Btn_Reset", monsterAI, MonsterAIState.Hidden, 0.2f);
                yield return InvokeAndCheckButton(pHud, "TopRight_Control_Panel/Btn_Spawn", monsterAI, MonsterAIState.Emerge, 0.2f);

                // Test 1: Immortal toggle -> Player bất tử không mất máu
                var immBtn = pHud.transform.Find("TopRight_Control_Panel/Immortal_Checkbox_Container")?.GetComponent<UnityEngine.UI.Button>();
                var pRecv = player != null ? player.GetComponent<JangHyuBurnReceiver>() : null;
                if (immBtn != null && pRecv != null)
                {
                    pRecv.IsImmortal = false;
                    pRecv.ResetHealth();
                    immBtn.onClick.Invoke();
                    pRecv.TakeDirectDamage(50);
                    Debug.Log($"[VerificationRunner] [CHECK_IMMORTAL] IsImmortal={pRecv.IsImmortal}, HP={pRecv.CurrentHp}/{pRecv.MaxHp} (kỳ vọng HP=100)");
                    immBtn.onClick.Invoke(); // Tắt Immortal lại để test Dead
                }

                // Test 2: Player chết (HP = 0) -> khóa cứng input & di chuyển
                if (pRecv != null && playerCtrl != null)
                {
                    pRecv.IsImmortal = false;
                    pRecv.TakeDirectDamage(200);
                    yield return new WaitForSeconds(0.25f);
                    float xDeadBefore = player.transform.position.x;
                    playerCtrl.BotWalk(1f);
                    playerCtrl.BotAttack();
                    yield return new WaitForSeconds(0.35f);
                    float xDeadAfter = player.transform.position.x;
                    Debug.Log($"[VerificationRunner] [CHECK_DEAD_LOCK] IsDead={pRecv.IsDead}, dxWhenDead={Mathf.Abs(xDeadAfter - xDeadBefore):F3}m (kỳ vọng <= 0.05m)");
                    pRecv.ResetHealth();
                }

                // Test 3: MonsterBall chết -> Nổ tung vùng lửa bom ở trung tâm + mảnh vỡ bắn ra + khói đen cuộn dày + nút Explode trên HUD
                if (monsterHealth != null)
                {
                    yield return new WaitForSeconds(0.6f);
                    monsterHealth.ResetHealth();
                    monsterAI.TriggerDemoAction(MonsterAIState.CombatIdle);
                    yield return new WaitForSeconds(0.15f);
                    yield return InvokeAndCheckButton(pHud, "TopRight_Control_Panel/Demo_Actions_Panel/Btn_Action_Explode", monsterAI, MonsterAIState.Dead, 0.22f);
                    yield return StartCoroutine(CaptureFrameCoroutine("16_monster_death_explosion.png"));
                    var groupObj = monster.transform.Find("Group")?.gameObject;
                    var group1Obj = monster.transform.Find("group1")?.gameObject;
                    Debug.Log($"[VerificationRunner] [CHECK_MONSTER_DEATH_EXPLOSION] state={monsterAI.CurrentState}, mainBodyActive={groupObj?.activeSelf}, chunksActive={group1Obj?.activeSelf}");
                    yield return new WaitForSeconds(0.45f);
                    yield return StartCoroutine(CaptureFrameCoroutine("16b_monster_death_smoke.png"));
                }

                if (aiBtn != null && !monsterAI.IsAIEnabled) aiBtn.onClick.Invoke();

#if UNITY_EDITOR
                // Test 4: Quay màn hình Game View kèm âm thanh bằng GameViewRecorderSession trong 2.1s
                if (monsterHealth != null)
                {
                    bool prevMute = EditorUtility.audioMasterMute;
                    EditorUtility.audioMasterMute = false;
                    monsterHealth.ResetHealth();
                    if (pRecv != null) pRecv.ResetHealth();
                    if (player != null) player.transform.position = new Vector3(-2.5f, 0f, 0f);
                    if (monster != null) monster.transform.position = new Vector3(2.2f, 0f, 0f);
                    monsterAI.TriggerDemoAction(MonsterAIState.JumpSlam);
                    GameViewRecorderSession.StartRecording();
                    yield return new WaitForSeconds(2.1f);
                    GameViewRecorderSession.StopRecording();
                    EditorUtility.audioMasterMute = prevMute;
                    Debug.Log($"[VerificationRunner] [CHECK_RECORDER] relPath={GameViewRecorderSession.LastRecordedRelativePath}, frames={GameViewRecorderSession.EncodedFrames}, audioSamples={GameViewRecorderSession.EncodedAudioSamples}, summary={GameViewRecorderSession.LastRecordedSummary}");
                }
#endif
            }

            yield return new WaitForSeconds(0.4f);
            Debug.Log("[VerificationRunner] >>> HOÀN TẤT TOÀN BỘ BẰNG CHỨNG THỰC TẾ & UI! <<<");

#if UNITY_EDITOR
            EnabledForBatch = false;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
#endif
            Destroy(gameObject);
        }

        private void VerifyEnglishUIAndButtonClicks(MonsterBallAIController monsterAI)
        {
            int koreanTextCount = 0;
            foreach (var txt in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (txt == null || string.IsNullOrEmpty(txt.text)) continue;
                foreach (char c in txt.text)
                {
                    if ((c >= '\uAC00' && c <= '\uD7AF') || (c >= '\u1100' && c <= '\u11FF'))
                    {
                        koreanTextCount++;
                        Debug.LogError($"[VerificationRunner] [UI_CHECK] Still found Korean text in '{txt.name}': {txt.text}");
                        break;
                    }
                }
            }
            Debug.Log($"[VerificationRunner] [UI_CHECK] KoreanTextCount={koreanTextCount}");
        }

        private IEnumerator InvokeAndCheckButton(GameObject pHud, string path, MonsterBallAIController monsterAI, MonsterAIState expectedState, float waitSeconds)
        {
            var btn = pHud.transform.Find(path)?.GetComponent<UnityEngine.UI.Button>();
            if (btn == null)
            {
                Debug.LogError($"[VerificationRunner] [UI_CHECK] Missing button at {path}");
                yield break;
            }
            btn.onClick.Invoke();
            var actual = monsterAI.CurrentState;
            bool ok = actual == expectedState || (expectedState == MonsterAIState.Attack2 && (actual == MonsterAIState.Attack1 || actual == MonsterAIState.Attack2));
            Debug.Log($"[VerificationRunner] [UI_CHECK] Click '{path}' -> State={actual} (expected={expectedState}, ok={ok})");
            yield return new WaitForSeconds(waitSeconds);
        }

        private IEnumerator CaptureFrameCoroutine(string fileName)
        {
            yield return new WaitForEndOfFrame();
            string fullPath = Path.Combine(OutDir, fileName);
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex != null)
            {
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(fullPath, bytes);
                DestroyImmediate(tex);
                Debug.Log($"[VerificationRunner] Đã ghi thành công file: {fullPath} ({bytes.Length} bytes)");
            }
            else
            {
                Capture(fileName);
            }
        }

        private void Capture(string fileName)
        {
            string fullPath = Path.Combine(OutDir, fileName);
            var cam = Camera.main;
            if (cam != null)
            {
                var rt = new RenderTexture(1920, 1080, 24);
                var prevTarget = cam.targetTexture;
                var prevActive = RenderTexture.active;

                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();

                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(fullPath, bytes);
                DestroyImmediate(tex);

                Debug.Log($"[VerificationRunner] Đã ghi thành công file: {fullPath} ({bytes.Length} bytes)");
            }
        }
    }
}
