using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("Tên cho dễ nhớ khi nhìn trong Inspector (không bắt buộc)")]
    public string name;

    [Tooltip("Toạ độ tile theo grid. X: cột, Y: hàng. (0,0) thường là góc trái dưới.")]
    public int tileX;

    [Tooltip("Toạ độ tile theo grid. X: cột, Y: hàng. (0,0) thường là góc trái dưới.")]
    public int tileY;

    [Tooltip("Prefab quái. Nếu để trống sẽ dùng defaultEnemyPrefab trong BattleManager.")]
    public GameObject enemyPrefab;
}

[System.Serializable]
public class RoundConfig
{
    [Tooltip("Tên round cho dễ quan sát, ví dụ: Round 1, Krugs, Wolves...")]
    public string roundName;

    [Tooltip("Danh sách các quái spawn trong round này.")]
    public EnemySpawnEntry[] spawns;
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Enemy settings")]
    [Tooltip("Prefab quái mặc định nếu từng spawn không gán prefab riêng.")]
    public GameObject defaultEnemyPrefab;

    [Tooltip("Cấu hình các round quái. Mỗi phần tử = 1 round.")]
    public RoundConfig[] rounds;

    [HideInInspector] public bool isBattleActive = false;
    [HideInInspector] public int currentRoundIndex = -1;   // 0-based (0 = Round 1)

    [HideInInspector] public List<Unit> playerUnits = new List<Unit>();
    [HideInInspector] public List<Unit> enemyUnits = new List<Unit>();

    /// <summary>
    /// Lưu vị trí bắt đầu của lính mình (trước khi ấn START).
    /// Key = Unit, Value = Tile ban đầu.
    /// </summary>
    private Dictionary<Unit, Tile> playerStartTiles = new Dictionary<Unit, Tile>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        isBattleActive = false;
        currentRoundIndex = -1;

