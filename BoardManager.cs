using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    [Header("Kích thước board")]
    public int width = 8;
    public int height = 8;

    [Header("Prefab ô cờ")]
    public GameObject tilePrefab;

    [Header("Layout")]
    public Transform boardOrigin;
    public float tileSize = 1f;

    [HideInInspector]
    public Tile[,] tiles;

    void Awake()
    {
        Instance = this;
        AutoDetectTileSize();
    }

    void Start()
    {
        GenerateBoard();
    }

    void AutoDetectTileSize()
    {
        if (tilePrefab == null) return;

        Renderer r = tilePrefab.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            tileSize = r.bounds.size.x;
        }
    }

    void GenerateBoard()
    {
        tiles = new Tile[width, height];

        Vector3 origin = boardOrigin != null ? boardOrigin.position : Vector3.zero;

        // 2 màu caro (muốn chỉnh thì đổi ở đây)
        Color colorLight = new Color(0.90f, 0.90f, 0.90f);
        Color colorDark = new Color(0.75f, 0.75f, 0.75f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = origin + new Vector3(x * tileSize, 0, y * tileSize);
                GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                tileObj.name = $"Tile_{x}_{y}";

                Tile tile = tileObj.GetComponent<Tile>();
                if (tile == null)
                    tile = tileObj.AddComponent<Tile>();

                tile.Init(x, y);

                // ───── tô caro ─────
                bool isDark = ((x + y) % 2 == 0);
                tile.SetColor(isDark ? colorDark : colorLight);

                tiles[x, y] = tile;
            }
        }
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public Tile GetTile(int x, int y)
    {
        if (!IsInside(x, y))
        {
            Debug.LogWarning($"GetTile out of range: ({x},{y})");
            return null;
        }

        return tiles[x, y];
    }

    // ───────── PATHFINDING PHẦN QUAN TRỌNG ─────────

    public bool IsWalkable(Tile t, Unit mover)
    {
        if (t == null) return false;

        // ô trống thì đi được
        if (t.currentUnit == null) return true;


        // cho phép "đứng" trên chính ô mình đang đứng
        return t.currentUnit == mover;
    }

    public List<Tile> GetNeighbors(Tile tile)
    {
        List<Tile> result = new List<Tile>();
        int x = tile.x;
        int y = tile.y;

        int[,] dirs = new int[,]
        {
            {1, 0},
            {-1, 0},
            {0, 1},
            {0, -1}
        };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dirs[i, 0];
            int ny = y + dirs[i, 1];

            if (!IsInside(nx, ny)) continue;

            Tile n = tiles[nx, ny];
            if (n != null)
                result.Add(n);
        }

        return result;
    }

    // BFS tìm đường ngắn nhất từ start → goal
    public List<Tile> FindPath(Tile start, Tile goal, Unit mover)
    {
        List<Tile> path = new List<Tile>();

        if (start == null || goal == null)
            return path;

        if (start == goal)
        {
            path.Add(start);
            return path;
        }

        Queue<Tile> open = new Queue<Tile>();
        HashSet<Tile> visited = new HashSet<Tile>();
        Dictionary<Tile, Tile> cameFrom = new Dictionary<Tile, Tile>();

        open.Enqueue(start);
        visited.Add(start);

        bool found = false;

        while (open.Count > 0)
        {
            Tile current = open.Dequeue();

            if (current == goal)
            {
                found = true;
                break;
            }

            foreach (Tile n in GetNeighbors(current))
            {
                if (visited.Contains(n)) continue;

                // có thể đi qua ô n nếu nó walkable hoặc chính goal
                if (!IsWalkable(n, mover) && n != goal) continue;

                visited.Add(n);
                cameFrom[n] = current;
                open.Enqueue(n);
            }
        }

        if (!found)
            return path; // trả về list rỗng

        // reconstruct path
        Tile cur = goal;
        path.Add(cur);
        while (cur != start)
        {
            cur = cameFrom[cur];
            path.Add(cur);
        }
        path.Reverse();

        return path;
    }

    // Tìm ô "đẹp" nhất để unit đứng mà vẫn đánh được target
    public Tile GetBestAttackTile(Unit self, Unit target)
    {
        if (self == null || target == null) return null;
        if (self.currentTile == null || target.currentTile == null) return null;

        Tile start = self.currentTile;
        Tile targetTile = target.currentTile;

        Tile bestTile = null;
        int bestPathLen = int.MaxValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile t = tiles[x, y];
                if (t == null) continue;

                // khoảng cách từ t đến target phải <= tầm đánh (theo ô)
                int dx = Mathf.Abs(t.x - targetTile.x);
                int dy = Mathf.Abs(t.y - targetTile.y);
                int distToTarget = dx + dy;

                if (distToTarget > self.attackRangeTiles)
                    continue;

                // ô này phải trống hoặc là chính ô hiện tại của mình
                if (t.currentUnit != null && t != start)
                    continue;

                // tìm path từ start tới t
                List<Tile> path = FindPath(start, t, self);
                if (path == null || path.Count == 0)
                    continue;

                int pathLen = path.Count;
                if (pathLen < bestPathLen)
                {
                    bestPathLen = pathLen;
                    bestTile = t;
                }
            }
        }

        return bestTile;
    }
}
