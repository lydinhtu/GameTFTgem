using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    public int width = 8;
    public int height = 8;

    [Header("Prefab ô cờ")]
    public GameObject tilePrefab;

    [Header("Tự động lấy kích thước ô từ prefab")]
    public float tileSize = 1f;   // sẽ được overwrite trong Awake

    public Tile[,] tiles;

    void Awake()
    {
        Instance = this;

        // ❗ lấy kích thước ô từ prefab để các ô khít nhau
        AutoDetectTileSize();
    }

    void Start()
    {
        GenerateBoard();
    }

    void AutoDetectTileSize()
    {
        if (tilePrefab == null) return;

        // lấy renderer trong prefab (của TileVisual / Cube)
        Renderer r = tilePrefab.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            // chiều rộng theo trục X (cũng dùng cho Z)
            tileSize = r.bounds.size.x;
            Debug.Log("TileSize auto = " + tileSize);
        }
    }

    void GenerateBoard()
    {
        tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // dùng tileSize đã auto-detect → ô khít nhau
                Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                tileObj.name = $"Tile_{x}_{y}";

                Tile tile = tileObj.GetComponent<Tile>();
                if (tile == null)
                    tile = tileObj.AddComponent<Tile>();

                tile.Init(x, y);

                tiles[x, y] = tile;
            }
        }
    }

    public Tile GetTile(int x, int y)
    {
        return tiles[x, y];
    }
}
