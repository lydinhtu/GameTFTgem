using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Enemy")]
    public GameObject enemyUnitPrefab;

    [HideInInspector] public bool isBattleActive = false;

    public List<Unit> playerUnits = new List<Unit>();
    public List<Unit> enemyUnits = new List<Unit>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // lúc mới vào game đang ở phase chuẩn bị
        isBattleActive = false;
    }

    // Gọi hàm này khi bấm nút START
    public void StartBattle()
    {
        if (isBattleActive) return;

        // xóa danh sách cũ nếu còn
        playerUnits.Clear();
        enemyUnits.Clear();

        // thu thập tất cả unit player ĐANG Ở TRÊN BOARD
        Unit[] allUnits = FindObjectsOfType<Unit>();
        foreach (var u in allUnits)
        {
            if (u.team != Team.Player) continue;
            if (u.currentTile == null) continue;

            // board của bạn ở z >= 0, bench ở z = -2 → ta loại bench ra
            if (u.currentTile.transform.position.z < 0f) continue; // đang ở bench

            playerUnits.Add(u);
        }

        // spawn 1 đợt quái đơn giản
        SpawnEnemyWave();

        isBattleActive = true;
        Debug.Log("BẮT ĐẦU COMBAT, playerUnits = " + playerUnits.Count + ", enemyUnits = " + enemyUnits.Count);
    }

    void SpawnEnemyWave()
    {
        // tạm thời: spawn 1 con enemy ở góc trên bên phải board
        // chỉnh lại pos cho khớp với board của bạn nếu cần
        Vector3 pos = new Vector3(7 * 1.2f, 0.5f, 7 * 1.2f);
        GameObject obj = Instantiate(enemyUnitPrefab, pos, Quaternion.identity);

        Unit enemy = obj.GetComponent<Unit>();
        if (enemy == null) enemy = obj.AddComponent<Unit>();

        enemy.unitName = "Enemy";
        enemy.team = Team.Enemy;

        // nếu board tồn tại, gán enemy vào ô góc trên bên phải
        if (BoardManager.Instance != null && BoardManager.Instance.tiles != null)
        {
            int targetX = Mathf.Clamp(BoardManager.Instance.width - 1, 0, BoardManager.Instance.width - 1);
            int targetY = Mathf.Clamp(BoardManager.Instance.height - 1, 0, BoardManager.Instance.height - 1);
            Tile spawnTile = BoardManager.Instance.GetTile(targetX, targetY);
            if (spawnTile != null)
            {
                enemy.SetTile(spawnTile);
            }
        }

        enemyUnits.Add(enemy);
    }

    public Unit FindClosestEnemy(Unit from)
    {
        List<Unit> targets = (from.team == Team.Player) ? enemyUnits : playerUnits;

        Unit closest = null;
        float minDist = float.MaxValue;

        foreach (var u in targets)
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

    public void OnUnitDied(Unit unit)
    {
        if (unit.team == Team.Player)
            playerUnits.Remove(unit);
        else
            enemyUnits.Remove(unit);

        // check thắng/thua
        if (playerUnits.Count == 0)
        {
            Debug.Log("Player THUA round này");
            isBattleActive = false;
        }
        else if (enemyUnits.Count == 0)
        {
            Debug.Log("Player THẮNG round này");
            isBattleActive = false;
        }
    }
}
