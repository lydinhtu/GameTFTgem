using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerUnitPrefab;

    void Start()
    {
        // chờ 1 frame cho Board generate xong rồi mới spawn
        Invoke(nameof(SpawnTestUnit), 0.1f);
    }

    void SpawnTestUnit()
    {
        // lấy ô (0,0) trên bàn
        Tile tile = BoardManager.Instance.GetTile(0, 0);

        // tạo 1 con tướng
        GameObject obj = Instantiate(playerUnitPrefab);
        Unit unit = obj.GetComponent<Unit>();

        unit.unitName = "Test Unit";
        unit.team = Team.Player;
        unit.SetTile(tile);
    }
}
