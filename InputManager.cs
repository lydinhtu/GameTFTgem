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
        if (unit == null) return;

        // ✅ LUÔN cho xem thông tin unit (cả tướng mình lẫn quái)
        // Nếu bạn có UI info, gọi ở đây
        // Ví dụ:
        // if (UnitInfoUI.Instance != null)
        //     UnitInfoUI.Instance.Show(unit);

        // ❌ KHÔNG BAO GIỜ SELECT QUÁI ĐỂ MOVE
        if (unit.team == Team.Enemy)
        {
            Debug.Log("Clicked enemy: chỉ xem info, không được chọn để di chuyển.");
            return;
        }

        // ❌ ĐANG TRONG TRẬN THÌ KHÔNG CHO CHỌN TƯỚNG ĐỂ MOVE
        if (BattleManager.Instance != null && BattleManager.Instance.isBattleActive)
        {
            Debug.Log("Battle is active -> cannot select unit for moving.");
            return;
        }

        // ✅ Chỉ tướng PLAYER, khi chưa combat, mới được chọn để move
        selectedUnit = unit;
        Debug.Log("Selected unit: " + unit.unitName);
    }

    public void OnTileClicked(Tile tile)
    {
        if (selectedUnit == null) return;

        // ❌ ĐANG TRONG TRẬN THÌ KHÔNG CHO MOVE
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