        // Vào game là chuẩn bị luôn round đầu tiên (spawn quái để preview)
        PrepareNextRound();
    }

    /// <summary>
    /// Được gọi khi bấm nút START.
    /// </summary>
    public void StartBattle()
    {
        if (isBattleActive)
            return;

        if (enemyUnits.Count == 0)
        {
            Debug.LogWarning("StartBattle được gọi nhưng không có quái nào trong round hiện tại.");
            return;
        }

        // Xây lại list playerUnits dựa trên các Unit đang đứng trên board
        playerUnits.Clear();
        playerStartTiles.Clear();   // reset vị trí start của round mới

        Unit[] allUnits = FindObjectsOfType<Unit>();
        foreach (Unit u in allUnits)
        {
            if (u.currentTile == null) continue;

            if (u.team == Team.Player)
            {
                // Chỉ tính các unit đang đứng trên board (không phải bench).
                // Nếu board của bạn vùng khác, chỉnh lại điều kiện này.
                float z = u.currentTile.transform.position.z;
                if (z >= 0f)
                {
                    if (!playerUnits.Contains(u))
                        playerUnits.Add(u);

                    // LƯU LẠI TILE BAN ĐẦU CỦA LÍNH MÌNH
                    if (!playerStartTiles.ContainsKey(u))
                    {
                        playerStartTiles.Add(u, u.currentTile);
                    }
                }
            }
        }

        // Bật isInBattle cho cả 2 bên
        foreach (Unit u in playerUnits)
            u.isInBattle = true;

        foreach (Unit u in enemyUnits)
        {
            if (u != null)
                u.isInBattle = true;
        }

        isBattleActive = true;

        // Clear selection để tránh kéo / move khi đang combat
        if (InputManager.Instance != null)
            InputManager.Instance.ClearSelection();

        Debug.Log($"ROUND {currentRoundIndex + 1} START. Player={playerUnits.Count}, Enemy={enemyUnits.Count}");
    }

    /// <summary>
    /// Chuẩn bị round tiếp theo:
    /// - Tăng currentRoundIndex
    /// - Xoá quái cũ
    /// - Spawn quái mới theo config.rounds (preview, chưa đánh)
    /// </summary>
    private void PrepareNextRound()
    {
        currentRoundIndex++;

        if (rounds == null || rounds.Length == 0)
        {
            Debug.LogError("BattleManager: rounds chưa được cấu hình trong Inspector.");
            return;
        }

        if (currentRoundIndex >= rounds.Length)
        {
            Debug.Log("Đã hoàn thành tất cả round quái!");
            return;
        }

        isBattleActive = false;

        // Xoá sạch quái cũ
        foreach (Unit e in enemyUnits)
        {
            if (e != null)
                Destroy(e.gameObject);
        }
        enemyUnits.Clear();

        // lúc chuẩn bị round mới, clear luôn cache vị trí start của round cũ
        playerStartTiles.Clear();

        RoundConfig config = rounds[currentRoundIndex];
        if (config == null || config.spawns == null || config.spawns.Length == 0)
        {
            Debug.LogWarning($"Round {currentRoundIndex + 1} không có spawn nào được cấu hình.");
            return;
        }

        BoardManager bm = BoardManager.Instance;
        if (bm == null)
        {
            Debug.LogError("BoardManager.Instance = null, không spawn được quái!");
            return;
        }

        // Spawn từng quái theo danh sách spawn của round
        foreach (EnemySpawnEntry entry in config.spawns)
        {
            if (entry == null) continue;

            Tile tile = bm.GetTile(entry.tileX, entry.tileY);
            if (tile == null)
            {
                Debug.LogError($"Round {currentRoundIndex + 1} spawn '{entry.name}': Tile ({entry.tileX},{entry.tileY}) không tồn tại.");
                continue;
            }

            GameObject prefabToUse = entry.enemyPrefab != null ? entry.enemyPrefab : defaultEnemyPrefab;
            if (prefabToUse == null)
            {
                Debug.LogError($"Round {currentRoundIndex + 1} spawn '{entry.name}': Chưa gán enemyPrefab và defaultEnemyPrefab cũng null.");
                continue;
            }

            GameObject obj = Instantiate(prefabToUse);
            Unit enemy = obj.GetComponent<Unit>();
            if (enemy == null) enemy = obj.AddComponent<Unit>();

            // Đặt unit lên tile (snap vị trí)
            enemy.SetTile(tile);

            // Gán các thuộc tính cơ bản
            enemy.team = Team.Enemy;
            enemy.isInBattle = false;    // PREVIEW, chưa đánh cho tới khi StartBattle

            enemyUnits.Add(enemy);
        }

        Debug.Log($"PrepareNextRound → ROUND {currentRoundIndex + 1} ({config.roundName}). Spawn {enemyUnits.Count} quái theo tile X/Y.");
    }

    /// <summary>
    /// Tìm mục tiêu gần nhất cho AI.
    /// </summary>
    public Unit FindClosestEnemy(Unit from)
    {
        List<Unit> targets = (from.team == Team.Player) ? enemyUnits : playerUnits;

        Unit closest = null;
        float minDist = float.MaxValue;

        foreach (Unit u in targets)
        {
            if (u == null) continue;

            float dist = Vector3.Distance(from.transform.position, u.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = u;
            }
        }

        return closest;
    }

    /// <summary>
    /// Hồi full máu và đưa lính mình về vị trí trước khi bắt đầu round.
    /// Gọi sau khi round kết thúc (thắng hoặc thua).
    /// </summary>
    private void ResetPlayerUnitsAfterBattle()
    {
        foreach (KeyValuePair<Unit, Tile> kvp in playerStartTiles)
        {
            Unit unit = kvp.Key;
            Tile startTile = kvp.Value;

            if (unit == null || startTile == null) continue;

            // Hết round rồi, không còn trong trạng thái combat
            unit.isInBattle = false;

            // HỒI FULL MÁU – CHỖ NÀY GIẢ ĐỊNH BẠN CÓ currentHP / maxHP
            // Nếu tên biến khác thì bạn đổi lại cho đúng.
            unit.currentHP = unit.maxHP;

            // ĐƯA VỀ LẠI TILE BAN ĐẦU
            unit.SetTile(startTile);
        }

        // Sau khi reset xong cho round hiện tại, có thể clear
        playerStartTiles.Clear();
    }

    /// <summary>
    /// Được Unit gọi khi chết.
    /// </summary>
    public void OnUnitDied(Unit unit)
    {
        if (unit.team == Team.Player)
            playerUnits.Remove(unit);
        else
            enemyUnits.Remove(unit);

        if (!isBattleActive)
            return;

        // THUA ROUND
        if (playerUnits.Count == 0)
        {
            Debug.Log($"Player THUA ở round {currentRoundIndex + 1}");
            isBattleActive = false;

            // Sau này bạn trừ máu người chơi ở đây
            // vd: PlayerHealth.Instance.TakeDamage(x);

            // Hồi máu + đưa các unit còn sống (nếu có) về chỗ cũ
            ResetPlayerUnitsAfterBattle();

            // TODO: tuỳ bạn chọn có cho chơi tiếp round sau hay game over
        }
        // THẮNG ROUND
        else if (enemyUnits.Count == 0)
        {
            Debug.Log($"Player THẮNG round {currentRoundIndex + 1}");
            isBattleActive = false;

            // Hồi máu + đưa lính mình về vị trí trước combat
            ResetPlayerUnitsAfterBattle();

            // Chuẩn bị round tiếp theo (spawn preview quái mới)
            PrepareNextRound();
        }
    }
}
