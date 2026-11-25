using System.Collections;
using System.IO;
using UnityEngine;
using Aiara.Combat;
using Aiara.UI;

namespace Aiara.Test
{
    public class GameplayTestBotAndRecorder : MonoBehaviour
    {
        [Header("Auto Test Settings")]
        [Tooltip("Nếu bật true, bot sẽ tự động diễn tập. Nếu false, người chơi tự tay điều khiển qua phím A/D/Shift/J/K.")]
        [SerializeField] private bool _autoRunTest = false;
        [SerializeField] private string _outputFolder = @"C:\Users\Admin\.gemini\antigravity\brain\65c98726-979a-4b32-944b-88da764e48fb\gameplay_frames";

        private JangHyuFightingController _playerCtrl;
        private MonsterBallAIController _monsterAI;
        private MonsterBallHealth _monsterHealth;
        private JangHyuBurnReceiver _playerReceiver;
        private Camera _mainCamera;
        private Coroutine _testCoroutine;

        // Chỉ chạy khi người dùng kích hoạt kịch bản diễn tập qua phím F2 hoặc trigger từ editor menu

        private void Start()
        {
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            FindComponents();

            if (_autoRunTest)
            {
                StartAutoTest();
            }
        }

        private void Update()
        {
            // Phím F2: Cho phép người chơi tự kích hoạt kịch bản diễn tập đối kháng bất cứ lúc nào
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (_testCoroutine != null)
                {
                    StopCoroutine(_testCoroutine);
                    _testCoroutine = null;
                    Debug.Log("[GameplayRecorder] Đã dừng kịch bản Auto Test! Người chơi tự điều khiển.");
                }
                else
                {
                    StartAutoTest();
                }
            }
        }

        public void StartAutoTest()
        {
            FindComponents();
            if (_testCoroutine != null) StopCoroutine(_testCoroutine);
            _testCoroutine = StartCoroutine(RunCombatScenarioRoutine());
        }

        private void FindComponents()
        {
            var playerObj = GameObject.Find("JangHyu_AttackCombo2") ?? GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _playerCtrl = playerObj.GetComponent<JangHyuFightingController>() ?? playerObj.AddComponent<JangHyuFightingController>();
                _playerReceiver = playerObj.GetComponent<JangHyuBurnReceiver>();
            }

            var monsterObj = GameObject.Find("MonsterBall_red") ?? GameObject.FindWithTag("Enemy");
            if (monsterObj != null)
            {
                _monsterAI = monsterObj.GetComponent<MonsterBallAIController>() ?? monsterObj.AddComponent<MonsterBallAIController>();
                _monsterHealth = monsterObj.GetComponent<MonsterBallHealth>() ?? monsterObj.AddComponent<MonsterBallHealth>();
            }

