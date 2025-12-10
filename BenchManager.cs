using UnityEngine;
using System.Collections;   // để dùng IEnumerator / Coroutine

public class BenchManager : MonoBehaviour
{
    public static BenchManager Instance;

    [Header("Cấu hình bench")]
    public int size = 8;               // số ô bench (đừng lớn hơn width của board)
    public GameObject tilePrefab;      // cùng prefab với BoardManager

    [HideInInspector]
    public float tileSize = 1f;
    [HideInInspector]
    public Tile[] benchTiles;

    void Awake()
    {
        Instance = this;
    }

    // Dùng IEnumerator Start() để có thể "chờ" BoardManager
    IEnumerator Start()
    {
        // Đợi đến khi BoardManager.Instance != null VÀ đã tạo xong mảng tiles
        while (BoardManager.Instance == null || BoardManager.Instance.tiles == null)
        {
            yield return null; // chờ 1 frame
        }

        // Lấy cùng tileSize với board
        tileSize = BoardManager.Instance.tileSize;
        Debug.Log("[BenchManager] tileSize lấy từ BoardManager = " + tileSize);

        GenerateBench();
    }

    void GenerateBench()
    {
        benchTiles = new Tile[size];

        int boardRowY = 0; // hàng dưới cùng trên board, nếu board sinh từ y=0 lên

        for (int i = 0; i < size; i++)
        {
            // tránh trường hợp bench nhiều hơn số cột của board
            if (i >= BoardManager.Instance.width)
            {
                Debug.LogWarning("[BenchManager] bench size > board width, dừng ở i = " + i);
                break;
            }

            // Lấy ô trên board theo cột i, hàng boardRowY
            Tile boardTile = BoardManager.Instance.GetTile(i, boardRowY);
            if (boardTile == null)
            {
                Debug.LogWarning("[BenchManager] Không tìm thấy Tile board tại (" + i + "," + boardRowY + ")");
                continue;
            }

            // Vị trí ô bench = ô board + lùi xuống dưới 1 tileSize
            Vector3 basePos = boardTile.transform.position;
            float benchOffset = 1f;
            Vector3 pos = basePos + new Vector3(0f, 0f, -tileSize - benchOffset);


            GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
            tileObj.name = $"BenchTile_{i}";

            Tile tile = tileObj.GetComponent<Tile>();
            if (tile == null)
                tile = tileObj.AddComponent<Tile>();

            tile.Init(i, -1);    // y = -1 để phân biệt với board
            benchTiles[i] = tile;
        }
    }

    // Kiểm tra còn ô trống trên bench không
    public bool HasFreeSlot()
    {
        foreach (Tile t in benchTiles)
        {
            if (t != null && t.currentUnit == null)
                return true;
        }
        return false;
    }

    // Spawn tướng xuống ô trống đầu tiên trên bench
    public void SpawnUnitToBench(GameObject unitPrefab)
    {
        foreach (Tile t in benchTiles)
        {
            if (t != null && t.currentUnit == null)
            {
                GameObject obj = Instantiate(unitPrefab);
                Unit unit = obj.GetComponent<Unit>();
                unit.SetTile(t);
                return;
            }
        }

        Debug.Log("Bench full, không thể spawn thêm!");
    }
}
