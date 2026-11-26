# Thiết kế UI điều khiển Quái vật & Chuyển ngữ tiếng Anh

## Mục tiêu
1. Sửa toàn bộ bảng UI điều khiển quái vật góc trên phải (`TopRight_Control_Panel`) dựa trên 3 video hướng dẫn (`Guide video 1, 2, 3`) để mọi nút bấm hoạt động thực sự trong Play Mode.
2. Bổ sung cụm nút **Demo Actions** cho phép bấm trực tiếp trên UI để kích hoạt ngay các hành động AI demo của MonsterBall.
3. Chuyển toàn bộ chữ tiếng Hàn hiển thị trên UI (`장휴 (JangHyu)`, `적구`, `생성`, `리셋`) sang tiếng Anh.

## Thiết kế chi tiết
- **Đổi nhãn UI sang tiếng Anh:**
  - `Player_Name`: `"JangHyu"`
  - `Monster_Name`: `"MonsterBall Red"`
  - `Btn_Spawn`: `"Spawn"`
  - `Btn_Reset`: `"Reset"`
- **Sửa binding runtime (`CombatUIManager`):**
  - Bind lại `onClick` trong runtime `BindPlayerHUDElements()` thay vì phụ thuộc listener lambda từ Editor script.
  - Biến `Dropdown_MonsterBallRed` thành `UnityEngine.UI.Dropdown` tương tác với các chế độ: `MonsterBallRed`, `MonsterBallRed (Burrowed)`, `MonsterBallRed (Active)`.
  - Đảm bảo `EventSystem` có module input hợp lệ khi chạy Play Mode.
- **Bổ sung `Demo_Actions_Panel` ngay dưới `TopRight_Control_Panel`:**
  - 5 nút demo trực tiếp: `Emerge`, `Roll`, `Jump Slam`, `Melee`, `Groggy`.
  - Gọi `MonsterBallAIController.TriggerDemoAction(MonsterAIState)` để thực hiện ngay lập tức hành động tương ứng (kể cả khi `AI` checkbox đang tắt, sau khi xong sẽ trả về `CombatIdle` nếu `AI` đang tắt hoặc tiếp tục AI loop nếu `AI` đang bật).
