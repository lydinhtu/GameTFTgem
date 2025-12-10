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
            // dùng BoardManager để spawn thẳng lên Tile, không dùng toạ độ world tự random nữa
            BoardManager bm = BoardManager.Instance;
            if (bm == null)
            {
                Debug.LogError("BoardManager.Instance = null, không spawn được enemy!");
                return;
            }

            // ví dụ: spawn ở góc trên bên phải của board
            int spawnX = bm.width - 1;
            int spawnY = bm.height - 1;

            Tile spawnTile = bm.GetTile(spawnX, spawnY);
            if (spawnTile == null)
            {
                Debug.LogError($"Không lấy được Tile để spawn enemy tại ({spawnX}, {spawnY})");
                return;
            }

            // tạo enemy
            GameObject obj = Instantiate(enemyUnitPrefab);
            Unit enemy = obj.GetComponent<Unit>();
            if (enemy == null) enemy = obj.AddComponent<Unit>();

            enemy.unitName = "Enemy";
            enemy.team = Team.Enemy;

            // QUAN TRỌNG: đặt enemy lên tile (SetTile sẽ tự move transform về vị trí ô)
            enemy.SetTile(spawnTile);

            // thêm vào list enemy
            enemyUnits.Add(enemy);

            Debug.Log($"Spawn enemy tại Tile ({spawnTile.x}, {spawnTile.y}) pos = {spawnTile.transform.position}");
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
