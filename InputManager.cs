using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    private Unit selectedUnit;

    private void Awake()
    {
        Instance = this;
    }

    // Cho BattleManager gọi để clear selection khi bắt đầu trận
    public void ClearSelection()
    {
        selectedUnit = null;
    }

    public void OnUnitClicked(Unit unit)
    {
        // 🔒 ĐANG TRONG TRẬN THÌ KHÔNG CHO CHỌN TƯỚNG
        if (BattleManager.Instance != null && BattleManager.Instance.isBattleActive)
        {
            Debug.Log("Battle is active -> cannot select unit.");
            return;
        }

        selectedUnit = unit;
        Debug.Log("Selected unit: " + unit.unitName);
    }

    public void OnTileClicked(Tile tile)
    {
        if (selectedUnit == null) return;

        // 🔒 ĐANG TRONG TRẬN THÌ KHÔNG CHO MOVE TƯỚNG
        if (BattleManager.Instance != null && BattleManager.Instance.isBattleActive)
        {
            Debug.Log("Battle is active -> cannot move unit.");
            return;
        }

        // Nếu ô trống thì cho unit đi sang
        if (tile.currentUnit == null)
        {
            selectedUnit.SetTile(tile);
            selectedUnit = null;
        }
        else
        {
            Debug.Log("Tile has unit already!");
        }
    }
}