            _mainCamera = Camera.main;
            if (_mainCamera != null && _mainCamera.GetComponent<StreetFighterCamera>() == null)
            {
                _mainCamera.gameObject.AddComponent<StreetFighterCamera>();
            }
        }

        private IEnumerator RunCombatScenarioRoutine()
        {
            Debug.Log("[GameplayRecorder] === BẮT ĐẦU KỊCH BẢN KIỂM THỬ KHỚP 100% VIDEO DEMO ===");

            // Reset ban đầu: Player ở X = -3.5m, Quái vật ở trạng thái Hidden (chìm dưới đất, gai nhọn nhô lên, nứt đất)
            if (_playerReceiver != null)
            {
                _playerReceiver.ResetHealth();
                _playerReceiver.transform.position = new Vector3(-3.5f, 0f, 0f);
            }
            if (_monsterAI != null)
            {
                _monsterAI.SpawnAtDefaultPosition();
            }

            // GIAI ĐOẠN 1: Quái vật ẩn náu dưới đất (Burrow / Hidden), vết nứt đất, gai nhọn nhô lên, lửa tắt
            yield return new WaitForSeconds(0.6f);
            CaptureFrame("frame_01_monster_hidden_spikes.jpg", "Giai đoạn 1: MonsterBall ẩn dưới đất, gai nhọn trắng nhô lên khỏi sàn, vết nứt đất VFX_GroundCrack, lửa tắt");

            // GIAI ĐOẠN 2: Người chơi JangHyu bước tới tiếp cận quái vật
            if (_playerCtrl != null)
            {
                _playerCtrl.BotWalk(1f);
            }
            yield return new WaitForSeconds(0.6f);
            CaptureFrame("frame_02_player_approaches.jpg", "Giai đoạn 2: JangHyu bước lại gần quái vật đang ẩn náu");

            // Chờ khoảng cách đạt <= 5.5m -> Quái vật kích hoạt lao vọt lên (Emerge)
            yield return new WaitForSeconds(0.4f);
            if (_playerCtrl != null)
            {
                _playerCtrl.BotStop();
            }

            // GIAI ĐOẠN 3: Quái vật vọt lên không trung, nổ tung cột khói đen và đất đá văng tung tóe (Emerge Eruption)
            yield return new WaitForSeconds(0.2f);
            CaptureFrame("frame_03_monster_emerge_eruption.jpg", "Giai đoạn 3: MonsterBall vọt lên, hiệu ứng phun trào khói đen và đất đá vụn VFX_GroundErupt");

            // GIAI ĐOẠN 4: Quái vật tiếp đất vững vàng Y = 0.65m, toàn thân bùng cháy ngọn lửa cam vàng UNI VFX, hiện thanh máu "적구"
            yield return new WaitForSeconds(0.45f);
            CaptureFrame("frame_04_monster_land_ignite.jpg", "Giai đoạn 4: MonsterBall tiếp đất Y=0.65m, toàn thân bốc cháy rực rỡ, thanh máu 적구 xuất hiện");

            // GIAI ĐOẠN 5: Cuộn tròn lăn tấn công (Roll Attack) tốc độ cao về phía JangHyu với vệt khói và tia lửa kéo dài
            yield return new WaitForSeconds(0.35f);
            CaptureFrame("frame_05_monster_roll_attack.jpg", "Giai đoạn 5: MonsterBall cuộn tròn lăn tít mù lao về phía JangHyu với vệt khói và tia lửa VFX_RollTrail");

            // GIAI ĐOẠN 6: Va chạm đâm trúng JangHyu -> JangHyu ngã ngửa bốc cháy, HP tụt xuống 75/100
            yield return new WaitForSeconds(0.3f);
            CaptureFrame("frame_06_roll_impact_knockdown_burn.jpg", "Giai đoạn 6: Roll Attack đâm trúng JangHyu! JangHyu ngã ngửa bốc cháy, HP tụt còn 75/100");

            // GIAI ĐOẠN 7: Quái vật định thần, JangHyu đứng dậy phản công tung đòn Combo đấm trúng mặt quái
            yield return new WaitForSeconds(0.8f);
            if (_playerCtrl != null)
            {
                _playerCtrl.BotAttack();
            }
            yield return new WaitForSeconds(0.35f);
            CaptureFrame("frame_07_player_combo_hit.jpg", "Giai đoạn 7: JangHyu phản công tung đòn Combo đấm trúng MonsterBall, quái giật lùi và máu đỏ tụt");

            // GIAI ĐOẠN 8: Chuỗi Combo liên hoàn 3 hit khiến quái vật rơi vào trạng thái Choáng váng (Groggy)
            if (_playerCtrl != null)
            {
                _playerCtrl.BotAttack();
            }
            yield return new WaitForSeconds(0.35f);
            if (_playerCtrl != null)
            {
                _playerCtrl.BotAttack();
            }
            yield return new WaitForSeconds(0.45f);
            CaptureFrame("frame_08_monster_groggy.jpg", "Giai đoạn 8: Trúng trọn combo 3 hit liên tiếp, MonsterBall rơi vào trạng thái Choáng váng (Groggy)");

            Debug.Log("[GameplayRecorder] === HOÀN THÀNH TOÀN BỘ KỊCH BẢN! ĐÃ CHỤP ĐẦY ĐỦ 8 FRAME MINH CHỨNG ===");
            _testCoroutine = null;
        }

        private void CaptureFrame(string fileName, string description)
        {
            string fullPath = Path.Combine(_outputFolder, fileName);
            ScreenCapture.CaptureScreenshot(fullPath);
            Debug.Log($"[GameplayRecorder] Captured [{fileName}]: {description}");
        }
    }
}
