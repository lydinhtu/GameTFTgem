# Hướng dẫn thêm code vào Unity

Các bước này giả định bạn đã clone repository này và mở project trong Unity (phiên bản tương thích với project gốc).

## 1. Import các script vào project
1. Mở Unity và đảm bảo project đang mở đúng thư mục chứa các file `*.cs` trong repo.
2. Kéo các file C# sau vào thư mục `Assets` của Unity nếu chưa có:
   - `Tile.cs`
   - `GridPathfinder.cs`
   - `Unit.cs`
   - `BoardManager.cs`
   - `BattleManager.cs`
   - `BenchManager.cs`
   - `InputManager.cs`
3. Unity sẽ tự động tạo `.meta` nếu chưa có; nếu repo đã có sẵn `.meta` thì chỉ cần giữ nguyên cấu trúc folder như hiện tại.

## 2. Thiết lập Prefab/Scene
1. Mở scene chính (ví dụ `Main` hoặc scene bạn đang dùng).
2. Đảm bảo trên scene có các GameObject sau và gán script tương ứng:
   - **BoardManager**: tham chiếu tới grid Tile[,] trên bàn.
   - **BattleManager**: điều khiển combat và tham chiếu BoardManager.
   - **BenchManager**: quản lý bench, tham chiếu BoardManager.
   - **InputManager**: xử lý click, tham chiếu BoardManager và BattleManager.
   - **GameManager** (nếu có): giữ tham chiếu tới các manager khác.
3. Mỗi **Tile** trên bàn cần được gán script `Tile.cs` và có Collider để nhận click.

## 3. Thiết lập Grid và Tile
1. Trong `BoardManager`, cấu hình kích thước lưới và prefab tile (nếu cần) để tạo ra mảng `Tile[,] boardGrid`.
2. Mỗi Tile cần thiết lập vật liệu/renderer để phân biệt; đảm bảo trường `IsWalkable` mặc định `true` và `IsOccupied` được cập nhật thông qua `Tile.SetOccupied(bool)` từ unit.
3. Kiểm tra lớp `GridPathfinder` đã gán trên một GameObject singleton (hoặc thêm mới) và tham chiếu tới `BoardManager` nếu cần.

## 4. Thiết lập Unit Prefab
1. Tạo prefab Unit và gán script `Unit.cs`.
2. Trong Inspector của Unit:
   - Thiết lập tốc độ di chuyển (`moveSpeed`).
   - Gán Animator/HP bar nếu dùng.
3. Đảm bảo Unit gọi `SetCurrentTile(Tile startTile)` khi spawn để đánh dấu tile đang chiếm.

## 5. Cập nhật Bench/Spawn
1. Trong `BenchManager`, khi spawn unit lên bench hoặc board, gọi `SetCurrentTile` và cập nhật `Tile.SetOccupied(true)` để ghi nhận chiếm chỗ.
2. Khi di chuyển giữa bench và board, dùng API di chuyển của `Unit` (coroutine di chuyển qua path) thay vì dịch chuyển tức thời.

## 6. Di chuyển và Input
1. `InputManager` lắng nghe click trên Tile (qua `Tile.OnMouseDown` hoặc raycast); trước khi ra lệnh di chuyển, kiểm tra `tile.IsWalkable && !tile.IsOccupied`.
2. Khi hợp lệ, gọi `GridPathfinder.Instance.FindPath(startTile, targetTile)` để lấy danh sách Tile.
3. Gửi danh sách này cho Unit (`StartCoroutine(unit.MoveAlongPath(path)))`; unit sẽ cập nhật `IsOccupied` khi rời/đến tile.

## 7. Kiểm tra runtime
1. Play scene và thử click để di chuyển unit; unit nên di chuyển theo từng tile thay vì teleport.
2. Kiểm tra console xem có lỗi NullReference hoặc missing reference; nếu có, kiểm tra lại các field đã drag-drop đúng trong Inspector.

## 8. Build/Version control
- Sau khi thiết lập xong, lưu scene và commit các thay đổi `.unity` hoặc `.prefab` nếu cần.
- Giữ cấu trúc và tên file như trong repo để tránh lỗi đường dẫn.
